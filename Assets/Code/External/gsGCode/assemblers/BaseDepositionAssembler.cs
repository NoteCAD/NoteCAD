﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;
using gs;

namespace gs
{
	public interface IDepositionAssembler
	{
	}


    public delegate BaseDepositionAssembler AssemblerFactoryF(GCodeBuilder builder, SingleMaterialFFFSettings settings);


    /// <summary>
    /// Assembler translates high-level commands from Compiler (eg MoveTo, ExtrudeTo, BeginRetract, etc)
    /// into GCode instructions, which it passes to GCodeBuilder instance.
    /// 
    /// To do this, Assembler maintains state machine for things like current nozzle position,
    /// extruder position, etc. 
    /// 
    /// TODO:
    ///   - need to reset accumulated extrusion distance to 0 once in a while, 
    ///     to avoid precision issues.
    ///   - support relative mode for position and extruder
    /// 
    /// 
    /// </summary>
	public abstract class BaseDepositionAssembler : IDepositionAssembler
	{
		public GCodeBuilder Builder;

        public enum ExtrudeParamType {
            ExtrudeParamA, ExtrudeParamE
        }

		/// <summary>
		/// Different machines use A or E for the extrude parameter
		/// </summary>
        public ExtrudeParamType ExtrudeParam = ExtrudeParamType.ExtrudeParamE;

		/// <summary>
		/// To keep things simple, we use the absolute coordinates of the slice polygons
		/// at the higher levels. However the printer often operates in some other coordinate
		/// system, for example relative to front-left corner. PositionShift is added to all x/y
		/// coordinates before they are passed to the GCodeBuilder.
		/// </summary>
		public Vector2d PositionShift = Vector2d.Zero;


        /// <summary>
        /// check that all points lie within bounds
        /// </summary>
        public bool EnableBoundsChecking = true;

        /// <summary>
        /// if EnableBoundsChecking=true, will assert if we try to move outside these bounds
        /// </summary>
        public AxisAlignedBox2d PositionBounds = AxisAlignedBox2d.Infinite;


        /// <summary>
        /// Generally, deposition-style 3D printers cannot handle large numbers of very small GCode steps.
        /// The result will be very chunky.
        /// So, we will cluster sequences of tiny steps into something that can actually be printed.
        /// </summary>
        public double MinExtrudeStepDistance = 0.0f;        // is set to FFFMachineInfo.MinPointSpacingMM in constructor below!!


        // Makerbot uses G1 for travel as well as extrude, so need to be able to override this
        public int TravelGCode = 0;

		public bool OmitDuplicateZ = false;
		public bool OmitDuplicateF = false;
		public bool OmitDuplicateE = false;

        // threshold for omitting "duplicate" Z/F/E parameters
        public double MoveEpsilon = 0.00001;


        public BaseDepositionAssembler(GCodeBuilder useBuilder, FFFMachineInfo machineInfo) 
		{
			Builder = useBuilder;
            currentPos = Vector3d.Zero;
            lastPos = Vector3d.Zero;
			extruderA = 0;
			currentFeed = 0;

            MinExtrudeStepDistance = machineInfo.MinPointSpacingMM;
        }


        /*
         * Subclasses must implement these
         */

        public abstract void AppendHeader();
        public abstract void AppendFooter();
        public abstract void EnableFan();
        public abstract void DisableFan();
		public abstract void UpdateProgress(int i);
		public abstract void ShowMessage(string s);

        /*
		 * These seem standard enough that we will provide a default implementation
		 */
        public virtual void SetExtruderTargetTemp(int temp, string comment = "set extruder temp C")
        {
            Builder.BeginMLine(104, comment).AppendI("S", temp);
        }
        public virtual void SetExtruderTargetTempAndWait(int temp, string comment = "set extruder temp C, and wait") 
		{
			Builder.BeginMLine(109, comment).AppendI("S", temp);
		}

