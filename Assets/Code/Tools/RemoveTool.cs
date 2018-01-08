using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoveTool : Tool {
	protected override void OnMouseDown(Vector3 pos, ISketchObject sko) {
		if(sko == null) return;
		if(!(sko is SketchObject)) return;
		(sko as SketchObject).Destroy();
	}
}
