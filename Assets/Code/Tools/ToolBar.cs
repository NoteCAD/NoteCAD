using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ToolBar : MonoBehaviour {

	Tool[] tools;
	Tool activeTool;
	public Tool defaultTool;
	public Color pressedColor;

	public Text description;

	public Tool ActiveTool {
		get {
			return activeTool;
		}

		set {
			ActivateTool(value);
		}
	}

	void Start () {
		tools = GetComponentsInChildren<Tool>();
		foreach(var t in tools) {
			t.toolbar = this;
		}
		ActiveTool = defaultTool;
	}


	private bool IsPointerOverUIObject() {
		PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
		eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
		List<RaycastResult> results = new List<RaycastResult>();
		EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
		return results.Count > 0;
	}

	float doubleClickTime;
	void Update() {
		doubleClickTime += Time.deltaTime;
		foreach(var t in tools) {
			foreach(var hk in t.hotkeys) {
				if(Input.GetKeyDown(hk)) {
					ActiveTool = t;
					break;
				}
			}
		}
		bool overUI = IsPointerOverUIObject();
		Debug.Log(overUI);
		bool mouseDown = (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetMouseButtonDown(0)) && !overUI;
#if UNITY_WEBGL
		//mouseDown = mouseDown || Input.touches.Length > 0 && Input.touches[0].phase == TouchPhase.Began;
#endif
		if(activeTool != null && mouseDown) {
			if(doubleClickTime < 0.3f) {
				activeTool.MouseDoubleClick(Tool.MousePos, DetailEditor.instance.hovered);
			}
			activeTool.MouseDown(Tool.MousePos, DetailEditor.instance.hovered);
			doubleClickTime = 0f;
		}

		bool mouseUp = Input.GetKeyUp(KeyCode.Mouse0) || Input.GetMouseButtonUp(0);
#if UNITY_WEBGL
		//mouseUp = mouseUp || Input.touches.Length > 0 && Input.touches[0].phase == TouchPhase.Ended;
#endif

		if(activeTool != null && mouseUp) {
			activeTool.MouseUp(Tool.MousePos, DetailEditor.instance.hovered);
		}

		if(activeTool != null) {
			activeTool.MouseMove(Tool.MousePos, DetailEditor.instance.hovered);
		}

		if(activeTool != null) {
			activeTool.DoUpdate();
			description.text = activeTool.GetDescription();
		}

		if(activeTool.shouldStop) {
			ActivateDefaultTool();
		}
	}

	void ActivateTool(Tool tool) {
		if(tool == activeTool) return;
		if(activeTool != null) {
			var btn = activeTool.GetComponent<Button>();
			var cb = btn.colors;
			cb.normalColor = Color.white;
			btn.colors = cb;

			activeTool.Deactivate();
		}
		activeTool = tool;
		if(activeTool != null) {
			var btn = activeTool.GetComponent<Button>();
			var cb = btn.colors;
			cb.normalColor = pressedColor;
			btn.colors = cb;
			activeTool.Activate();
			description.text = activeTool.GetDescription();
		}
	}

	public void ActivateDefaultTool() {
		ActiveTool = defaultTool;
	}

}
