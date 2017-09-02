using System.Collections.Generic;
using UnityEngine;

public class Constraint : SketchObject {

	LineCanvas canvas;
	bool firstDrawn = false;

	public Constraint(Sketch sk) : base(sk) {
		sk.AddConstraint(this);
		var go = new GameObject("constraint");
		canvas = go.AddComponent<LineCanvas>();
	}

	public void Draw() {
		if(firstDrawn && !IsChanged()) return;
		firstDrawn = true;
		canvas.Clear();
		OnDraw(canvas);
	}
	
	protected virtual bool IsChanged() {
		return false;
	}

	protected virtual void OnDraw(LineCanvas canvas) {
		
	}
}

public class ValueConstraint : Constraint {

	protected Param value = new Param("value");
	public bool reference;

	public ValueConstraint(Sketch sk) : base(sk) {
	}

	public override IEnumerable<Param> parameters {
		get {
			if(!reference) yield break;
			yield return value;
		}
	}

}

