using System.Collections.Generic;
using UnityEngine;

public class PointsDistance : ValueConstraint {

	public PointEntity p0 { get; private set; }
	public PointEntity p1 { get; private set; }

	public PointsDistance(Sketch sk, PointEntity p0, PointEntity p1) : base(sk) {
		this.p0 = p0;
		this.p1 = p1;
		value.value = (p0.GetPosition() - p1.GetPosition()).magnitude;
	}

	public override IEnumerable<Exp> equations {
		get {
			yield return (p1.exp - p0.exp).Magnitude() - value.exp;
		}
	}

	protected override void OnDraw(LineCanvas canvas) {
		var p0p = p0.GetPosition();
		var p1p = p1.GetPosition();
		var dir = p1p - p0p;
		var perp = Vector3.Cross(dir, Vector3.forward).normalized;
		var p2 = p0p + perp * 4f;
		var p3 = p1p + perp * 4f;
		var m = perp;
		canvas.DrawLine(p0p, p2 + m);
		canvas.DrawLine(p1p, p3 + m);
		canvas.DrawLine(p2, p3);
	}

	protected override bool IsChanged() {
		return p0.IsChanged() || p1.IsChanged();
	}
}
