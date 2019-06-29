using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PolygonTool : Tool {

	LineEntity[] lines = null;
	Vector3 click;
	CircleEntity circle;

	[System.Serializable]
	class PolygonOptions { 
		int corners_ = 6;
		public int corners { get { return corners_; } set { corners_ = Mathf.Clamp(value, 3, 32); } }
	}

	PolygonOptions options = new PolygonOptions();

	PolygonTool() {
		enableHoverFilter = true;
	}

	protected override bool OnTryHover(Constraint c) {
		return false;
	}

	protected override bool OnTryHover(IEntity e) {
		if(lines == null) return CanConstrainCoincident(e);
		return false;
	}

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		var sk = DetailEditor.instance.currentSketch;
		if(sk == null) return;
		if(lines != null) {
			for(int i = 0; i < lines.Length; i++) {
				lines[i].p1.isSelectable = true;
				lines[i].p0.isSelectable = true;
				lines[i].isSelectable = true;
				var pOn = new PointOn(sk.GetSketch(), lines[i].p0, circle);
				pOn.SetValue(1.0 / lines.Length * i);
				pOn.reference = false;
			}
			
			setCornerPos(lines[1].p1.pos);

			lines = null;
			StopTool();
		} else {
			editor.PushUndo();
			click = pos;
			circle = SpawnEntity(new CircleEntity(sk.GetSketch()), construction: true);
			circle.center.pos = pos;
			circle.angleFixed = false;
			lines = new LineEntity[options.corners];
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
			}
			AutoConstrainCoincident(circle.center, sko as IEntity);
		}
	}

	void setCornerPos(Vector3 pos) {
		if(lines == null) return;
		var p1 = new Vector3(pos.x, lines[0].p0.pos.y);
		var p3 = new Vector3(lines[0].p0.pos.x, pos.y);
		var r = (pos - click).magnitude;
		circle.radius = r;
		var a = 0.0f;
		for(int i = 0; i < lines.Length; i++) {
			lines[i].p0.pos = click + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0.0f) * r;
			a = 2.0f * Mathf.PI / (lines.Length) * (i + 1);
			lines[i].p1.pos = click + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0.0f) * r;
		}
	}

	protected override void OnMouseMove(Vector3 pos, ICADObject entity) {
		if(lines != null) {
			setCornerPos(pos);
		}
	}

	protected override void OnDeactivate() {
		if(lines != null) {
			for(int i = 0; i < lines.Length; i++) {
				lines[i].Destroy();
				lines[i] = null;
			}
			lines = null;
			editor.PopUndo();
		}
	}

	protected override void OnActivate() {
		Inspect(options);
	}

	protected override string OnGetDescription() {
		return "click where you want to create center of polygon then you can control size of polygon by moving mouse cursor.";
	}

}
