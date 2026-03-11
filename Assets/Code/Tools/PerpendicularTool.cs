using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NoteCAD;

public class PerpendicularTool : Tool {

	IEntity l0;

	PerpendicularTool() {
		enableHoverFilter = true;
	}

	protected override bool OnTryHover(Constraint c) {
		return false;
	}

	protected override bool OnTryHover(IEntity e) {
		return e.type != IEntityType.Point && CanConstrainCoincident(e) && e.TangentAt(0.0) != null && !e.IsSameAs(l0);
	}

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		var entity = sko as IEntity;
		if(entity == null) return;

		if(entity.type != IEntityType.Line && entity.TangentAt(0.0) == null) return;
		if(l0 != null) {
			editor.PushUndo();
			if(entity.type == IEntityType.Line && l0.type == IEntityType.Line) {
				new Perpendicular(DetailEditor.instance.currentSketch.GetSketch(), l0, entity);
			} else if(entity.TangentAt(0.0) != null && l0.TangentAt(0.0) != null) {
				new Tangent(DetailEditor.instance.currentSketch.GetSketch(), l0, entity, perpendicular:true);
			}
			l0 = null;
		} else {
			l0 = entity;
		}
	}

	protected override void OnDeactivate() {
		l0 = null;
	}

	protected override string OnGetDescription() {
		return "hover and click two different entities.";
	}

}
