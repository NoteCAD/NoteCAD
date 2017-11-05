using System;
using System.Collections.Generic;
using UnityEngine;

public class EqualLineLine : Constraint {

	public LineEntity l0 { get; private set; }
	public LineEntity l1 { get; private set; }

	public EqualLineLine(Sketch sk) : base(sk) { }

	public EqualLineLine(Sketch sk, LineEntity l0, LineEntity l1) : base(sk) {
		this.l0 = AddEntity(l0);
		this.l1 = AddEntity(l1);
	}

	public override IEnumerable<Exp> equations {
		get {
			ExpVector d0 = l0.p1.exp - l0.p0.exp;
			ExpVector d1 = l1.p1.exp - l1.p0.exp;
			yield return d0.Magnitude() - d1.Magnitude();
		}
	}

	void DrawStroke(LineCanvas canvas, LineEntity line) {
		Vector3 dir = (line.p1.GetPosition() - line.p0.GetPosition()).normalized;
		Vector3 perp = Vector3.Cross(dir, Vector3.forward);
		Vector3 pos = (line.p1.GetPosition() + line.p0.GetPosition()) / 2f;
		canvas.DrawLine(pos + perp, pos - perp);
	}

	protected override void OnDraw(LineCanvas canvas) {
		DrawStroke(canvas, l0);
		DrawStroke(canvas, l1);
	}

	protected override bool OnIsChanged() {
		return l0.IsChanged() || l1.IsChanged();
	}
	
	protected override void OnRead(System.Xml.XmlNode xml) {
		l0 = GetEntity(0) as LineEntity;
		l1 = GetEntity(1) as LineEntity;
	}

}
