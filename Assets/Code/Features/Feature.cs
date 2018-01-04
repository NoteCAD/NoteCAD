using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public abstract class Feature {
	IEnumerator<Entity> entities { get { yield break; } }
	Feature source_;
	public Feature source {
		get {
			return source_;
		}
		set {
			if(source_ != null) {
				source_.children.Remove(this);
			}
			source_ = value;
			if(source_ != null) {
				source_.children.Add(this);
			}
		}
	}
	List<Feature> children = new List<Feature>();

	bool dirty_;
	public bool dirty {
		get {
			return dirty_;
		}
	}

	public void MarkDirty() {
		dirty_ = true;
		foreach(var c in children) {
			c.MarkDirty();
		}
	}

	protected virtual void OnUpdate() { }

	public void Update() {
		OnUpdate();
	}

	protected virtual void OnUpdateDirty() { }

	public void UpdateDirty() {
		if(!dirty) return;
		OnUpdateDirty();
		dirty_ = false;
	}

	public virtual void Write(XmlTextWriter xml) {
		//xml.WriteAttributeString("guid", guid.ToString());
		xml.WriteStartElement("feature");
		xml.WriteAttributeString("type", this.GetType().Name);
		OnWrite(xml);
		xml.WriteEndElement();
	}

	protected virtual void OnWrite(XmlTextWriter xml) {

	}

	public virtual void Read(XmlNode xml) {
		//guid = new Guid(xml.Attributes["guid"].Value);
		OnRead(xml);
	}

	protected virtual void OnRead(XmlNode xml)  {
	
	}

	protected virtual void OnClear() {

	}

	public void Clear() {
		OnClear();
	}

	public SketchObject Hover(Vector3 mouse, Camera camera, ref double dist) {
		return OnHover(mouse, camera, ref dist);
	}

	protected virtual SketchObject OnHover(Vector3 mouse, Camera camera, ref double dist) {
		return null;
	}
}

public abstract class MeshFeature : Feature {

	public Mesh GenerateMesh() {
		return OnGenerateMesh();
	}

	protected abstract Mesh OnGenerateMesh();
}

