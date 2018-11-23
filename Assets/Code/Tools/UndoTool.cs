using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UndoTool : Tool {

	protected override void OnActivate() {
		editor.undoRedo.Undo();
		StopTool();
	}

}
