using System;
using System.Collections.Generic;
using System.Linq;
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
	public Param startAngle = new Param("a0");
	public Param deltaAngle = new Param("da");
	public ExpBasis2d basis = new ExpBasis2d();

	public override IEntityType type { get { return IEntityType.EllipticArc; } }

	public EllipticArcEntity(Sketch sk) : base(sk) {
		c  = AddChild(new PointEntity(sk));
		p0 = AddChild(new PointEntity(sk));
		p1 = AddChild(new PointEntity(sk));
		basis.SetPosParams(c.x, c.y);
	}

	public double radius0 { get { return r0.value; } set { r0.value = value; } }
	public double radius1 { get { return r1.value; } set { r1.value = value; } }

	public override IEnumerable<PointEntity> points {
		get {
			yield return p0;
			yield return p1;
			yield return c;
		}
	}

	public override bool IsChanged() {
		return c.IsChanged() || r0.changed || r1.changed || startAngle.changed || deltaAngle.changed || basis.changed;
	}

	public override IEnumerable<Param> parameters {
		get {
			yield return r0;
			yield return r1;
			yield return startAngle;
			yield return deltaAngle;
			foreach(var p in basis.parameters) yield return p;
		}
	}

	public override IEnumerable<Exp> equations {
		get {
			foreach(var e in basis.equations) yield return e;
			// Constrain p0 to start of arc
			var ep0 = StartExp();
			yield return p0.exp.x - ep0.x;
			yield return p0.exp.y - ep0.y;
			// Constrain p1 to end of arc
			var ep1 = EndExp();
			yield return p1.exp.x - ep1.x;
			yield return p1.exp.y - ep1.y;
		}
	}

	ExpVector StartExp() {
		return basis.TransformPosition(new ExpVector(
			Exp.Cos(startAngle.exp) * Exp.Abs(r0),
			Exp.Sin(startAngle.exp) * Exp.Abs(r1),
			0.0));
	}

	ExpVector EndExp() {
		var endAngle = startAngle.exp + deltaAngle.exp;
		return basis.TransformPosition(new ExpVector(
			Exp.Cos(endAngle) * Exp.Abs(r0),
			Exp.Sin(endAngle) * Exp.Abs(r1),
			0.0));
	}

	public PointEntity begin  { get { return p0; } }
	public PointEntity end    { get { return p1; } }
	public PointEntity center { get { return c;  } }

	public IEnumerable<IEnumerable<Vector3>> segmentPoints {
		get {
			yield return getSegmentsUsingPointOn(36);
		}
	}

	public override BBox bbox {
		get { return new BBox(c.pos, (float)Math.Max(Math.Abs(r0.value), Math.Abs(r1.value))); }
	}

	// Copy only the orientation vectors (u, v) from another basis; position comes from c.
	public void CopyBasisOrientationFrom(ExpBasis2d src) {
		var srcList = src.parameters.ToList();
		var dstList = basis.parameters.ToList();
		// Order in ExpBasis2d.parameters: ux, uy, vx, vy, px, py
		dstList[0].value = srcList[0].value; // ux
		dstList[1].value = srcList[1].value; // uy
		dstList[2].value = srcList[2].value; // vx
		dstList[3].value = srcList[3].value; // vy
		// dstList[4] = c.x, dstList[5] = c.y — already linked via SetPosParams
	}

	protected override void OnWrite(Writer xml) {
		xml.WriteAttribute("r0", Math.Abs(r0.value));
		xml.WriteAttribute("r1", Math.Abs(r1.value));
		xml.WriteAttribute("a0", startAngle.value);
		xml.WriteAttribute("da", deltaAngle.value);
		xml.WriteAttribute("basis", basis.ToString());
	}

	protected override void OnRead(XmlNode xml) {
		r0.value = xml.Attributes["r0"].Value.ToDouble();
		r1.value = xml.Attributes["r1"].Value.ToDouble();
		startAngle.value = xml.Attributes["a0"].Value.ToDouble();
		deltaAngle.value = xml.Attributes["da"].Value.ToDouble();
		basis.FromString(xml.Attributes["basis"].Value);
	}

	public override ExpVector PointOn(Exp t) {
		var angle = startAngle.exp + t * deltaAngle.exp;
		return basis.TransformPosition(new ExpVector(
			Exp.Cos(angle) * Exp.Abs(r0),
			Exp.Sin(angle) * Exp.Abs(r1),
			0.0));
	}

	public override Exp Length() {
		// Arc length = EllInt(endAngle, |r0|, |r1|) - EllInt(startAngle, |r0|, |r1|)
		// EllInt(phi, r0, r1) = integral_0^phi sqrt(r0^2*sin^2(t) + r1^2*cos^2(t)) dt
		var a0 = startAngle.exp;
		var a1 = startAngle.exp + deltaAngle.exp;
		return Exp.EllInt(a1, Exp.Abs(r0), Exp.Abs(r1)) - Exp.EllInt(a0, Exp.Abs(r0), Exp.Abs(r1));
	}

	public override Exp Radius() {
		return null;
	}

	public override ExpVector Center() {
		return c.exp;
	}

	protected override Entity OnSplit(Vector3 position) {
		double t = FindParameter(position);
		double splitAngle = startAngle.value + t * deltaAngle.value;
		double oldEndAngle = startAngle.value + deltaAngle.value;

		// Evaluate current end position before modifying params
		var pOn = new Param("pOn");
		var ptOn = PointOn(pOn);
		pOn.value = 1.0;
		var oldEndPos = ptOn.Eval();

		var part = new EllipticArcEntity(sketch);
		part.c.pos = c.pos;
		part.r0.value = r0.value;
		part.r1.value = r1.value;
		part.CopyBasisOrientationFrom(basis);

		// Part: from splitAngle to original end
		part.startAngle.value = splitAngle;
		part.deltaAngle.value = oldEndAngle - splitAngle;

		// This arc: from startAngle to splitAngle
		deltaAngle.value = splitAngle - startAngle.value;

		// Set initial endpoint positions (solver will re-enforce via equations)
		p1.pos = position;
		part.p0.pos = position;
		part.p1.pos = oldEndPos;

		return part;
	}

}
