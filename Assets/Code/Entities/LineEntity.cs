using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineEntity : Entity {

	public PointEntity p0;
	public PointEntity p1;

	LineBehaviour behaviour;

	public LineEntity(Sketch sk) : base(sk) {
		p0 = new PointEntity(sk);
		p1 = new PointEntity(sk);
		behaviour = GameObject.Instantiate(EntityConfig.instance.linePrefab);
		behaviour.entity = this;
	}

	protected override GameObject gameObject {
		get {
			return behaviour.gameObject;
		}
	}

	public override IEnumerable<PointEntity> points {
		get {
			yield return p0;
			yield return p1;
		}
	}
}