        public virtual void SetBedTargetTemp(int temp, string comment = "set bed temp C")
        {
            Builder.BeginMLine(140, comment).AppendI("S", temp);
        }
        public virtual void SetBedTargetTempAndWait(int temp, string comment = "set bed temp C, and wait") 
		{
			Builder.BeginMLine(190, comment).AppendI("S", temp);			
		}


        /*
         * Position commands
         */

        protected Vector3d currentPos;
        protected Vector3d lastPos;
        public Vector3d NozzlePosition
		{
			get {
                return lastPos;
            }
		}

		protected double currentFeed;
		public double FeedRate 
		{
			get { return currentFeed; }
		}

        protected double extruderA;
		public double ExtruderA 
		{
			get { return extruderA; }
		}

        protected bool in_retract;
        protected double retractA;
		public bool InRetract
		{
			get { return in_retract; }
		}

        protected bool in_travel;
		public bool InTravel 
		{
			get { return in_travel; }
		}

        public bool InExtrude {
            get { return InTravel == false; }
        }




        /*
         * Code below is all to support MinExtrudeStepDistance > 0
         * In that case, we want to only emit the next gcode extrusion point once we have travelled
         * at least MinExtrudeStepDistance distance. To do that, we will skip points
         * until we have moved far enough, then emit one.
         * 
         * Currently we are saving all the skipped points in a queue, although we only use
         * the last two to emit. Current strategy is to lerp along the last line segment,
         * so that the emitted point is exactly at linear-arc-length MinExtrudeStepDistance.
         * The remainder of the last line goes back on the queue.
         * 
         * This does end up clipping sharp corners if they are made of multiple closely-spaced
         * points. However the scale of the clipping should be on the order of the filament
         * width, so it probably doesn't make a visual difference. And (anecdotally) it may
         * actually produce cleaner results in the corners...
         * 
         * [TODO] This is actually speed-dependent. We could modulate the clipping step size
         * by the movement speed. Or, we could slow down to hit the higher precision?
         * Might be preferable to just be consistent per layer, though...
         */



        // stores info we need to emit an extrude gcode point
        protected struct QueuedExtrude
        {
            public Vector3d toPos;
            public double feedRate;
            public double extruderA;
            public char extrudeChar;
            public string comment;

            static public QueuedExtrude lerp(ref QueuedExtrude a, ref QueuedExtrude b, double t)
            {
                QueuedExtrude newp = new QueuedExtrude();
                newp.toPos = Vector3d.Lerp(a.toPos, b.toPos, t);
                newp.feedRate = Math.Max(a.feedRate, b.feedRate);
                newp.extruderA = MathUtil.Lerp(a.extruderA, b.extruderA, t);
                newp.extrudeChar = a.extrudeChar;
                newp.comment = (a.comment == null) ? a.comment : b.comment;
                return newp;
            }
        }


        protected QueuedExtrude[] extrude_queue = new QueuedExtrude[1024];
        protected double extrude_queue_len = 0;
        int next_queue_index = 0;
        

        // we do not actually queue travel moves, but we might need to flush extrude queue
        protected virtual void queue_travel(Vector3d toPos, double feedRate, string comment)
        {
            Util.gDevAssert(InExtrude == false);
            if (EnableBoundsChecking && PositionBounds.Contains(toPos.xy) == false)
                throw new Exception("BaseDepositionAssembler.queue_move: tried to move outside of bounds!");

            lastPos = toPos;

            // flush any pending extrude
            flush_extrude_queue();

            emit_travel(toPos, feedRate, comment);
        }

