using UnityEngine;
using System.Collections.Generic;
    
//------------------------------------------------------------------------------
//
// class GeomUtils
//
//------------------------------------------------------------------------------

public static class GeomUtils {
	
	public static float getAngle(Vector3 dir0, Vector3 dir1) {
		dir0 = dir0.normalized;
		dir1 = dir1.normalized;
		float cos_angle = Mathf.Clamp(Vector3.Dot(dir0, dir1), -1.0f, 1.0f);
		float angle = Mathf.Acos(cos_angle);
		if (Vector3.Cross(dir0, dir1).y < 0.0f) {
			angle = - angle;
		}
		return angle;
	}
	
	public static float getAngle(Vector3 p0, Vector3 p1, Vector3 p2) {
		Vector3 dir0 = (p1 - p0).normalized;
		Vector3 dir1 = (p2 - p0).normalized;
		float cos_angle = Mathf.Clamp(Vector3.Dot(dir0, dir1), -1.0f, 1.0f);
		float angle = Mathf.Acos(cos_angle);
		if (Vector3.Cross(dir0, dir1).y < 0.0f) {
			angle = 2 * Mathf.PI - angle;
		}
		return angle;
	}
	
	public static Vector3 projectToPlane(Vector3 pos, Vector3 normal, Vector3 point) {
		Plane p = new Plane(normal, point);
		return pos - p.GetDistanceToPoint(pos) * normal;
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

	public static Classify classify(Vector3 P, Vector3 A, Vector3 B, float EPS) {
		
		Vector3 l = B - A;
	    l.z = 0;
		l = l.normalized;
		
		Plane plane = new Plane(Vector3.Cross(l, Vector3.forward).normalized, A);
		float ro = plane.GetDistanceToPoint(P);

		if ((P - A).sqrMagnitude < EPS) return Classify.etTOUCHA;
		if ((P - B).sqrMagnitude < EPS) return Classify.etTOUCHB;

		if (ro < -EPS) return Classify.etLEFT;
		if (ro >  EPS)  return Classify.etRIGHT;

		plane = new Plane(l, P);
	
		float roA = plane.GetDistanceToPoint(A);
		float roB = plane.GetDistanceToPoint(B);
	
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
	
	public static Cross classifyCollinearSegmentCross(Classify a, Classify b) {

		if (a == Classify.etTOUCHA && b == Classify.etBEYOND) return Cross.TOUCH;
		if (a == Classify.etTOUCHB && b == Classify.etBEHIND) return Cross.TOUCH;
		if (b == Classify.etTOUCHA && a == Classify.etBEYOND) return Cross.TOUCH;
		if (b == Classify.etTOUCHB && a == Classify.etBEHIND) return Cross.TOUCH;
		if (a == Classify.etTOUCHA && b == Classify.etTOUCHA) return Cross.TOUCH;
		if (a == Classify.etTOUCHB && b == Classify.etTOUCHB) return Cross.TOUCH;

		
		if (isPointOnLine(a)) {
			if (isPointOnLine(b)) return Cross.INTERSECTION;
			return Cross.TOUCH;
		}

		if (isPointOnLine(b)) {
			if (a == Classify.etBEYOND) return Cross.TOUCH;
			if (isPointOnLine(a)) return Cross.INTERSECTION;
			return Cross.TOUCH;
		}

		return Cross.NONE;
	}
	
	public static Cross isSegmentsCrossed(Vector3 A1, Vector3 B1, Vector3 A2, Vector3 B2, ref Vector3 itr, float USEEPS) {
		Vector3 L1 = B1 - A1;
		Vector3 L2 = B2 - A2;
	
		Classify A1C2 = classify(A1, A2, B2, USEEPS);
		Classify B1C2 = classify(B1, A2, B2, USEEPS);
	
		Classify A2C1 = classify(A2, A1, B1, USEEPS);
		Classify B2C1 = classify(B2, A1, B1, USEEPS);
		
		if (isSegmentOutside(A1C2, B1C2)) return Cross.NONE;
		if (isSegmentOutside(A2C1, B2C1)) return Cross.NONE;
		
		/*Cross co_cross = classifyCollinearSegmentCross(A1C2, B1C2);
		if (co_cross != Cross.NONE) return co_cross;*/

		Cross co_cross = classifyCollinearSegmentCross(A2C1, B2C1);
		if (co_cross != Cross.NONE) return co_cross;

		Vector3 N1 = Vector3.Cross(L1, Vector3.forward).normalized;
		Plane plane = new Plane(N1, A1);
		Ray ray = new Ray(A2, L2);
		float enter = 0.0f;
		
		if (plane.Raycast(ray, out enter) == false) return Cross.NONE;
		
		itr = ray.GetPoint(enter);
		
		return Cross.INTERSECTION;
	}
	
	
}

//------------------------------------------------------------------------------
