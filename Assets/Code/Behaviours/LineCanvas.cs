using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineCanvas : DraftStroke {

	public void DrawArc(Vector3 p0, Vector3 p1, Vector3 c, Vector3 vz) {
		int subdiv = 32;

		float angle = Mathf.Acos(Vector3.Dot((p0 - c).normalized, (p1 - c).normalized)) * Mathf.Rad2Deg;
		
		if(Vector3.Dot(Vector3.Cross(p0 - c, p1 - c), vz) < 0.0) angle = -angle;
		
		var rv = p0 - c;
		var prev = rv;
		for(int i = 0; i < subdiv; i++) {
			var nrv = c + Quaternion.AngleAxis(angle / (subdiv - 1) * i, vz) * rv;
			if(i > 0) {
				DrawLine(nrv, prev);
			}
			prev = nrv;
		}
	}

}
