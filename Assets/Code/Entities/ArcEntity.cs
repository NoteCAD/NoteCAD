using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcEntity : Entity, ISegmentaryEntity {

	public PointEntity p0;
	public PointEntity p1;
	public PointEntity c;

	LineBehaviour behaviour;

	public ArcEntity(Sketch sk) : base(sk) {
		p0 = AddChild(new PointEntity(sk));
		p1 = AddChild(new PointEntity(sk));
		c = AddChild(new PointEntity(sk));
		behaviour = GameObject.Instantiate(EntityConfig.instance.linePrefab);
		behaviour.entity = this;
		behaviour.LateUpdate();
	}

	protected override GameObject gameObject {
		get {
			return behaviour.gameObject;
		}
	}

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
			int subdiv = 32;
			var vz = Vector3.forward;
			var rot = Quaternion.AngleAxis(angle / (subdiv - 1), vz);
			for(int i = 0; i < subdiv; i++) {
				yield return rv + cp;
				rv = rot * rv;
			}
		}
	}

	protected override void OnDraw(LineCanvas canvas) {
		Vector3 prev = Vector3.zero;
		bool hasPrev = false;
		foreach(var p in segmentPoints) {
			if(hasPrev) {
				canvas.DrawLine(prev, p);
			}
			prev = p;
			hasPrev = true;
		}
	}

	public double radius {
		get {
			return (p1.pos - c.pos).magnitude;
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

}