        // actually emit travel move gcode
        protected virtual void emit_travel(Vector3d toPos, double feedRate, string comment)
        {
            double write_x = toPos.x + PositionShift.x;
            double write_y = toPos.y + PositionShift.y;

            Builder.BeginGLine(TravelGCode, comment).
                   AppendF("X", write_x).AppendF("Y", write_y);

            if (OmitDuplicateZ == false || MathUtil.EpsilonEqual(currentPos.z, toPos.z, MoveEpsilon) == false) {
                Builder.AppendF("Z", toPos.z);
            }
            if (OmitDuplicateF == false || MathUtil.EpsilonEqual(currentFeed, feedRate, MoveEpsilon) == false) {
                Builder.AppendF("F", feedRate);
            }

            currentPos = toPos;
            currentFeed = feedRate;
        }


        // push an extrude move onto queue
        protected virtual void queue_extrude(Vector3d toPos, double feedRate, double e, char extrudeChar, string comment, bool bIsRetract)
        {
            Util.gDevAssert(InExtrude || bIsRetract);
            if (EnableBoundsChecking && PositionBounds.Contains(toPos.xy) == false)
                throw new Exception("BaseDepositionAssembler.queue_extrude: tried to move outside of bounds!");

            lastPos = toPos;

            QueuedExtrude p = new QueuedExtrude() {
                toPos = toPos, feedRate = feedRate, extruderA = e, extrudeChar = extrudeChar, comment = comment
            };

            // we cannot queue a retract, so flush queue and emit the retract/unretract
            bool bForceEmit = (bIsRetract) || (toPos.z != NozzlePosition.z);
            if (bForceEmit) {
                flush_extrude_queue();
                emit_extrude(p);
                return;
            }

            // push this point onto queue. this will also update the extrude_queue_len
            double prev_len = extrude_queue_len;
            append_to_queue(p);

            // if we haven't moved far enough to emit a point, we wait
            if (extrude_queue_len < MinExtrudeStepDistance) {
                return;
            }
            // ok we moved far enough from last point to emit

            // if queue has one point, just emit it
            int last_i = next_queue_index-1;
            if (last_i == 0) {
                flush_extrude_queue();
                return;
            }

            // otherwise we lerp between last two points so that we emit at
            // point where accumulated linear arclength is exactly MinExtrudeStepDistance.
            double a = prev_len, b = extrude_queue_len;
            double t = (MinExtrudeStepDistance-a) / (b-a);
            Util.gDevAssert(t > -0.0001 && t < 1.0001);
            t = MathUtil.Clamp(t, 0, 1);
            QueuedExtrude last_p = extrude_queue[next_queue_index - 1];
            QueuedExtrude emit_p = QueuedExtrude.lerp(ref extrude_queue[next_queue_index-2], ref last_p, t);

            // emit and clear queue
            emit_extrude(emit_p);
            next_queue_index = 0;
            extrude_queue_len = 0;

            // now we re-submit last point. This pushes the remaining bit of the last segment
            // back onto the queue. (should we skip this if t > nearly-one?)
            queue_extrude(last_p.toPos, last_p.feedRate, last_p.extruderA, last_p.extrudeChar, last_p.comment, false);
        }
        protected virtual void queue_extrude_to(Vector3d toPos, double feedRate, double extrudeDist, string comment, bool bIsRetract)
        {
            if (ExtrudeParam == ExtrudeParamType.ExtrudeParamA)
                queue_extrude(toPos, feedRate, extrudeDist, 'A', comment, bIsRetract);
            else
                queue_extrude(toPos, feedRate, extrudeDist, 'E', comment, bIsRetract);
        }

        // emit gcode for an extrude move
        protected virtual void emit_extrude(QueuedExtrude p)
        {
            double write_x = p.toPos.x + PositionShift.x;
            double write_y = p.toPos.y + PositionShift.y;
            Builder.BeginGLine(1, p.comment).
                   AppendF("X", write_x).AppendF("Y", write_y);

            if (OmitDuplicateZ == false || MathUtil.EpsilonEqual(p.toPos.z, currentPos.z, MoveEpsilon) == false) {
                Builder.AppendF("Z", p.toPos.z);
            }
            if (OmitDuplicateF == false || MathUtil.EpsilonEqual(p.feedRate, currentFeed, MoveEpsilon) == false) {
                Builder.AppendF("F", p.feedRate);
            }
            if (OmitDuplicateE == false || MathUtil.EpsilonEqual(p.extruderA, extruderA, MoveEpsilon) == false) {
                Builder.AppendF(p.extrudeChar.ToString(), p.extruderA);
            }

            currentPos = p.toPos;
            currentFeed = p.feedRate;
            extruderA = p.extruderA;
        }

