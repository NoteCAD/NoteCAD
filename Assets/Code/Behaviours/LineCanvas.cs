using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LineCanvas : MonoBehaviour {
	LineRenderer lines;
	
	public void Clear() {
		for(int i = 0; i < transform.childCount; i++) {
			Destroy(transform.GetChild(i).gameObject);
		}
	}

	public void DrawLine(Vector3 p0, Vector3 p1) {
		var go = GameObject.Instantiate(EntityConfig.instance.lineCanvasPrefab, transform);
		var lines = go.GetComponent<LineRenderer>();
		lines.positionCount = 2;
		lines.SetPosition(lines.positionCount - 2, p0 - new Vector3(0f, 0f, 1f));
		lines.SetPosition(lines.positionCount - 1, p1 - new Vector3(0f, 0f, 1f));
	}

	public void DrawArc(Vector3 p0, Vector3 p1, Vector3 c, Vector3 vz) {
		int subdiv = 32;

		var go = GameObject.Instantiate(EntityConfig.instance.lineCanvasPrefab, transform);
		var lines = go.GetComponent<LineRenderer>();
		lines.positionCount = subdiv;

		float angle = Mathf.Acos(Vector3.Dot((p0 - c).normalized, (p1 - c).normalized)) * Mathf.Rad2Deg;
		
		if(Vector3.Dot(Vector3.Cross(p0 - c, p1 - c), vz) < 0.0) angle = -angle;
		
		var rv = p0 - c;
		
		for(int i = 0; i < subdiv; i++) {
			var nrv = Quaternion.AngleAxis(angle / (subdiv - 1) * i, vz) * rv;
			lines.SetPosition(i, nrv + c);
		}
	}
	
}
