using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextTool : Tool {

	TextEntity txt;

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		if(txt != null) {
			AutoConstrainCoincident(txt.p[3], sko as IEntity);
			txt.isSelectable = true;
			foreach(var p in txt.p) p.isSelectable = true;
			txt = null;
			StopTool();
		} else {
			editor.PushUndo();
			var sk = DetailEditor.instance.currentSketch;
			if(sk == null) return;
			txt = SpawnEntity(new TextEntity(sk.GetSketch()));
			txt.text = "Text";
			txt.p[0].pos = pos;
			txt.UpdatePoints();
			AutoConstrainCoincident(txt.p[0], sko as IEntity);
			foreach(var p in txt.p) p.isSelectable = false;
			txt.isSelectable = false;
		}
	}


	protected override void OnMouseMove(Vector3 pos, ICADObject entity) {
		if(txt != null) {
			txt.p[3].pos = pos;
			txt.UpdatePoints();
		}
	}

	protected override void OnDeactivate() {
		if(txt != null) {
			txt.Destroy();
			editor.PopUndo();
		}
	}

	protected override string OnGetDescription() {
		return "click where you want to create text";
	}

}