        // push point onto queue and update accumulated length
        protected virtual void append_to_queue(QueuedExtrude p)
        {
            double dt = (next_queue_index == 0) ?
                currentPos.xy.Distance(p.toPos.xy) : extrude_queue[next_queue_index-1].toPos.xy.Distance(p.toPos.xy);
            extrude_queue_len += dt;

            extrude_queue[next_queue_index] = p;
            next_queue_index = Math.Min(next_queue_index + 1, extrude_queue.Length - 1); ;
        }

        // emit point at end of queue and clear it
        protected virtual void flush_extrude_queue()
        {
            if (next_queue_index > 0) {
                emit_extrude(extrude_queue[next_queue_index - 1]);
                next_queue_index = 0;
            }
            extrude_queue_len = 0;
        }



        /*
         * Assembler API that Compiler uses
         */


		public virtual void AppendMoveTo(double x, double y, double z, double f, string comment = null) 
		{
            queue_travel(new Vector3d(x, y, z), f, comment);
		}
        public virtual void AppendMoveTo(Vector3d pos, double f, string comment = null)
        {
            AppendMoveTo(pos.x, pos.y, pos.z, f, comment);
        }



        public virtual void AppendExtrudeTo(Vector3d pos, double feedRate, double extrudeDist, string comment = null)
        {
            if (ExtrudeParam == ExtrudeParamType.ExtrudeParamA)
                AppendMoveToA(pos, feedRate, extrudeDist, comment);
            else
                AppendMoveToE(pos, feedRate, extrudeDist, comment);
        }


        protected virtual void AppendMoveToE(double x, double y, double z, double f, double e, string comment = null) 
		{
            queue_extrude(new Vector3d(x, y, z), f, e, 'E', comment, false);
		}
        protected virtual void AppendMoveToE(Vector3d pos, double f, double e, string comment = null)
        {
            AppendMoveToE(pos.x, pos.y, pos.z, f, e, comment);
        }


        protected virtual void AppendMoveToA(double x, double y, double z, double f, double a, string comment = null) 
		{
            queue_extrude(new Vector3d(x, y, z), f, a, 'A', comment, false);
		}
        protected virtual void AppendMoveToA(Vector3d pos, double f, double a, string comment = null)
        {
            AppendMoveToA(pos.x, pos.y, pos.z, f, a, comment);
        }



        public virtual void BeginRetractRelativeDist(Vector3d pos, double feedRate, double extrudeDelta, string comment = null)
        {
            BeginRetract(pos, feedRate, ExtruderA + extrudeDelta, comment);
        }
        public virtual void BeginRetract(Vector3d pos, double feedRate, double extrudeDist, string comment = null) {
			if (in_retract)
				throw new Exception("BaseDepositionAssembler.BeginRetract: already in retract!");
			if (extrudeDist > extruderA)
				throw new Exception("BaseDepositionAssembler.BeginRetract: retract extrudeA is forward motion!");

            // need to flush any pending extrudes here, so that extruderA is at actual last extrude value
            flush_extrude_queue();
            retractA = extruderA;
            queue_extrude_to(pos, feedRate, extrudeDist, (comment == null) ? "Retract" : comment, true);
            in_retract = true;
		}


