using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityBehaviour : MonoBehaviour {
	public Entity entity;

	public void OnMouseEnter() {
		Sketch.instance.hovered = entity;
	}

	public void OnMouseExit() {
		if(Sketch.instance.hovered != entity) return;
		Sketch.instance.hovered = null;
	}

}
