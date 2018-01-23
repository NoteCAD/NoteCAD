using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceTool : Tool {


	IEntity p0;
	PointsDistance constraint;
	Vector3 click;

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		click = pos;
		if(constraint != null) {
			constraint = null;
			return;
		}
		var entity = sko as IEntity;
		if(entity == null) return;

		if(p0 == null && entity is LineEntity) {
			var line = entity as LineEntity;
			constraint = new PointsDistance(line.sketch, line.p0, line.p1);
			constraint.pos = pos;
			return;
		}

		if(!entity.IsPoint()) return;
		if(p0 != null) {
			constraint = new PointsDistance(DetailEditor.instance.currentSketch.GetSketch(), p0, entity);
			constraint.pos = pos;
			p0 = null;
		} else {
			p0 = entity;
		}
	}

	protected override void OnDeactivate() {
		p0 = null;
		constraint = null;
	}

	protected override void OnMouseMove(Vector3 pos, ICADObject entity) {
		if(constraint != null) {
			constraint.Drag(pos - click);
		}
		click = pos;
	}

	protected override string OnGetDescription() {
		return "click a two points or a line for constraining distance/length and then click where you want to create dimension value. You can change dimension value by double clicking it when MoveTool is active.";
	}

}