		public virtual void EndRetract(Vector3d pos, double feedRate, double extrudeDist = -9999, string comment = null) {
			if (! in_retract)
				throw new Exception("BaseDepositionAssembler.EndRetract: already in retract!");
			if (extrudeDist != -9999 && MathUtil.EpsilonEqual(extrudeDist, retractA, 0.0001) == false )
				throw new Exception("BaseDepositionAssembler.EndRetract: restart position is not same as start of retract!");
			if (extrudeDist == -9999)
				extrudeDist = retractA;
            queue_extrude_to(pos, feedRate, extrudeDist, (comment == null) ? "End Retract" : comment, true);
			in_retract = false;
		}


		public virtual void BeginTravel() {
			if (in_travel)
				throw new Exception("BaseDepositionAssembler.BeginTravel: already in travel!");
			in_travel = true;
		}


		public virtual void EndTravel()
		{
			if (in_travel == false)
				throw new Exception("BaseDepositionAssembler.EndTravel: not in travel!");
			in_travel = false;
		}


		public virtual void AppendTravelTo(double x, double y, double z, double f)
		{
            throw new NotImplementedException("BaseDepositionAssembler.AppendTravelTo");
		}


        public virtual void AppendComment(string comment)
        {
            Builder.AddCommentLine(comment);
        }


        public virtual void AppendDwell(int milliseconds, string comment = null)
        {
            flush_extrude_queue();

            Builder.BeginGLine(4, (comment != null) ? comment : "dwell" )
                .AppendI("P", milliseconds);
        }


        /// <summary>
        /// Assembler may internally queue up a series of points, to optimize gcode emission.
        /// Call this to ensure that everything is written out to GCodeBuilder
        /// </summary>
        public virtual void FlushQueues()
        {
            flush_extrude_queue();
        }





        protected virtual void AddStandardHeader(SingleMaterialFFFSettings Settings)
        {
            Builder.AddCommentLine("; Generated on " + DateTime.Now.ToLongDateString());
            Builder.AddCommentLine("; Print Settings");
            Builder.AddCommentLine("; Layer Height: " + Settings.LayerHeightMM);
            Builder.AddCommentLine("; Nozzle Diameter: " + Settings.Machine.NozzleDiamMM + "  Filament Diameter: " + Settings.Machine.FilamentDiamMM);
            Builder.AddCommentLine("; Extruder Temp: " + Settings.ExtruderTempC);
            Builder.AddCommentLine(string.Format("; Speeds Extrude: {0}  Travel: {1} Z: {2}", Settings.RapidExtrudeSpeed, Settings.RapidTravelSpeed, Settings.ZTravelSpeed));
            Builder.AddCommentLine(string.Format("; Retract Distance: {0}  Speed: {1}", Settings.RetractDistanceMM, Settings.RetractSpeed));
            Builder.AddCommentLine(string.Format("; Shells: {0}  InteriorShells: {1}", Settings.Shells, Settings.InteriorSolidRegionShells));
            Builder.AddCommentLine(string.Format("; RoofLayers: {0}  FloorLayers: {1}", Settings.RoofLayers, Settings.FloorLayers));
            Builder.AddCommentLine(string.Format("; InfillX: {0}", Settings.SparseLinearInfillStepX));
            Builder.AddCommentLine(string.Format("; Support: {0}  Angle {1} SpacingX: {2}  Shell: {3}  Gap: {4}  VolScale: {5}", 
                Settings.GenerateSupport, Settings.SupportOverhangAngleDeg, Settings.SupportSpacingStepX, Settings.EnableSupportShell, Settings.SupportSolidSpace, Settings.SupportVolumeScale));
            Builder.AddCommentLine(string.Format("; ClipOverlaps: {0}  Tolerance: {1}", Settings.ClipSelfOverlaps, Settings.SelfOverlapToleranceX));
            Builder.AddCommentLine(string.Format("; LayerRange: {0}-{1}", Settings.LayerRangeFilter.a, Settings.LayerRangeFilter.b));
        }


	}


}