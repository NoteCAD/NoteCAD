using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointTool : Tool {

	protected override void OnMouseDown(Vector3 pos, SketchObject entity) {
		var p = new PointEntity(Sketch.instance);
		p.SetPosition(pos);
	}

}
