using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class Tool : MonoBehaviour {

	[HideInInspector] public ToolBar toolbar;
	public KeyCode[] hotkeys;

	public bool shouldStop { get; private set; }

	void Start() {
		GetComponent<Button>().onClick.AddListener(Click);
	}

	void Click() {
		toolbar.ActiveTool = this;
	}

	protected virtual void OnActivate() { }
	protected virtual void OnDeactivate() { }
	protected virtual void OnUpdate() { }
	protected virtual void OnMouseDown(Vector3 pos, SketchObject sko) { }
	protected virtual void OnMouseUp(Vector3 pos, SketchObject sko) { }
	protected virtual void OnMouseMove(Vector3 pos, SketchObject sko) { }
	protected virtual void OnMouseDoubleClick(Vector3 pos, SketchObject sko) { }

	public void Activate() {
		shouldStop = false;
		OnActivate();
	}

	public void Deactivate() {
		OnDeactivate();
	}

	public void DoUpdate() {
		OnUpdate();
	}

	public void MouseDown(Vector3 pos, SketchObject entity) {
		OnMouseDown(pos, entity);
	}

	public void MouseUp(Vector3 pos, SketchObject entity) {
		OnMouseUp(pos, entity);
	}

	public void MouseMove(Vector3 pos, SketchObject entity) {
		OnMouseMove(pos, entity);
	}

	public void MouseDoubleClick(Vector3 pos, SketchObject entity) {
		OnMouseDoubleClick(pos, entity);
	}

	public bool IsActive() {
		return toolbar.ActiveTool == this;
	}

	public static Vector3 MousePos {
		get {
			var mousePos = Input.mousePosition;
#if UNITY_WEBGL
			if(Input.touches.Length > 0) mousePos = Input.touches[0].position;
#endif
			var plane = new Plane(Camera.main.transform.forward, Vector3.zero);
			var ray = Camera.main.ScreenPointToRay(mousePos);
			float cast;
			plane.Raycast(ray, out cast);
			return ray.GetPoint(cast);
		}
	}

	public static Vector3 CenterPos {
		get {
			var plane = new Plane(Camera.main.transform.forward, Vector3.zero);
			var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));
			float cast;
			plane.Raycast(ray, out cast);
			return ray.GetPoint(cast);
		}
	}

	public void StopTool() {
		shouldStop = true;
	}

	protected bool AutoConstrainCoincident(PointEntity point, Entity with) {
		if(with is PointEntity) {
			var p1 = with as PointEntity;
			new PointsCoincident(point.sketch, point, p1);
			point.SetPosition(p1.GetPosition());
			return true;
		}
		return false;
	}

}
