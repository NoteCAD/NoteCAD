using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointTool : Tool {

	protected override void OnMouseDown(Vector3 pos, Entity entity) {
		var p = Sketch.instance.CreatePoint();
		p.SetPosition(pos);
	}

}
