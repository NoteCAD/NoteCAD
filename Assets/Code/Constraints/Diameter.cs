using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;

[Serializable]
public class Diameter : ValueConstraint {

	public Diameter(Sketch sk) : base(sk) { }

	public bool showAsRadius = false;

	public Diameter(Sketch sk, IEntity c) : base(sk) {
		showAsRadius = (c.type == IEntityType.Arc);
		AddEntity(c);
		Satisfy();
	}

	Exp radius { get { return GetEntity(0).Radius(); } }
	ExpVector center { get { return GetEntity(0).CenterInPlane(null); } }

	public override IEnumerable<Exp> equations {
		get {
			yield return radius * 2.0 - value.exp;
		}
	}

	public override double LabelToValue(double label) {
		return showAsRadius ? label / 2.0 : label;
	}

	public override double ValueToLabel(double value) {
		return showAsRadius ? value * 2.0 : value;
	}

	protected override void OnDraw(LineCanvas canvas) {
		var p = GetEntity(0).CenterInPlane(null).Eval();
		var lo = getPlane().projectVectorInto(getLabelOffset());
		var dir = (lo - p).normalized;
		
		float r = (float)value.exp.Eval() / 2f;


		
		if(showAsRadius) {
			var rpt = p + dir * r;
			drawPointsDistance(p, p + dir * r, canvas, Camera.main, arrow0: false, arrow1: true);
			canvas.DrawLine(rpt, lo);
		} else {
			drawPointsDistance(p - dir * r, p + dir * r, canvas, Camera.main);
		}
		
		//drawLabel(renderer, camera, "Ø" + getValueString());

	}

	protected override string OnGetLabelValue() {
		return (showAsRadius ? "R" : "Ø") + base.OnGetLabelValue();
	}

	protected override Matrix4x4 OnGetBasis() {
		return sketch.plane.GetTransform() * Matrix4x4.Translate(GetEntity(0).CenterInPlane(sketch.plane).Eval());
	}

	protected override void OnWriteValueConstraint(XmlTextWriter xml) {
		xml.WriteAttributeString("showAsRadius", showAsRadius.ToString());
	}

	protected override void OnReadValueConstraint(XmlNode xml) {
		if(xml.Attributes["showAsRadius"] != null) {
			var value = GetValue();
			showAsRadius = Convert.ToBoolean(xml.Attributes["showAsRadius"].Value);
			SetValue(value);
		}
	}


}
