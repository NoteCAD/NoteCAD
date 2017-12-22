using System.Collections.Generic;

public class PointsCoincident : Constraint {

	public PointEntity p0 { get { return GetEntity(0) as PointEntity; } set { SetEntity(0, value); } }
	public PointEntity p1 { get { return GetEntity(1) as PointEntity; } set { SetEntity(1, value); } }

	public IEnumerable<PointEntity> points {
		get {
			yield return p0;
			yield return p1;
		}
	}

	public PointsCoincident(Sketch sk) : base(sk) { }

	public PointsCoincident(Sketch sk, PointEntity p0, PointEntity p1) : base(sk) {
		AddEntity(p0);
		AddEntity(p1);
	}

	public override IEnumerable<Exp> equations {
		get {
			yield return p0.x.exp - p1.x;
			yield return p0.y.exp - p1.y;
			yield return p0.z.exp - p1.z;
		}
	}

	public PointEntity GetOtherPoint(PointEntity p) {
		if(p0 == p) return p1;
		return p0;
	}
}
