using System;
using System.Collections.Generic;
using System.Linq;
using g3;

namespace gs 
{
	/// <summary>
	/// Convert a GCodeFile to a single huge PathSet
	/// </summary>
	public class GCodeToTubeMeshes : IGCodeListener
	{
        public Polygon2d TubeProfile = Polygon2d.MakeCircle(0.1f, 8);

        public Dictionary<double, List<DMesh3>> LayerMeshes;

        PolyLine3d ActivePath;
        ToolpathTypes ActivePathType;

        public GCodeToTubeMeshes() 
		{
        }


        public DMesh3 GetCombinedMesh(int nLayerStep = 1)
        {
            DMesh3 fullMesh = new DMesh3();
            double[] z = LayerMeshes.Keys.ToArray();
            List<DMesh3>[] layers = LayerMeshes.Values.ToArray();
            Array.Sort(z, layers);
            nLayerStep = MathUtil.Clamp(nLayerStep, 1, 999999);
            for ( int li = 0; li < layers.Length; li += nLayerStep) {
                var meshlist = layers[li];
                foreach (var mesh in meshlist)
                    MeshEditor.Append(fullMesh, mesh);
            }
            return fullMesh;
        }



        void append_path_mesh(PolyLine3d pathLine)
        {
            TubeGenerator tubegen = new TubeGenerator() {
                Vertices = new List<Vector3d>(pathLine.Vertices),
                Polygon = TubeProfile,
                ClosedLoop = false,
                Capped = true,
                NoSharedVertices = true
            };
            DMesh3 tubeMesh = tubegen.Generate().MakeDMesh();
            List<DMesh3> layerList;
            if ( LayerMeshes.TryGetValue(ActivePath[0].z, out layerList) == false ) {
                layerList = new List<DMesh3>();
                LayerMeshes[ActivePath[0].z] = layerList;
            }
            layerList.Add(tubeMesh);
        }


        void push_active_path() {
            if (ActivePath != null && ActivePath.VertexCount > 1 ) {
                if (ActivePathType == ToolpathTypes.Deposition)
                    append_path_mesh(ActivePath);
            }
            ActivePath = null;
		}
        void discard_active_path() {
            ActivePath = null;
        }

		public void Begin() {
            LayerMeshes = new Dictionary<double, List<DMesh3>>();
            ActivePath = new PolyLine3d();
		}
		public void End() {
			push_active_path();
		}


		public void BeginTravel() {

			var newPath = new PolyLine3d();

            if (ActivePath != null && ActivePath.VertexCount > 0) {
                newPath.AppendVertex(ActivePath.End);
			}

			push_active_path();
			ActivePath = newPath;
            ActivePathType = ToolpathTypes.Travel;
		}
		public void BeginDeposition() {

            var newPath = new PolyLine3d();
			if (ActivePath != null && ActivePath.VertexCount > 0) {
                newPath.AppendVertex(ActivePath.End);
			}

			push_active_path();
			ActivePath = newPath;
            ActivePathType = ToolpathTypes.Deposition;

        }


        public void LinearMoveToAbsolute3d(LinearMoveData move)
		{
			if (ActivePath == null)
				throw new Exception("GCodeToLayerPaths.LinearMoveToAbsolute3D: ActivePath is null!");

			// if we are doing a Z-move, convert to 3D path
			bool bZMove = (ActivePath.VertexCount > 0 && ActivePath.End.z != move.position.z);
            if (bZMove)
                ActivePathType = ToolpathTypes.PlaneChange;

            ActivePath.AppendVertex(move.position);
		}


		public void CustomCommand(int code, object o) {
			if ( code == (int)CustomListenerCommands.ResetExtruder ) {
				push_active_path();
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
