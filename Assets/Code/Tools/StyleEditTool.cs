using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class StyleEditTool : Tool {

	protected override void OnActivate() {
		var selected = StylesUI.instance.selectedStyle;
		if(selected == null) {
			StopTool();
			return;
		}
		editor.PushUndo();
		Inspect(selected);
	}

	protected override void OnDeactivate() {
		StylesUI.instance.UpdateStyles();
	}

	protected override string OnGetDescription() {
		return "change options of selected style";
	}
}
