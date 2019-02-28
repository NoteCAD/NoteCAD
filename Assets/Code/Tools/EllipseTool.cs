using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EllipseTool : Tool {

	EllipseEntity current;
	bool canCreate = true;

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {

		if(current != null) {
			if(!canCreate) return;
			current.c.isSelectable = true;
			current.isSelectable = true;
			for(double t = 0.0; t < 1.0; t += 0.25) {
				var p = new PointEntity(editor.currentSketch.GetSketch());
				current.AddChild(p);
				var pc = new PointOn(editor.currentSketch.GetSketch(), p, current);
				pc.reference = false;
				pc.SetValue(t);
				pc.isVisible = false;
				p.pos = current.PointOn(t).Eval();
			}
			current = null;
			return;
		}

		if(DetailEditor.instance.currentSketch == null) return;
		editor.PushUndo();
		current = new EllipseEntity(DetailEditor.instance.currentSketch.GetSketch());
		current.center.pos = pos;
		AutoConstrainCoincident(current.center, sko as IEntity);

		current.isSelectable = false;
		current.c.isSelectable = false;
	}

	protected override void OnMouseMove(Vector3 pos, ICADObject entity) {
		if(current != null) {
			var delta = current.center.pos - pos;
			current.r0.value = Mathf.Abs(delta.x);
			current.r1.value = Mathf.Abs(delta.y);
			canCreate = true;
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
		return "click where you want to create the center point and then you can control the radius of the ellipse by moving mouse and fix it by clicking left button";
	}

}
