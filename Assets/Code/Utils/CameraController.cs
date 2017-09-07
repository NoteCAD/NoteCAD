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
		camera.orthographicSize -= Input.mouseScrollDelta.y;
		if(camera.orthographicSize < 0.001f) camera.orthographicSize = 0.001f;
	}
}
