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
		Vector3 p0p = p0.GetPosition();
		Vector3 p1p = p1.GetPosition();
		Vector3 dir = p1p - p0p;
		Vector3 perp = Vector3.Cross(dir, Vector3.forward).normalized;
		float dist = Vector3.Dot(position - p0p, perp);
		Vector3 p2 = p0p + perp * dist;
		Vector3 p3 = p1p + perp * dist;
		float s = Mathf.Sign(dist);
		Vector3 m = perp * s;
		canvas.DrawLine(p0p, p2 + m);
		canvas.DrawLine(p1p, p3 + m);
		canvas.DrawLine(p2, p3);

		var b = GetBasis();
		var p = b.MultiplyPoint(Vector3.zero);
		canvas.DrawLine(p, b.MultiplyPoint(Vector3.left));
		canvas.DrawLine(p, b.MultiplyPoint(Vector3.up));
	}

	protected override bool OnIsChanged() {
		return p0.IsChanged() || p1.IsChanged();
	}

	protected override Matrix4x4 OnGetBasis() {
		var pos = (p0.GetPosition() + p1.GetPosition()) * 0.5f;
		var dir = p0.GetPosition() - p1.GetPosition();
		var ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
		var rot = Quaternion.AngleAxis(ang, Vector3.forward);
		return Matrix4x4.TRS(pos, rot, Vector3.one); 
	}
}
