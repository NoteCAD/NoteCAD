using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PointsDistance : ValueConstraint {

	public IEntity p0 { get { return GetEntity(0); } set { SetEntity(0, value); } }
	public IEntity p1 { get { return GetEntity(1); } set { SetEntity(1, value); } }

	public ExpVector p0exp { get { return p0.PointExpInPlane(sketch.plane); } }
	public ExpVector p1exp { get { return p1.PointExpInPlane(sketch.plane); } }

	public PointsDistance(Sketch sk) : base(sk) { }

	public PointsDistance(Sketch sk, IEntity p0, IEntity p1) : base(sk) {
		AddEntity(p0);
		AddEntity(p1);
		Satisfy();
	}

	public override IEnumerable<Exp> equations {
		get {
			yield return (p1exp - p0exp).Magnitude() - value.exp;
		}
	}

	protected override void OnDraw(LineCanvas canvas) {
		Vector3 p0p = p0.PointExpInPlane(null).Eval();
		Vector3 p1p = p1.PointExpInPlane(null).Eval();
		drawPointsDistance(p0p, p1p, canvas, Camera.main, false, true, true, 0);
	}

	protected override Matrix4x4 OnGetBasis() {
		var p0pos = p0.PointExpInPlane(null).Eval();
		var p1pos = p1.PointExpInPlane(null).Eval();
		return getPointsDistanceBasis(p0pos, p1pos, sketch.plane);
	}

}
