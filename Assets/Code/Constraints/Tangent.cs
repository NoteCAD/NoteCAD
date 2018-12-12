using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

[Serializable]
public class Tangent : Constraint {

	public enum Option {
		Codirected,
		Antidirected
	}

	Option option_;
	Param t0 = new Param("t0");
	Param t1 = new Param("t1");

	public Option option { get { return option_; } set { option_ = value; sketch.MarkDirtySketch(topo:true); } }
	protected override Enum optionInternal { get { return option; } set { option = (Option)value; } }

	public override IEnumerable<Param> parameters {
		get {
			double tv0 = 0.0;
			double tv1 = 0.0;
			Exp c = null;
			Param p = null;
			if(!IsCoincident(ref tv0, ref tv1, ref c, ref p)) {
				yield return t0; 
				yield return t1;
			} else {
				if(p != null) yield return p;
			}
		}
	}

	public Tangent(Sketch sk) : base(sk) { }

	public Tangent(Sketch sk, IEntity l0, IEntity l1) : base(sk) {
		AddEntity(l0);
		AddEntity(l1);
		Satisfy();
		ChooseBestOption();
	}

	bool Satisfy() {
		EquationSystem sys = new EquationSystem();
		sys.AddParameters(parameters);
		addAngle = false;
		var exprs = equations.ToList();
		addAngle = true;
		sys.AddEquations(equations);

		double bestI = 0.0;
		double bestJ = 0.0;
		double min = -1.0;
		for(double i = 0.0; i < 1.0; i += 0.25 / 2.0) {
			for(double j = 0.0; j < 1.0; j += 0.25 / 2.0) {
				t0.value = i;
				t1.value = j;
				sys.Solve();
				double cur_value = exprs.Sum(e => Math.Abs(e.Eval()));
				if(min >= 0.0 && min < cur_value) continue;
				bestI = t0.value;
				bestJ = t1.value;
				min = cur_value;
			}
		}
		t0.value = bestI;
		t1.value = bestJ;
		return true;
	}

	bool IsCoincident(ref double tv0, ref double tv1, ref Exp c, ref Param p) {
		var l0 = GetEntity(0);
		var l1 = GetEntity(1);
		var s0 = l0 as ISegmentaryEntity;
		var s1 = l1 as ISegmentaryEntity;
		if(s0 != null && s1 != null) {
			if (s0.begin.IsCoincidentWith(s1.begin))	{ tv0 = 0.0; tv1 = 0.0; return true; }
			if (s0.begin.IsCoincidentWith(s1.end))		{ tv0 = 0.0; tv1 = 1.0; return true; }
			if (s0.end.IsCoincidentWith(s1.begin))		{ tv0 = 1.0; tv1 = 0.0; return true; }
			if (s0.end.IsCoincidentWith(s1.end))		{ tv0 = 1.0; tv1 = 1.0; return true; }
		}
		if(s0 != null) {
			PointOn pOn = null;
			if(s0.begin.IsCoincidentWithCurve(l1, ref pOn)) { tv0 = 0.0; p = t1; c = new Exp(t1) - pOn.GetValueParam(); return true; }
			if(s0.end.IsCoincidentWithCurve(l1, ref pOn))	{ tv0 = 1.0; p = t1; c = new Exp(t1) - pOn.GetValueParam(); return true; }
		}
		if(s1 != null) {
			PointOn pOn = null;
			if(s1.begin.IsCoincidentWithCurve(l0, ref pOn)) { p = t0; c = new Exp(t0) - pOn.GetValueParam(); tv1 = 0.0; return true; }
			if(s1.end.IsCoincidentWithCurve(l0, ref pOn))   { p = t0; c = new Exp(t0) - pOn.GetValueParam(); tv1 = 1.0; return true; }
		}
		return false;
	}
	bool addAngle = true;
	public override IEnumerable<Exp> equations {
		get {
			var l0 = GetEntity(0);
			var l1 = GetEntity(1);

			ExpVector dir0 = l0.TangentAt(t0);
			ExpVector dir1 = l1.TangentAt(t1);

			dir0 = l0.plane.DirFromPlane(dir0);
			dir0 = sketch.plane.DirToPlane(dir0);

			dir1 = l1.plane.DirFromPlane(dir1);
			dir1 = sketch.plane.DirToPlane(dir1);

			if(addAngle) {
				Exp angle = sketch.is3d ? ConstraintExp.angle3d(dir0, dir1) : ConstraintExp.angle2d(dir0, dir1);
				switch(option) {
					case Option.Codirected: yield return angle; break;
					case Option.Antidirected: yield return Exp.Abs(angle) - Math.PI; break;
				}
			}
			double tv0 = t0.value;
			double tv1 = t1.value;
			Exp c = null;
			Param p = null;
			if(IsCoincident(ref tv0, ref tv1, ref c, ref p)) {
				t0.value = tv0;
				t1.value = tv1;
				if(c != null) yield return c;
			} else {
				var eq = l1.PointOnInPlane(t1, sketch.plane) - l0.PointOnInPlane(t0, sketch.plane);

				yield return eq.x;
				yield return eq.y;
				if(sketch.is3d) yield return eq.z;
			}
		}
	}

	protected virtual bool OnSatisfy() {
		EquationSystem sys = new EquationSystem();
		sys.revertWhenNotConverged = false;
		sys.AddParameter(t0);
		sys.AddParameter(t1);
		sys.AddEquations(equations);
		return sys.Solve() == EquationSystem.SolveResult.OKAY;
	}


	protected override void OnDraw(LineCanvas canvas) {
		var l0 = GetEntity(0);
		var dir = l0.TangentAt(t0).Eval();
		dir = l0.plane.DirFromPlane(dir).normalized;
		var perp = Vector3.Cross(dir, sketch.plane.n).normalized;
		var pos = l0.PointOnInPlane(t0, null).Eval();

		ref_points[0] = ref_points[1] = sketch.plane.ToPlane(pos);
		var size = getPixelSize() * 10f;
		perp *= size;
		dir *= size;

		canvas.DrawLine(pos + dir, pos - dir);
		canvas.DrawLine(pos - perp, pos + perp);

		//GetEntity(0).DrawExtend(canvas, t0.value, 0.05);
		//GetEntity(1).DrawExtend(canvas, t1.value, 0.05);

	}

	protected override void OnWrite(XmlTextWriter xml) {
		xml.WriteAttributeString("t0", t0.value.ToStr());
		xml.WriteAttributeString("t1", t1.value.ToStr());
	}

	protected override void OnRead(XmlNode xml) {
		t0.value = xml.Attributes["t0"].Value.ToDouble();
		t1.value = xml.Attributes["t1"].Value.ToDouble();
	}

}
