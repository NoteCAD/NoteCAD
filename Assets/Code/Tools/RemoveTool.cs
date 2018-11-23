using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoveTool : Tool {
	protected override void OnActivate() {
		editor.PushUndo();
		foreach(var idp in DetailEditor.instance.selection) {
			DetailEditor.instance.RemoveById(idp);
		}
		StopTool();
	}

}
