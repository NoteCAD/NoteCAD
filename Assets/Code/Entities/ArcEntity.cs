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
			yield return (p0.exp - c.exp).Magnitude() - (p1.exp - c.exp).Magnitude();
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

	public float GetAngle() {
		var angle = GeomUtils.GetAngle(p0.pos - c.pos, p1.pos - c.pos);
		if(angle == 0f) angle = 2f * Mathf.PI;
		if(angle < 0f) angle += 2f * Mathf.PI;
		return angle;
	}

	public PointEntity begin { get { return p0; } }
	public PointEntity end { get { return p1; } }
	public PointEntity center { get { return c; } }
	public IEnumerable<Vector3> segmentPoints {
		get {
			float angle = GetAngle() * Mathf.Rad2Deg;
			var cp = c.pos;
			var rv = p0.pos - cp;
			int subdiv = (int)Mathf.Ceil(angle / 10f);
			var vz = Vector3.forward;
			var rot = Quaternion.AngleAxis(angle / (subdiv - 1), vz);
			for(int i = 0; i < subdiv; i++) {
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
		var cos = Exp.Cos(t);
		var sin = Exp.Sin(t);
		var rv = p0.exp - c.exp;

		return c.exp + new ExpVector(
			cos * rv.x - sin * rv.y, 
			sin * rv.x + cos * rv.y, 
			0.0
		);
	}
}
