using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LengthTool : Tool {

	ValueConstraint constraint;
	//Vector3 click;

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		/*
		if(constraint != null) {
			MoveTool.instance.EditConstraintValue(constraint);
			constraint = null;
			return;
		}*/
		var entity = sko as IEntity;
		if(entity == null) return;

		if(entity.Length() != null) {
			editor.PushUndo();
			constraint = new Length(DetailEditor.instance.currentSketch.GetSketch(), entity);
			//constraint.pos = WorldPlanePos;
			//click = WorldPlanePos;
			MoveTool.instance.EditConstraintValue(constraint, pushUndo:false);
			constraint = null;
		}
	}

	protected override void OnDeactivate() {
		constraint = null;
	}

	protected override void OnMouseMove(Vector3 pos, ICADObject entity) {
		/*
		if(constraint != null) {
			constraint.Drag(WorldPlanePos - click);
		}
		click = WorldPlanePos;
		*/
	}

	protected override string OnGetDescription() {
		return "click a entity to constrain length and then click where you want to create dimension value. You can change dimension value by double clicking it when MoveTool is active.";
	}

}
