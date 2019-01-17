using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using System;

public class MoveTool : Tool {

	ICADObject current;
	Vector3 click;
	Vector3 worldClick;
	Vector3 firstClickCenter;
	double deltaR;
	List<Exp> drag = new List<Exp>();
	Param dragXP = new Param("dragX", reduceable: false);
	Param dragYP = new Param("dragY", reduceable: false);
	Param dragZP = new Param("dragZ", reduceable: false);
	ValueConstraint valueConstraint;
	bool shouldPushUndo = true;
	public InputField input;
	public static MoveTool instance;
	//bool canMove = true;

	protected override void OnStart() {
		input.onEndEdit.AddListener(OnEndEdit);
		instance = this;
	}

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		ClearDrag();
		if(!Input.GetKey(KeyCode.LeftShift)) DetailEditor.instance.selection.Clear();
		if(valueConstraint != null) return;
		if(sko == null) return;
		DetailEditor.instance.selection.Add(sko.id);
		var entity = sko as IEntity;
		current = sko;
		click = pos;
		worldClick = WorldPlanePos;
		int count = 0;
		if(entity != null) count = entity.points.Count();
		if(count == 0) return;
		editor.PushUndo();

		DetailEditor.instance.suppressCombine = true;
		DetailEditor.instance.suppressHovering = true;

		dragXP.value = 0;
		dragYP.value = 0;
		dragZP.value = 0;
		if(entity.IsCircular()) {
			var dragR = entity.Radius().Drag(dragXP.exp);
			DetailEditor.instance.AddDrag(dragR);
			drag.Add(dragR);
			firstClickCenter = entity.CenterInPlane(null).Eval();
			deltaR = entity.Radius().Eval() - (firstClickCenter - worldClick).magnitude;
		} else {
			foreach(var ptExp in entity.points) {
				var dragX = ptExp.x.Drag(dragXP.exp + ptExp.x.Eval());
				var dragY = ptExp.y.Drag(dragYP.exp + ptExp.y.Eval());
				var dragZ = ptExp.z.Drag(dragZP.exp + ptExp.z.Eval());
				drag.Add(dragX);
				drag.Add(dragY);
				drag.Add(dragZ);
				//Debug.Log("x: " + dragX);
				//Debug.Log("y: " + dragY);
				//Debug.Log("z: " + dragZ);
				DetailEditor.instance.AddDrag(dragX);
				DetailEditor.instance.AddDrag(dragY);
				DetailEditor.instance.AddDrag(dragZ);
			}
		}
	}

	void ClearDrag() {
		current = null;
		foreach(var d in drag) {
			DetailEditor.instance.RemoveDrag(d);
			DetailEditor.instance.suppressCombine = false;
			DetailEditor.instance.suppressHovering = false;
		}
		drag.Clear();
		//canMove = true;
	}

	protected override void OnDeactivate() {
		ClearDrag();
		valueConstraint = null;
		input.gameObject.SetActive(false);
	}

	protected override void OnMouseMove(Vector3 pos, ICADObject sko) {
		if(current == null) return;
		var delta = pos - click;
		var worldDelta = WorldPlanePos - worldClick;
		
		if(drag.Count > 0) {
			if(current is IEntity && (current as IEntity).IsCircular()) {
				var circle = current as IEntity;
				dragXP.value = (firstClickCenter - WorldPlanePos).magnitude + deltaR;
			} else {
				dragXP.value += delta.x;
				dragYP.value += delta.y;
				dragZP.value += delta.z;
			}
		} else if(current is Constraint) {
			(current as Constraint).Drag(worldDelta);
		}
		click = pos;
		worldClick = WorldPlanePos;
	}

	protected override void OnMouseUp(Vector3 pos, ICADObject sko) {
		ClearDrag();
	}
	
	public void EditConstraintValue(ValueConstraint constraint, bool pushUndo = true) {
		valueConstraint = constraint;
		this.shouldPushUndo = pushUndo;
		input.gameObject.SetActive(true);
		input.text = Math.Abs(valueConstraint.GetValue()).ToStr();
		input.Select();
		UpdateInputPosition();
	}

	public bool IsConstraintEditing(ValueConstraint constraint) {
		return valueConstraint == constraint;
	}

	protected override void OnMouseDoubleClick(Vector3 pos, ICADObject sko) {
		if(sko is ValueConstraint) {
			EditConstraintValue(sko as ValueConstraint);
		}
	}

	void UpdateInputPosition() {
		if(valueConstraint != null) {
			input.gameObject.transform.position = Camera.main.WorldToScreenPoint(valueConstraint.pos);
		}
	}

	private void Update() {
		UpdateInputPosition();
	}

	void OnEndEdit(string value) {
		if(valueConstraint == null) return;
		var sign = Math.Sign(valueConstraint.GetValue());
		if(sign == 0) sign = 1;
		if(shouldPushUndo) editor.PushUndo();
		valueConstraint.SetValue(sign * value.ToDouble());
		valueConstraint = null;
		input.gameObject.SetActive(false);
	}

	protected override string OnGetDescription() {
		return "hover over an entity, hold down left mouse button to move it. Double click on any dimension to edit. Click on Help icon for additional info.";
	}

}
