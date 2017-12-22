using System.Collections.Generic;
using UnityEngine;

public class PointsDistance : ValueConstraint {

	public PointEntity p0 { get { return GetEntity(0) as PointEntity; } set { SetEntity(0, value); } }
	public PointEntity p1 { get { return GetEntity(1) as PointEntity; } set { SetEntity(1, value); } }

	public PointsDistance(Sketch sk) : base(sk) { }

	public PointsDistance(Sketch sk, PointEntity p0, PointEntity p1) : base(sk) {
		AddEntity(p0);
		AddEntity(p1);
		Satisfy();
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
		float dist = Vector3.Dot(pos - p0p, perp);
		
		Vector3 p2 = p0p + perp * dist;
		Vector3 p3 = p1p + perp * dist;
		
		float s = Mathf.Sign(dist);
		Vector3 m = perp * s;
		canvas.DrawLine(p0p, p2 + m);
		canvas.DrawLine(p1p, p3 + m);

		float len = Vector3.Dot(pos - p0p, dir) / Vector3.Dot(dir, dir);
		Vector3 u = dir.normalized;
		Vector3 v = perp.normalized;

		var a = (len < 0f || len > 1f) ? -1.0f : 1.0f;

		canvas.DrawLine(p2, p2 + a * u * 1.5f + v * 0.5f);
		canvas.DrawLine(p2, p2 + a * u * 1.5f - v * 0.5f);
		canvas.DrawLine(p3, p3 - a * u * 1.5f + v * 0.5f);
		canvas.DrawLine(p3, p3 - a * u * 1.5f - v * 0.5f);

		if(len < 0f) {
			p2 += dir * len;
			p3 += u * 2.0f; 
		}
		if(len > 1f) {
			p3 += dir * (len - 1f);
			p2 -= u * 2.0f;
		}
		canvas.DrawLine(p2, p3);
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
