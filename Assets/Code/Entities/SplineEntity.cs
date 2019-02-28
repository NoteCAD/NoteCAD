using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SplineEntity : Entity, ISegmentaryEntity {

	public PointEntity[] p = new PointEntity[4];

	public SplineEntity(Sketch sk) : base(sk) {
		for(int i = 0; i < p.Length; i++) {
			p[i] = AddChild(new PointEntity(sk));
		}
	}

	public override IEntityType type { get { return IEntityType.Spline; } }

	public override IEnumerable<PointEntity> points {
		get {
			for(int i = 0; i < p.Length; i++) {
				yield return p[i];
			}
		}
	}

	public override bool IsChanged() {
		return p.Any(po => po.IsChanged());
	}

	public PointEntity begin { get { return p[0]; } }
	public PointEntity end { get { return p[3]; } }
	public IEnumerable<Vector3> segmentPoints {
		get {
			/*var e = PointOn(1.0);
			var box = bbox;
			var d = box.max - box.min;
			var eps = d.magnitude * 0.002;
			return subdivision(0.0, 1.0, PointOn(0.0), PointOn(1.0), (float)eps).Concat(Enumerable.Repeat(e, 1));
			*/
			return getSegments(32, t => PointOn(t));
		}
	}
	
	IEnumerable<Vector3> subdivision(double t0, double t1, Vector3 p0, Vector3 p1, float eps) {
		if(p0 == p1) yield break;
		double t = (t0 + t1) / 2.0;
		var p = PointOn(t);
		if(Math.Abs(GeomUtils.DistancePointLine2D(p, p0, p1)) < eps && (t0 != 0.0 || t1 != 1.0)) {
			yield return p0;
			yield return p;
		} else {
			for(var e = subdivision(t0, t, p0, p, eps).GetEnumerator(); e.MoveNext(); ) yield return e.Current;
			for(var e = subdivision(t, t1, p, p1, eps).GetEnumerator(); e.MoveNext(); ) yield return e.Current;
		}
	}
	
	public override BBox bbox {
		get {
			var box = new BBox(p[0].pos, p[1].pos);
			box.Include(p[2].pos);
			box.Include(p[3].pos);
			return box;
		}
	}
	/*
	protected override Entity OnSplit(Vector3 position) {
		var part = new ArcEntity(sketch);
		part.center.pos = center.pos;
		part.p1.pos = p1.pos;
		p1.pos = position;
		part.p0.pos = p1.pos;
		return part;
	}
	*/
	public override ExpVector PointOn(Exp t) {
		var p0 = p[0].exp;
		var p1 = p[1].exp;
		var p2 = p[2].exp;
		var p3 = p[3].exp;
		var t2 = t * t;
		var t3 = t2 * t;
		return p1 * (3.0 * t3 - 6.0 * t2 + 3.0 * t) + p3 * t3 + p2 * (3.0 * t2 - 3.0 * t3) - p0 * (t3 - 3.0 * t2 + 3.0 * t - 1.0);
	}

	public Vector3 PointOn(double t) {
		var p0 = p[0].pos;
		var p1 = p[1].pos;
		var p2 = p[2].pos;
		var p3 = p[3].pos;
		var t2 = t * t;
		var t3 = t2 * t;
		return p1 * (float)(3.0 * t3 - 6.0 * t2 + 3.0 * t) + p3 * (float)t3 + p2 * (float)(3.0 * t2 - 3.0 * t3) - p0 * (float)(t3 - 3.0 * t2 + 3.0 * t - 1.0);
	}

	public override Exp Length() {
		return null;
	}

	public override Exp Radius() {
		return null;
	}

	public override ExpVector Center() {
		return null;
	}

}
