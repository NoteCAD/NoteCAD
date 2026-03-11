using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class StyleRemoveTool : Tool {

	protected override void OnActivate() {
		var selected = StylesUI.instance.selectedStyle;
		if(selected == null) {
			StopTool();
			return;
		}
		editor.PushUndo();
		editor.GetDetail().styles.RemoveStyle(selected.guid);
		StylesUI.instance.UpdateStyles();
		StopTool();
	}

	protected override string OnGetDescription() {
		return "Styles";
	}
}
