using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineCanvas : DraftStroke {

	public void DrawSegments(IEnumerable<Vector3> points) {
		if(points == null) return;
		Vector3 prev = Vector3.zero;
		bool first = true;
		int count = 0;
		foreach(var ep in points) {
			if(!first) {
				DrawLine(prev, ep);
			}
			first = false;
			prev = ep;
			count++;
		}
		if(count == 1) {
			DrawPoint(prev);
		}
	}

	public void DrawSegmentsAsPoints(IEnumerable<Vector3> points) {
		if(points == null) return;
		int count = 0;
		foreach(var ep in points) {
			DrawPoint(ep);
			count++;
		}
		Debug.Log("count : " + count);
	}

	public void DrawArc(Vector3 p0, Vector3 p1, Vector3 c, Vector3 vz, int subdiv = 32) {

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

	public void DrawPoint(Vector3 pos) {
		DrawLine(pos, pos);
	}	


}
