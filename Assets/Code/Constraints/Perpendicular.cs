using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Perpendicular : Constraint {

	public enum Option {
		LeftHand,
		RightHand
	}

	Option option_;

	public Option option { get { return option_; } set { option_ = value; sketch.MarkDirtySketch(topo:true); } }
	protected override Enum optionInternal { get { return option; } set { option = (Option)value; } }

	public Perpendicular(Sketch sk) : base(sk) { }

	public Perpendicular(Sketch sk, IEntity l0, IEntity l1) : base(sk) {
		AddEntity(l0);
		AddEntity(l1);
		ChooseBestOption();
	}

	public override IEnumerable<Exp> equations {
		get {
			var l0 = GetEntityOfType(IEntityType.Line, 0);
			var l1 = GetEntityOfType(IEntityType.Line, 1);

			ExpVector d0 = l0.GetPointAtInPlane(0, sketch.plane) - l0.GetPointAtInPlane(1, sketch.plane);
			ExpVector d1 = l1.GetPointAtInPlane(0, sketch.plane) - l1.GetPointAtInPlane(1, sketch.plane);

			Exp angle = sketch.is3d ? ConstraintExp.angle3d(d0, d1) : ConstraintExp.angle2d(d0, d1);
			switch(option) {
				case Option.LeftHand: yield return angle - Math.PI / 2.0; break;
				case Option.RightHand: yield return angle + Math.PI / 2.0; break;
			}
		}
	}

	void DrawStroke(LineCanvas canvas, IEntity line, int rpt) {
		var p0 = line.GetPointAtInPlane(0, null).Eval();
		var p1 = line.GetPointAtInPlane(1, null).Eval();
		float len = (p1 - p0).magnitude;
		float size = Mathf.Min(len, 10f * getPixelSize());
		Vector3 dir = (p1 - p0).normalized * size / 2f;
		Vector3 perp = Vector3.Cross(p1 - p0, Vector3.forward).normalized * 3f * getPixelSize();
		Vector3 pos = (p1 + p0) / 2f;
		ref_points[rpt] = sketch.plane.ToPlane(pos);
		canvas.DrawLine(pos + dir + perp, pos - dir + perp);
		canvas.DrawLine(pos + dir - perp, pos - dir - perp);
	}
	/*
	protected override void OnDraw(LineCanvas canvas) {
		var l0 = GetEntityOfType(IEntityType.Line, 0);
		var l1 = GetEntityOfType(IEntityType.Line, 1);
		DrawStroke(canvas, l0, 0);
		DrawStroke(canvas, l1, 1);
		if(DetailEditor.instance.hovered == this) {
			DrawReferenceLink(canvas, Camera.main);
		}
	}
	*/
	protected override void OnDraw(LineCanvas canvas) {

		var line0 = GetEntityOfType(IEntityType.Line, 0);
		var line1 = GetEntityOfType(IEntityType.Line, 1);
		
		ExpVector p0 = null;
		ExpVector p1 = null;
		ExpVector p2 = null;
		
		for(int i = 0; i < 2; i++) {
			for(int j = 0; j < 2; j++) {
				if(line0.GetPointAtInPlane(i, null).ValuesEquals(line1.GetPointAtInPlane(j, null), 1e-6))  {
					p0 = line0.GetPointAtInPlane(i, null);
					p1 = line0.GetPointAtInPlane((i + 1) % 2, null);
					p2 = line1.GetPointAtInPlane((j + 1) % 2, null);
				}
			}
		}
		
		float pix = getPixelSize();
		
		if(p0 != null) {
			Vector3 p = p0.Eval();
			Vector3 dir1 = p1.Eval() - p;
			Vector3 dir2 = p2.Eval() - p;
			dir1 = dir1.normalized * pix * 13f;
			dir2 = dir2.normalized * pix * 13f;
			Vector3 corner = p + dir1 + dir2;
			canvas.DrawLine(p + dir1, corner);
			canvas.DrawLine(p + dir2, corner);
			ref_points[0] = ref_points[1] = sketch.plane.ToPlane(corner);
		} else {
			for(int i=0; i<2; i++) {
				var line = GetEntityOfType(IEntityType.Line, i);
				Vector3 a = line.GetPointAtInPlane(0, null).Eval();
				Vector3 b = line.GetPointAtInPlane(1, null).Eval();
				Vector3 dir = normalize(a - b);
				Vector3 center = a + (b - a) * 0.5f;
				
				Vector3 plane = getVisualPlaneDir(Camera.main.transform.forward);
				
				Vector3 perp = normalize(Vector3.Cross(dir, plane));
				Vector3 p = center - perp * pix * 8.0f;
				canvas.DrawLine(p - dir * pix * 8.0f, p + dir * pix * 8.0f);
				canvas.DrawLine(p, p - perp * pix * 13.0f);
				ref_points[i] = sketch.plane.ToPlane(p - perp * pix * 6.0f);
			}

			if(DetailEditor.instance.hovered == this) {
				DrawReferenceLink(canvas, Camera.main);
			}
		}
	}


}
