using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineEntity : Entity, ISegmentaryEntity {

	public PointEntity p0;
	public PointEntity p1;

	public LineEntity(Sketch sk) : base(sk) {
		p0 = AddChild(new PointEntity(sk));
		p1 = AddChild(new PointEntity(sk));
	}

	public override IEntityType type { get { return IEntityType.Line; } }

	public override IEnumerable<PointEntity> points {
		get {
			yield return p0;
			yield return p1;
		}
	}

	public override bool IsChanged() {
		return p0.IsChanged() || p1.IsChanged();
	}

	public PointEntity begin { get { return p0; } }
	public PointEntity end { get { return p1; } }
	public IEnumerable<Vector3> segmentPoints {
		get {
			yield return p0.GetPosition();
			yield return p1.GetPosition();
		}
	}

	public override BBox bbox { get { return new BBox(p0.pos, p1.pos); } }

	protected override Entity OnSplit(Vector3 position) {
		var part = new LineEntity(sketch);
		part.p1.pos = p1.pos;
		p1.pos = position;
		part.p0.pos = p1.pos;
		return part;
	}

}
