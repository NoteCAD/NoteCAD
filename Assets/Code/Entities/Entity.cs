using System.Collections.Generic;
using UnityEngine;

public abstract partial class Entity : SketchObject {

	List<Constraint> constraints = new List<Constraint>();
	List<Entity> children = new List<Entity>();
	public Entity parent { get; private set; }

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
		while(constraints.Count > 0) {
			constraints[0].Destroy();
		}
		while(children.Count > 0) {
			children[0].Destroy();
			children.RemoveAt(0);
		}
		GameObject.Destroy(gameObject);
	}

}