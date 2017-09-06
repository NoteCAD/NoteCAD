using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MoveTool : Tool {

	SketchObject current;
	Vector3 click;

	Exp dragX;
	Exp dragY;
	Param dragXP = new Param("dragX");
	Param dragYP = new Param("dragY");

	protected override void OnMouseDown(Vector3 pos, SketchObject sko) {
		OnDeactivate();
		if(sko == null) return;
		var entity = sko as Entity;
		current = sko;
		click = pos;
		int count = 0;
		if(entity != null) count = entity.points.Count();
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

	protected override void OnMouseMove(Vector3 pos, SketchObject entity) {
		if(current == null) return;
		var delta = pos - click;
		if(dragX != null) {
			dragXP.value += delta.x;
			dragYP.value += delta.y;
		} else {
			current.Drag(delta);
		}
		click = pos;
	}

	protected override void OnMouseUp(Vector3 pos, SketchObject entity) {
		OnDeactivate();
	}



}
