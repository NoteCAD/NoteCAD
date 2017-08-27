using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineBehaviour : EntityBehaviour {

	void Update() {
		var line = entity as LineEntity;
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
}
