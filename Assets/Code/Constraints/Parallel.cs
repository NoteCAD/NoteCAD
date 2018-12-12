using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Parallel : Constraint {

	public enum Option {
		Codirected,
		Antidirected
	}

	Option option_;

	public Option option { get { return option_; } set { option_ = value; sketch.MarkDirtySketch(topo:true); } }
	protected override Enum optionInternal { get { return option; } set { option = (Option)value; } }

	public Parallel(Sketch sk) : base(sk) { }

	public Parallel(Sketch sk, IEntity l0, IEntity l1) : base(sk) {
		AddEntity(l0);
		AddEntity(l1);
		ChooseBestOption();
	}

	public override IEnumerable<Exp> equations {
		get {
			var l0 = GetEntityOfType(IEntityType.Line, 0);
			var l1 = GetEntityOfType(IEntityType.Line, 1);

			ExpVector d0 = l0.GetPointAtInPlane(0, sketch.plane) - l0.GetPointAtInPlane(1, sketch.plane);
			ExpVector d1 = l1.GetPointAtInPlane(0, sketch.plane) - l1.GetPointAtInPlane(1, sketch.plane);

			Exp angle = sketch.is3d ? ConstraintExp.angle3d(d0, d1) : ConstraintExp.angle2d(d0, d1);
			switch(option) {
				case Option.Codirected: yield return angle; break;
				case Option.Antidirected: yield return Exp.Abs(angle) - Math.PI; break;
			}
		}
	}

	void DrawStroke(LineCanvas canvas, IEntity line, int rpt) {
		var p0 = line.GetPointAtInPlane(0, null).Eval();
		var p1 = line.GetPointAtInPlane(1, null).Eval();
		float len = (p1 - p0).magnitude;
		float size = Mathf.Min(len, 10f * getPixelSize());
		Vector3 dir = (p1 - p0).normalized * size / 2f;
		Vector3 perp = Vector3.Cross(p1 - p0, Camera.main.transform.forward).normalized * 3f * getPixelSize();
		Vector3 pos = (p1 + p0) / 2f;
		ref_points[rpt] = sketch.plane.ToPlane(pos);
		canvas.DrawLine(pos + dir + perp, pos - dir + perp);
		canvas.DrawLine(pos + dir - perp, pos - dir - perp);
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
