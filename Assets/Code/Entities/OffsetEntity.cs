using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Linq;

[Serializable]
public class OffsetEntity : Entity, ISegmentaryEntity {

	[NonSerialized]
	public PointEntity p0;

	[NonSerialized]
	public PointEntity p1;

	[NonSerialized]
	public IEntity source;


	int subdivision_ = 64;
	public int subdivision {
		get {
			return subdivision_;
		}
		set {
			subdivision_ = value;
			sketch.MarkDirtySketch(entities:true);
		}
	}
	public ExpressionData offset;

	public OffsetEntity(Sketch sk) : base(sk) {
		p0 = AddChild(new PointEntity(sk));
		p1 = AddChild(new PointEntity(sk));

		offset = new ExpressionData(this, false);
		offset.source = "1";
	}

	public override IEntityType type { get { return IEntityType.Offset; } }

	public override IEnumerable<Exp> equations {
		get {
			ExpVector e0 = source.OffsetAt(0.0, offset.expression);

			var eq0 = e0 - p0.exp;
			yield return eq0.x;
			yield return eq0.y;

			ExpVector e1 = source.OffsetAt(1.0, offset.expression);

			var eq1 = e1 - p1.exp;
			yield return eq1.x;
			yield return eq1.y;
		}
	}

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
	public IEnumerable<IEnumerable<Vector3>> segmentPoints {
		get {
			yield return segmentPts;
		}
	}

	public IEnumerable<Vector3> segmentPts {
		get {
			Param pOn = new Param("pOn");
			var on = PointOn(pOn);
			var subdiv = subdivision;
			for(int i = 0; i <= subdiv; i++) {
				pOn.value = (double)i / subdiv;
				yield return on.Eval();
			}
		}
	}	


	public override ExpVector PointOn(Exp t) {
		return source.OffsetAt(t, offset.expression);
	}

	public override Exp Length() {
		return null;
	}

	public override Exp Radius() {
		return null;
	}

	public override ExpVector Center() {
		return null;
	}

	protected override void OnWrite(Writer xml) {
		xml.WriteAttribute("offset", offset.source);
		xml.WriteAttribute("subdiv", subdivision_.ToString());
		xml.WriteAttribute("source", source.id.ToString());
	}

	protected override void OnRead(XmlNode xml) {
		offset.source = xml.Attributes["offset"].Value;
		subdivision_ = Convert.ToInt32(xml.Attributes["subdiv"].Value);
	}

	protected override void OnAfterRead(XmlNode xml)
	{
		var path = IdPath.From(xml.Attributes["source"].Value);
		if(sketch.idMapping != null) {
			source = sketch.GetChild(sketch.idMapping[path.path.Last()]) as IEntity;
		} else {
			source = sketch.feature.detail.GetObjectById(path) as IEntity;
		}
	}
}
