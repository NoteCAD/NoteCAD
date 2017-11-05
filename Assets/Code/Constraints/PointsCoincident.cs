using System.Collections.Generic;

public class PointsCoincident : Constraint {

	public PointEntity p0 { get; private set; }
	public PointEntity p1 { get; private set; }

	public IEnumerable<PointEntity> points {
		get {
			yield return p0;
			yield return p1;
		}
	}

	public PointsCoincident(Sketch sk) : base(sk) { }

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

	protected override void OnRead(System.Xml.XmlNode xml) {
		p0 = GetEntity(0) as PointEntity;
		p1 = GetEntity(1) as PointEntity;
	}
}
