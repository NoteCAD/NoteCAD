using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineBehaviour : MonoBehaviour {

	public LineEntity line;
	public Vector3 oldPos;

	void Update() {
		var p0 = line.p0.GetPosition();
		var p1 = line.p1.GetPosition();
		var dir = p1 - p0;
		if(dir.magnitude > Mathf.Epsilon) {
			transform.forward = dir.normalized;
			transform.localScale = new Vector3(1f, 1f, dir.magnitude);
		} else {
			transform.localScale = Vector3.zero;
		}
		transform.position = (p1 + p0) * 0.5f;
	}

	public void OnMouseDown() {
		oldPos = Tool.MousePos;
	}

	public void OnMouseDrag() {
		var curPos = Tool.MousePos;
		var delta = curPos - oldPos;
		line.p0.SetPosition(line.p0.GetPosition() + delta);
		line.p1.SetPosition(line.p1.GetPosition() + delta);
		oldPos = curPos;
	}


}
