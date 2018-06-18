using System;
using g3;

namespace gs 
{
	using LinearToolpath = LinearToolpath3<PrintVertex>;


	// we will insert these in PathSet when we are
	// instructed to reset extruder stepper
	public class ResetExtruderPathHack : SentinelToolpath
	{
	}

	/// <summary>
	/// Convert a GCodeFile to a single huge ToolpathSet
	/// </summary>
	public class GCodeToToolpaths : IGCodeListener
	{
		public ToolpathSet PathSet;
		public IBuildLinearToolpath<PrintVertex> ActivePath;

		public GCodeToToolpaths() 
		{
		}


		void push_active_path() {
			if ( ActivePath != null && ActivePath.VertexCount > 0 )
				PathSet.Append(ActivePath);
			ActivePath = null;
		}

		public void Begin() {
			PathSet = new ToolpathSet();
			ActivePath = new LinearToolpath();
		}
		public void End() {
			push_active_path();
		}


		public void BeginTravel() {

			var newPath = new LinearToolpath();
			newPath.Type = ToolpathTypes.Travel;
			if (ActivePath != null && ActivePath.VertexCount > 0) {
				PrintVertex curp = new PrintVertex(ActivePath.End.Position, GCodeUtil.UnspecifiedValue, GCodeUtil.UnspecifiedValue);
				newPath.AppendVertex(curp);
			}

			push_active_path();
			ActivePath = newPath;		
		}
		public void BeginDeposition() {
				
			var newPath = new LinearToolpath();
			newPath.Type = ToolpathTypes.Deposition;
			if (ActivePath != null && ActivePath.VertexCount > 0) {
				PrintVertex curp = new PrintVertex(ActivePath.End.Position, GCodeUtil.UnspecifiedValue, GCodeUtil.UnspecifiedValue);
				newPath.AppendVertex(curp);
			}

			push_active_path();
			ActivePath = newPath;				
		}


		public void LinearMoveToAbsolute3d(LinearMoveData move)
		{
			if (ActivePath == null)
				throw new Exception("GCodeToLayerPaths.LinearMoveToAbsolute3D: ActivePath is null!");

			// if we are doing a Z-move, convert to 3D path
			bool bZMove = (ActivePath.VertexCount > 0 && ActivePath.End.Position.z != move.position.z);
			if ( bZMove )
				ActivePath.ChangeType( ToolpathTypes.PlaneChange );

			PrintVertex vtx = new PrintVertex(
				move.position, move.rate, move.extrude.x );
			
			if ( move.source != null )
				vtx.Source = move.source;

			ActivePath.AppendVertex(vtx);
		}


		public void CustomCommand(int code, object o) {
			if ( code == (int)CustomListenerCommands.ResetExtruder ) {
				push_active_path();
				PathSet.Append( new ResetExtruderPathHack() );
			}
		}



		public void LinearMoveToRelative3d(LinearMoveData move)
		{
			throw new NotImplementedException();
		}

		public void LinearMoveToAbsolute2d(LinearMoveData move) {
			throw new NotImplementedException();
		}

		public void LinearMoveToRelative2d(LinearMoveData move) {
			throw new NotImplementedException();
		}


		public void ArcToRelative2d( Vector2d pos, double radius, bool clockwise, double rate = 0 ) {
			throw new NotImplementedException();
		}

	}
}
