using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcEntity : Entity, ISegmentaryEntity {

	public PointEntity p0;
	public PointEntity p1;
	public PointEntity c;

	public ArcEntity(Sketch sk) : base(sk) {
		p0 = AddChild(new PointEntity(sk));
		p1 = AddChild(new PointEntity(sk));
		c = AddChild(new PointEntity(sk));
	}

	public override IEntityType type { get { return IEntityType.Arc; } }

	public override IEnumerable<Exp> equations {
		get {
			if(!p0.IsCoincidentWith(p1)) {
				yield return (p0.exp - c.exp).Magnitude() - (p1.exp - c.exp).Magnitude();
			}
		}
	}

	public override IEnumerable<PointEntity> points {
		get {
			yield return p0;
			yield return p1;
			yield return c;
		}
	}

	public override bool IsChanged() {
		return p0.IsChanged() || p1.IsChanged() || c.IsChanged();
	}

	public Exp GetAngleExp() {
		if(!p0.IsCoincidentWith(p1)) {
			var d0 = p0.exp - c.exp;
			var d1 = p1.exp - c.exp;
			return ConstraintExp.angle2d(d0, d1, angle360: true);
		}
		return Math.PI * 2.0;
	}

	public double GetAngle() {
		var angle = GeomUtils.GetAngle(p0.pos - c.pos, p1.pos - c.pos);
		if(angle <= 0f) angle += 2f * Mathf.PI;
		return angle;
	}

	public PointEntity begin { get { return p0; } }
	public PointEntity end { get { return p1; } }
	public PointEntity center { get { return c; } }
	public IEnumerable<Vector3> segmentPoints {
		get {
			float angle = (float)GetAngle() * Mathf.Rad2Deg;
			var cp = c.pos;
			var rv = p0.pos - cp;
			int subdiv = Math.Max(Math.Abs((int)Mathf.Ceil(angle / 10f)), 1);
			if(subdiv == 0) yield break;
			var vz = Vector3.forward;
			var rot = Quaternion.AngleAxis(angle / subdiv, vz);
			for(int i = 0; i <= subdiv; i++) {
				yield return rv + cp;
				rv = rot * rv;
			}
		}
	}	

	public double radius {
		get {
			return (p1.pos - c.pos).magnitude;
		}
	}

	public Exp radiusExp {
		get {
			return (p0.exp - c.exp).Magnitude();
		}
	}

	public override BBox bbox { get { return new BBox(center.pos, (float)radius); } }

	protected override Entity OnSplit(Vector3 position) {
		var part = new ArcEntity(sketch);
		part.center.pos = center.pos;
		part.p1.pos = p1.pos;
		p1.pos = position;
		part.p0.pos = p1.pos;
		return part;
	}

	/*
	protected override double OnSelect(Vector3 mouse, Camera camera, Matrix4x4 tf) {
		float angle = GetAngle() * Mathf.Rad2Deg;
		Debug.Log(angle);
		var cp = c.pos;
		var rv = p0.pos - cp;
		int subdiv = (int)Math.Ceiling(angle / 30);
		var vz = Vector3.forward;
		var rot = Quaternion.AngleAxis(angle / (subdiv - 1), vz);
		var prev = Vector3.zero;
		double min = -1;
		for(int i = 0; i < subdiv; i++) {
			var pos =  camera.WorldToScreenPoint(tf.MultiplyPoint(rv + cp));
			if(i > 0) {
				var dist = GeomUtils.DistancePointSegment2D(mouse, prev, pos);
				if(min > 0 && dist > min) continue;
				min = dist;
			}
			prev = pos;
			rv = rot * rv;
		}
		return min;
	}
	*/

	public override ExpVector PointOn(Exp t) {
		var angle = GetAngleExp();
		var cos = Exp.Cos(angle * t);
		var sin = Exp.Sin(angle * t);
		var rv = p0.exp - c.exp;

		return c.exp + new ExpVector(
			cos * rv.x - sin * rv.y, 
			sin * rv.x + cos * rv.y, 
			0.0
		);
	}

	public override ExpVector TangentAt(Exp t) {
		var angle = GetAngleExp();
		var cos = Exp.Cos(angle * t + Math.PI / 2);
		var sin = Exp.Sin(angle * t + Math.PI / 2);
		var rv = p0.exp - c.exp;

		return new ExpVector(
			cos * rv.x - sin * rv.y, 
			sin * rv.x + cos * rv.y, 
			0.0
		);
	}
	
	public override Exp Length() {
		return GetAngleExp() * Radius();
	}

	public override Exp Radius() {
		return (p0.exp - c.exp).Magnitude();
	}

	public override ExpVector Center() {
		return c.exp;
	}

}
