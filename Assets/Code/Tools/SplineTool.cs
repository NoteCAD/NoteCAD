using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplineTool : Tool {

	SplineEntity prev;
	SplineEntity current;
	bool canCreate = true;

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {

		if(current != null) {
			if(!canCreate) return;
			current.p[2].pos = pos;
			foreach(var p in current.p) p.isSelectable = true;
			current.isSelectable = true;
			if(prev != null) {
				new Tangent(current.sketch, current, prev);
			}
			if(AutoConstrainCoincident(current.p[3], sko as IEntity)) {
				current = null;
				StopTool();
				return;
			}
		}
		if(DetailEditor.instance.currentSketch == null) return;
		editor.PushUndo();
		var newEntity = new SplineEntity(DetailEditor.instance.currentSketch.GetSketch());
		foreach(var p in newEntity.p) p.pos = pos;
		if(current == null) {
			AutoConstrainCoincident(newEntity.p[0], sko as IEntity);
		} else {
			new PointsCoincident(current.sketch, current.p[3], newEntity.p[0]);
		}

		prev = current;
		current = newEntity;
		foreach(var p in current.p) p.isSelectable = false;
		current.isSelectable = false;
	}
	protected override void OnMouseMove(Vector3 pos, ICADObject entity) {
		if(current != null) {
			current.p[3].pos = pos;
			if(prev == null) {
				current.p[1].pos = (current.p[0].pos + current.p[3].pos) * 0.5f;
				current.p[2].pos = (current.p[0].pos + current.p[3].pos) * 0.5f;
			} else {
				var dir0 = current.p[3].pos - current.p[0].pos;
				var dir1 = prev.p[3].pos - prev.p[0].pos;
				var dir = (dir0.normalized + dir1.normalized).normalized;
				current.p[1].pos = current.p[0].pos + dir * dir0.magnitude / 3f;
				current.p[2].pos = current.p[3].pos - Vector3.Reflect(dir * dir0.magnitude / 3f, new Vector3(dir0.y, -dir0.x, 0f).normalized);
				prev.p[2].pos = prev.p[3].pos - dir * dir1.magnitude / 3f;
			}
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
		prev = null;
	}

	protected override string OnGetDescription() {
		return "click where you want to create the beginning and the ending points of the spline";
	}

}
