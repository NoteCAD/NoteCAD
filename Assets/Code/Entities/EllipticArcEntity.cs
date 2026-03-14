using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using NoteCAD;

[Serializable]
public class EllipticArcEntity : Entity, ISegmentaryEntity {

	[NonSerialized]
	public PointEntity p0;

	[NonSerialized]
	public PointEntity p1;

	[NonSerialized]
	public PointEntity c;

	public Param r0 = new Param("r0");
	public Param r1 = new Param("r1");
	public ExpBasis2d basis = new ExpBasis2d();

	public Param a0 = new Param("a0");
	public Param a1 = new Param("a1");

	public EllipticArcEntity(Sketch sk) : base(sk) {
		p0 = AddChild(new PointEntity(sk));
		p1 = AddChild(new PointEntity(sk));
		c = AddChild(new PointEntity(sk));
		basis.SetPosParams(c.x, c.y);
	}

	public override IEntityType type { get { return IEntityType.EllipticArc; } }

	public override IEnumerable<Exp> equations {
		get {
			foreach(var e in basis.equations) yield return e;
			var pt0 = EllipsePointAt(a0.exp);
			yield return pt0.x - p0.exp.x;
			yield return pt0.y - p0.exp.y;
			var pt1 = EllipsePointAt(a1.exp);
			yield return pt1.x - p1.exp.x;
			yield return pt1.y - p1.exp.y;
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
			yield return r0;
			yield return r1;
			yield return a0;
			yield return a1;
			foreach(var p in basis.parameters) yield return p;
		}
	}

	public override bool IsChanged() {
		return p0.IsChanged() || p1.IsChanged() || c.IsChanged() || r0.changed || r1.changed || a0.changed || a1.changed || basis.changed;
	}

	ExpVector EllipsePointAt(Exp angle) {
		return basis.TransformPosition(new ExpVector(
			Exp.Cos(angle) * Exp.Abs(r0),
			Exp.Sin(angle) * Exp.Abs(r1),
			0.0
		));
	}

	public Exp GetAngleExp() {
		return a1.exp - a0.exp;
	}

	public double GetAngle() {
		var da = a1.value - a0.value;
		while(da <= 0.0) da += 2.0 * Math.PI;
		while(da > 2.0 * Math.PI) da -= 2.0 * Math.PI;
		return da;
	}

	public PointEntity begin { get { return p0; } }
	public PointEntity end { get { return p1; } }
	public PointEntity center { get { return c; } }

	public IEnumerable<IEnumerable<Vector3>> segmentPoints {
		get {
			yield return getSegmentsUsingPointOn(36);
		}
	}

	public double radius0 { get { return r0.value; } set { r0.value = value; } }
	public double radius1 { get { return r1.value; } set { r1.value = value; } }

	public override BBox bbox { get { return new BBox(center.pos, (float)Math.Max(Math.Abs(r0.value), Math.Abs(r1.value))); } }

	protected override Entity OnSplit(Vector3 position) {
		var t = FindParameter(position);
		var da = GetAngle();
		var splitAngle = a0.value + t * da;

		var part = new EllipticArcEntity(sketch);
		part.r0.value = r0.value;
		part.r1.value = r1.value;
		part.basis.FromString(basis.ToString());
		part.a0.value = splitAngle;
		part.a1.value = a1.value;
		part.p0.pos = position;
		part.p1.pos = p1.pos;

		a1.value = splitAngle;
		p1.pos = position;

		return part;
	}

	public override double FindParameter(Vector3 pos) {
		double da = GetAngle();
		if(da < 1e-6) return 0.0;
		if(Math.Abs(r0.value) < 1e-6 || Math.Abs(r1.value) < 1e-6) return 0.0;
		var uVec = basis.u.Eval();
		var vVec = basis.v.Eval();
		var delta = pos - center.pos;
		double xLocal = Vector3.Dot(delta, uVec);
		double yLocal = Vector3.Dot(delta, vVec);
		double angle = Math.Atan2(yLocal / Math.Abs(r1.value), xLocal / Math.Abs(r0.value));
		double angleFromStart = angle - a0.value;
		while(angleFromStart < 0.0) angleFromStart += 2.0 * Math.PI;
		while(angleFromStart > 2.0 * Math.PI) angleFromStart -= 2.0 * Math.PI;
		return Mathf.Clamp01((float)(angleFromStart / da));
	}

	public override ExpVector PointOn(Exp t) {
		var da = GetAngleExp();
		var angle = a0.exp + t * da;
		return basis.TransformPosition(new ExpVector(
			Exp.Cos(angle) * Exp.Abs(r0),
			Exp.Sin(angle) * Exp.Abs(r1),
			0.0
		));
	}

	public override ExpVector TangentAt(Exp t) {
		var da = GetAngleExp();
		var angle = a0.exp + t * da;
		return basis.TransformDirection(new ExpVector(
			-Exp.Sin(angle) * Exp.Abs(r0),
			Exp.Cos(angle) * Exp.Abs(r1),
			0.0
		));
	}

	public override Exp Length() {
		// Arc length = ∫[a0 to a1] sqrt(r0²·sin²(t) + r1²·cos²(t)) dt
		// Two forms (same k = sqrt(1-(rmin/rmax)²)):
		//   r0 >= r1: integrand = rmax·sqrt(1-k²·cos²t) → substitute u=π/2-t
		//             L = rmax·(E(π/2−a0, k) − E(π/2−a1, k))
		//   r0 <  r1: integrand = rmax·sqrt(1-k²·sin²t) directly
		//             L = rmax·(E(a1, k) − E(a0, k))
		var ar0 = Exp.Abs(r0);
		var ar1 = Exp.Abs(r1);
		var absDiff = Exp.Abs(ar0 - ar1);
		var rmax = (ar0 + ar1 + absDiff) / 2.0;
		var rmin = (ar0 + ar1 - absDiff) / 2.0;
		var k = Exp.Sqrt(Exp.one - Exp.Sqr(rmin) / Exp.Sqr(rmax));
		var cond = new Exp(Exp.Op.GEqual, ar0, ar1);
		var L0 = rmax * (Exp.EllInt(Math.PI / 2.0 - a0.exp, k) - Exp.EllInt(Math.PI / 2.0 - a1.exp, k));
		var L1 = rmax * (Exp.EllInt(a1.exp, k) - Exp.EllInt(a0.exp, k));
		return new Exp(Exp.Op.If, cond, L0, L1);
	}

	public override Exp Radius() {
		return null;
	}

	public override ExpVector Center() {
		return c.exp;
	}

	protected override void OnWrite(Writer xml) {
		xml.WriteAttribute("r0", Math.Abs(r0.value));
		xml.WriteAttribute("r1", Math.Abs(r1.value));
		xml.WriteAttribute("a0", a0.value);
		xml.WriteAttribute("a1", a1.value);
		xml.WriteAttribute("basis", basis.ToString());
	}

	protected override void OnRead(XmlNode xml) {
		r0.value = xml.Attributes["r0"].Value.ToDouble();
		r1.value = xml.Attributes["r1"].Value.ToDouble();
		a0.value = xml.Attributes["a0"].Value.ToDouble();
		a1.value = xml.Attributes["a1"].Value.ToDouble();
		basis.FromString(xml.Attributes["basis"].Value);
	}

}
