using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AngleTool : Tool {

	LineEntity l0;
	AngleConstraint constraint;
	Vector3 click;

	protected override void OnMouseDown(Vector3 pos, ISketchObject sko) {
		click = pos;
		if(constraint != null) {
			constraint = null;
			return;
		}
		var entity = sko as Entity;
		if(entity == null) return;
	

		if(!(entity is LineEntity)) return;
		var l = entity as LineEntity;
		if(l0 != null) {
			constraint = new AngleConstraint(entity.sketch, l0, l);
			constraint.pos = pos;
			l0 = null;
		} else {
			l0 = l;
		}
	}

	protected override void OnDeactivate() {
		l0 = null;
		constraint = null;
	}

	protected override void OnMouseMove(Vector3 pos, ISketchObject entity) {
		if(constraint != null) {
			constraint.Drag(pos - click);
		}
		click = pos;
	}

}
