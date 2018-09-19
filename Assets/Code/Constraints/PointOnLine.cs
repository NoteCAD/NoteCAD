using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PointOnLine : ValueConstraint {

	public IEntity point { get { return GetEntity(0); } set { SetEntity(0, value); } }
	public IEntity line { get { return GetEntity(1); } set { SetEntity(1, value); } }

	public ExpVector pointExp { get { return point.PointExpInPlane(sketch.plane); } }
	public ExpVector lineP0Exp { get { return line.PointsInPlane(sketch.plane).ToArray()[0]; } }
	public ExpVector lineP1Exp { get { return line.PointsInPlane(sketch.plane).ToArray()[1]; } }

	public Vector3 pointPos { get { return point.PointExpInPlane(null).Eval(); } }
	public Vector3 lineP0Pos { get { return line.PointsInPlane(null).ToArray()[0].Eval(); } }
	public Vector3 lineP1Pos { get { return line.PointsInPlane(null).ToArray()[1].Eval(); } }

	public PointOnLine(Sketch sk) : base(sk) { }

	public PointOnLine(Sketch sk, IEntity point, IEntity line) : base(sk) {
		AddEntity(point);
		AddEntity(line);
		SetValue(1.0);
		Satisfy();
	}

	public override IEnumerable<Exp> equations {
		get {
			var p = pointExp;
			var p0 = lineP0Exp;
			var p1 = lineP1Exp;

			var eq = p - (p0 + (p1 - p0) * value);
			yield return eq.x;
			yield return eq.y;
			if(sketch.is3d) yield return eq.z;
		}
	}

	public override IEnumerable<Param> parameters {
		get {
			yield return value;
		}
	}

	protected override void OnDraw(LineCanvas canvas) {
		
		var lip0 = lineP0Pos;
		var lip1 = lineP1Pos;
		var p0 = pointPos;

		drawCameraCircle(canvas, Camera.main, p0, R_CIRLE_R * getPixelSize()); 
		drawLineExtendInPlane(sketch.plane, canvas, lip0, lip1, p0, 6f * getPixelSize());
	}

	protected override Matrix4x4 OnGetBasis() {
		var p0 = point.PointExpInPlane(sketch.plane).Eval();
		if(!sketch.is3d) p0.z = 0;
		return getPlane().GetTransform() * Matrix4x4.Translate(p0);
	}

}
