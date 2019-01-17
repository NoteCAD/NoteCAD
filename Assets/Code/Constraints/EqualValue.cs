using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

[Serializable]
public class EqualValue : ValueConstraint {

	public EqualValue(Sketch sk) : base(sk) {
	}

	protected override bool OnSatisfy() {
		var c0 = GetConstraint(0) as ValueConstraint;
		var c1 = GetConstraint(1) as ValueConstraint;
		if(Math.Sign(c0.GetValue()) != Math.Sign(c1.GetValue())) {
			value.value = -1;
		}
		return true;
	}

	public EqualValue(Sketch sk, ValueConstraint c0, ValueConstraint c1) : base(sk) {
		AddConstraint(c0);
		AddConstraint(c1);
		value.value = 1.0;
		Satisfy();
	}

	public override IEnumerable<Exp> equations {
		get {
			var c0 = GetConstraint(0) as ValueConstraint;
			var c1 = GetConstraint(1) as ValueConstraint;
			yield return c0.GetValueParam().exp - c1.GetValueParam().exp * value;
		}
	}

	void DrawStroke(LineCanvas canvas, ValueConstraint c, int rpt) {

		ref_points[rpt] = c.pos;
		if(rpt == 0) {
			Vector3 up = c.GetBasis().GetColumn(1);
			pos = c.pos + up.normalized * getPixelSize() * 30f;
		}
	}

	protected override void OnDraw(LineCanvas canvas) {
		DrawStroke(canvas, GetConstraint(0) as ValueConstraint, 0);
		DrawStroke(canvas, GetConstraint(1) as ValueConstraint, 1);
		
		if(DetailEditor.instance.hovered == this) {
			DrawReferenceLink(canvas, Camera.main);
		}
	}

	protected override string OnGetLabelValue() {
		return base.OnGetLabelValue() + ":1";
	}

	protected override Matrix4x4 OnGetBasis() {
		return sketch.plane.GetTransform();
	}
}
