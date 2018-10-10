using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Diameter : ValueConstraint {

	public CircleEntity circle { get { return GetEntity(0) as CircleEntity; } set { SetEntity(0, value); } }

	public Exp radius { get { return circle.radius; } }

	public Diameter(Sketch sk) : base(sk) { }

	public Diameter(Sketch sk, CircleEntity c) : base(sk) {
		AddEntity(c);
		Satisfy();
	}

	public override IEnumerable<Exp> equations {
		get {
			yield return radius * 2.0 - value.exp;
		}
	}

	protected override void OnDraw(LineCanvas canvas) {
		var pl = getPlane();
		var p = pl.FromPlane(circle.center.pos);
		var lo = getLabelOffset();
		var dir = (lo - p).normalized;
		
		float r = (float)value.exp.Eval() / 2f;
		drawPointsDistance(p - dir * r, p + dir * r, canvas, Camera.main, false);
		
		//drawLabel(renderer, camera, "Ø" + getValueString());

	}

	protected override Matrix4x4 OnGetBasis() {
		return getPlane().GetTransform() * Matrix4x4.Translate(circle.center.pos);
	}

}
