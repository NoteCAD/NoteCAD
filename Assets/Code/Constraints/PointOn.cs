using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class PointOn : ValueConstraint {

	public IEntity point { get { return GetEntity(0); } set { SetEntity(0, value); } }
	public IEntity on { get { return GetEntity(1); } set { SetEntity(1, value); } }

	public ExpVector pointExp { get { return point.PointExpInPlane(sketch.plane); } }
	public Vector3 pointPos { get { return point.PointExpInPlane(null).Eval(); } }
	public override bool valueVisible { get { return !reference; } }

	public PointOn(Sketch sk) : base(sk) { }

	public PointOn(Sketch sk, IEntity point, IEntity on) : base(sk) {
		reference = true;
		AddEntity(point);
		AddEntity(on);
		SetValue(0.5);
		Satisfy();
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
		
		//var lip0 = lineP0Pos;
		//var lip1 = lineP1Pos;
		var p0 = pointPos;

		drawCameraCircle(canvas, Camera.main, p0, R_CIRLE_R * getPixelSize()); 
		//drawLineExtendInPlane(sketch.plane, canvas, lip0, lip1, p0, 6f * getPixelSize());
	}

	protected override Matrix4x4 OnGetBasis() {
		var p0 = point.PointExpInPlane(sketch.plane).Eval();
		if(!sketch.is3d) p0.z = 0;
		return getPlane().GetTransform() * Matrix4x4.Translate(p0);
	}

	public override double LabelToValue(double label) {
		if(on.type == IEntityType.Arc || on.type == IEntityType.Circle) {
			return label / 180.0 * Math.PI;
		}
		return base.LabelToValue(label);
	}

	public override double ValueToLabel(double value) {
		if(on.type == IEntityType.Arc || on.type == IEntityType.Circle) {
			return value * 180.0 / Math.PI;
		}
		return base.ValueToLabel(value);
	}
}
