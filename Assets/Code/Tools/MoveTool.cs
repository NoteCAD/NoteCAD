using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class MoveTool : Tool {

	SketchObject current;
	Vector3 click;

	Exp dragX;
	Exp dragY;
	Param dragXP = new Param("dragX");
	Param dragYP = new Param("dragY");
	ValueConstraint valueConstraint;
	public InputField input;
	bool canMove = true;

	private void Start() {
		input.onEndEdit.AddListener(OnEndEdit);
	}

	protected override void OnMouseDown(Vector3 pos, SketchObject sko) {
		ClearDrag();
		if(valueConstraint != null) return;
		if(sko == null) return;
		var entity = sko as Entity;
		current = sko;
		click = pos;
		int count = 0;
		if(entity != null) count = entity.points.Count();
		if(count == 0) return;

		if(count == 1) {
			var pt = entity.points.First();
			dragXP.value = pt.x.value;
			dragYP.value = pt.y.value;
			dragX = new Exp(pt.x).Drag(dragXP);
			dragY = new Exp(pt.y).Drag(dragYP);
			Sketch.instance.SetDrag(dragX, dragY);
		}
	}

	void ClearDrag() {
		current = null;
		if(dragX != null) {
			Sketch.instance.SetDrag(null, null);
		}
		dragX = null;
		dragY = null;
		canMove = true;
	}

	protected override void OnDeactivate() {
		ClearDrag();
		valueConstraint = null;
		input.gameObject.SetActive(false);
	}

	protected override void OnMouseMove(Vector3 pos, SketchObject sko) {
		if(current == null) return;
		var delta = pos - click;
		if(dragX != null) {
			dragXP.value += delta.x;
			dragYP.value += delta.y;
		} else {
			current.Drag(delta);
		}
		click = pos;
	}

	protected override void OnMouseUp(Vector3 pos, SketchObject sko) {
		ClearDrag();
	}
	
	protected override void OnMouseDoubleClick(Vector3 pos, SketchObject sko) {
		if(sko is ValueConstraint) {
			valueConstraint = sko as ValueConstraint;
			input.gameObject.SetActive(true);
			input.text = valueConstraint.GetValue().ToString();
			input.Select();
			UpdateInputPosition();
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
		valueConstraint.SetValue(double.Parse(value));
		valueConstraint = null;
		input.gameObject.SetActive(false);
	}

}
