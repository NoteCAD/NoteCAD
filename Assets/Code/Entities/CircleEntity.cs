using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using NoteCAD;

[Serializable]
public class CircleEntity : Entity, ILoopEntity {

	[NonSerialized]
	public PointEntity c;

	public Param r = new Param("r");
	Param a = new Param("a");
	
	bool angleFixed_ = true;
	public bool angleFixed {
		get {
			return angleFixed_;
		}
		set {
			angleFixed_ = value;
			sketch.MarkDirtySketch(constraints:true);
		}
	}

	public override IEntityType type { get { return IEntityType.Circle; } }

	public CircleEntity(Sketch sk) : base(sk) {
		c = AddChild(new PointEntity(sk));
	}

	public double radius { get { return r.value; } set { r.value = value; } } 
	public double angle { get { return a.value / Math.PI * 180.0; } set { a.value = value / 180.0 * Math.PI; } } 

	public override IEnumerable<PointEntity> points {
		get {
			yield return c;
		}
	}

	public override bool IsChanged() {
		return c.IsChanged() || r.changed;
	}

	public override IEnumerable<Param> parameters {
		get {
			yield return r;
			if(!angleFixed) yield return a;
		}
	}

	public PointEntity center { get { return c; } }
	
	IEnumerable<Vector3> CirclePoints() {
		var cp = center.pos;
		var rv = Vector3.left * Mathf.Abs((float)r.value);
		int subdiv = 36;
		var vz = Vector3.forward;
		for(int i = 0; i < subdiv; i++) {
			var nrv = Quaternion.AngleAxis((float)a.value + 360.0f / (subdiv - 1) * i, vz) * rv;
			yield return nrv + cp;
		}
	}

	public IEnumerable<IEnumerable<Vector3>> loopPoints {
		get {
			yield return CirclePoints();
		}
	}

	public IEnumerable<IEnumerable<Vector3>> segmentPoints =>
		loopPoints.Select(loop => loop.Concat(loop.Take(1)));

	protected override void OnWrite(Writer xml) {
		xml.WriteAttribute("r", Math.Abs(r.value));
		if(a.value != 0.0) xml.WriteAttribute("a", a.value);
		if(angleFixed_ == false) xml.WriteAttribute("angleFixed", angleFixed_);
	}

	protected override void OnRead(XmlNode xml) {
		r.value = xml.Attributes["r"].Value.ToDouble();
		if(xml.Attributes["a"] != null) a.value = xml.Attributes["a"].Value.ToDouble();
		if(xml.Attributes["angleFixed"] != null) angleFixed_ = Convert.ToBoolean(xml.Attributes["angleFixed"].Value);
	}

	public override ExpVector PointOn(Exp t) {
		var ang = t * 2.0 * Math.PI + a.exp;
		return c.exp + new ExpVector(Exp.Cos(ang), Exp.Sin(ang), 0.0) * Radius();
	}

	public override ExpVector TangentAt(Exp t) {
		var ang = t * 2.0 * Math.PI + a.exp;
		return new ExpVector(-Exp.Sin(ang), Exp.Cos(ang), 0.0);
	}

	public override Exp Length() {
		return new Exp(2.0) * Math.PI * Radius();
	}

	public override Exp Radius() {
		return Exp.Abs(r);
	}

	public override ExpVector Center() {
		return center.exp;
	}

}
