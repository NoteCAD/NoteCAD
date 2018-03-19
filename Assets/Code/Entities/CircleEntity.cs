using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class CircleEntity : Entity, ILoopEntity {

	public PointEntity c;
	public Param radius = new Param("r");

	public override IEntityType type { get { return IEntityType.Circle; } }

	public CircleEntity(Sketch sk) : base(sk) {
		c = AddChild(new PointEntity(sk));
	}

	public override IEnumerable<PointEntity> points {
		get {
			yield return c;
		}
	}

	public override bool IsChanged() {
		return c.IsChanged() || radius.changed;
	}

	public override IEnumerable<Param> parameters {
		get {
			yield return radius;
		}
	}

	public PointEntity center { get { return c; } }
	public IEnumerable<Vector3> loopPoints {
		get {
			float angle = 360;
			var cp = center.pos;
			var rv = Vector3.left * (float)radius.value;
			int subdiv = 32;
			var vz = Vector3.forward;
			for(int i = 0; i < subdiv; i++) {
				var nrv = Quaternion.AngleAxis(angle / (subdiv - 1) * i, vz) * rv;
				yield return nrv + cp;
			}
		}
	}

	public override bool IsCrossed(Entity e, ref Vector3 itr) {
		return false;
	}

	protected override void OnWrite(XmlTextWriter xml) {
		xml.WriteAttributeString("r", radius.value.ToStr());
	}

	protected override void OnRead(XmlNode xml) {
		radius.value = xml.Attributes["r"].Value.ToDouble();
	}
}
