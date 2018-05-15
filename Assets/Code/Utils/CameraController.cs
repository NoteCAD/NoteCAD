using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour {

	new Camera camera;
	bool shift;
	bool rotate;
	Vector3 click;
	Vector3 rotPoint;
	Vector3 screenClick;
	public float rotateSensitivity = 0.3f;
	public float scaleFactor = 0.2f;
	public float animationTime = 1.0f;
	public float minSize = 0.01f;
	public float maxSize = 10000f;
	Quaternion srcRot;
	Quaternion dstRot;
	Vector3 srcPos;
	Vector3 dstPos;
	float phase = 1.0f;
	double orthoSize;

	public static CameraController instance;

	public void Start() {
		instance = this;
	}

	private void Awake() {
		camera = GetComponent<Camera>();
		orthoSize = camera.orthographicSize;
	}

	public void AnimateToPlane(IPlane plane) {
		srcRot = camera.transform.rotation;
		dstRot = plane.GetRotation();
		srcPos = camera.transform.position;
		dstPos = plane.o;
		phase = 0.0f;
	}

	void Update() {
		if(phase < 1.0f) {
			phase += Time.deltaTime * 1.0f / animationTime;
			phase = Mathf.Clamp01(phase);
			camera.transform.rotation = Quaternion.Lerp(srcRot, dstRot, phase);
			camera.transform.position = Vector3.Lerp(srcPos, dstPos, phase);
			return;
		}
		var pos = Tool.WorldMousePos;
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
			click = Tool.WorldMousePos;
		}
		if(rotate) {
			var delta = -(Input.mousePosition - screenClick).magnitude * rotateSensitivity;
			var axis = Vector3.Cross(pos - click, camera.transform.forward).normalized;
			camera.transform.RotateAround(rotPoint, axis,  delta);
			click = Tool.WorldMousePos;
			screenClick = Input.mousePosition;
		}
		if(!EventSystem.current.IsPointerOverGameObject() && Input.mouseScrollDelta.y != 0f) {
			var mouse = Input.mouseScrollDelta.y;
			if(mouse < 0f) {
				mouse = -1f;
				if(orthoSize > maxSize) mouse = 0f;
			} else 
			if(mouse > 0f) {
				mouse = 1f;
				if(orthoSize < minSize) mouse = 0f;
			}

			var count = (int)Mathf.Abs(Input.mouseScrollDelta.y);

			for(int i = 0; i < count; i++) {
				double factor = mouse * scaleFactor;
				if(factor < 0.0) {
					factor = 1.0 / (1.0 + factor);
				} else {
					factor = 1.0 - factor;
				}
				var mousePos = pos;
				var centerPos = Tool.CenterPos;
				var delta = (centerPos - mousePos) * ((float)factor - 1f);
				camera.transform.position += delta;
				orthoSize *= factor;
				camera.orthographicSize = (float)orthoSize;
				//camera.farClipPlane = (float)(orthoSize * 10.0);
				//camera.nearClipPlane = (float)(-orthoSize * 10.0);
			}
		}
	}

	private void OnDrawGizmos() {
		//Gizmos.DrawCube(Tool.WorldPlanePos, new Vector3(1, 1, 1));
	}
}
