using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

[Serializable]
public class EllipseEntity : Entity, ILoopEntity {

	public PointEntity c;
	public Param r0 = new Param("r0");
	public Param r1 = new Param("r1");
	ExpBasis2d basis = new ExpBasis2d();

	public override IEntityType type { get { return IEntityType.Ellipse; } }

	public EllipseEntity(Sketch sk) : base(sk) {
		c = AddChild(new PointEntity(sk));
		basis.SetPosParams(c.x, c.y);
	}

	public double radius0 { get { return r0.value; } set { r0.value = value; } } 
	public double radius1 { get { return r1.value; } set { r1.value = value; } } 

	public override IEnumerable<PointEntity> points {
		get {
			yield return c;
		}
	}

	public override bool IsChanged() {
		return c.IsChanged() || r0.changed || r1.changed;
	}

	public override IEnumerable<Param> parameters {
		get {
			yield return r0;
			yield return r1;
			foreach(var p in basis.parameters) yield return p;
		}
	}

	public override IEnumerable<Exp> equations {
		get {
			foreach(var e in basis.equations) yield return e;
		}
	}

	public PointEntity center { get { return c; } }
	public IEnumerable<Vector3> loopPoints {
		get {
			return getSegmentsUsingPointOn(36);
		}
	}

	public override bool IsCrossed(Entity e, ref Vector3 itr) {
		return false;
	}

	protected override void OnWrite(XmlTextWriter xml) {
		xml.WriteAttributeString("r0", Math.Abs(r0.value).ToStr());
		xml.WriteAttributeString("r1", Math.Abs(r1.value).ToStr());
		xml.WriteAttributeString("basis", basis.ToString());
	}

	protected override void OnRead(XmlNode xml) {
		r0.value = xml.Attributes["r0"].Value.ToDouble();
		r1.value = xml.Attributes["r1"].Value.ToDouble();
		basis.FromString(xml.Attributes["basis"].Value);
	}

	public override ExpVector PointOn(Exp t) {
		var angle = t * 2.0 * Math.PI;
		return basis.TransformPosition(new ExpVector(Exp.Cos(angle) * Exp.Abs(r0), Exp.Sin(angle) * Exp.Abs(r1), 0.0));
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
