using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

	Camera camera;
	bool shift;
	Vector3 click;

	private void Awake() {
		camera = GetComponent<Camera>();
	}

	void Update () {
		var pos = Tool.MousePos;
		if(Input.GetKeyDown(KeyCode.Mouse2)) {
			shift = true;
			click = pos;
		}
		if(Input.GetKeyUp(KeyCode.Mouse2)) {
			shift = false;
		}
		if(shift) {
			camera.transform.position -= pos - click;
			click = Tool.MousePos;
		}
		if(Input.mouseScrollDelta.y != 0f) {
			var factor = 1f - Input.mouseScrollDelta.y * 0.1f;
			var mousePos = camera.ScreenToWorldPoint(Input.mousePosition);
			var centerPos = camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0.0f));
			var delta = (centerPos - mousePos) * (factor - 1f);
			delta.z = 0f;
			camera.transform.position += delta;
			camera.orthographicSize *= factor;
		}
	}
}
