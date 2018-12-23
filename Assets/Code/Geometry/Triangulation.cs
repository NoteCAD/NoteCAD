using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

	public static List<Vector3> Triangulate(List<Vector3> points, LineCanvas canvas = null) {
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
						if(canvas) {
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