using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class MoveTool : Tool {

	SketchObject current;
	Vector3 click;
	List<Exp> drag = new List<Exp>();
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
		if(DetailEditor.instance.currentSketch == null) return;
		if(valueConstraint != null) return;
		if(sko == null) return;
		var entity = sko as Entity;
		current = sko;
		click = pos;
		int count = 0;
		if(entity != null) count = entity.points.Count();
		if(count == 0) return;
		dragXP.value = 0;
		dragYP.value = 0;
		foreach(var pt in entity.points) {
			var dragX = new Exp(pt.x).Drag(dragXP.exp + pt.x.value);
			var dragY = new Exp(pt.y).Drag(dragYP.exp + pt.y.value);
			drag.Add(dragX);
			drag.Add(dragY);
			DetailEditor.instance.currentSketch.AddDrag(dragX);
			DetailEditor.instance.currentSketch.AddDrag(dragY);
		}
	}

	void ClearDrag() {
		current = null;
		foreach(var d in drag) {
			DetailEditor.instance.currentSketch.RemoveDrag(d);
		}
		drag.Clear();
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
		if(drag.Count > 0) {
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
