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

	// Returns true when there is a separating axis (i.e. the boxes do NOT overlap on this axis).
	static bool IsSeparated(Vector2 axis, OBB a, OBB b) {
		float centerDist = Mathf.Abs(Vector2.Dot(b.center - a.center, axis));
		float rA = Mathf.Abs(Vector2.Dot(a.axisX * a.halfX, axis))
		         + Mathf.Abs(Vector2.Dot(a.axisY * a.halfY, axis));
		float rB = Mathf.Abs(Vector2.Dot(b.axisX * b.halfX, axis))
		         + Mathf.Abs(Vector2.Dot(b.axisY * b.halfY, axis));
		return centerDist > rA + rB;
	}

	// SAT overlap test using the four candidate separating axes (two per box).
	public bool Overlaps(OBB other) {
		return !IsSeparated(axisX, this, other)
		    && !IsSeparated(axisY, this, other)
		    && !IsSeparated(other.axisX, this, other)
		    && !IsSeparated(other.axisY, this, other);
	}
}
