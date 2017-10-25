using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Xml;

public abstract partial class Entity : SketchObject {

	List<Constraint> usedInConstraints = new List<Constraint>();
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
		
}

public interface ISegmentaryEntity {
	PointEntity begin { get; }
	PointEntity end { get; }
	IEnumerable<Vector3> segmentPoints { get; }
}

public interface ILoopEnitity {
}