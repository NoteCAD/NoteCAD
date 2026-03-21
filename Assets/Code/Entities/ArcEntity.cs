using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NoteCAD;

[Serializable]
public class ArcEntity : Entity, ISegmentaryEntity {

	[NonSerialized]
	public PointEntity p0;

	[NonSerialized]
	public PointEntity p1;

	[NonSerialized]
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

	public IEnumerable<IEnumerable<Vector3>> segmentPoints {
		get {
			yield return segmentPts;
		}
	}

	public IEnumerable<Vector3> segmentPts {
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

	public override OBB? obb {
		get {
			var p0v = new Vector2(p0.pos.x, p0.pos.y);
			var p1v = new Vector2(p1.pos.x, p1.pos.y);
			var cv  = new Vector2(c.pos.x,  c.pos.y);
			float r = (float)radius;

			Vector2 chord = p1v - p0v;
			float chordLen = chord.magnitude;

			if(chordLen < 1e-6f) {
				// Full circle or degenerate: OBB is the circle's bounding square.
				return new OBB(cv, Vector2.right, r, r);
			}

			Vector2 axisX = chord / chordLen;
			Vector2 axisY = new Vector2(-axisX.y, axisX.x);

			float a0angle = Mathf.Atan2(p0v.y - cv.y, p0v.x - cv.x);
			// The arc always sweeps counter-clockwise (see ArcEntity.segmentPts / GetAngle).
			float arcAngle = (float)GetAngle();

			// Project both endpoints explicitly (math guarantees p0→Y=0, p1→Y=0).
			float minX = Mathf.Min(Vector2.Dot(p0v - p0v, axisX), Vector2.Dot(p1v - p0v, axisX));
			float maxX = Mathf.Max(Vector2.Dot(p0v - p0v, axisX), Vector2.Dot(p1v - p0v, axisX));
			float minY = Mathf.Min(Vector2.Dot(p0v - p0v, axisY), Vector2.Dot(p1v - p0v, axisY));
			float maxY = Mathf.Max(Vector2.Dot(p0v - p0v, axisY), Vector2.Dot(p1v - p0v, axisY));

			// Extremal angles along axisX are at angle α and α+π;
			// extremal angles along axisY are at α+π/2 and α+3π/2,
			// where α is the world-space angle of axisX.
			float alpha = Mathf.Atan2(axisX.y, axisX.x);
			float[] extremalAngles = {
				alpha,
				alpha + Mathf.PI,
				alpha + Mathf.PI / 2f,
				alpha + 3f * Mathf.PI / 2f
			};
			foreach(float angle in extremalAngles) {
				// Mathf.Repeat maps the signed difference into [0, 2π) for the CCW arc span check.
				float diff = Mathf.Repeat(angle - a0angle, 2f * Mathf.PI);
				if(diff < arcAngle) {
					var pt = cv + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
					float px = Vector2.Dot(pt - p0v, axisX);
					float py = Vector2.Dot(pt - p0v, axisY);
					if(px < minX) minX = px;
					if(px > maxX) maxX = px;
					if(py < minY) minY = py;
					if(py > maxY) maxY = py;
				}
			}

			var obbCenter = p0v + axisX * ((minX + maxX) / 2f) + axisY * ((minY + maxY) / 2f);
			return new OBB(obbCenter, axisX, (maxX - minX) / 2f, (maxY - minY) / 2f);
		}
	}

	protected override Entity OnSplit(Vector3 position) {
		var part = new ArcEntity(sketch);
		part.center.pos = center.pos;
		part.p1.pos = p1.pos;
		p1.pos = position;
		part.p0.pos = p1.pos;
		return part;
	}

	public override double FindParameter(Vector3 pos, int subdiv) {
		var toP0 = p0.pos - c.pos;
		var toPos = pos - c.pos;
		float arcAngle = (float)GetAngle();
		if(arcAngle < 1e-6f) return 0.0;
		float angleToPos = GeomUtils.GetAngle(toP0, toPos);
		if(angleToPos < 0f) angleToPos += 2f * Mathf.PI;
		return (double)Mathf.Clamp01(angleToPos / arcAngle);
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
