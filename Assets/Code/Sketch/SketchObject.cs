using System.Collections.Generic;
using System.Xml;
using System;
using UnityEngine;

public abstract class CADObject {
	
	public Guid guid { get; private set; }

	CADObject parent;

}

public abstract class SketchObject {

	Sketch sk;
	public Sketch sketch { get { return sk; } }
	public bool isDestroyed { get; private set; }
	public Guid guid { get; private set; }

	public SketchObject(Sketch sketch) {
		sk = sketch;
		guid = Guid.NewGuid();
	}
	protected virtual GameObject gameObject { get { return null; } }

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
			if(gameObject == null) return;
			var r = gameObject.GetComponent<Renderer>();
			var t = gameObject.GetComponent<UnityEngine.UI.Text>();
			if(isError) return;
			if(!hovered) {
				if(r != null) {
					r.material.color = oldColor;
					r.material.renderQueue--;
				} else if(t != null) {
					t.color = oldColor;
				}
			} else {
				if(r != null) {
					oldColor = r.material.color;
					r.material.color = Color.yellow;
					r.material.renderQueue++;
				} else if(t != null) {
					oldColor = t.color;
					t.color = Color.yellow;
				}
			}
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
			if(gameObject == null) return;
			var r = gameObject.GetComponent<Renderer>();
			var t = gameObject.GetComponent<UnityEngine.UI.Text>();
			if(!error) {
				if(r != null) {
					r.material.color = oldColor;
					r.material.renderQueue--;
				} else if(t != null) {
					t.color = oldColor;
				}
			} else {
				if(r != null) {
					oldColor = r.material.color;
					r.material.color = Color.red;
					r.material.renderQueue++;
				} else if(t != null) {
					oldColor = t.color;
					t.color = Color.red;
				}
			}
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
			if(gameObject == null) return;
			var c = gameObject.GetComponent<Collider>();
			if(c == null) return;
			c.enabled = selectable;
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
		xml.WriteAttributeString("guid", guid.ToString());
		OnWrite(xml);
	}

	protected virtual void OnWrite(XmlTextWriter xml) {

	}

	public virtual void Read(XmlNode xml) {
		guid = new Guid(xml.Attributes["guid"].Value);
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

	public double Select(Vector3 mouse, Camera camera) {
		return OnSelect(mouse, camera);
	}

	protected virtual double OnSelect(Vector3 mouse, Camera camera) {
			return -1.0;
	}

}
