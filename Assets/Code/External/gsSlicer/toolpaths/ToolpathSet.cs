using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using g3;

namespace gs 
{
	public class ToolpathSet : IToolpathSet
	{
		List<IToolpath> Paths;

		ToolpathTypes eType;
		bool isPlanar;
		bool isLinear;

		public ToolpathSet() 
		{
			Paths = new List<IToolpath>();

			eType = ToolpathTypes.Custom;
			isPlanar = isLinear = false;
		}

		public ToolpathTypes Type { 
			get { return eType; }
		}
		public bool IsPlanar { 
			get { return isPlanar; }
		}
		public bool IsLinear {
			get { return isLinear; }
		}


		public void Append(IToolpath path) {
			if ( Paths.Count == 0 ) {
				eType = path.Type;
				isPlanar = path.IsPlanar;
				isLinear = path.IsLinear;
			} else if ( eType != path.Type ) {
				eType = ToolpathTypes.Composite;
				isPlanar = isPlanar && path.IsPlanar;
				isLinear = isLinear && path.IsLinear;
			}
			Paths.Add(path);
		}


		public void AppendChildren( IToolpathSet paths ) {
			foreach ( var p in paths )
				Append(p);
		}


		public IEnumerator<IToolpath> GetEnumerator() {
			return Paths.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator() {
			return Paths.GetEnumerator();
		}

		public virtual Vector3d StartPosition {
			get {
				return Paths[0].StartPosition;
			}
		}

		public virtual Vector3d EndPosition {
			get {
				return Paths[Paths.Count - 1].EndPosition;
			}
		}


		public AxisAlignedBox3d Bounds
		{
			get {
				AxisAlignedBox3d box = AxisAlignedBox3d.Empty;
				foreach ( var p in Paths )
					box.Contain(p.Bounds);
				return box;
			}
		}

		public AxisAlignedBox3d ExtrudeBounds
		{
			get {
				AxisAlignedBox3d box = AxisAlignedBox3d.Empty;
				foreach ( var p in Paths ) {
                    if (p is ToolpathSet)
                        box.Contain((p as ToolpathSet).ExtrudeBounds);
					else if ( p.Type == ToolpathTypes.Deposition )
						box.Contain(p.Bounds);
				}
				return box;				
			}
		}


		public bool HasFinitePositions {
			get { return false; }
		}
		public IEnumerable<Vector3d> AllPositionsItr()
		{
			return Enumerable.Empty<Vector3d>();
		}



		public List<double> GetZValues() {
			HashSet<double> Zs = new HashSet<double>();
			ToolpathUtil.ApplyToLeafPaths(this, (ipath) => {
				if ( ipath is LinearToolpath3<IToolpathVertex> ) {
					foreach ( var v in (ipath as LinearToolpath3<IToolpathVertex>) ) {
						Zs.Add( v.Position.z );
					}
				}
			});
			return new List<double>(Zs);
		}

	}
}
