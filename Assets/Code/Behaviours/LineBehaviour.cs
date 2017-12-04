using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineBehaviour : EntityBehaviour {

	public void Update() {
		var line = entity as ISegmentaryEntity;
		var p0 = line.begin.pos;
		var p1 = line.end.pos;
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
