using System.Collections.Generic;
using UnityEngine;

public abstract class SketchObject {

	Sketch sk;
	public Sketch sketch { get { return sk; } }

	public SketchObject(Sketch sketch) {
		sk = sketch;
	}
	protected abstract GameObject gameObject { get; }

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
			var r = gameObject.GetComponent<Renderer>();
			var t = gameObject.GetComponent<UnityEngine.UI.Text>();
			hovered = value;
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

	bool selectable = true;
	public bool isSelectable {
		get {
			return selectable;
		}
		set {
			if(value == selectable) return;
			selectable = value;
			var c = gameObject.GetComponent<Collider>();
			if(c == null) return;
			c.enabled = selectable;
		}
	}

}
