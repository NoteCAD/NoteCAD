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
		lines.SetPosition(lines.positionCount - 2, p0);
		lines.SetPosition(lines.positionCount - 1, p1);
	}
	
}
