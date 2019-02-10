using Csg;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public abstract class Feature : CADObject {
	IEnumerator<Entity> entities { get { yield break; } }
	Feature source_;
	public Detail detail_;
	public IdGenerator idGenerator = new IdGenerator();

	public Detail detail {
		get {
			return detail_;
		}

		set {
			detail_ = value;
			if(detail_ != null && guid_ == Id.Null) {
				guid_ = detail.idGenerator.New();
			}
		}
	}

	protected Id guid_;
	public override Id guid {
		get {
			return guid_;
		}
	}

	public virtual Bounds bounds { get { return new Bounds(); } }

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

	public bool sourceChanged { get; private set; }

	public void MarkDirty(bool srcChanged = false) {
		dirty_ = true;
		sourceChanged = sourceChanged || srcChanged;
		foreach(var c in children) {
			//if(c is SketchFeatureBase && (c as SketchFeatureBase).solveParent) continue;
			Debug.Log("MarkDirty srcChanged!");
			c.MarkDirty(true);
		}
		/*
		var s = this;
		while(s is SketchFeatureBase && (s as SketchFeatureBase).solveParent && s.source != null) {
			s = s.source;
			s.MarkDirty();
		}*/
	}

	protected virtual void OnUpdate() { }

	public virtual void Update() {
		OnUpdate();
	}

	protected virtual void OnUpdateDirty() { }

	public virtual void UpdateDirty() {
		if(!dirty) return;
		OnUpdateDirty();
		dirty_ = false;
		sourceChanged = false;
	}

	public void Write(XmlTextWriter xml) {
		xml.WriteStartElement("feature");
		xml.WriteAttributeString("type", this.GetType().Name);
		xml.WriteAttributeString("id", guid.ToString());
		if(source != null) {
			xml.WriteAttributeString("source", source.guid.ToString());
		}
		OnWrite(xml);
		xml.WriteEndElement();
	}

	protected virtual void OnWrite(XmlTextWriter xml) {

	}

	public void Read(XmlNode xml) {
		guid_ = detail.idGenerator.Create(xml.Attributes["id"].Value);
		if(xml.Attributes.GetNamedItem("source") != null) {
			var srcGuid = detail.idGenerator.Create(xml.Attributes["source"].Value);
			source = detail.GetFeature(srcGuid);
		}

		OnRead(xml);
	}

	protected virtual void OnRead(XmlNode xml)  {
	
	}

	protected virtual void OnClear() {

	}

	public virtual void Clear() {
		OnClear();
	}

	static public bool HoverPoint(Vector3 mouse, Camera camera, ref double min, Vector3 p) {
		var p0 = camera.WorldToScreenPoint(p);
		p0.z = 0f;
		double d = (p0 - mouse).magnitude;
		if(d > Sketch.hoverRadius) return false;
		if(min >= 0.0 && d > min) return false;
		min = d;
		return true;
	}

	static public bool HoverSegment(Vector3 mouse, Camera camera, ref double min, Vector3 v0, Vector3 v1) {
		var p0 = camera.WorldToScreenPoint(v0);
		var p1 = camera.WorldToScreenPoint(v1);
		double d = GeomUtils.DistancePointSegment2D(mouse, p0, p1);
		if(d > Sketch.hoverRadius) return false;
		if(min >= 0.0 && d > min) return false;
		min = d;
		return true;
	}

	public virtual void MarqueeSelect(Rect rect, bool wholeObject, Camera camera, UnityEngine.Matrix4x4 tf, ref List<ICADObject> result) {
	}

	public virtual ICADObject Hover(Vector3 mouse, Camera camera, UnityEngine.Matrix4x4 tf, ref double dist) {
		return OnHover(mouse, camera, tf, ref dist);
	}

	protected virtual ICADObject OnHover(Vector3 mouse, Camera camera, UnityEngine.Matrix4x4 tf, ref double dist) {
		return null;
	}

	public virtual void Draw(UnityEngine.Matrix4x4 tf) {
		OnDraw(tf);
	}

	protected virtual void OnDraw(UnityEngine.Matrix4x4 tf) {

	}

	public virtual void Show(bool state) {
		OnShow(state);
	}

	protected virtual void OnShow(bool state) {

	}

	bool visible_ = true;

	[HideInInspector]
	public bool visible {
		set {
			if(visible_ == value) return;
			visible_ = value;
			Show(visible_);
		}

		get {
			return visible_;
		}
	}

	public virtual void Activate(bool state) {
		OnActivate(state);
	}

	protected virtual void OnActivate(bool state) {

	}

	bool active_ = false;
	[HideInInspector]
	public bool active {
		set {
			if(active_ == value) return;
			active_ = value;
			Activate(active_);
		}

		get {
			return active_;
		}
	}

	public virtual bool ShouldHoverWhenInactive() {
		return true;
	}

	public virtual void GenerateEquations(EquationSystem sys) {
		OnGenerateEquations(sys);
	}

	protected virtual void OnGenerateEquations(EquationSystem sys) {
	}

}

public enum CombineOp {
	Union,
	Difference,
	Intersection,
	Assembly
}

public abstract class MeshFeature : SketchFeatureBase {

	Solid solid_ = new Solid();
	public Solid combined;
	CombineOp operation_ = CombineOp.Union;
	public CombineOp operation {
		get {
			return operation_;
		}

		set {
			if(value == operation_) return;
			operation_ = value;
			MarkDirty();
		}
	}


	public Solid solid {
		get {
			return solid_;
		}
	}

	public override void UpdateDirty() {
		if(!dirty) return;
		base.UpdateDirty();
		solid_ = OnGenerateMesh();
		combined = null;
	}

	public Solid GenerateMesh() {
		return OnGenerateMesh();
	}

	protected virtual void OnWriteMeshFeature(XmlTextWriter xml) {

	}

	protected virtual void OnReadMeshFeature(XmlNode xml) {

	}

	protected sealed override void OnWriteSketchFeatureBase(XmlTextWriter xml) {
		xml.WriteAttributeString("op", operation.ToString());
		OnWriteMeshFeature(xml);
	}

	protected sealed override void OnReadSketchFeatureBase(XmlNode xml) {
		xml.Attributes["op"].Value.ToEnum(ref operation_);
		OnReadMeshFeature(xml);
	}

	protected abstract Solid OnGenerateMesh();
}

