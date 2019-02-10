using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CopyTool : Tool {
	protected override void OnActivate() {
		GUIUtility.systemCopyBuffer = DetailEditor.instance.CopySelection();
		StopTool();
	}

}
