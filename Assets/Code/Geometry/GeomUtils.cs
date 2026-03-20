using UnityEngine;
using System.Collections.Generic;
    
//------------------------------------------------------------------------------
//
// class GeomUtils
//
//------------------------------------------------------------------------------

public static class GeomUtils {

	public static float GetAngle(Vector3 d0, Vector3 d1) {
		var du = d1.x * d0.x + d1.y * d0.y;
		var dv = d0.x * d1.y - d0.y * d1.x;
		return Mathf.Atan2(dv, du);
	}
		
	public static float DistancePointLine2D(Vector3 p, Vector3 p0, Vector3 p1) {
		return ((p0.y - p1.y) * p.x + (p1.x - p0.x) * p.y + p0.x * p1.y - p1.x * p0.y) / Mathf.Sqrt((p1.x - p0.x) * (p1.x - p0.x) + (p1.y - p0.y) * (p1.y - p0.y));
	}

	public static float DistancePointSegment2D(Vector3 p, Vector3 p0, Vector3 p1) {
		if(p0.x == p1.x && p0.y == p1.y) return (p - p0).magnitude;
		p0.z = 0;
		p1.z = 0;
		p.z = 0;
		var dir = p1 - p0;
		var t = Mathf.Clamp01(Vector3.Dot(dir, p - p0) / Vector3.Dot(dir, dir));
		var pp = p0 + dir * t;
		return (pp - p).magnitude;
	}

	public static float DistancePointSegment3D(Vector3 p, Vector3 p0, Vector3 p1) {
		var dir = p1 - p0;
		var t = Mathf.Clamp01(Vector3.Dot(dir, p - p0) / Vector3.Dot(dir, dir));
		var pp = p0 + dir * t;
		return (pp - p).magnitude;
	}

	public static float DistancePointLine3D(Vector3 p, Vector3 p0, Vector3 p1) {
		var dir = p1 - p0;
		var t = Vector3.Dot(dir, p - p0) / Vector3.Dot(dir, dir);
		var pp = p0 + dir * t;
		return (pp - p).magnitude;
	}

	public static Vector3 projectToPlane(Vector3 pos, Vector3 normal, Vector3 point) {
		Plane p = new Plane(normal, point);
		return pos - p.GetDistanceToPoint(pos) * normal;
	}

	public static Vector3 projectPointToLine(Vector3 pos, Vector3 lp0, Vector3 lp1) {
		var dir = lp1 - lp0;
		float t = Vector3.Dot(pos - lp0, dir) / Vector3.Dot(dir, dir);
		return lp0 + t * dir;
	}
	
	public enum Classify {
		etLEFT,
		etRIGHT,
		etORIGIN,
		etDESTINATION,
		etBEHIND,
		etBEYOND,
		etBETWEEN,
		etTOUCHA,
		etTOUCHB,
		etOUTSIDE,
	}
	
	public enum Cross {
		NONE,
		TOUCH,
		INTERSECTION,
		COLLINEAR,
	}

