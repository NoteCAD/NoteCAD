using System.Collections.Generic;

public class PointsDistance : ValueConstraint {

	public PointEntity p0 { get; private set; }
	public PointEntity p1 { get; private set; }

	public PointsDistance(Sketch sk, PointEntity p0, PointEntity p1) : base(sk) {
		this.p0 = p0;
		this.p1 = p1;
		value.value = (p0.GetPosition() - p1.GetPosition()).magnitude;
	}

	public override IEnumerable<Exp> equations {
		get {
			yield return (p1.exp - p0.exp).Magnitude() - value.exp;
		}
	}
}
