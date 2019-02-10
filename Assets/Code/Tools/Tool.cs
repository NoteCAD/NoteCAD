using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class Tool : MonoBehaviour {

	[HideInInspector] public ToolBar toolbar;
	public KeyCode[] hotkeys;
	public bool ctrl;
	public string text;
	public Sprite icon;

	public DetailEditor editor { get { return DetailEditor.instance; } }

	public bool shouldStop { get; private set; }

	void Start() {
		GetComponent<Button>().onClick.AddListener(Click);
		OnStart();
	}

	void Click() {
		toolbar.ActiveTool = this;
	}

	protected virtual void OnStart() { }
	protected virtual void OnActivate() { }
	protected virtual void OnDeactivate() { }
	protected virtual void OnUpdate() { }
	protected virtual void OnMouseDown(Vector3 pos, ICADObject sko) { }
	protected virtual void OnMouseUp(Vector3 pos, ICADObject sko) { }
	protected virtual void OnMouseMove(Vector3 pos, ICADObject sko) { }
	protected virtual void OnMouseDoubleClick(Vector3 pos, ICADObject sko) { }
	public virtual bool CanActivate() { return true; }

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

	public void MouseDown(Vector3 pos, ICADObject sko) {
		OnMouseDown(pos, sko);
	}

	public void MouseUp(Vector3 pos, ICADObject sko) {
		OnMouseUp(pos, sko);
	}

	public void MouseMove(Vector3 pos, ICADObject sko) {
		OnMouseMove(pos, sko);
	}

	public void MouseDoubleClick(Vector3 pos, ICADObject sko) {
		OnMouseDoubleClick(pos, sko);
	}

	public bool IsActive() {
		return toolbar.ActiveTool == this;
	}

	public static Vector3 MousePos {
		get {
			var sk = DetailEditor.instance.currentWorkplane;
			var pos = WorldPlanePos;
			if(sk != null) {
				pos = sk.WorldToLocal(pos);
			}
			return pos;
		}
	}

	public static Vector3 WorldMousePos {
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

	public static Vector3 WorldPlanePos {
		get {
			var mousePos = Input.mousePosition;
#if UNITY_WEBGL
			if(Input.touches.Length > 0) mousePos = Input.touches[0].position;
#endif
			var plane = new Plane(Camera.main.transform.forward, Vector3.zero);
			var sk = DetailEditor.instance.currentWorkplane;
			if(sk != null) {
				plane = new Plane(sk.GetNormal(), sk.GetPosition());
			}
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

	protected bool AutoConstrainCoincident(PointEntity point, IEntity with) {
		if(with == null) return false;
		if(with.type == IEntityType.Point) {
			new PointsCoincident(point.sketch, point, with);
			point.SetPosition(with.GetPointAtInPlane(0, point.sketch.plane).Eval());
		} else {
			new PointOn(point.sketch, point, with);
			return false;
		}
		return true;
	}

	public string GetDescription() {
		var result = this.GetType().Name;
		if(hotkeys.Length > 0) {
			result += " [" + (ctrl ? "Ctrl + " : "") + hotkeys[0].ToString() + "]";
		}
		var desc = OnGetDescription();
		if(desc != "") {
			result += ": " + desc;
		}
		return result;
	}

	public string GetTooltip() {
		var result = this.GetType().Name + ". " + OnGetDescription();
		if(hotkeys.Length > 0) {
			result += "[" + (ctrl ? "Ctrl + " : "") + hotkeys[0].ToString() + "]";
		}
		return result;
	}

	protected virtual string OnGetDescription() {
		return "";
	}

	protected virtual string OnGetTooltip() {
		return "";
	}

	public string GetRichText() {
		if(hotkeys == null || hotkeys.Length == 0) return text;
		var hk = hotkeys[0].ToString();
		if(hk.Length != 1) return text;
		var index = text.IndexOf(hk, System.StringComparison.OrdinalIgnoreCase);
		var openColor = "<color=\"#6ECEEFFF\">";
		var closeColor = "</color>";
		if(index < 0 || ctrl) return text + " [" + openColor + (ctrl ? "Ctrl+" : "") + hk  + closeColor + "]";
		return text.Substring(0, index) + openColor + text[index] + closeColor + text.Substring(index + 1, text.Length - index - 1);
	}

}