	// Classifies point P relative to the directed line segment A→B in 2D.
	// Returns one of the Classify enum values that describe P's spatial relationship to the segment.
	// EPS is the tolerance used for squared-distance endpoint snapping and perpendicular-distance side tests.
	public static Classify classify(Vector3 P, Vector3 A, Vector3 B, float EPS) {

		// Check whether P is coincident with an endpoint.
		// Uses squared distance directly (no sqrt), matching the original tolerance convention.
		if ((P - A).sqrMagnitude < EPS) return Classify.etTOUCHA;
		if ((P - B).sqrMagnitude < EPS) return Classify.etTOUCHB;

		// 2D direction vector of the segment (z component is ignored throughout).
		float dx = B.x - A.x;
		float dy = B.y - A.y;
		float len = Mathf.Sqrt(dx * dx + dy * dy);

		// Guard against degenerate zero-length segment (A == B): treat P as BETWEEN.
		if (len == 0.0f) return Classify.etBETWEEN;

		// Signed perpendicular distance from P to the directed line A→B, normalized by |AB|.
		//   ro > 0  →  P is to the right of A→B
		//   ro < 0  →  P is to the left  of A→B
		// Computed as the z-component of cross(AB, AP) divided by |AB|, equivalent to
		// the original Plane-based approach but without allocating a Plane object.
		float ro = ((P.x - A.x) * dy - (P.y - A.y) * dx) / len;

		if (ro < -EPS) return Classify.etLEFT;
		if (ro >  EPS) return Classify.etRIGHT;

		// P lies on (or very near) the infinite line through A and B.
		// Determine where P falls along the segment by projecting onto the segment direction.
		// We only need the signs of roA and roB, so no division by len is required.
		//   roA = dot(A - P, AB):  > 0 means A is ahead of P in the AB direction
		//   roB = dot(B - P, AB):  > 0 means B is ahead of P in the AB direction
		float roA = (A.x - P.x) * dx + (A.y - P.y) * dy;
		float roB = (B.x - P.x) * dx + (B.y - P.y) * dy;

		if (roA > 0.0f && roB > 0.0f) return Classify.etBEYOND;
		if (roA < 0.0f && roB < 0.0f) return Classify.etBEHIND;

		return Classify.etBETWEEN;
	}
	
	
	static bool isSegmentOutside(Classify a, Classify b) {
		return
			a == Classify.etLEFT && b == Classify.etLEFT || 
			a == Classify.etRIGHT && b == Classify.etRIGHT ||
			a == Classify.etBEHIND && b == Classify.etBEHIND ||
			a == Classify.etBEYOND && b == Classify.etBEYOND;
				
	}
	
	static bool isPointTouch(Classify a) {
		return a == Classify.etTOUCHA || a == Classify.etTOUCHB || a == Classify.etBETWEEN;
	}
	
	static bool isPointOnLine(Classify a) {
		return a == Classify.etTOUCHA || a == Classify.etTOUCHB || a == Classify.etBETWEEN || a == Classify.etBEHIND || a == Classify.etBEYOND;
	}
	
	// Classifies whether two collinear segments cross, touch, or are disjoint,
	// given the Classify results of the second segment's endpoints relative to the first segment.
	public static Cross classifyCollinearSegmentCross(Classify a, Classify b) {

		// Endpoint-to-endpoint touch cases: one end of segment 2 coincides with an endpoint of
		// segment 1, while the other end of segment 2 lies completely outside segment 1.
		if (a == Classify.etTOUCHA && b == Classify.etBEYOND) return Cross.TOUCH;
		if (a == Classify.etTOUCHB && b == Classify.etBEHIND) return Cross.TOUCH;
		if (b == Classify.etTOUCHA && a == Classify.etBEYOND) return Cross.TOUCH;
		if (b == Classify.etTOUCHB && a == Classify.etBEHIND) return Cross.TOUCH;
		if (a == Classify.etTOUCHA && b == Classify.etTOUCHA) return Cross.TOUCH;
		if (a == Classify.etTOUCHB && b == Classify.etTOUCHB) return Cross.TOUCH;

		// If both endpoints of segment 2 lie on the line through segment 1 (collinear and overlapping),
		// the segments intersect (share a common sub-interval).
		if (isPointOnLine(a)) {
			if (isPointOnLine(b)) return Cross.INTERSECTION;
			return Cross.TOUCH;
		}

		// One endpoint of segment 2 is on the line; the other is not → single touch point.
		if (isPointOnLine(b)) return Cross.TOUCH;

		return Cross.NONE;
	}
	
