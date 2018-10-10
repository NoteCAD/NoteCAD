using System.Collections.Generic;
using System.Xml;
using System;
using UnityEngine;

public interface ICADObject {
	IdPath id { get; }
}

public abstract class CADObject : ICADObject {
	public abstract Id guid { get; }
	public abstract ICADObject GetChild(Id guid);
	public abstract CADObject parentObject { get; }

	public IdPath id {
		get {
			return GetRelativePath(null);
		}
	}

	public IdPath GetRelativePath(CADObject from) {
		var result = new IdPath();
		var p = this;
		while(p != null) {
			if(p == from) return result;
			if(p.guid == Id.Null) return result;
			result.path.Insert(0, p.guid);
			p = p.parentObject;
		}
		return result;
	}

	public virtual ICADObject GetObjectById(IdPath id, int index = 0) {
		if(id.path.Count == 0) return null;
		var r = GetChild(id.path[index]);
		var co = r as CADObject;
		if(co == null || index + 1 >= id.path.Count) return r;
		return co.GetObjectById(id, index + 1);
	}
}

public abstract class SketchObject : CADObject, ICADObject {

	Sketch sk;
	public Sketch sketch { get { return sk; } }
	public bool isDestroyed { get; private set; }

	Id guid_;
	public override Id guid { get { return guid_; } }

	public override CADObject parentObject {
		get {
			return sketch;
		}
	}

	public override ICADObject GetChild(Id guid) {
		return null;
	}

	public SketchObject(Sketch sketch) {
		sk = sketch;
		guid_ = sketch.idGenerator.New();
	}

	public virtual IEnumerable<Param> parameters { get { yield break; } }
	public virtual IEnumerable<Exp> equations { get { yield break; } }

	protected virtual void OnDrag(Vector3 delta) { }

	public void Drag(Vector3 delta) {
		OnDrag(delta);
	}

	Color oldColor;

	bool hovered;
	public bool isHovered {
		get {
			return hovered;
		}
		set {
			if(value == hovered) return;
			hovered = value;
		}
	}

	bool error;
	public bool isError {
		get {
			return error;
		}
		set {
			if(value == error) return;
			if(value) {
				isHovered = false;
			}
			error = value;
		}
	}

	bool selectable = true;
	public bool isSelectable {
		get {
			return selectable;
		}
		set {
			if(value == selectable) return;
			selectable = value;
		}
	}

	public virtual void Destroy() {
		if(isDestroyed) return;
		isDestroyed = true;
		sketch.Remove(this);
		OnDestroy();
	}

	protected virtual void OnDestroy() {

	}

	public virtual void Write(XmlTextWriter xml) {
		xml.WriteAttributeString("id", guid.ToString());
		OnWrite(xml);
	}

	protected virtual void OnWrite(XmlTextWriter xml) {

	}

	public virtual void Read(XmlNode xml) {

		guid_ = sketch.idGenerator.Create(xml.Attributes["id"].Value);
		OnRead(xml);
	}

	protected virtual void OnRead(XmlNode xml)  {
	
	}

	public void Draw(LineCanvas canvas) {
		OnDraw(canvas);
	}

	public virtual bool IsChanged() {
		return OnIsChanged();
	}

	protected virtual bool OnIsChanged() {
		return false;
	}

	protected virtual void OnDraw(LineCanvas canvas) {
		
	}

	public double Select(Vector3 mouse, Camera camera, Matrix4x4 tf) {
		return OnSelect(mouse, camera, tf);
	}

	protected virtual double OnSelect(Vector3 mouse, Camera camera, Matrix4x4 tf) {
		return -1.0;
	}

}
