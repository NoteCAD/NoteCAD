using System;
using g3;

namespace gs
{
	// [TODO] be able to not hardcode this type?
	using LinearToolpath = LinearToolpath3<PrintVertex>;



    public interface ThreeAxisPrinterCompiler
    {
        // current nozzle position
        Vector3d NozzlePosition { get; }

        // compiler will call this to emit status messages / etc
        Action<string> EmitMessageF { get; set; }

        void Begin();
        void AppendPaths(ToolpathSet paths);
        void AppendComment(string comment);
        void End();
    }



	public class SingleMaterialFFFCompiler : ThreeAxisPrinterCompiler
	{
		SingleMaterialFFFSettings Settings;
		GCodeBuilder Builder;
        BaseDepositionAssembler Assembler;

        AssemblerFactoryF AssemblerF;

        /// <summary>
        /// compiler will call this to emit status messages / etc
        /// </summary>
        public virtual Action<string> EmitMessageF { get; set; }


        public SingleMaterialFFFCompiler(GCodeBuilder builder, SingleMaterialFFFSettings settings, AssemblerFactoryF AssemblerF )
		{
			Builder = builder;
			Settings = settings;
            this.AssemblerF = AssemblerF;
		}


		public Vector3d NozzlePosition {
			get { return Assembler.NozzlePosition; }
		}
		public double ExtruderA {
			get { return Assembler.ExtruderA; }
		}
		public bool InRetract {
			get { return Assembler.InRetract; }
		}
		public bool InTravel {
			get { return Assembler.InTravel; }
		}

		public virtual void Begin() {
            Assembler = AssemblerF(Builder, Settings);
			Assembler.AppendHeader();
		}


		public virtual void End() {
            Assembler.FlushQueues();

            Assembler.UpdateProgress(100);
			Assembler.AppendFooter();
		}


        /// <summary>
        /// Compile this set of toolpaths and pass to assembler
        /// </summary>
		public virtual void AppendPaths(ToolpathSet paths)
        {
            Assembler.FlushQueues();

            CalculateExtrusion calc = new CalculateExtrusion(paths, Settings);
			calc.Calculate(Assembler.NozzlePosition, Assembler.ExtruderA, Assembler.InRetract);


            int path_index = 0;
			foreach (var gpath in paths) {
                path_index++;

                if ( IsCommandToolpath(gpath) ) {
                    ProcessCommandToolpath(gpath);
                    continue;
                }

				LinearToolpath p = gpath as LinearToolpath;

				if (p[0].Position.Distance(Assembler.NozzlePosition) > 0.00001)
					throw new Exception("SingleMaterialFFFCompiler.AppendPaths: path " + path_index + ": Start of path is not same as end of previous path!");

				int i = 0;
				if ((p.Type == ToolpathTypes.Travel || p.Type == ToolpathTypes.PlaneChange) && Assembler.InTravel == false) {
					Assembler.DisableFan();

					// do retract cycle
					if (p[0].Extrusion.x < Assembler.ExtruderA) {
                        if (Assembler.InRetract)
                            throw new Exception("SingleMaterialFFFCompiler.AppendPaths: path " + path_index + ": already in retract!");
						Assembler.BeginRetract(p[0].Position, Settings.RetractSpeed, p[0].Extrusion.x);
					}
					Assembler.BeginTravel();

				} else if (p.Type == ToolpathTypes.Deposition) {

					// end travel / retract if we are in that state
					if (Assembler.InTravel) {
						if (Assembler.InRetract) {
							Assembler.EndRetract(p[0].Position, Settings.RetractSpeed, p[0].Extrusion.x);
						}
						Assembler.EndTravel();
						Assembler.EnableFan();
					}

				}

				i = 1;      // do not need to emit code for first point of path, 
							// we are already at this pos

				for (; i < p.VertexCount; ++i) {
					if (p.Type == ToolpathTypes.Travel) {
						Assembler.AppendMoveTo(p[i].Position, p[i].FeedRate, "Travel");
					} else if (p.Type == ToolpathTypes.PlaneChange) {
						Assembler.AppendMoveTo(p[i].Position, p[i].FeedRate, "Plane Change");
					} else {
						Assembler.AppendExtrudeTo(p[i].Position, p[i].FeedRate, p[i].Extrusion.x);
					}
				}

			}


            Assembler.FlushQueues();
        }



        public virtual void AppendComment(string comment)
        {
            Assembler.AppendComment(comment);
        }



        /// <summary>
        /// Command toolpaths are used to pass special commands/etc to the Assembler.
        /// The positions will be ignored
        /// </summary>
        protected virtual bool IsCommandToolpath(IToolpath toolpath)
        {
            return toolpath.Type == ToolpathTypes.Custom
                || toolpath.Type == ToolpathTypes.CustomAssemblerCommands;
        }


        /// <summary>
        /// Called on toolpath if IsCommandToolpath() returns true
        /// </summary>
        protected virtual void ProcessCommandToolpath(IToolpath toolpath)
        {
            if (toolpath.Type == ToolpathTypes.CustomAssemblerCommands) {
                AssemblerCommandsToolpath assembler_path = toolpath as AssemblerCommandsToolpath;
                if (assembler_path != null && assembler_path.AssemblerF != null) {
                    assembler_path.AssemblerF(Assembler, this);
                } else {
                    emit_message("ProcessCommandToolpath: invalid " + toolpath.Type.ToString());
                }

            } else {
                emit_message("ProcessCommandToolpath: unhandled type " + toolpath.Type.ToString());
            }
            
        }



        protected virtual void emit_message(string text, params object[] args)
        {
            if (EmitMessageF != null)
                EmitMessageF(string.Format(text, args));
        }

    }
}
