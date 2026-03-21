using UnityEngine;

// Oriented Bounding Box (2D) for entity collision pre-filtering.
// Axes are orthonormal; Overlaps uses the Separated Axis Theorem (SAT).
public struct OBB {
	public Vector2 center;
	public Vector2 axisX;
	public Vector2 axisY;
	public float halfX;
	public float halfY;

	public OBB(Vector2 center, Vector2 axisX, float halfX, float halfY) {
		this.center = center;
		this.axisX = axisX;
		this.axisY = new Vector2(-axisX.y, axisX.x);
		this.halfX = halfX;
		this.halfY = halfY;
	}

	// SAT overlap test using the four candidate separating axes (two per box).
	//
	// Optimisations over a naive per-axis IsSeparated helper:
	//   1. The center offset d is computed once and reused across all four tests.
	//   2. When the test axis is one of a box's own orthonormal axes, that box's
	//      projected half-size is simply its stored halfX/halfY — no dot product
	//      needed (saves two dot products per axis test, eight in total).
	//   3. The four relative-orientation scalars (cij = dot(axisI, other.axisJ))
	//      are computed once and shared between the tests that need them.
	public bool Overlaps(OBB other) {
		Vector2 d = other.center - center;

		// Relative orientation between the two frames.
		float c00 = Vector2.Dot(axisX, other.axisX);
		float c01 = Vector2.Dot(axisX, other.axisY);
		float c10 = Vector2.Dot(axisY, other.axisX);
		float c11 = Vector2.Dot(axisY, other.axisY);

		// Axis: this.axisX — projection of 'this' = halfX (own axis).
		if(Mathf.Abs(Vector2.Dot(d, axisX)) > halfX
		        + Mathf.Abs(c00) * other.halfX + Mathf.Abs(c01) * other.halfY)
			return false;

		// Axis: this.axisY — projection of 'this' = halfY (own axis).
		if(Mathf.Abs(Vector2.Dot(d, axisY)) > halfY
		        + Mathf.Abs(c10) * other.halfX + Mathf.Abs(c11) * other.halfY)
			return false;

		// Axis: other.axisX — projection of 'other' = other.halfX (own axis).
		if(Mathf.Abs(Vector2.Dot(d, other.axisX)) > other.halfX
		        + Mathf.Abs(c00) * halfX + Mathf.Abs(c10) * halfY)
			return false;

		// Axis: other.axisY — projection of 'other' = other.halfY (own axis).
		if(Mathf.Abs(Vector2.Dot(d, other.axisY)) > other.halfY
		        + Mathf.Abs(c01) * halfX + Mathf.Abs(c11) * halfY)
			return false;

		return true;
	}
}
