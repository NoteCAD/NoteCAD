using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointBehaviour : MonoBehaviour {

	public PointEntity point;
	public Vector3 oldPos;
	Exp dragX;
	Exp dragY;
	Param dragXP = new Param("dragX");
	Param dragYP = new Param("dragY");
	Color color;

	void Start() {
		color = GetComponent<Renderer>().material.color;
	}

	void Update() {
		transform.position = point.GetPosition();
	}

	public void OnMouseDown() {
		oldPos = Tool.MousePos;
		dragXP.value = oldPos.x;
		dragYP.value = oldPos.y;
		dragX = new Exp(point.x).Drag(dragXP);
		dragY = new Exp(point.y).Drag(dragYP);
		Sketch.instance.SetDrag(dragX, dragY);
	}

	public void OnMouseEnter() {
		Sketch.instance.hovered = point;
	}

	public void OnMouseExit() {
		if(Sketch.instance.hovered != point) return;
		Sketch.instance.hovered = null;
	}

	public void OnMouseUp() {
		Sketch.instance.SetDrag(null, null);
	}

	public void OnMouseDrag() {
		var curPos = Tool.MousePos;
		//point.SetPosition(point.GetPosition() + curPos - oldPos);
		oldPos = curPos;
		dragXP.value = oldPos.x;
		dragYP.value = oldPos.y;
	}

}
