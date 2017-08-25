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

	void Update () {
		transform.position = point.GetPosition();
	}


	public static Vector3 GetMousePos() {
		var plane = new Plane(Camera.main.transform.forward, Vector3.zero);
		var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		float cast;
		plane.Raycast(ray, out cast);
		return ray.GetPoint(cast);
	}

	public void OnMouseDown() {
		oldPos = GetMousePos();
		dragXP.value = oldPos.x;
		dragYP.value = oldPos.y;
		dragX = new Exp(point.x).Drag(dragXP);
		dragY = new Exp(point.y).Drag(dragYP);
		Sketch.instance.SetDrag(dragX, dragY);
	}

	public void OnMouseUp() {
		Sketch.instance.SetDrag(null, null);
	}

	public void OnMouseDrag() {
		var curPos = GetMousePos();
		//point.SetPosition(point.GetPosition() + curPos - oldPos);
		oldPos = curPos;
		dragXP.value = oldPos.x;
		dragYP.value = oldPos.y;
	}

}
