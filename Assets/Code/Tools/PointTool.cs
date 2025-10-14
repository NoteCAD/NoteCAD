using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NoteCAD;

public class PointTool : Tool {

	PointTool() {
		enableHoverFilter = true;
	}

	protected override bool OnTryHover(Constraint c) {
		return false;
	}

	protected override bool OnTryHover(IEntity e) {
		return e.type != IEntityType.Point && CanConstrainCoincident(e);
	}

	protected override void OnMouseDown(Vector3 pos, ICADObject entity) {
		editor.PushUndo();
		var p = new PointEntity(DetailEditor.instance.currentSketch.GetSketch());
		p.SetPosition(pos);
		if(entity is IEntity) {
			AutoConstrainCoincident(p, entity as IEntity);
		}
	}

}
