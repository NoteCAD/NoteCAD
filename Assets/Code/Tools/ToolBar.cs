using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToolBar : MonoBehaviour {

	Tool[] tools;
	Tool activeTool;
	public Tool startupTool;

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
		ActiveTool = startupTool;
	}

	void Update() {
		foreach(var t in tools) {
			foreach(var hk in t.hotkeys) {
				if(Input.GetKeyDown(hk)) {
					ActiveTool = t;
					break;
				}
			}
		}

		if(activeTool != null && Input.GetKeyDown(KeyCode.Mouse0)) {
			activeTool.MouseDown(Tool.MousePos, Sketch.instance.hovered);
		}

		if(activeTool != null && Input.GetKeyUp(KeyCode.Mouse0)) {
			activeTool.MouseUp(Tool.MousePos, Sketch.instance.hovered);
		}

		if(activeTool != null) {
			activeTool.MouseMove(Tool.MousePos, Sketch.instance.hovered);
		}

		if(activeTool != null) {
			activeTool.DoUpdate();
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
			cb.normalColor = Color.blue;
			btn.colors = cb;
			activeTool.Activate();
		}
	}

}
