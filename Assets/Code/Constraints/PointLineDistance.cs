using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[Serializable]
public class PointLineDistance : ValueConstraint {

	public IEntity point { get { return GetEntity(0); } set { SetEntity(0, value); } }
	public IEntity line { get { return GetEntity(1); } set { SetEntity(1, value); } }

	public ExpVector pointExp { get { return point.PointExpInPlane(sketch.plane); } }
	public ExpVector lineP0Exp { get { return line.PointsInPlane(sketch.plane).ToArray()[0]; } }
	public ExpVector lineP1Exp { get { return line.PointsInPlane(sketch.plane).ToArray()[1]; } }

	public Vector3 pointPos { get { return point.PointExpInPlane(null).Eval(); } }
	public Vector3 lineP0Pos { get { return line.PointsInPlane(null).ToArray()[0].Eval(); } }
	public Vector3 lineP1Pos { get { return line.PointsInPlane(null).ToArray()[1].Eval(); } }

	public PointLineDistance(Sketch sk) : base(sk) { }

	public PointLineDistance(Sketch sk, IEntity p0, IEntity p1) : base(sk) {
		AddEntity(p0);
		AddEntity(p1);
		SetValue(1.0);
		Satisfy();
	}

	public override IEnumerable<Exp> equations {
		get {
			yield return ConstraintExp.pointLineDistance(pointExp, lineP0Exp, lineP1Exp, sketch.is3d) - value;
		}
	}

	protected override void OnDraw(LineCanvas canvas) {
		
		var lip0 = lineP0Pos;
		var lip1 = lineP1Pos;
		var p0 = pointPos;
		
		if(GetValue() == 0.0) {
			drawCameraCircle(canvas, Camera.main, p0, R_CIRLE_R * getPixelSize()); 
		} else {
			drawPointLineDistance(lip0, lip1, p0, canvas, Camera.main);
			//drawLineExtendInPlane(getPlane(), renderer, lip0, lip1, p0, R_DASH * camera->getPixelSize()); 
		}
	}

	protected override Matrix4x4 OnGetBasis() {
		var lip0 = lineP0Pos;
		var lip1 = lineP1Pos;
		var p0 = pointPos;
		return getPointLineDistanceBasis(lip0, lip1, p0, getPlane());
	}

}
