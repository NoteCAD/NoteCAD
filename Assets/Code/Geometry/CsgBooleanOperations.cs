using Csg;

namespace NoteCAD.Geometry {

	/// <summary>
	/// Boolean operations backed by the managed CSG library.
	/// Works on all platforms including WebGL.
	/// </summary>
	public class CsgBooleanOperations : IBooleanOperations {
		public Solid Union(Solid a, Solid b) => Solids.Union(a, b);
		public Solid Difference(Solid a, Solid b) => Solids.Difference(a, b);
		public Solid Intersection(Solid a, Solid b) => Solids.Intersection(a, b);
		public Solid Assembly(Solid a, Solid b) => Solids.Assembly(a, b);
	}

}
