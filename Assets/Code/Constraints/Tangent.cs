using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Tangent : Constraint {

	public enum Option {
		Codirected,
		Antidirected
	}

	Option option_;
	Param ta = new Param("ta");
	Param tb = new Param("tb");

	public Option option { get { return option_; } set { option_ = value; sketch.MarkDirtySketch(topo:true); } }
	protected override Enum optionInternal { get { return option; } set { option = (Option)value; } }

	public override IEnumerable<Param> parameters {
		get {
			yield return ta; 
			yield return tb; 
		}
	}

	public Tangent(Sketch sk) : base(sk) { }

	public Tangent(Sketch sk, IEntity l0, IEntity l1) : base(sk) {
		AddEntity(l0);
		AddEntity(l1);
		ChooseBestOption();
	}

	public override IEnumerable<Exp> equations {
		get {
			var l0 = GetEntity(0);
			var l1 = GetEntity(1);

			ExpVector dir0 = l0.TangentAt(ta);
			ExpVector dir1 = l1.TangentAt(tb);

			dir0 = l0.plane.DirFromPlane(dir0);
			dir0 = sketch.plane.DirToPlane(dir0);

			dir1 = l1.plane.DirFromPlane(dir1);
			dir1 = sketch.plane.DirToPlane(dir1);

			Exp angle = sketch.is3d ? ConstraintExp.angle3d(dir0, dir1) : ConstraintExp.angle2d(dir0, dir1);
			switch(option) {
				case Option.Codirected: yield return angle; break;
				case Option.Antidirected: yield return Exp.Abs(angle) - Math.PI; break;
			}

			var eq = l1.PointOnInPlane(tb, sketch.plane) - l0.PointOnInPlane(ta, sketch.plane);

			yield return eq.x;
			yield return eq.y;
			if(sketch.is3d) yield return eq.z;

		}
	}

	void DrawStroke(LineCanvas canvas, IEntity line, int rpt) {
		var p0 = line.GetPointAtInPlane(0, null).Eval();
		var p1 = line.GetPointAtInPlane(1, null).Eval();
		float len = (p1 - p0).magnitude;
		float size = Mathf.Min(len, 10f * getPixelSize());
		Vector3 dir = (p1 - p0).normalized * size / 2f;
		Vector3 perp = Vector3.Cross(p1 - p0, Vector3.forward).normalized * 3f * getPixelSize();
		Vector3 pos = (p1 + p0) / 2f;
		ref_points[rpt] = pos;
		canvas.DrawLine(pos + dir + perp, pos - dir + perp);
		canvas.DrawLine(pos + dir - perp, pos - dir - perp);
	}

	protected override void OnDraw(LineCanvas canvas) {
		var l0 = GetEntity(0);
		var dir = l0.TangentAt(ta).Eval().normalized;
		dir = l0.plane.DirFromPlane(dir);
		var perp = Vector3.Cross(dir, sketch.plane.n);

		var pos = l0.PointOnInPlane(ta, null).Eval();

		ref_points[0] = ref_points[1] = pos;
		var size = getPixelSize() * 10f;
		dir *= size;
		perp *= size;

		canvas.DrawLine(pos + dir, pos - dir);
		canvas.DrawLine(pos - perp, pos + perp);
	}

}
