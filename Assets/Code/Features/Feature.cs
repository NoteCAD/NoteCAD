using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public abstract class Feature : CADObject {
	IEnumerator<Entity> entities { get { yield break; } }
	Feature source_;
	public Detail detail;

	Guid guid_;
	public override Guid guid {
		get {
			return guid_;
		}
		protected set {
			guid_ = value;
		}
	}

	public override CADObject parentObject {
		get {
			return detail;
		}
	}

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

	public abstract GameObject gameObject { get; }

	bool dirty_ = true;
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

	public Feature() {
		guid = Guid.NewGuid();
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
		xml.WriteStartElement("feature");
		xml.WriteAttributeString("type", this.GetType().Name);
		xml.WriteAttributeString("guid", guid.ToString());
		if(source != null) {
			xml.WriteAttributeString("source", source.guid.ToString());
		}
		OnWrite(xml);
		xml.WriteEndElement();
	}

	protected virtual void OnWrite(XmlTextWriter xml) {

	}

	public virtual void Read(XmlNode xml) {
		guid = new Guid(xml.Attributes["guid"].Value);
		if(xml.Attributes.GetNamedItem("source") != null) {
			var srcGuid = new Guid(xml.Attributes["source"].Value);
			source = detail.GetFeature(srcGuid);
		}

		OnRead(xml);
	}

	protected virtual void OnRead(XmlNode xml)  {
	
	}

	protected virtual void OnClear() {

	}

	public void Clear() {
		OnClear();
	}

	public ISketchObject Hover(Vector3 mouse, Camera camera, Matrix4x4 tf, ref double dist) {
		return OnHover(mouse, camera, tf, ref dist);
	}

	protected virtual ISketchObject OnHover(Vector3 mouse, Camera camera, Matrix4x4 tf, ref double dist) {
		return null;
	}


	protected virtual void OnShow(bool state) {

	}

	bool visible_ = true;
	public bool visible {
		set {
			if(visible_ == value) return;
			visible_ = value;
			OnShow(visible_);
		}

		get {
			return visible_;
		}
	}

	protected virtual void OnActivate(bool state) {

	}

	bool active_ = true;
	public bool active {
		set {
			if(active_ == value) return;
			active_ = value;
			OnActivate(active_);
		}

		get {
			return active_;
		}
	}

	public virtual bool ShouldHoverWhenInactive() {
		return true;
	}

	public void GenerateEquations(EquationSystem sys) {
		OnGenerateEquations(sys);
	}

	protected virtual void OnGenerateEquations(EquationSystem sys) {
	}

}

public abstract class MeshFeature : Feature {

	public Mesh GenerateMesh() {
		return OnGenerateMesh();
	}

	protected abstract Mesh OnGenerateMesh();
}

