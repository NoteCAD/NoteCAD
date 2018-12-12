using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineTool : Tool {

	LineEntity current;
	bool canCreate = true;

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {

		if(current != null) {
			//if(!canCreate) return;
			current.p1.SetPosition(pos);
			current.p1.isSelectable = true;
			current.p0.isSelectable = true;
			current.isSelectable = true;
			if(AutoConstrainCoincident(current.p1, sko as IEntity)) {
				current = null;
				StopTool();
				return;
			}
		}
		var sk = DetailEditor.instance.currentSketch;
		if(sk == null) return;
		editor.PushUndo();
		var newLine = new LineEntity(sk.GetSketch());
		newLine.p0.SetPosition(pos);
		newLine.p1.SetPosition(pos);
		if(current == null) {
			AutoConstrainCoincident(newLine.p0, sko as IEntity);
		} else {
			new PointsCoincident(current.sketch, current.p1, newLine.p0);
		}

		current = newLine;
		current.isSelectable = false;
		current.p0.isSelectable = false;
		current.p1.isSelectable = false;
	}

	protected override void OnMouseMove(Vector3 pos, ICADObject entity) {
		if(current != null) {
			current.p1.SetPosition(pos);
			//var itr = new Vector3();
			canCreate = true;//!current.sketch.IsCrossed(current, ref itr);
			current.isError = !canCreate;
		} else {
			canCreate = true;
		}
	}

	protected override void OnDeactivate() {
		if(current != null) {
			current.Destroy();
			current = null;
			editor.PopUndo();
		}
		canCreate = true;
	}

	protected override string OnGetDescription() {
		return "click where you want to create the beginning and the ending points of the line";
	}

}
