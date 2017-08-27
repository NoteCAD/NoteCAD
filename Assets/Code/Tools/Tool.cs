using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class Tool : MonoBehaviour {

	[HideInInspector] public ToolBar toolbar;
	public KeyCode[] hotkeys;

	void Start() {
		GetComponent<Button>().onClick.AddListener(Click);
	}

	void Click() {
		toolbar.ActiveTool = this;
	}

	protected virtual void OnActivate() { }
	protected virtual void OnDeactivate() { }
	protected virtual void OnUpdate() { }
	protected virtual void OnMouseDown(Vector3 pos, Entity entity) { }
	protected virtual void OnMouseUp(Vector3 pos, Entity entity) { }
	protected virtual void OnMouseMove(Vector3 pos, Entity entity) { }

	public void Activate() {
		OnActivate();
	}

	public void Deactivate() {
		OnDeactivate();
	}

	public void DoUpdate() {
		OnUpdate();
	}

	public void MouseDown(Vector3 pos, Entity entity) {
		OnMouseDown(pos, entity);
	}

	public void MouseUp(Vector3 pos, Entity entity) {
		OnMouseUp(pos, entity);
	}

	public void MouseMove(Vector3 pos, Entity entity) {
		OnMouseMove(pos, entity);
	}

	public bool IsActive() {
		return toolbar.ActiveTool == this;
	}

	public static Vector3 MousePos {
		get {
			var plane = new Plane(Camera.main.transform.forward, Vector3.zero);
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			float cast;
			plane.Raycast(ray, out cast);
			return ray.GetPoint(cast);
		}
	}

}
