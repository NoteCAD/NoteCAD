using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NoteCAD;

public class FixTool : Tool {

	FixTool() {
	}

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		var entity = sko as IEntity;
		if(entity == null) return;

		editor.PushUndo();
		new Fixation(DetailEditor.instance.currentSketch.GetSketch(), entity);
	}

	protected override string OnGetDescription() {
		return "click an object to apply fixation.";
	}

}
