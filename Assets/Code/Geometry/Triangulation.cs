using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Triangulation {

	public static bool IsConvex(Vector3 a, Vector3 b, Vector3 c) {
		return Vector3.Cross(a - b, c - b).z > 0f;
	}

	public static List<Vector3> Triangulate(List<Vector3> points) {
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

	static bool TriangleContains2d(Vector3 a, Vector3 b, Vector3 c, Vector3 p) {
		var n = new Vector3(0f, 0f, 1f);
		var ab = Vector3.Cross(b - a, n);
		var bc = Vector3.Cross(c - b, n);
		var ca = Vector3.Cross(a - c, n);

		if(Vector3.Cross(ab, bc).z < 0) {
			ab = -ab;
			bc = -bc;
			ca = -ca;
		}

		if(Vector3.Dot(ab, p) - Vector3.Dot(ab, a) > 0f) return false;
		if(Vector3.Dot(bc, p) - Vector3.Dot(bc, b) > 0f) return false;
		if(Vector3.Dot(ca, p) - Vector3.Dot(ca, c) > 0f) return false;

		return true;
	}
}