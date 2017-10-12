using System.Collections.Generic;

public class PointsCoincident : Constraint {

	public PointEntity p0 { get; private set; }
	public PointEntity p1 { get; private set; }

	public PointsCoincident(Sketch sk, PointEntity p0, PointEntity p1) : base(sk) {
		this.p0 = AddEntity(p0);
		this.p1 = AddEntity(p1);
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
