using System.Collections.Generic;
using UnityEngine;

public abstract class Entity : SketchObject {

	public Entity(Sketch sketch) : base(sketch) {
		sketch.AddEntity(this);
	}

	protected abstract GameObject gameObject { get; }

	public virtual IEnumerable<PointEntity> points { get { yield break; } }

	Color oldColor;

	bool hovered;
	public bool isHovered {
		get {
			return hovered;
		}
		set {
			if(value == hovered) return;
			var r = gameObject.GetComponent<Renderer>();
			hovered = value;
			if(!hovered) {
				r.material.color = oldColor;
				r.material.renderQueue--;
			} else {
				oldColor = r.material.color;
				r.material.color = Color.yellow;
				r.material.renderQueue++;
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