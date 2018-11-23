using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RedoTool : Tool {

	protected override void OnActivate() {
		editor.undoRedo.Redo();
		StopTool();
	}

}
