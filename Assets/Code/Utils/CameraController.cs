using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

	Camera camera;
	bool shift;
	bool rotate;
	Vector3 click;
	Vector3 rotPoint;
	Vector3 screenClick;
	public float rotateSensitivity = 0.3f;
	public float scaleFactor = 0.2f;

	private void Awake() {
		camera = GetComponent<Camera>();
	}

	void Update () {
		var pos = Tool.MousePos;
		if(Input.GetKeyDown(KeyCode.Mouse2)) {
			shift = true;
			screenClick = Input.mousePosition;
			click = pos;
		}
		if(Input.GetKeyUp(KeyCode.Mouse2)) {
			shift = false;
		}
		if(Input.GetKeyDown(KeyCode.Mouse1)) {
			rotate = true;
			click = pos;
			screenClick = Input.mousePosition;
			rotPoint = pos;
		}
		if(Input.GetKeyUp(KeyCode.Mouse1)) {
			rotate = false;
		}
		if(shift) {
			camera.transform.position -= pos - click;
			click = Tool.MousePos;
		}
		if(rotate) {
			var delta = -(Input.mousePosition - screenClick).magnitude * rotateSensitivity;
			var axis = Vector3.Cross(pos - click, camera.transform.forward).normalized;
			camera.transform.RotateAround(rotPoint, axis,  delta);
			click = Tool.MousePos;
			screenClick = Input.mousePosition;
		}
		if(Input.mouseScrollDelta.y != 0f) {
			var factor = 1f - Input.mouseScrollDelta.y * scaleFactor;
			var mousePos = pos;
			var centerPos = Tool.CenterPos;
			var delta = (centerPos - mousePos) * (factor - 1f);
			camera.transform.position += delta;
			camera.orthographicSize *= factor;
		}
	}

	private void OnDrawGizmos() {
		Gizmos.DrawCube(Tool.MousePos, new Vector3(1, 1, 1));
	}
}
