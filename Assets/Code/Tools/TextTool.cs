using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextTool : Tool {

	TextEntity txt;
	
	[System.Serializable]
	class Options { 
		public string text;
	}

	Options options = new Options();

	TextTool() {
		enableHoverFilter = true;
	}

	protected override bool OnTryHover(Constraint c) {
		return false;
	}

	protected override bool OnTryHover(IEntity e) {
		return CanConstrainCoincident(e);
	}
	
	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		if(txt != null) {
			AutoConstrainCoincident(txt.p[3], sko as IEntity);
			txt.isSelectable = true;
			foreach(var p in txt.p) p.isSelectable = true;
			txt = null;
			StopTool();
		} else if(options.text != "") {
			editor.PushUndo();
			var sk = DetailEditor.instance.currentSketch;
			if(sk == null) return;
			txt = SpawnEntity(new TextEntity(sk.GetSketch()));
			txt.text = options.text;
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
		return "enter text in option window and then click where you want to create text entity";
	}

	protected override void OnActivate() {
		options.text = "";
		Inspect(options);
	}

}
