using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ConstraintExp {
	public static Exp angle2d(ExpVector d0, ExpVector d1, bool angle360 = false) {
		Exp nu = d1.x * d0.x + d1.y * d0.y;
		Exp nv = d0.x * d1.y - d0.y * d1.x;
		if(angle360) return Math.PI - Exp.Atan2(nv, -nu);
		return Exp.Atan2(nv, nu);
	}
	
	public static Exp angle3d(ExpVector d0, ExpVector d1) {
		return Exp.Atan2(ExpVector.Cross(d0, d1).Magnitude(), ExpVector.Dot(d0, d1));
	}

}
