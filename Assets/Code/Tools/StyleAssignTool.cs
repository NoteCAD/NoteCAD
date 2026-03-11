using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class StyleAssignTool : Tool {

	protected override void OnActivate() {
		if(editor.selection.Count == 0) return;
		editor.PushUndo();
		var selected = StylesUI.instance.selectedStyle;
		foreach(var id in editor.selection) {
			var obj = editor.GetDetail().GetObjectById(id) as SketchObject;
			if(obj == null) continue;
			obj.style = selected;
		}
		StopTool();
	}

	protected override string OnGetDescription() {
		return "Styles";
	}
}
