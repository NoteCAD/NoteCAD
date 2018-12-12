using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

[Serializable]
public class PointOn : ValueConstraint {

	public IEntity point { get { return GetEntity(0); } set { SetEntity(0, value); } }
	public IEntity on { get { return GetEntity(1); } set { SetEntity(1, value); } }

	public ExpVector pointExp { get { return point.PointExpInPlane(sketch.plane); } }
	public Vector3 pointPos { get { return point.PointExpInPlane(null).Eval(); } }
	public override bool valueVisible { get { return !reference; } }

	public PointOn(Sketch sk) : base(sk) {
		selectByRefPoints = true;
	}

	public PointOn(Sketch sk, IEntity point, IEntity on) : base(sk) {
		reference = true;
		AddEntity(point);
		AddEntity(on);
		SetValue(0.51);
		Satisfy();
		selectByRefPoints = true;
	}

	protected override bool OnSatisfy() {
		EquationSystem sys = new EquationSystem();
		sys.AddParameters(parameters);
		var exprs = equations.ToList();
		sys.AddEquations(equations);

		double bestI = 0.0;
		double min = -1.0;
		for(double i = 0.0; i < 1.0; i += 0.25 / 2.0) {
			value.value = i;
			sys.Solve();
			double cur_value = exprs.Sum(e => Math.Abs(e.Eval()));
			if(min >= 0.0 && min < cur_value) continue;
			bestI = value.value;
			min = cur_value;
		}
		value.value = bestI;
		return true;
	}

	public override IEnumerable<Exp> equations {
		get {
			var p = pointExp;
			var eq = on.PointOnInPlane(value, sketch.plane) - p;

			yield return eq.x;
			yield return eq.y;
			if(sketch.is3d) yield return eq.z;
		}
	}

	protected override void OnDraw(LineCanvas canvas) {
		var p0 = pointPos;
		drawCameraCircle(canvas, Camera.main, p0, R_CIRLE_R * getPixelSize());
		if(!reference) {
			pos = on.OffsetAt(value.value, 20f * getPixelSize()).Eval();
		} else {
			pos = on.PointOn(value.value).Eval();
		}
		ref_points[0] = ref_points[1] = sketch.plane.ToPlane(p0);
		//on.DrawExtend(canvas, value.value, 0.05);
	}

	protected override Matrix4x4 OnGetBasis() {
		var p0 = point.PointExpInPlane(sketch.plane).Eval();
		if(!sketch.is3d) p0.z = 0;
		return getPlane().GetTransform() * Matrix4x4.Translate(p0);
	}

	public override double LabelToValue(double label) {
		switch(on.type) {
			//case IEntityType.Arc:
			//case IEntityType.Circle:
			case IEntityType.Helix:
				return label / 180.0 * Math.PI;
		}
		return base.LabelToValue(label);
	}

	public override double ValueToLabel(double value) {
		switch(on.type) {
			//case IEntityType.Arc:
			//case IEntityType.Circle:
			case IEntityType.Helix:
				return value * 180.0 / Math.PI;
		}
		return base.ValueToLabel(value);
	}
}
