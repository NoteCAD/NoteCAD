using UnityEngine;

public abstract class Entity {

	public Entity(Sketch sketch) {
		sketch.AddEntity(this);
	}

	public abstract GameObject gameObject { get; }
}