using System;
using System.Collections.Generic;
using g3;


namespace gs
{
	// [TODO] find a way to not hardcode this??
	using LinearToolpath = LinearToolpath3<PrintVertex>;


	/// <summary>
	/// Utility class to simplify ToolpathSet construction.
	/// </summary>
	public class ToolpathSetBuilder
	{
		public ToolpathSet Paths;

		Vector3d currentPos;
		public Vector3d Position {
			get { return currentPos; }
		}

		public ToolpathSetBuilder(ToolpathSet paths = null)
		{
			Paths = (paths == null) ? new ToolpathSet() : paths;
		}

		public void Initialize(Vector3d startPos) {
			currentPos = startPos;
		}

		public virtual void AppendPath(IToolpath p) {
            if (IsCommandToolpath(p)) {
                Paths.Append(p);

            } else {

                if (!currentPos.EpsilonEqual(p.StartPosition, MathUtil.Epsilon))
                    throw new Exception("PathSetBuilder.AppendPath: disconnected path");
                Paths.Append(p);
                currentPos = p.EndPosition;
            }
        }

		public virtual Vector3d AppendZChange(double ZChange, double fSpeed) {
			LinearToolpath zup = new LinearToolpath(ToolpathTypes.PlaneChange);
			zup.AppendVertex(new PrintVertex(currentPos, GCodeUtil.UnspecifiedValue));
			Vector3d toPos = new Vector3d(currentPos); 
			toPos.z += ZChange;
			zup.AppendVertex(new PrintVertex(toPos, fSpeed));
			AppendPath(zup);
			return currentPos;
		}


		public virtual Vector3d AppendTravel(Vector2d toPos, double fSpeed)
		{
			return AppendTravel(new Vector3d(toPos.x, toPos.y, currentPos.z), fSpeed);
		}
		public virtual Vector3d AppendTravel(Vector3d toPos, double fSpeed)
		{
			LinearToolpath travel = new LinearToolpath(ToolpathTypes.Travel);
			travel.AppendVertex(new PrintVertex(currentPos, GCodeUtil.UnspecifiedValue));
			travel.AppendVertex(new PrintVertex(toPos, fSpeed));
			if ( travel.Length > MathUtil.Epsilonf )		// discard tiny travels
				AppendPath(travel);
			return currentPos;
		}
		public virtual Vector3d AppendTravel(List<Vector2d> toPoints, double fSpeed)
        {
			LinearToolpath travel = new LinearToolpath(ToolpathTypes.Travel);
			travel.AppendVertex(new PrintVertex(currentPos, GCodeUtil.UnspecifiedValue));
			foreach (Vector2d pos2 in toPoints) {
				Vector3d pos = new Vector3d(pos2.x, pos2.y, currentPos.z);
				travel.AppendVertex(new PrintVertex(pos, fSpeed));
			}
			if (travel.Length > MathUtil.Epsilonf)
				AppendPath(travel);
			return currentPos;
		}
		public virtual Vector3d AppendTravel(List<Vector3d> toPoints, double fSpeed)
        {
			LinearToolpath travel = new LinearToolpath(ToolpathTypes.Travel);
			travel.AppendVertex(new PrintVertex(currentPos, GCodeUtil.UnspecifiedValue));
			foreach (Vector3d pos in toPoints) {
				travel.AppendVertex(new PrintVertex(pos, fSpeed));
			}
			if (travel.Length > MathUtil.Epsilonf)
				AppendPath(travel);
			return currentPos;
		}



        public virtual Vector3d AppendExtrude(Vector2d toPos, double fSpeed,
            FillTypeFlags pathTypeFlags = FillTypeFlags.Unknown)
        {
            return AppendExtrude(new Vector3d(toPos.x, toPos.y, currentPos.z), fSpeed, pathTypeFlags);
        }
        public virtual Vector3d AppendExtrude(Vector3d toPos, double fSpeed,
            FillTypeFlags pathTypeFlags = FillTypeFlags.Unknown)
        {
            LinearToolpath extrusion = new LinearToolpath(ToolpathTypes.Deposition);
            extrusion.TypeModifiers = pathTypeFlags;
            extrusion.AppendVertex(new PrintVertex(currentPos, GCodeUtil.UnspecifiedValue));
            extrusion.AppendVertex(new PrintVertex(toPos, fSpeed));
            AppendPath(extrusion);
            return currentPos;
        }



        public virtual Vector3d AppendExtrude(List<Vector2d> toPoints, double fSpeed, 
            FillTypeFlags pathTypeFlags = FillTypeFlags.Unknown,
            List<TPVertexFlags> perVertexFlags = null )
        {
			LinearToolpath extrusion = new LinearToolpath(ToolpathTypes.Deposition);
            extrusion.TypeModifiers = pathTypeFlags;
            extrusion.AppendVertex(new PrintVertex(currentPos, GCodeUtil.UnspecifiedValue));

            for (int i = 0; i < toPoints.Count; ++i) {
                Vector3d pos = new Vector3d(toPoints[i].x, toPoints[i].y, currentPos.z);
                extrusion.AppendVertex(new PrintVertex(pos, fSpeed));
            }
            if (perVertexFlags != null)
                ToolpathUtil.AddPerVertexFlags(extrusion, perVertexFlags);
            AppendPath(extrusion);
			return currentPos;
		}


		public virtual Vector3d AppendExtrude(List<Vector3d> toPoints, double fSpeed, 
            FillTypeFlags pathTypeFlags = FillTypeFlags.Unknown,
            List<TPVertexFlags> perVertexFlags = null)
        {
			LinearToolpath extrusion = new LinearToolpath(ToolpathTypes.Deposition);
            extrusion.TypeModifiers = pathTypeFlags;
			extrusion.AppendVertex(new PrintVertex(currentPos, GCodeUtil.UnspecifiedValue));
            for (int i = 0; i < toPoints.Count; ++i)
                extrusion.AppendVertex(new PrintVertex(toPoints[i], fSpeed));
            if (perVertexFlags != null)
                ToolpathUtil.AddPerVertexFlags(extrusion, perVertexFlags);
			AppendPath(extrusion);
			return currentPos;
		}



		/// <summary>
		/// Dwell for a number of milliseconds (1000ms=1s). Optionally, retract/unretract.
		/// </summary>
		public virtual void AppendDwell(int ms, bool bRetract)
		{
			AssemblerCommandsToolpath dwell_path = new AssemblerCommandsToolpath() {
				AssemblerF = (iassembler, icomplier) => {
					BaseDepositionAssembler asm = iassembler as BaseDepositionAssembler;
					if (asm == null)
						throw new Exception("ToolpathSetBuilder.AppendDwell: unsupported assembler");
					asm.FlushQueues();
					if ( bRetract )
						asm.BeginRetractRelativeDist(asm.NozzlePosition, 9999, -1.0f);
					asm.AppendDwell(ms);
					if (bRetract)
						asm.EndRetract(asm.NozzlePosition, 9999);					
				}
			};
			AppendPath(dwell_path);			
		}


        /// <summary>
        /// Command toolpaths are used to pass special commands/etc to compiler.
        /// These toolpaths do not affect the current extruder position/etc.
        /// </summary>
        protected virtual bool IsCommandToolpath(IToolpath toolpath)
        {
            return toolpath.Type == ToolpathTypes.Custom
                || toolpath.Type == ToolpathTypes.CustomAssemblerCommands;
        }


    }
}
