using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Meshing;

public static class Triangulation {

	public static bool IsClockwise(List<Vector3> points) {
		int minIndex = 0;
		for(int i = 1; i < points.Count; i++) {
			if(points[minIndex].y < points[i].y) continue;
			minIndex = i;
		}
		var a = points[(minIndex - 1 + points.Count) % points.Count];
		var b = points[minIndex];
		var c = points[(minIndex + 1) % points.Count];
		return IsConvex(a, b, c);
	}

	public static bool IsConvex(Vector3 a, Vector3 b, Vector3 c) {
		return Vector3.Cross(a - b, c - b).z > 0f;
	}

	public static List<Vector3> Triangulate(List<Vector3> points, ICanvas canvas = null) {
		List<Vector3> result = new List<Vector3>();
		bool processed = true;
		while(points.Count > 2 && processed) {
			processed = false;
			for(int i = 0; i < points.Count; i++) {
				var a = points[(i - 1 + points.Count) % points.Count];
				var b = points[i];
				var c = points[(i + 1) % points.Count];

				if(IsConvex(a, b, c)) {
					bool contains = false;
					for(int j = 0; j < points.Count; j++) {
						if(j == i || j == i - 1 || j == i + 1) continue;
						if(!TriangleContains2d(a, b, c, points[j])) continue;
						contains = true;
						break;
					}
					if(!contains) {
						if(canvas != null) {
							canvas.DrawLine(a, b);
							canvas.DrawLine(b, c);
							canvas.DrawLine(c, a);
						}
						result.Add(a);
						result.Add(b);
						result.Add(c);
						points.RemoveAt(i--);
						processed = true;
						if(points.Count < 3) break;
					}
				}
			}
		}
		return result;
	}

	public static List<Vector3> TriangulateWithHoles(List<Vector3> outer, List<List<Vector3>> holes) {
		var poly = new Polygon();

		// Outer contour: Triangle.NET expects CCW for outer boundary.
		// Our outer polygon is CW, so reverse before adding.
		// Deduplicate first to prevent StackOverflow in DivconqRecurse.
		var outerDedup = DeduplicateVertices(outer);
		if(outerDedup.Count < 3) return Triangulate(new List<Vector3>(outer));
		var outerVerts = outerDedup.Select(v => new Vertex(v.x, v.y)).ToList();
		outerVerts.Reverse();
		poly.Add(new Contour(outerVerts));

		if(holes != null) {
			foreach(var hole in holes) {
				if(hole.Count < 3) continue;
				if(Mathf.Abs(SignedArea2x(hole)) < 1e-9f) continue;
				// Holes are CW (same orientation as outer polygons).
				// Triangle.NET locates the hole region via FindInteriorPoint regardless of winding.
				var holeDedup = DeduplicateVertices(hole);
				if(holeDedup.Count < 3) continue;
				var holeVerts = holeDedup.Select(v => new Vertex(v.x, v.y)).ToList();
				poly.Add(new Contour(holeVerts), true);
			}
		}

		var options = new ConstraintOptions { ConformingDelaunay = false };
		try {
			var mesh = poly.Triangulate(options);
			// Triangle.NET outputs CCW triangles; return CW (matching Triangulate output).
			var result = new List<Vector3>(mesh.Triangles.Count * 3);
			foreach(var tri in mesh.Triangles) {
				var v0 = tri.GetVertex(0);
				var v1 = tri.GetVertex(1);
				var v2 = tri.GetVertex(2);
				// Reverse CCW → CW
				result.Add(new Vector3((float)v0.x, (float)v0.y, 0f));
				result.Add(new Vector3((float)v2.x, (float)v2.y, 0f));
				result.Add(new Vector3((float)v1.x, (float)v1.y, 0f));
			}
			return result;
		} catch(System.Exception e) {
			// Fall back to simple ear-clipping on the outer contour when Triangle.NET fails.
			Debug.LogWarning($"TriangulateWithHoles fell back to ear-clipping: {e.Message}");
			return Triangulate(new List<Vector3>(outer));
		}
	}

	// Removes consecutive duplicate vertices (within eps) and also removes the last
	// vertex if it duplicates the first, preventing Triangle.NET's DivconqRecurse from
	// entering infinite recursion on degenerate input.
	static List<Vector3> DeduplicateVertices(List<Vector3> points, float eps = 1e-6f) {
		if(points.Count == 0) return points;
		float epsSq = eps * eps;
		var result = new List<Vector3>();
		result.Add(points[0]);
		for(int i = 1; i < points.Count; i++) {
			float dx = points[i].x - result[result.Count - 1].x;
			float dy = points[i].y - result[result.Count - 1].y;
			if(dx * dx + dy * dy > epsSq) {
				result.Add(points[i]);
			}
		}
		// Remove last if it wraps back to first
		if(result.Count > 1) {
			float dx = result[result.Count - 1].x - result[0].x;
			float dy = result[result.Count - 1].y - result[0].y;
			if(dx * dx + dy * dy <= epsSq) {
				result.RemoveAt(result.Count - 1);
			}
		}
		return result;
	}

	// Returns twice the signed area of a polygon (shoelace sum without the ½ factor).
	// Positive for CW winding (screen/Unity convention), negative for CCW.
	// Comparing the raw sum against an epsilon is valid for degeneracy checks because
	// a near-zero area polygon is near-zero regardless of the ½ factor.
	public static float SignedArea2x(List<Vector3> polygon) {
		float area = 0f;
		for(int k = 0; k < polygon.Count; k++) {
			int nk = (k + 1) % polygon.Count;
			area += polygon[k].x * polygon[nk].y - polygon[nk].x * polygon[k].y;
		}
		return area;
	}

	static readonly Vector3 rayDir = Vector3.forward;
	static readonly double EPS = 1e-6;
	static bool TriangleContains2d(Vector3 a, Vector3 b, Vector3 c, Vector3 p) {
		// Find vectors for two edges sharing vert0
		var edge1 = b - a;
		var edge2 = c - a;

		// Begin calculating determinant - also used to calculate U parameter
		var pvec = Vector3.Cross(rayDir, edge2);

		// If determinant is near zero, ray lies in plane of triangle
		double det = Vector3.Dot(edge1, pvec);
		double inv_det = 1.0 / det;

		// Calculate distance from vert0 to ray origin
		var tvec = p - a;

		// Calculate U parameter and test bounds
		double u = Vector3.Dot(tvec, pvec) * inv_det;
		if (u < 0.0 - EPS || u > 1.0 + EPS) {
			return false;
		}

		// Prepare to test V parameter
		var qvec = Vector3.Cross(tvec, edge1);

		// Calculate V parameter and test bounds
		double v = Vector3.Dot(rayDir, qvec) * inv_det;
		if (v < 0.0 - EPS || u + v > 1.0 + EPS) {
			return false;
		}
		return true;
	}
}