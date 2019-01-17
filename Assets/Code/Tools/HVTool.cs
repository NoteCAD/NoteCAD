using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HVTool : Tool {

	IEntity p0;
	HVConstraint constraint;
	//Vector3 click;
	public bool vertical = false;

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		//click = pos;
		var entity = sko as IEntity;
		if(entity == null) return;

		if(p0 == null && entity.type == IEntityType.Line) {
			editor.PushUndo();
			constraint = new HVConstraint(DetailEditor.instance.currentSketch.GetSketch(), entity);
			constraint.orientation = vertical ? HVOrientation.OX : HVOrientation.OY;
			return;
		}

		if(entity.type != IEntityType.Point) return;
		if(p0 != null) {
			editor.PushUndo();
			constraint = new HVConstraint(DetailEditor.instance.currentSketch.GetSketch(), p0, entity);
			constraint.orientation = vertical ? HVOrientation.OX : HVOrientation.OY;
			p0 = null;
			constraint = null;
		} else {
			p0 = entity;
		}
	}

	protected override void OnDeactivate() {
		p0 = null;
		constraint = null;
	}

	protected override string OnGetDescription() {
		return "click a two points or a line for constraining horizontal/vertical.";
	}

}
