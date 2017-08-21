using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointBehaviour : MonoBehaviour {

	public PointEntity point;
	public Vector3 oldPos;

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
	}

	public void OnMouseDrag() {
		var curPos = GetMousePos();
		point.SetPosition(point.GetPosition() + curPos - oldPos);
		oldPos = curPos;
	}

}
