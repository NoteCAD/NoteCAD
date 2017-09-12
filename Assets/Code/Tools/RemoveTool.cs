using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoveTool : Tool {
	protected override void OnMouseDown(Vector3 pos, SketchObject sko) {
		if(sko == null) return;
		sko.Destroy();
	}
}
