using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ConstraintExp {

	public static ExpVector rotateDir2d(ExpVector rv, Exp angle) {
		var cos = Exp.Cos(angle);
		var sin = Exp.Sin(angle);
		return new ExpVector(
			cos * rv.x - sin * rv.y, 
			sin * rv.x + cos * rv.y, 
			rv.z
		);
	}

	public static Exp angle2d(ExpVector d0, ExpVector d1, bool angle360 = false) {
		Exp nu = d1.x * d0.x + d1.y * d0.y;
		Exp nv = d0.x * d1.y - d0.y * d1.x;
		if(angle360) return Math.PI - Exp.Atan2(nv, -nu);
		return Exp.Atan2(nv, nu);
	}
	
	public static Exp angle3d(ExpVector d0, ExpVector d1) {
		return Exp.Atan2(ExpVector.Cross(d0, d1).Magnitude(), ExpVector.Dot(d0, d1));
	}

	public static double angle2d(Vector3 d0, Vector3 d1, bool angle360 = false) {
		var nu = d1.x * d0.x + d1.y * d0.y;
		var nv = d0.x * d1.y - d0.y * d1.x;
		if(angle360) return Math.PI - Math.Atan2(nv, -nu);
		return Math.Atan2(nv, nu);
	}
	
	public static double angle3d(Vector3 d0, Vector3 d1) {
		return Math.Atan2(Vector3.Cross(d0, d1).magnitude, Vector3.Dot(d0, d1));
	}

	public static Exp pointLineDistance(ExpVector p, ExpVector p0, ExpVector p1, bool is3d) {
		if(is3d) {
			var d = p0 - p1;
			return ExpVector.Cross(d, p0 - p).Magnitude() / d.Magnitude();
		}
		return ((p0.y - p1.y) * p.x + (p1.x - p0.x) * p.y + p0.x * p1.y - p1.x * p0.y) / Exp.Sqrt(Exp.Sqr(p1.x - p0.x) + Exp.Sqr(p1.y - p0.y));
	}

}
