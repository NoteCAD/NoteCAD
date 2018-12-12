using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[Serializable]
public class LineCircleDistance : ValueConstraint {

	public IEntity line { get { return GetEntity(0); } set { SetEntity(0, value); } }
	public IEntity circle { get { return GetEntity(1); } set { SetEntity(1, value); } }

	public ExpVector centerExp { get { return circle.Center(); } }
	public ExpVector lineP0Exp { get { return line.PointsInPlane(sketch.plane).ToArray()[0]; } }
	public ExpVector lineP1Exp { get { return line.PointsInPlane(sketch.plane).ToArray()[1]; } }

	public Vector3 centerPos { get { return circle.CenterInPlane(null).Eval(); } }
	public Vector3 lineP0Pos { get { return line.PointsInPlane(null).ToArray()[0].Eval(); } }
	public Vector3 lineP1Pos { get { return line.PointsInPlane(null).ToArray()[1].Eval(); } }

	public enum Option {
		Positive,
		Negative
	}

	Option option_;
	public Option option { get { return option_; } set { option_ = value; sketch.MarkDirtySketch(topo:true); } }

	protected override Enum optionInternal { get { return option; } set { option = (Option)value; } }	public LineCircleDistance(Sketch sk) : base(sk) { }


	public LineCircleDistance(Sketch sk, IEntity line, IEntity circle) : base(sk) {
		AddEntity(line);
		AddEntity(circle);
		SetValue(1.0);
		ChooseBestOption();
		Satisfy();
	}

	public override IEnumerable<Exp> equations {
		get {
			switch(option) {
				case Option.Positive: yield return ConstraintExp.pointLineDistance(centerExp, lineP0Exp, lineP1Exp, sketch.is3d) - circle.Radius() - value; break;
				case Option.Negative: yield return ConstraintExp.pointLineDistance(centerExp, lineP0Exp, lineP1Exp, sketch.is3d) + circle.Radius() + value; break;
			}
		}
	}

	protected override void OnDraw(LineCanvas canvas) {
		
		var lip0 = sketch.plane.projectVectorInto(lineP0Pos);
		var lip1 = sketch.plane.projectVectorInto(lineP1Pos);
		var c = sketch.plane.projectVectorInto(centerPos);
		var n = Vector3.Cross(lip1 - lip0, c - lip0).normalized;
		var perp = Vector3.Cross(lip1 - lip0, n).normalized;
		var p0 = c + perp * (float)circle.Radius().Eval();
		
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
		var p0 = centerPos;
		return getPointLineDistanceBasis(lip0, lip1, p0, getPlane());
	}

}
