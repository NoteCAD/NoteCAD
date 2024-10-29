using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleTool : Tool {

	CircleEntity current;
	Diameter dimension;
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
			dimension?.Destroy();
			dimension = null;
			return;
		}

		if(DetailEditor.instance.currentSketch == null) return;
		editor.PushUndo();
		current = SpawnEntity(new CircleEntity(DetailEditor.instance.currentSketch.GetSketch()));
		current.center.pos = pos;
		if (editor.GetDetail().settings.drawingDimensions) {
			dimension = new Diameter(current.sketch, current);
			dimension.labelX = 0.0001f;
			dimension.labelY = 0.0001f;
			dimension.enabled = false;
		}
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
		current?.Destroy();
		current = null;
		dimension?.Destroy();
		dimension = null;
		canCreate = true;
	}

	protected override string OnGetDescription() {
		return "click where you want to create the center point and then you can control the radius of the circle by moving mouse and fix it by clicking left button";
	}

}
