using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

[Serializable]
public class Equal : ValueConstraint {

	public Equal(Sketch sk) : base(sk) { selectByRefPoints = true; }

	[Serializable]
	public enum LengthType {
		Length,
		Radius,
		Diameter
	}

	LengthType[] lengthType = new LengthType[2];

	[SerializeField]
	public LengthType FirstLengthType {
		get {
			if(GetEntity(0).Radius() == null) return LengthType.Length;
			return lengthType[0];
		}

		set {
			if(GetEntity(0).Radius() == null) return;
			lengthType[0] = value;
			sketch.MarkDirtySketch(constraints:true);
		}
	}

	[SerializeField]
	public LengthType SecondLengthType {
		get {
			if(GetEntity(1).Radius() == null) return LengthType.Length;
			return lengthType[1];
		}

		set {
			if(GetEntity(1).Radius() == null) return;
			lengthType[1] = value;
			sketch.MarkDirtySketch(constraints:true);
		}
	}

	public Equal(Sketch sk, IEntity l0, IEntity l1) : base(sk) {
		AddEntity(l0);
		AddEntity(l1);
		value.value = 1.0;
		selectByRefPoints = true;
	}

	public override IEnumerable<Exp> equations {
		get {
			Exp[] len = new Exp[2];

			for(int i = 0; i < 2; i++) {
				var e = GetEntity(i);
				switch(lengthType[i]) {
					case LengthType.Length: len[i] = e.Length(); break;
					case LengthType.Radius: len[i] = e.Radius(); break;
					case LengthType.Diameter: len[i] = e.Radius() * 2.0; break;
				}
			}
			yield return len[0] - len[1] * value;
		}
	}

	void DrawStroke(LineCanvas canvas, IEntity e, int rpt) {

		Vector3 dir = e.TangentAtInPlane(0.5, null).Eval();
		Vector3 perp = Vector3.Cross(dir, Camera.main.transform.forward).normalized * 5f * getPixelSize();
		Vector3 pos = e.PointOnInPlane(0.5, null).Eval();
		ref_points[rpt] = sketch.plane.ToPlane(pos);
		if(rpt == 0) {
			this.pos = e.OffsetAtInPlane(0.5, 20f * getPixelSize(), null).Eval();
		}
		canvas.DrawLine(pos + perp, pos - perp);
	}

	protected override void OnDraw(LineCanvas canvas) {
		DrawStroke(canvas, GetEntity(0), 0);
		DrawStroke(canvas, GetEntity(1), 1);
		
		if(DetailEditor.instance.hovered == this) {
			DrawReferenceLink(canvas, Camera.main);
		}
	}

	protected override string OnGetLabelValue() {
		return base.OnGetLabelValue() + ":1";
	}

	protected override Matrix4x4 OnGetBasis() {
		return sketch.plane.GetTransform();
	}

	protected override void OnWriteValueConstraint(XmlTextWriter xml) {
		xml.WriteAttributeString("firstLength", lengthType[0].ToString());
		xml.WriteAttributeString("secondLength", lengthType[1].ToString());
	}

	protected override void OnReadValueConstraint(XmlNode xml) {
		if(xml.Attributes["firstLength"] != null) xml.Attributes["firstLength"].Value.ToEnum(ref lengthType[0]);
		if(xml.Attributes["secondLength"] != null) xml.Attributes["secondLength"].Value.ToEnum(ref lengthType[1]);
	}
}
