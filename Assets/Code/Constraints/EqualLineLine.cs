using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EqualLineLine : Constraint {

	public EqualLineLine(Sketch sk) : base(sk) { }

	public EqualLineLine(Sketch sk, IEntity l0, IEntity l1) : base(sk) {
		AddEntity(l0);
		AddEntity(l1);
	}

	public override IEnumerable<Exp> equations {
		get {
			var l0 = GetEntityOfType(IEntityType.Line, 0);
			var l1 = GetEntityOfType(IEntityType.Line, 1);
			ExpVector d0 = l0.GetPointAtInPlane(0, sketch.plane) - l0.GetPointAtInPlane(1, sketch.plane);
			ExpVector d1 = l1.GetPointAtInPlane(0, sketch.plane) - l1.GetPointAtInPlane(1, sketch.plane);
			yield return d0.Magnitude() - d1.Magnitude();
		}
	}

	void DrawStroke(LineCanvas canvas, IEntity line, int rpt) {
		var p0 = line.GetPointAtInPlane(0, null).Eval();
		var p1 = line.GetPointAtInPlane(1, null).Eval();
		Vector3 dir = (p1 - p0).normalized;
		Vector3 perp = Vector3.Cross(dir, Vector3.forward) * 5f * getPixelSize();
		Vector3 pos = (p1 + p0) / 2f;
		ref_points[rpt] = pos;
		canvas.DrawLine(pos + perp, pos - perp);
	}

	protected override void OnDraw(LineCanvas canvas) {
		var l0 = GetEntityOfType(IEntityType.Line, 0);
		var l1 = GetEntityOfType(IEntityType.Line, 1);
		DrawStroke(canvas, l0, 0);
		DrawStroke(canvas, l1, 1);
		if(DetailEditor.instance.hovered == this) {
			DrawReferenceLink(canvas, Camera.main);
		}
	}

}
