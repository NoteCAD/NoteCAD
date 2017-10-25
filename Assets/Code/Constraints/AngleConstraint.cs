using System;
using System.Collections.Generic;
using UnityEngine;

public class AngleConstraint : ValueConstraint {

	public LineEntity l0 { get; private set; }
	public LineEntity l1 { get; private set; }

	public AngleConstraint(Sketch sk) : base(sk) { }

	public AngleConstraint(Sketch sk, LineEntity l0, LineEntity l1) : base(sk) {
		this.l0 = AddEntity(l0);
		this.l1 = AddEntity(l1);
		Satisfy();
	}

	public override IEnumerable<Exp> equations {
		get {
			var p = GetPoints();
			ExpVector d0 = p[0].exp - p[1].exp;
			ExpVector d1 = p[3].exp - p[2].exp;
			Exp du = d1.x * d0.x + d1.y * d0.y;
			Exp dv = d0.x * d1.y - d0.y * d1.x;
			yield return Exp.Atan2(dv, du) - value;
		}
	}

	PointEntity[] GetPoints() {
		if(l0.p0.IsCoincidentWith(l1.p0)) return new PointEntity[4] { l0.p1, l0.p0, l1.p0, l1.p1 };
		if(l0.p0.IsCoincidentWith(l1.p1)) return new PointEntity[4] { l0.p1, l0.p0, l1.p1, l1.p0 };
		if(l0.p1.IsCoincidentWith(l1.p0)) return new PointEntity[4] { l0.p0, l0.p1, l1.p0, l1.p1 };
		if(l0.p1.IsCoincidentWith(l1.p1)) return new PointEntity[4] { l0.p0, l0.p1, l1.p1, l1.p0 };
		return new PointEntity[4] { l0.p0, l0.p1, l1.p0, l1.p1 };
	}

	protected override void OnDraw(LineCanvas canvas) {
		/*
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

		float len = Vector3.Dot(position - p0p, dir) / Vector3.Dot(dir, dir);
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
		*/
	}

	protected override bool OnIsChanged() {
		return l0.IsChanged() || l1.IsChanged();
	}
	
	protected override Matrix4x4 OnGetBasis() {
		var points = GetPoints();
		var pos = points[1].GetPosition();
		var dir = l0.p0.GetPosition() - l0.p1.GetPosition();
		var ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
		var rot = Quaternion.AngleAxis(ang, Vector3.forward);
		return Matrix4x4.TRS(pos, rot, Vector3.one); 
	}

	public override double LabelToValue(double label) {
		return label * Math.PI / 180.0;
	}

	public override double ValueToLabel(double value) {
		return value / Math.PI * 180.0;
	}

	protected override void OnRead(System.Xml.XmlNode xml) {
		l0 = GetEntity(0) as LineEntity;
		l1 = GetEntity(1) as LineEntity;
	}

}
