using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public enum HVOrientation {
	OX,
	OY,
	OZ
}

public class HVConstraint : Constraint {

	public IEntity p0 { get { return GetEntity(0); } set { SetEntity(0, value); } }
	public IEntity p1 { get { return GetEntity(1); } set { SetEntity(1, value); } }

	public ExpVector p0exp { get { return p0.PointExpInPlane(sketch.plane); } }
	public ExpVector p1exp { get { return p1.PointExpInPlane(sketch.plane); } }

	public HVOrientation orientation = HVOrientation.OX;

	public HVConstraint(Sketch sk) : base(sk) { }

	public HVConstraint(Sketch sk, IEntity p0, IEntity p1) : base(sk) {
		AddEntity(p0);
		AddEntity(p1);
	}

	public override IEnumerable<Exp> equations {
		get {
			switch(orientation) {
				case HVOrientation.OX: yield return p0exp.x - p1exp.x; break;
				case HVOrientation.OY: yield return p0exp.y - p1exp.y; break;
				case HVOrientation.OZ: yield return p0exp.z - p1exp.z; break;
			}
		}
	}

	void DrawStroke(LineCanvas canvas, LineEntity line) {
		Vector3 dir = (line.p1.GetPosition() - line.p0.GetPosition()).normalized;
		Vector3 perp = Vector3.Cross(dir, Vector3.forward);
		Vector3 pos = (line.p1.GetPosition() + line.p0.GetPosition()) / 2f;
		canvas.DrawLine(pos + perp, pos - perp);
	}

	/*protected override void OnDraw(LineCanvas canvas) {
		DrawStroke(canvas, l0);
		DrawStroke(canvas, l1);
	}

	protected override bool OnIsChanged() {
		return l0.IsChanged() || l1.IsChanged();
	}*/

	protected override void OnWrite(XmlTextWriter xml) {
		xml.WriteAttributeString("orientation", orientation.ToString());
	}

	protected override void OnRead(XmlNode xml) {
		xml.Attributes["orientation"].Value.ToEnum(ref orientation);
	}

}
