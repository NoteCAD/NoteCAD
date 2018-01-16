using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointTool : Tool {

	protected override void OnMouseDown(Vector3 pos, ICADObject entity) {
		var p = new PointEntity(DetailEditor.instance.currentSketch.GetSketch());
		p.SetPosition(pos);
	}

}
