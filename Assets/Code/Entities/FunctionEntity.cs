using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

[Serializable]
public class FunctionEntity : Entity, ISegmentaryEntity {

	public PointEntity p0;
	public PointEntity p1;
	public PointEntity c;

	string function_x;
	string function_y;
	int subdivision_ = 16;
	public int subdivision {
		get {
			return subdivision_;
		}
		set {
			subdivision_ = value;
			sketch.MarkDirtySketch(entities:true);
		}
	}
	ExpBasis2d basis = new ExpBasis2d();

	bool tBeginFixed_ = false;
	public bool tBeginFixed {
		get {
			return tBeginFixed_;
		}
		set {
			tBeginFixed_ = value;
			sketch.MarkDirtySketch(entities:true, topo:true);
		}
	}

	bool tEndFixed_ = false;
	public bool tEndFixed {
		get {
			return tEndFixed_;
		}
		set {
			tEndFixed_ = value;
			sketch.MarkDirtySketch(entities:true, topo:true);
		}
	}

	public double tBegin {
		get {
			return t0.value;
		}
		set {
			t0.value = value;
		}
	}

	public double tEnd {
		get {
			return t1.value;
		}
		set {
			t1.value = value;
		}
	}

	public string x {
		get {
			return function_x;
		}
		set {
			if(function_x == value) return;
			function_x = value;
			parser.SetString(function_x);
			var e = parser.Parse();
			if(e != null) {
				exp.x = e;
				Debug.Log("x = " + e.ToString());
				sketch.MarkDirtySketch(entities:true, topo:true);
			}
		}
	}

	public string y {
		get {
			return function_y;
		}
		set {
			if(function_y == value) return;
			function_y = value;
			parser.SetString(function_y);
			var e = parser.Parse();
			if(e != null) {
				exp.y = e;
				Debug.Log("y = " + e.ToString());
				sketch.MarkDirtySketch(entities:true, topo:true);
			}
		}
	}

	ExpParser parser;
	ExpVector exp = new ExpVector(0.0, 0.0, 0.0);
	Param t = new Param("t");
	Param t0 = new Param("t0", 0.0);
	Param t1 = new Param("t1", 1.0);

	void InitParser() {
		parser = new ExpParser("0");
		parser.parameters.Add(t);
		x = "t";
		y = "cos(t * pi)";

	}

	public FunctionEntity(Sketch sk) : base(sk) {
		p0 = AddChild(new PointEntity(sk));
		p1 = AddChild(new PointEntity(sk));
		c = AddChild(new PointEntity(sk));
		InitParser();
	}

	public override IEntityType type { get { return IEntityType.Function; } }


	public ExpVector GetExpClone(Exp t) {
		var e = new ExpVector(exp.x.DeepClone(), exp.y.DeepClone(), 0.0);
		if(t != null) {
			e.x.Substitute(this.t, t);
			e.y.Substitute(this.t, t);
			e.z.Substitute(this.t, t);
		}
		return e;
	}

	public override IEnumerable<Exp> equations {
		get {
			ExpVector e0 = basis.TransformPosition(GetExpClone(t0));

			var eq0 = e0 - p0.exp;
			yield return eq0.x;
			yield return eq0.y;

			//if(!p0.IsCoincidentWith(p1)) {
				ExpVector e1 = basis.TransformPosition(GetExpClone(t1));

				var eq1 = e1 - p1.exp;
				yield return eq1.x;
				yield return eq1.y;
			//}

			var eqc = basis.p - c.exp;
			yield return eqc.x;
			yield return eqc.y;

			foreach(var e in basis.equations) yield return e;

		}
	}

	public override IEnumerable<PointEntity> points {
		get {
			yield return p0;
			yield return p1;
			yield return c;
		}
	}

	public override IEnumerable<Param> parameters {
		get {
			if(!tBeginFixed) yield return t0;
			if(!tEndFixed) yield return t1;
			foreach(var p in basis.parameters) yield return p;
		}
	}

	public override bool IsChanged() {
		return p0.IsChanged() || p1.IsChanged() || c.IsChanged() || t0.changed || t1.changed;
	}

	public PointEntity begin { get { return p0; } }
	public PointEntity end { get { return p1; } }
	public IEnumerable<Vector3> segmentPoints {
		get {
			Param pOn = new Param("pOn");
			var on = PointOn(pOn);
			var subdiv = (int)Math.Ceiling(subdivision * Math.Abs(t1.value - t0.value));
			for(int i = 0; i <= subdiv; i++) {
				pOn.value = (double)i / subdiv;
				yield return on.Eval();
			}
		}
	}	

	//public override BBox bbox { get { return new BBox(center.pos, (float)radius); } }

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
		var newt = t0.exp + (t1.exp - t0.exp) * t;
		return basis.TransformPosition(GetExpClone(newt));
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

	protected override void OnWrite(XmlTextWriter xml) {
		xml.WriteAttributeString("x", x);
		xml.WriteAttributeString("y", y);
		xml.WriteAttributeString("t0", t0.value.ToStr());
		xml.WriteAttributeString("t1", t1.value.ToStr());
		xml.WriteAttributeString("t0fix", tBeginFixed_.ToString());
		xml.WriteAttributeString("t1fix", tEndFixed_.ToString());
		xml.WriteAttributeString("subdiv", subdivision_.ToString());
		xml.WriteAttributeString("basis", basis.ToString());
	}

	protected override void OnRead(XmlNode xml) {
		x = xml.Attributes["x"].Value;
		y = xml.Attributes["y"].Value;
		t0.value = xml.Attributes["t0"].Value.ToDouble();
		t1.value = xml.Attributes["t1"].Value.ToDouble();
		tBeginFixed_ = Convert.ToBoolean(xml.Attributes["t0fix"].Value);
		tEndFixed_ = Convert.ToBoolean(xml.Attributes["t1fix"].Value);
		subdivision_ = Convert.ToInt32(xml.Attributes["subdiv"].Value);
		basis.FromString(xml.Attributes["basis"].Value);
	}
}
