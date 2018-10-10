using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoveTool : Tool {
	protected override void OnActivate() {
		foreach(var idp in DetailEditor.instance.selection) {
			DetailEditor.instance.RemoveById(idp);
		}
		StopTool();
	}

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		if(sko == null) return;
		if(!(sko is SketchObject)) return;
		(sko as SketchObject).Destroy();
	}

	protected override string OnGetDescription() {
		return "click some entity or constraint to remove it.";
	}

}
