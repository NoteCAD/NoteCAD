using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class StyleAddTool : Tool {

	protected override void OnActivate() {
		editor.PushUndo();
		var style = editor.GetDetail().styles.AddStyle();
		style.stroke.name = "New style " + style.guid.ToString();
		StylesUI.instance.UpdateStyles();
		StylesUI.instance.SelectStyle(style);
		Inspect(style.stroke);
	}

	protected override void OnDeactivate() {
		StylesUI.instance.UpdateStyles();
	}

	protected override string OnGetDescription() {
		return "change options of newly created style";
	}
}
