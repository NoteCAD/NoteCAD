using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleTool : Tool {

	CircleEntity current;
	bool canCreate = true;

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {

		if(current != null) {
			if(!canCreate) return;
			current.c.isSelectable = true;
			current.isSelectable = true;
			current = null;
			return;
		}

		current = new CircleEntity(DetailEditor.instance.currentSketch.GetSketch());
		current.center.pos = pos;
		AutoConstrainCoincident(current.center, sko as Entity);

		current.isSelectable = false;
		current.c.isSelectable = false;
	}

	protected override void OnMouseMove(Vector3 pos, ICADObject entity) {
		if(current != null) {
			current.radius.value = (current.center.pos - pos).magnitude;
			var itr = new Vector3();
			canCreate = true;//!current.sketch.IsCrossed(current, ref itr);
			current.isError = !canCreate;
		} else {
			canCreate = true;
		}
	}

	protected override void OnDeactivate() {
		if(current != null) {
			current.Destroy();
			current = null;
		}
		canCreate = true;
	}

}
