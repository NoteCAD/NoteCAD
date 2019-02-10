using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PasteTool : Tool {
	protected override void OnActivate() {
		editor.PushUndo();
		var result = editor.Paste(GUIUtility.systemCopyBuffer);
		if(result == null) {
			editor.PopUndo();
		} else {
			editor.selection.Clear();
			editor.selection.UnionWith(result);
		}
		StopTool();
	}

}
