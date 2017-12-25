using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Xml;

public abstract partial class Entity : SketchObject {

	protected List<Constraint> usedInConstraints = new List<Constraint>();
	List<Entity> children = new List<Entity>();
	public Entity parent { get; private set; }

	public IEnumerable<Constraint> constraints { get { return usedInConstraints.AsEnumerable(); } }

	protected T AddChild<T>(T e) where T : Entity {
		children.Add(e);
		e.parent = this;
		return e;
	}

	public Entity(Sketch sketch) : base(sketch) {
		sketch.AddEntity(this);
	}

	public virtual IEnumerable<PointEntity> points { get { yield break; } }

	public virtual BBox bbox { get { return new BBox(Vector3.zero, Vector3.zero); } }

	protected override void OnDrag(Vector3 delta) {
		foreach(var p in points) {
			p.Drag(delta);
		}
	}

	public override void Destroy() {
		if(isDestroyed) return;
		base.Destroy();
		if(parent != null) {
			parent.Destroy();
		}
		while(usedInConstraints.Count > 0) {
			usedInConstraints[0].Destroy();
		}
		while(children.Count > 0) {
			children[0].Destroy();
			children.RemoveAt(0);
		}
		GameObject.Destroy(gameObject);
	}

	public override void Write(XmlTextWriter xml) {
		xml.WriteStartElement("entity");
		xml.WriteAttributeString("type", this.GetType().Name);
		base.Write(xml);
		if(children.Count > 0) {
			xml.WriteStartElement("children");
			foreach(var c in children) {
				c.Write(xml);
			}
			xml.WriteEndElement();
		}
		xml.WriteEndElement();
	}

	public override void Read(XmlNode xml) {
		base.Read(xml);
		foreach(XmlNode xmlChildren in xml.ChildNodes) {
			if(xmlChildren.Name != "children") continue;
			int i = 0;
			foreach(XmlNode xmlChild in xmlChildren.ChildNodes) {
				children[i].Read(xmlChild);
				i++;
			}
		}
	}

	public virtual bool IsCrossed(Entity e, ref Vector3 itr) {
		if(!e.bbox.Overlaps(bbox)) return false;
		if(this is ISegmentaryEntity && e is ISegmentaryEntity) {
			var self = this as ISegmentaryEntity;
			var entity = e as ISegmentaryEntity;

			Vector3 selfPrev = Vector3.zero;
			bool selfFirst = true;
			foreach(var sp in self.segmentPoints) {
				if(!selfFirst) {
					Vector3 otherPrev = Vector3.zero;
					bool otherFirst = true;
					foreach(var ep in entity.segmentPoints) {
						if(!otherFirst) {
							if(GeomUtils.isSegmentsCrossed(selfPrev, sp, otherPrev, ep, ref itr, 1e-6f) == GeomUtils.Cross.INTERSECTION) {
								return true;
							}
						}
						otherFirst = false;
						otherPrev = ep;
					}
				}
				selfFirst = false;
				selfPrev = sp;
			}
		}
		return false;
	}

	public bool IsEnding(PointEntity p) {
		if(!(this is ISegmentaryEntity)) return false;
		var se = this as ISegmentaryEntity;
		return se.begin == p || se.end == p;
	}

	protected virtual Entity OnSplit(Vector3 position) {
		return null;
	}

	public Entity Split(Vector3 position) {
		return OnSplit(position);
	}
		
}

public interface ISegmentaryEntity {
	PointEntity begin { get; }
	PointEntity end { get; }
	IEnumerable<Vector3> segmentPoints { get; }
}

public interface ILoopEntity {
	IEnumerable<Vector3> loopPoints { get; }
}