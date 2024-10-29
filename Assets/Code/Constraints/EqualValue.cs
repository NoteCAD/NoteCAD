using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

[Serializable]
public class EqualValue : ValueConstraint {

	public override bool IsDimension { get { return false; } }

	public EqualValue(Sketch sk) : base(sk) { }
	public EqualValue(Sketch sk, Id id) : base(sk, id) { }

	protected override bool OnSatisfy() {
		var c0 = GetConstraint(0) as ValueConstraint;
		var c1 = GetConstraint(1) as ValueConstraint;
		if(Math.Sign(c0.GetValue()) != Math.Sign(c1.GetValue())) {
			valueParam.value = -1;
		}
		return true;
	}

	public EqualValue(Sketch sk, ValueConstraint c0, ValueConstraint c1) : base(sk) {
		AddConstraint(c0);
		AddConstraint(c1);
		valueParam.value = 1.0;
		var c = GetConstraint(0) as ValueConstraint;
		Vector3 up = c.GetBasis().GetColumn(1);
		pos = c.pos + up.normalized * getPixelSize() * 30f;
		Satisfy();
	}

	protected override IEnumerable<Exp> constraintEquations	 {
		get {
			var c0 = GetConstraint(0) as ValueConstraint;
			var c1 = GetConstraint(1) as ValueConstraint;
			yield return c0.GetValueExp() - c1.GetValueExp() * value;
		}
	}

	public override ValueUnits units => ValueUnits.FRACTION;

	void DrawStroke(ICanvas canvas, ValueConstraint c, int rpt) {
		ref_points[rpt] = c.pos;
	}

	protected override void OnDraw(ICanvas canvas) {
		DrawStroke(canvas, GetConstraint(0) as ValueConstraint, 0);
		DrawStroke(canvas, GetConstraint(1) as ValueConstraint, 1);
		
		if(shouldDrawLink) {
			DrawReferenceLink(canvas, Camera.main);
		}
	}

	protected override string OnGetLabelValue() {
		return base.OnGetLabelValue() + ":1";
	}

	protected override Matrix4x4 OnGetBasis() {
		return (GetConstraint(0) as ValueConstraint).GetBasis();
	}
}
