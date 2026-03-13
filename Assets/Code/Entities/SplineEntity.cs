using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using NoteCAD;

[Serializable]
public class SplineEntity : Entity, ISegmentaryEntity {

	[NonSerialized]
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
	public IEnumerable<IEnumerable<Vector3>> segmentPoints {
		get {
			/*var e = PointOn(1.0);
			var box = bbox;
			var d = box.max - box.min;
			var eps = d.magnitude * 0.002;
			return subdivision(0.0, 1.0, PointOn(0.0), PointOn(1.0), (float)eps).Concat(Enumerable.Repeat(e, 1));
			*/
			yield return getSegments(32, t => PointOn(t));
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
		protected override Entity OnSplit(Vector3 position) {
		double t = FindParameter(position);
		var part = new SplineEntity(sketch);
		var p01 = Vector3.Lerp(p[0].pos, p[1].pos, (float)t);
		var p12 = Vector3.Lerp(p[1].pos, p[2].pos, (float)t);
		var p23 = Vector3.Lerp(p[2].pos, p[3].pos, (float)t);
		var p012 = Vector3.Lerp(p01, p12, (float)t);
		var p123 = Vector3.Lerp(p12, p23, (float)t);
		var p0123 = Vector3.Lerp(p012, p123, (float)t);
		part.p[0].pos = p0123;
		part.p[1].pos = p123;
		part.p[2].pos = p23;
		part.p[3].pos = p[3].pos;
		p[1].pos = p01;
		p[2].pos = p012;
		p[3].pos = p0123;
		return part;
	}

	public override double FindParameter(Vector3 pos) {
		int steps = 32;
		double best_t = 0.0;
		double best_dist = double.MaxValue;
		for(int i = 0; i <= steps; i++) {
			double t = (double)i / steps;
			var pt = PointOn(t);
			var d = (pt - pos).sqrMagnitude;
			if(d < best_dist) {
				best_dist = d;
				best_t = t;
			}
		}
		double lo = System.Math.Max(0.0, best_t - 1.0 / steps);
		double hi = System.Math.Min(1.0, best_t + 1.0 / steps);
		for(int iter = 0; iter < 20; iter++) {
			double tl = lo + (hi - lo) / 3.0;
			double tr = lo + 2.0 * (hi - lo) / 3.0;
			var pl = PointOn(tl);
			var pr = PointOn(tr);
			if((pl - pos).sqrMagnitude < (pr - pos).sqrMagnitude) hi = tr;
			else lo = tl;
		}
		return (lo + hi) / 2.0;
	}
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
