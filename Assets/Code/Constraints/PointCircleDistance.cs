using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class PointCircleDistance : ValueConstraint {

	public PointCircleDistance(Sketch sk) : base(sk) { }

	public PointCircleDistance(Sketch sk, IEntity pt, IEntity c) : base(sk) {
		AddEntity(pt);
		AddEntity(c);
		Satisfy();
	}

	public override IEnumerable<Exp> equations {
		get {
			var point = GetEntity(0);
			var circle = GetEntity(1);
			var pPos = point.GetPointAtInPlane(0, getPlane());
			var cCen = circle.Center();
			var cRad = circle.Radius();

			yield return (pPos - cCen).Magnitude() - cRad - value.exp;
		}
	}
	
	protected override void OnDraw(LineCanvas canvas) {
		var point = GetEntity(0);
		var circle = GetEntity(1);
		var pPos = point.GetPointAtInPlane(0, null).Eval();
		var cCen = circle.CenterInPlane(null).Eval();
		var cRad = (float)circle.Radius().Eval();
		var dir = (pPos - cCen).normalized;
		drawPointsDistance(pPos, cCen + dir * cRad, canvas, Camera.main, false, true, true, 0);
	}

	protected override Matrix4x4 OnGetBasis() {
		var point = GetEntity(0);
		var circle = GetEntity(1);
		var pPos = point.GetPointAtInPlane(0, null).Eval();
		var cCen = circle.CenterInPlane(null).Eval();
		return getPointsDistanceBasis(pPos, cCen, sketch.plane);
	}

}