	// Tests whether two 2D line segments A1→B1 and A2→B2 cross, touch, or are disjoint.
	// On a proper intersection (Cross.INTERSECTION) the 2D intersection point is written to itr.
	// USEEPS is the tolerance for bounding-box expansion, endpoint snapping, and parallel-line detection.
	public static Cross isSegmentsCrossed(Vector3 A1, Vector3 B1, Vector3 A2, Vector3 B2, ref Vector3 itr, float USEEPS) {
		// Quick rejection: non-overlapping bounding boxes guarantee no intersection.
		if(!BBox.Overlaps2d(A1, B1, A2, B2, USEEPS)) return Cross.NONE;
		Vector3 L1 = B1 - A1;
		Vector3 L2 = B2 - A2;

		// Classify each endpoint of one segment relative to the other segment's supporting line.
		Classify A1C2 = classify(A1, A2, B2, USEEPS);   // A1 relative to line A2→B2
		Classify B1C2 = classify(B1, A2, B2, USEEPS);   // B1 relative to line A2→B2

		Classify A2C1 = classify(A2, A1, B1, USEEPS);   // A2 relative to line A1→B1
		Classify B2C1 = classify(B2, A1, B1, USEEPS);   // B2 relative to line A1→B1
		
		// If both endpoints of one segment are strictly on the same side of the other segment's
		// line (both left, both right, both behind, or both beyond), there is no crossing.
		if (isSegmentOutside(A1C2, B1C2)) return Cross.NONE;
		if (isSegmentOutside(A2C1, B2C1)) return Cross.NONE;
		
		/*Cross co_cross = classifyCollinearSegmentCross(A1C2, B1C2);
		if (co_cross != Cross.NONE) return co_cross;*/

		// Handle degenerate (collinear/touching) cases using segment 2 classified against segment 1.
		Cross co_cross = classifyCollinearSegmentCross(A2C1, B2C1);
		if (co_cross != Cross.NONE) return co_cross;

		// A proper (non-degenerate) crossing exists. Compute the intersection point using
		// parametric line equations:
		//   P = A1 + t·L1  and  P = A2 + s·L2
		// Solving the 2×2 system for t gives:
		//   denom = L1 × L2  (2D cross product; zero when lines are parallel)
		//   t     = (A2 - A1) × L2 / denom
		float denom = L1.x * L2.y - L1.y * L2.x;

		// Guard against degenerate parallel lines (should be rare here given the classify checks above).
		// denom is a cross-product magnitude that scales with |L1|·|L2|; use a relative epsilon so
		// the test is independent of segment length rather than the distance-based USEEPS.
		if (Mathf.Abs(denom) < 1e-10f * (L1.sqrMagnitude + L2.sqrMagnitude)) return Cross.NONE;

		float diffX = A2.x - A1.x;
		float diffY = A2.y - A1.y;
		float t = (diffX * L2.y - diffY * L2.x) / denom;

		itr = new Vector3(A1.x + t * L1.x, A1.y + t * L1.y, 0f);
		
		return Cross.INTERSECTION;
	}

	public static bool isLinesCrossed(Vector3 a1, Vector3 b1, Vector3 a2, Vector3 b2, ref Vector3 itr, float USEEPS) {
		var d1 = b1 - a1;
		var d2 = b2 - a2;
		var n1 = new Vector3(d1.y, -d1.x);
		var n2 = new Vector3(d2.y, -d2.x);
		n1.z = -Vector3.Dot(a1, n1);
		n2.z = -Vector3.Dot(a2, n2);
		itr = Vector3.Cross(n1, n2);
		if(Mathf.Abs(itr.z) < USEEPS) return false;
		itr.x /= itr.z;
		itr.y /= itr.z;
		itr.z = 0f;
		return true;
	}

	public static Bounds Transformed(this Bounds bounds, Matrix4x4 tf) {
		return TransformBounds(tf, bounds);
	}

    static Bounds TransformBounds(Matrix4x4 _transform, Bounds _localBounds) {
        var center = _transform.MultiplyPoint(_localBounds.center);
 
        // transform the local extents' axes
        var extents = _localBounds.extents;
        var axisX = _transform.MultiplyVector(new Vector3(extents.x, 0, 0));
        var axisY = _transform.MultiplyVector(new Vector3(0, extents.y, 0));
        var axisZ = _transform.MultiplyVector(new Vector3(0, 0, extents.z));
 
        // sum their absolute value to get the world extents
        extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
        extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
        extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);
 
        return new Bounds { center = center, extents = extents };
    }

	public static double ParallelEps = 1e-6;
	public static bool IsVectorsParallel(Vector3 v0, Vector3 v1) {
		var cross = Vector3.Cross(v0, v1);
		return cross.sqrMagnitude < ParallelEps * ParallelEps;
	}
	
}

//------------------------------------------------------------------------------
