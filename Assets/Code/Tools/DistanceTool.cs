using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceTool : Tool {


	PointEntity p0;
	PointsDistance constraint;
	Vector3 click;

	protected override void OnMouseDown(Vector3 pos, SketchObject sko) {
		click = pos;
		if(constraint != null) {
			constraint = null;
			return;
		}
		var entity = sko as Entity;
		if(entity == null) return;

		if(p0 == null && entity is LineEntity) {
			var line = entity as LineEntity;
			constraint = new PointsDistance(line.sketch, line.p0, line.p1);
			constraint.pos = pos;
			return;
		}

		if(!(entity is PointEntity)) return;
		var p = entity as PointEntity;
		if(p0 != null) {
			constraint = new PointsDistance(entity.sketch, p0, p);
			constraint.pos = pos;
			p0 = null;
		} else {
			p0 = p;
		}
	}

	protected override void OnDeactivate() {
		p0 = null;
		constraint = null;
	}

	protected override void OnMouseMove(Vector3 pos, SketchObject entity) {
		if(constraint != null) {
			constraint.Drag(pos - click);
		}
		click = pos;
	}

}
