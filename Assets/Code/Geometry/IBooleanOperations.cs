using Csg;

namespace NoteCAD.Geometry {

	/// <summary>
	/// Abstraction over solid boolean operations.
	/// Implementations may use the managed CSG library (all platforms including WebGL)
	/// or OpenCASCADE (native binary builds only).
	/// </summary>
	public interface IBooleanOperations {
		Solid Union(Solid a, Solid b);
		Solid Difference(Solid a, Solid b);
		Solid Intersection(Solid a, Solid b);
		Solid Assembly(Solid a, Solid b);
	}

}
