using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace gs
{

    public struct InterpretArgs
    {
        public GCodeLine.LType eTypeFilter;


        public bool HasTypeFilter { get { return eTypeFilter != GCodeLine.LType.Blank; } }

        public static readonly InterpretArgs Default = new InterpretArgs() {
            eTypeFilter = GCodeLine.LType.Blank
        };
    }


    public interface IGCodeInterpreter
    {
        void AddListener(IGCodeListener listener);
        void Interpret(GCodeFile file, InterpretArgs args);
    }


	/// <summary>
	/// GCodeInterpreter passes this to GCodeListener for each G1 line
	/// </summary>
	public struct LinearMoveData {

		public Vector3d position;
		public double rate;
		public Vector3d extrude;
		public GCodeLine source;

		public LinearMoveData(Vector2d pos, 
		                      double rateIn = GCodeUtil.UnspecifiedValue ) {
			position = new Vector3d(pos.x, pos.y, GCodeUtil.UnspecifiedValue);
			rate = rateIn;
			extrude = GCodeUtil.UnspecifiedPosition;
			source = null;
		}
		public LinearMoveData(Vector3d pos, double rateIn = GCodeUtil.UnspecifiedValue) {
			position = new Vector3d(pos.x, pos.y, GCodeUtil.UnspecifiedValue);
			rate = rateIn;
			extrude = GCodeUtil.UnspecifiedPosition;
			source = null;
		}
		public LinearMoveData(Vector3d pos, double rateIn, Vector3d extrudeIn) {
			position = pos;
			rate = rateIn;
			extrude = extrudeIn;
			source = null;
		}
	}


	// codes to pass to IGCodeListener.CustomCommand
	// this is basically just slightly better than complete hacks
	public enum CustomListenerCommands
	{
		ResetPosition = 0,		// object should be Vector3d
		ResetExtruder = 1		// object should be Vector3d 

	}


    public interface IGCodeListener
    {
        void Begin();
		void End();

		void BeginTravel();
		void BeginDeposition();

		// for hacks
		void CustomCommand(int code, object o);

		void LinearMoveToAbsolute2d(LinearMoveData move);
		void LinearMoveToRelative2d(LinearMoveData move);
		void ArcToRelative2d(Vector2d pos, double radius, bool clockwise, double rate = 0);

		void LinearMoveToAbsolute3d(LinearMoveData move);
		void LinearMoveToRelative3d(LinearMoveData move);
    }

}
