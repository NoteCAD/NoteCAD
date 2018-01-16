using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class MoveTool : Tool {

	ICADObject current;
	Vector3 click;
	List<Exp> drag = new List<Exp>();
	Param dragXP = new Param("dragX");
	Param dragYP = new Param("dragY");
	Param dragZP = new Param("dragZ");
	ValueConstraint valueConstraint;
	public InputField input;
	bool canMove = true;

	private void Start() {
		input.onEndEdit.AddListener(OnEndEdit);
	}

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		ClearDrag();
		if(valueConstraint != null) return;
		if(sko == null) return;
		var entity = sko as IEntity;
		current = sko;
		click = pos;
		int count = 0;
		if(entity != null) count = entity.points.Count();
		if(count == 0) return;
		dragXP.value = 0;
		dragYP.value = 0;
		dragZP.value = 0;
		foreach(var ptExp in entity.points) {
			var dragX = ptExp.x.Drag(dragXP.exp + ptExp.x.Eval());
			var dragY = ptExp.y.Drag(dragYP.exp + ptExp.y.Eval());
			var dragZ = ptExp.z.Drag(dragZP.exp + ptExp.z.Eval());
			drag.Add(dragX);
			drag.Add(dragY);
			drag.Add(dragZ);
			DetailEditor.instance.AddDrag(dragX);
			DetailEditor.instance.AddDrag(dragY);
			DetailEditor.instance.AddDrag(dragZ);
		}
	}

	void ClearDrag() {
		current = null;
		foreach(var d in drag) {
			DetailEditor.instance.RemoveDrag(d);
		}
		drag.Clear();
		canMove = true;
	}

	protected override void OnDeactivate() {
		ClearDrag();
		valueConstraint = null;
		input.gameObject.SetActive(false);
	}

	protected override void OnMouseMove(Vector3 pos, ICADObject sko) {
		if(current == null) return;
		var delta = pos - click;
		if(drag.Count > 0) {
			dragXP.value += delta.x;
			dragYP.value += delta.y;
			dragZP.value += delta.z;
		} else if(current is Constraint) {
			(current as Constraint).Drag(delta);
		}
		click = pos;
	}

	protected override void OnMouseUp(Vector3 pos, ICADObject sko) {
		ClearDrag();
	}
	
	protected override void OnMouseDoubleClick(Vector3 pos, ICADObject sko) {
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
