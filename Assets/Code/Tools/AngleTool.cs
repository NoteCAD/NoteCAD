using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AngleTool : Tool {

	LineEntity l0;
	AngleConstraint constraint;
	Vector3 click;

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		click = WorldPlanePos;
		if(constraint != null) {
			MoveTool.instance.EditConstraintValue(constraint);
			constraint = null;
			return;
		}
		var entity = sko as Entity;
		if(entity == null) return;
	

		if(!(entity is LineEntity)) return;
		var l = entity as LineEntity;
		if(l0 != null) {
			constraint = new AngleConstraint(entity.sketch, l0, l);
			constraint.pos = WorldPlanePos;
			l0 = null;
		} else {
			l0 = l;
		}
	}

	protected override void OnDeactivate() {
		l0 = null;
		constraint = null;
	}

	protected override void OnMouseMove(Vector3 pos, ICADObject entity) {
		if(constraint != null) {
			constraint.Drag(WorldPlanePos - click);
		}
		click = WorldPlanePos;
	}

	protected override string OnGetDescription() {
		return "hover and click two different lines. You can change dimension value by double clicking it when MoveTool is active.";
	}

}
