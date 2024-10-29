using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

[Serializable]
public class DisplayLink : Constraint {

	public DisplayLink(Sketch sk) : base(sk) { }
	public DisplayLink(Sketch sk, Id id) : base(sk, id) { }
	public DisplayLink(Sketch sk, IEntity l0, IEntity l1) : base(sk) {
		AddEntity(l0);
		AddEntity(l1);

	}
	
	void DrawCircle(ICanvas canvas, IEntity e, int rpt) {
		Vector3 pos = e.PointOnInPlane(0.5, null).Eval();
		drawCameraCircle(canvas, Camera.main, pos, 5f * getPixelSize());
		ref_points[rpt] = sketch.plane.ToPlane(pos);
	}

	protected override void OnDraw(ICanvas canvas) {
		DrawCircle(canvas, GetEntity(0), 0);
		DrawCircle(canvas, GetEntity(1), 1);
		DrawReferenceLink(canvas, Camera.main, drawCircles: false);
	}

}
