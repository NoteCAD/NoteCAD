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
	// Optimisations:
	//   1. Center offset d is computed once and reused across all four tests.
	//   2. When the test axis is one of a box's own orthonormal axes, that box's
	//      projected half-size is simply its stored halfX/halfY — no dot product needed.
	//   3. The absolute relative-orientation scalars (aij = |dot(axisI, other.axisJ)|)
	//      are precomputed once; each is reused in two of the four tests.
	public bool Overlaps(OBB other) {
		Vector2 d = other.center - center;

		// Absolute relative-orientation scalars (each reused across two axis tests).
		float a00 = Mathf.Abs(Vector2.Dot(axisX, other.axisX));
		float a01 = Mathf.Abs(Vector2.Dot(axisX, other.axisY));
		float a10 = Mathf.Abs(Vector2.Dot(axisY, other.axisX));
		float a11 = Mathf.Abs(Vector2.Dot(axisY, other.axisY));

		// Axis: this.axisX — projection of 'this' = halfX (own axis).
		if(Mathf.Abs(Vector2.Dot(d, axisX)) > halfX + a00 * other.halfX + a01 * other.halfY)
			return false;

		// Axis: this.axisY — projection of 'this' = halfY (own axis).
		if(Mathf.Abs(Vector2.Dot(d, axisY)) > halfY + a10 * other.halfX + a11 * other.halfY)
			return false;

		// Axis: other.axisX — projection of 'other' = other.halfX (own axis).
		if(Mathf.Abs(Vector2.Dot(d, other.axisX)) > other.halfX + a00 * halfX + a10 * halfY)
			return false;

		// Axis: other.axisY — projection of 'other' = other.halfY (own axis).
		if(Mathf.Abs(Vector2.Dot(d, other.axisY)) > other.halfY + a01 * halfX + a11 * halfY)
			return false;

		return true;
	}
}
