using System;
using System.Collections;
using System.Collections.Generic;
using g3;

namespace gs 
{
	public class LinearToolpath3<T>  : IBuildLinearToolpath<T> where T : IToolpathVertex
	{
		List<T> Path;
		ToolpathTypes _pathtype;	// access via Type property
        FillTypeFlags _pathtype_flags = FillTypeFlags.Unknown;

        // todo: add speed
        //  ?? extend PolyLine3d ??

        public LinearToolpath3(ToolpathTypes type = ToolpathTypes.Travel)
		{
			Path = new List<T>();
			_pathtype = type;
		}
		public LinearToolpath3(ILinearToolpath<T> copy) {
			Path = new List<T>();
			_pathtype = copy.Type;		
			foreach ( T v in copy )
				Path.Add(v);
		}

		public T this[int key] { 
			get {
				return Path[key];
			}
		}

		public bool IsLinear {
			get { return true; }
		}

		public bool IsPlanar {
			get { 
				double z = Path[0].Position.z;
                  for ( int i = 1; i < Path.Count; ++i ) {
					if ( Path[i].Position.z != z )
						return false;
				}
				return true;
			}
		}

		public double Length {
			get {
				double sum = 0;
				for (int i = 1; i < Path.Count; ++i)
					sum += Path[i].Position.Distance(Path[i - 1].Position);
				return sum;
			}
		}

		public ToolpathTypes Type {
			get { return _pathtype; }
			set { _pathtype = value; }
		}

        public FillTypeFlags TypeModifiers {
            get { return _pathtype_flags; }
            set { _pathtype_flags = value; }
        }

        public virtual Vector3d StartPosition {
			get {
				return Path[0].Position;
			}
		}

		public virtual Vector3d EndPosition {
			get {
				return Path[Path.Count - 1].Position;
			}
		}

		public AxisAlignedBox3d Bounds { 
			get {
				return BoundsUtil.Bounds(this, (vtx) => { return vtx.Position; });
			}
		}


		public bool HasFinitePositions {
			get { return true; }
		}
		public IEnumerable<Vector3d> AllPositionsItr() {
			foreach (var v in Path)
				yield return v.Position;
		}


		public IEnumerator<T> GetEnumerator() {
			return Path.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator() {
			return Path.GetEnumerator();
		}


		public int VertexCount {
			get { return Path.Count; }
		}
		public void AppendVertex(T v) {
			if ( Path.Count == 0 || End.Position.DistanceSquared(v.Position) > MathUtil.Epsilon )	
				Path.Add(v);
		}
		public void UpdateVertex(int i, T v)
		{
			Path[i] = v;
		}
		public T Start { 
			get { return Path[0]; }
		}
		public T End { 
			get { return Path[Path.Count-1]; }
		}
		public void ChangeType(ToolpathTypes type) {
			Type = type;
		}


        // computes opening angle in XY plane at vtx i
        public double PlanarAngleD(int i)
        {
            Vector2d c = Path[i].Position.xy;
            Vector2d prev = Path[i - 1].Position.xy;
            Vector2d next = Path[i + 1].Position.xy;
            return Vector2d.AngleD((prev - c).Normalized, (next - c).Normalized);
        }
	}
}
