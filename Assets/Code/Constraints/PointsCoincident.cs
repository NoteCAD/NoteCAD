using System.Collections.Generic;

public class PointsCoincident : Constraint {

	public IEntity p0 { get { return GetEntity(0); } set { SetEntity(0, value); } }
	public IEntity p1 { get { return GetEntity(1); } set { SetEntity(1, value); } }

	public PointsCoincident(Sketch sk) : base(sk) { }

	public PointsCoincident(Sketch sk, IEntity p0, IEntity p1) : base(sk) {
		AddEntity(p0);
		AddEntity(p1);
	}

	public override IEnumerable<Exp> equations {
		get {
			var pe0 = p0.GetPointAtInPlane(0, sketch.plane);
			var pe1 = p1.GetPointAtInPlane(0, sketch.plane);
			yield return pe0.x - pe1.x;
			yield return pe0.y - pe1.y;
			if(sketch.is3d) yield return pe0.z - pe1.z;
		}
	}

	public IEntity GetOtherPoint(IEntity p) {
		if(p0 == p) return p1;
		return p0;
	}
}
