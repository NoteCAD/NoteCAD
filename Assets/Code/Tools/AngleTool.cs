using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AngleTool : Tool {

	IEntity l0;
	AngleConstraint constraint;
	Vector3 click;

	static IEntity[] GetPoints(LineEntity l0, LineEntity l1) {
		if(l0.p0.IsCoincidentWith(l1.p0)) return new IEntity[4] { l0.p1, l0.p0, l1.p0, l1.p1 };
		if(l0.p0.IsCoincidentWith(l1.p1)) return new IEntity[4] { l0.p1, l0.p0, l1.p1, l1.p0 };
		if(l0.p1.IsCoincidentWith(l1.p0)) return new IEntity[4] { l0.p0, l0.p1, l1.p0, l1.p1 };
		if(l0.p1.IsCoincidentWith(l1.p1)) return new IEntity[4] { l0.p0, l0.p1, l1.p1, l1.p0 };
		return new IEntity[4] { l0.p0, l0.p1, l1.p0, l1.p1 };
	}

	public static AngleConstraint CreateConstraint(IEntity l0, IEntity l1) {
		if(l0 is LineEntity && l1 is LineEntity) {
			var pts = GetPoints(l0 as LineEntity, l1 as LineEntity);
			return new AngleConstraint(DetailEditor.instance.currentSketch.GetSketch(), pts);
		}
		return new AngleConstraint(DetailEditor.instance.currentSketch.GetSketch(), l0, l1);
	}

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		click = WorldPlanePos;
		if(constraint != null) {
			MoveTool.instance.EditConstraintValue(constraint, pushUndo:false);
			constraint = null;
			return;
		}
		var entity = sko as IEntity;
		if(entity == null) return;

		if(l0 == null && entity.type == IEntityType.Arc) {
			editor.PushUndo();
			constraint = new AngleConstraint(DetailEditor.instance.currentSketch.GetSketch(), entity);
			constraint.pos = WorldPlanePos;
		}

		if(entity.type != IEntityType.Line) return;
		if(l0 != null) {
			editor.PushUndo();
			constraint = CreateConstraint(l0, entity);
			constraint.pos = WorldPlanePos;
			l0 = null;
		} else {
			l0 = entity;
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
