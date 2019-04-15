using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RectTool : Tool {

	LineEntity[] lines = new LineEntity[4];

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		if(lines[0] != null) {
			foreach(var l in lines) {
				l.p1.isSelectable = true;
				l.p0.isSelectable = true;
				l.isSelectable = true;
			}
			
			AutoConstrainCoincident(lines[1].p1, sko as IEntity);
			setCornerPos(lines[1].p1.pos);

			for(int i = 0; i < lines.Length; i++) lines[i] = null;
			StopTool();
		} else {
			editor.PushUndo();
			var sk = DetailEditor.instance.currentSketch;
			if(sk == null) return;
			for(int i = 0; i < lines.Length; i++) {
				lines[i] = SpawnEntity(new LineEntity(sk.GetSketch()));
				lines[i].p0.pos = pos;
				lines[i].p1.pos = pos;
				lines[i].isSelectable = false;
				lines[i].p0.isSelectable = false;
				lines[i].p1.isSelectable = false;
			}
			for(int i = 0; i < lines.Length; i++) {
				new PointsCoincident(sk.GetSketch(), lines[i].p1, lines[(i + 1) % lines.Length].p0);
				var hv = new HVConstraint(sk.GetSketch(), lines[i]);
				hv.orientation = (i % 2 == 0) ? HVOrientation.OY : HVOrientation.OX;
			}
			AutoConstrainCoincident(lines[0].p0, sko as IEntity);
			lines[3].p1.pos = lines[0].p0.pos;
		}
	}

	void setCornerPos(Vector3 pos) {
		var p1 = new Vector3(pos.x, lines[0].p0.pos.y);
		var p3 = new Vector3(lines[0].p0.pos.x, pos.y);

		lines[0].p1.pos = p1;
		lines[1].p0.pos = p1;
						
		lines[1].p1.pos = pos;
		lines[2].p0.pos = pos;

		lines[2].p1.pos = p3;
		lines[3].p0.pos = p3;
	}

	protected override void OnMouseMove(Vector3 pos, ICADObject entity) {
		if(lines[1] != null) {
			setCornerPos(pos);
		}
	}

	protected override void OnDeactivate() {
		if(lines[0] != null) {
			for(int i = 0; i < lines.Length; i++) {
				lines[i].Destroy();
				lines[i] = null;
			}
			editor.PopUndo();
		}
	}

	protected override string OnGetDescription() {
		return "click where you want to create points of the rect";
	}

}
