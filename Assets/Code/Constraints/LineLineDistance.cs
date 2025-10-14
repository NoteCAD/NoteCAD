using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using NoteCAD;

[Serializable]
public class LineLineDistance : ValueConstraint {

	public IEntity line0 { get { return GetEntity(0); } set { SetEntity(0, value); } }
	public IEntity line1 { get { return GetEntity(1); } set { SetEntity(1, value); } }

	public ExpVector line0P0Exp { get { return line0.PointsInPlane(sketch.plane).ToArray()[0]; } }
	public ExpVector line0P1Exp { get { return line0.PointsInPlane(sketch.plane).ToArray()[1]; } }
	public ExpVector line1P0Exp { get { return line1.PointsInPlane(sketch.plane).ToArray()[0]; } }
	public ExpVector line1P1Exp { get { return line1.PointsInPlane(sketch.plane).ToArray()[1]; } }

	public Vector3 line0P0Pos { get { return line0.PointsInPlane(null).ToArray()[0].Eval(); } }
	public Vector3 line0P1Pos { get { return line0.PointsInPlane(null).ToArray()[1].Eval(); } }
	public Vector3 line1P0Pos { get { return line1.PointsInPlane(null).ToArray()[0].Eval(); } }
	public Vector3 line1P1Pos { get { return line1.PointsInPlane(null).ToArray()[1].Eval(); } }

	public LineLineDistance(Sketch sk) : base(sk) { }
	public LineLineDistance(Sketch sk, Id id) : base(sk, id) { }

	public LineLineDistance(Sketch sk, IEntity e0, IEntity e1) : base(sk) {
		AddEntity(e0);
		AddEntity(e1);
		SetValue(1.0);
		Satisfy();
	}

	Param t0 = new Param("p0", 0.0);
	Param t1 = new Param("p1", 0.0);

	public override IEnumerable<Param> parameters {
		get {
			if (sketch.is3d) {
				yield return t0;
				yield return t1;
			}
		}
	}

	protected override IEnumerable<Exp> constraintEquations {
		get {
			if (sketch.is3d) {
				var d0 = line0P1Exp - line0P0Exp;
				var d1 = line1P1Exp - line1P0Exp;
				var p0 = line0P0Exp + d0 * t0;
				var p1 = line1P0Exp + d1 * t1;
				var dp = p1 - p0;
				yield return (p0 - p1).Magnitude() - value;
				yield return ExpVector.Dot(dp, d0);
				yield return ExpVector.Dot(dp, d1);
				yield break;
			}
			// 2d
			yield return ConstraintExp.pointLineDistance(line1P0Exp, line0P0Exp, line0P1Exp, sketch.is3d) - value;
		}
	}

	public override ValueUnits units => ValueUnits.LENGTH;

	protected override void OnDraw(ICanvas canvas) {
		
		var lip0 = line0P0Pos;
		var lip1 = line0P1Pos;
		var p0 = line1P0Pos;
		
		if(GetValue() == 0.0) {
			drawCameraCircle(canvas, Camera.main, p0, R_CIRLE_R * getPixelSize()); 
			SetLabelPos(p0);
		} else {
			drawPointLineDistance(lip0, lip1, p0, canvas, Camera.main);
			//drawLineExtendInPlane(getPlane(), renderer, lip0, lip1, p0, R_DASH * camera->getPixelSize()); 
		}
	}

	protected override Matrix4x4 OnGetBasis() {
		var lip0 = line0P0Pos;
		var lip1 = line0P1Pos;
		var p0 = line1P0Pos;
		return getPointLineDistanceBasis(lip0, lip1, p0, getPlane());
	}

}
