using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleTool : Tool {

	CircleEntity current;
	bool canCreate = true;

	CircleTool() {
		enableHoverFilter = true;
	}

	protected override bool OnTryHover(Constraint c) {
		return false;
	}

	protected override bool OnTryHover(IEntity e) {
		if(current == null) return CanConstrainCoincident(e);
		return false;
	}

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {

		if(current != null) {
			if(!canCreate) return;
			current.c.isSelectable = true;
			current.isSelectable = true;
			current = null;
			return;
		}

		if(DetailEditor.instance.currentSketch == null) return;
		editor.PushUndo();
		current = SpawnEntity(new CircleEntity(DetailEditor.instance.currentSketch.GetSketch()));
		current.center.pos = pos;
		AutoConstrainCoincident(current.center, sko as IEntity);

		current.isSelectable = false;
		current.c.isSelectable = false;
	}

	protected override void OnMouseMove(Vector3 pos, ICADObject entity) {
		if(current != null) {
			current.r.value = (current.center.pos - pos).magnitude;
			//var itr = new Vector3();
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

	protected override string OnGetDescription() {
		return "click where you want to create the center point and then you can control the radius of the circle by moving mouse and fix it by clicking left button";
	}

}
