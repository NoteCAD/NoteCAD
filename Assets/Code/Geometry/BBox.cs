using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BBox {
	public Vector3 min;
	public Vector3 max;

	public BBox(Vector3 p0, Vector3 p1) {
		min = new Vector3(Mathf.Min(p0.x, p1.x), Mathf.Min(p0.y, p1.y), Mathf.Min(p0.z, p1.z));
		max = new Vector3(Mathf.Max(p0.x, p1.x), Mathf.Max(p0.y, p1.y), Mathf.Max(p0.z, p1.z));
	}

	public BBox(Vector3 point, float size) {
		min = new Vector3(point.x - size, point.y - size, point.z - size);
		max = new Vector3(point.x + size, point.y + size, point.z + size);
	}

	public bool Overlaps(BBox o) {
		if(min.x > o.max.x) return false;
		if(max.x < o.min.x) return false;
		if(min.y > o.max.y) return false;
		if(max.y < o.min.y) return false;
		if(min.z > o.max.z) return false;
		if(max.z < o.min.z) return false;
		return true;
	}

	public static bool Overlaps2d(Vector3 b0p0, Vector3 b0p1, Vector3 b1p0, Vector3 b1p1, float eps) {
		var b0minX = Mathf.Min(b0p0.x, b0p1.x) - eps;
		var b1maxX = Mathf.Max(b1p0.x, b1p1.x) + eps;
		if(b0minX > b1maxX) return false;

		var b0maxX = Mathf.Max(b0p0.x, b0p1.x) + eps;
		var b1minX = Mathf.Min(b1p0.x, b1p1.x) - eps;
		if(b0maxX < b1minX) return false;

		var b0minY = Mathf.Min(b0p0.y, b0p1.y) - eps;
		var b1maxY = Mathf.Max(b1p0.y, b1p1.y) + eps;
		if(b0minY > b1maxY) return false;

		var b0maxY = Mathf.Max(b0p0.y, b0p1.y) + eps;
		var b1minY = Mathf.Min(b1p0.y, b1p1.y) - eps;
		if(b0maxY < b1minY) return false;

		return true;
	}

	public void Include(Vector3 p) {
		min = new Vector3(Mathf.Min(min.x, p.x), Mathf.Min(min.y, p.y), Mathf.Min(min.z, p.z));
		max = new Vector3(Mathf.Max(max.x, p.x), Mathf.Max(max.y, p.y), Mathf.Max(max.z, p.z));
	}
}
