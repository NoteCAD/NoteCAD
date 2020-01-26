using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquationTool : Tool {

	ValueConstraint constraint;

	EquationTool() {
		enableHoverFilter = true;
	}

	protected override bool OnTryHover(Constraint c) {
		return false;
	}

	protected override bool OnTryHover(IEntity e) {
		return false;
	}

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		editor.PushUndo();
		constraint = new EquationConstraint(DetailEditor.instance.currentSketch.GetSketch());
		constraint.pos = pos;
	}

	protected override void OnActivate() {
		editor.PushUndo();
		constraint = new EquationConstraint(DetailEditor.instance.currentSketch.GetSketch());
		constraint.pos = MousePos;
	}

	protected override void OnDeactivate() {
		if(constraint != null) {
			constraint.Destroy();
			editor.PopUndo();
		}
		constraint = null;
	}

	protected override void OnMouseMove(Vector3 pos, ICADObject entity) {
		constraint.pos = pos;
	}

	protected override string OnGetDescription() {
		return "click a entity to constrain length and then click where you want to create dimension value. You can change dimension value by double clicking it when MoveTool is active.";
	}

}
