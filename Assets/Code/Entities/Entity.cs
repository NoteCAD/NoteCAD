using System.Collections.Generic;
using UnityEngine;

public abstract class Entity : SketchObject {

	public Entity(Sketch sketch) : base(sketch) {
		sketch.AddEntity(this);
	}

	public abstract GameObject gameObject { get; }

	public virtual IEnumerable<Param> parameters { get { yield break; } }
	public virtual IEnumerable<Exp> equations { get { yield break; } }

}