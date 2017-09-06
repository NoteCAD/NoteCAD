using System.Collections.Generic;
using UnityEngine;

public class Constraint : SketchObject {

	LineCanvas canvas;
	bool firstDrawn = false;
	public bool changed;
	protected override GameObject gameObject { get { return canvas.gameObject; } }

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
	
	public virtual bool IsChanged() {
		return OnIsChanged();
	}

	protected virtual bool OnIsChanged() {
		return false;
	}

	protected virtual void OnDraw(LineCanvas canvas) {
		
	}
}

public class ValueConstraint : Constraint {

	protected Param value = new Param("value");
	public bool reference;
	Vector3 position_;
	public Vector3 position {
		get {
			return GetBasis().MultiplyPoint(position_);
		}
		set {
			var newPos = GetBasis().inverse.MultiplyPoint(value);
			if(position_ == newPos) return;
			position_ = newPos;
			changed = true;
		}
	}
	//public bool changed;
	ConstraintBehaviour behaviour;
	protected override GameObject gameObject { get { return behaviour.text.gameObject; } }

	public ValueConstraint(Sketch sk) : base(sk) {
		behaviour = GameObject.Instantiate(EntityConfig.instance.constraint);
		behaviour.constraint = this;
	}

	public override IEnumerable<Param> parameters {
		get {
			if(!reference) yield break;
			yield return value;
		}
	}

	protected override void OnDrag(Vector3 delta) {
		if(delta == Vector3.zero) return;
		position += delta;
	}

	public override bool IsChanged() {
		return base.IsChanged() || changed;
	}


	public Matrix4x4 GetBasis() {
		return OnGetBasis();
	}

	protected virtual Matrix4x4 OnGetBasis() {
		return Matrix4x4.identity;
	}

	public double GetValue() {
		return value.value;
	}

	public void SetValue(double v) {
		value.value = v;
	} 

}

