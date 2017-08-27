using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MoveTool : Tool {

	Entity current;
	Vector3 click;

	Exp dragX;
	Exp dragY;
	Param dragXP = new Param("dragX");
	Param dragYP = new Param("dragY");

	protected override void OnMouseDown(Vector3 pos, Entity entity) {
		OnDeactivate();
		if(entity == null) return;
		current = entity;
		click = pos;
		int count = entity.points.Count();
		if(count == 0) return;

		if(count == 1) {
			var pt = entity.points.First();
			dragXP.value = pt.x.value;
			dragYP.value = pt.y.value;
			dragX = new Exp(pt.x).Drag(dragXP);
			dragY = new Exp(pt.y).Drag(dragYP);
			Sketch.instance.SetDrag(dragX, dragY);
		}
	}

	protected override void OnDeactivate() {
		current = null;
		if(dragX != null) {
			Sketch.instance.SetDrag(null, null);
		}
		dragX = null;
		dragY = null;
	}

	protected override void OnMouseMove(Vector3 pos, Entity entity) {
		if(current == null) return;
		var delta = pos - click;
		if(dragX != null) {
			dragXP.value += delta.x;
			dragYP.value += delta.y;
		} else {
			foreach(var p in current.points) {
				p.x.value += delta.x;
				p.y.value += delta.y;
			}
		}
		click = pos;
	}

	protected override void OnMouseUp(Vector3 pos, Entity entity) {
		OnDeactivate();
	}



}
