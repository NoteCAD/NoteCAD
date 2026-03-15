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
			// p0 and p1 must lie on the ellipse: (u/r0)² + (v/r1)² = 1
			var d0 = p0.exp - c.exp;
			var u0 = ExpVector.Dot(d0, basis.u) / Exp.Abs(r0);
			var v0 = ExpVector.Dot(d0, basis.v) / Exp.Abs(r1);
			yield return Exp.Sqr(u0) + Exp.Sqr(v0) - 1.0;
			var d1 = p1.exp - c.exp;
			var u1 = ExpVector.Dot(d1, basis.u) / Exp.Abs(r0);
			var v1 = ExpVector.Dot(d1, basis.v) / Exp.Abs(r1);
			yield return Exp.Sqr(u1) + Exp.Sqr(v1) - 1.0;
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
			foreach(var p in basis.parameters) yield return p;
		}
	}

	public override bool IsChanged() {
		return p0.IsChanged() || p1.IsChanged() || c.IsChanged() || r0.changed || r1.changed || basis.changed;
	}

	// Inverse-transform a world point into the ellipse's local frame,
	// normalised by the semi-axes so that the result lies on the unit circle.
	ExpVector LocalNormalized(ExpVector worldPt) {
		var d = worldPt - c.exp;
		return new ExpVector(
			ExpVector.Dot(d, basis.u) / Exp.Abs(r0),
			ExpVector.Dot(d, basis.v) / Exp.Abs(r1),
			0.0
		);
	}

	// Parametric angle of p0 in the ellipse's local frame (Atan2 of the
	// normalised coordinates).  This replaces the stored a0 parameter.
	Exp ComputeStartAngle() {
		var n = LocalNormalized(p0.exp);
		return Exp.Atan2(n.y, n.x);
	}

	ExpVector EllipsePointAt(Exp angle) {
		return basis.TransformPosition(new ExpVector(
			Exp.Cos(angle) * Exp.Abs(r0),
			Exp.Sin(angle) * Exp.Abs(r1),
			0.0
		));
	}

	public Exp GetAngleExp() {
		// CCW parametric sweep from p0 to p1, measured on the unit circle
		// obtained by dividing local coords by the respective semi-axes.
		var n0 = LocalNormalized(p0.exp);
		var n1 = LocalNormalized(p1.exp);
		return ConstraintExp.angle2d(n0, n1, angle360: true);
	}

	public double GetAngle() {
		if(Math.Abs(r0.value) < 1e-6 || Math.Abs(r1.value) < 1e-6) return 0.0;
		var uVec = basis.u.Eval();
		var vVec = basis.v.Eval();
		var d0 = p0.pos - c.pos;
		var d1 = p1.pos - c.pos;
		float nx0 = (float)(Vector3.Dot(d0, uVec) / Math.Abs(r0.value));
		float ny0 = (float)(Vector3.Dot(d0, vVec) / Math.Abs(r1.value));
		float nx1 = (float)(Vector3.Dot(d1, uVec) / Math.Abs(r0.value));
		float ny1 = (float)(Vector3.Dot(d1, vVec) / Math.Abs(r1.value));
		return ConstraintExp.angle2d(
			new Vector3(nx0, ny0, 0f),
			new Vector3(nx1, ny1, 0f),
			angle360: true
		);
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
		var part = new EllipticArcEntity(sketch);
		part.r0.value = r0.value;
		part.r1.value = r1.value;
		part.basis.FromString(basis.ToString());
		part.p0.pos = position;
		part.p1.pos = p1.pos;

		p1.pos = position;

		return part;
	}

	public override double FindParameter(Vector3 pos) {
		double da = GetAngle();
		if(da < 1e-6) return 0.0;
		if(Math.Abs(r0.value) < 1e-6 || Math.Abs(r1.value) < 1e-6) return 0.0;
		var uVec = basis.u.Eval();
		var vVec = basis.v.Eval();
		var d0 = p0.pos - c.pos;
		var dPos = pos - c.pos;
		double nx0 = Vector3.Dot(d0, uVec) / Math.Abs(r0.value);
		double ny0 = Vector3.Dot(d0, vVec) / Math.Abs(r1.value);
		double nxPos = Vector3.Dot(dPos, uVec) / Math.Abs(r0.value);
		double nyPos = Vector3.Dot(dPos, vVec) / Math.Abs(r1.value);
		double a0v = Math.Atan2(ny0, nx0);
		double aPos = Math.Atan2(nyPos, nxPos);
		double angleFromStart = aPos - a0v;
		while(angleFromStart < 0.0) angleFromStart += 2.0 * Math.PI;
		while(angleFromStart > 2.0 * Math.PI) angleFromStart -= 2.0 * Math.PI;
		return Mathf.Clamp01((float)(angleFromStart / da));
	}

	public override ExpVector PointOn(Exp t) {
		var a0exp = ComputeStartAngle();
		var angle = a0exp + t * GetAngleExp();
		return basis.TransformPosition(new ExpVector(
			Exp.Cos(angle) * Exp.Abs(r0),
			Exp.Sin(angle) * Exp.Abs(r1),
			0.0
		));
	}

	public override ExpVector TangentAt(Exp t) {
		var a0exp = ComputeStartAngle();
		var angle = a0exp + t * GetAngleExp();
		return basis.TransformDirection(new ExpVector(
			-Exp.Sin(angle) * Exp.Abs(r0),
			Exp.Cos(angle) * Exp.Abs(r1),
			0.0
		));
	}

	public override Exp Length() {
		// Arc length = r0 * (E(π/2−a0, k) − E(π/2−a1, k))
		// Derived from ∫[a0..a1] sqrt(r0²sin²t + r1²cos²t) dt.
		// Requires r0 >= r1 (r0 is the major semi-axis).  When r0 < r1, swap
		// the radii and rotate the basis by 90° before creating the entity so
		// that the convention is always satisfied.  k² = 1−(r1/r0)² ≥ 0.
		var ar0 = Exp.Abs(r0);
		var ar1 = Exp.Abs(r1);
		var k = Exp.Sqrt(Exp.one - Exp.Sqr(ar1) / Exp.Sqr(ar0));
		var a0exp = ComputeStartAngle();
		var a1exp = a0exp + GetAngleExp();
		return ar0 * (Exp.EllInt(Math.PI / 2.0 - a0exp, k) - Exp.EllInt(Math.PI / 2.0 - a1exp, k));
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
		xml.WriteAttribute("basis", basis.ToString());
	}

	protected override void OnRead(XmlNode xml) {
		r0.value = xml.Attributes["r0"].Value.ToDouble();
		r1.value = xml.Attributes["r1"].Value.ToDouble();
		basis.FromString(xml.Attributes["basis"].Value);
	}

}
