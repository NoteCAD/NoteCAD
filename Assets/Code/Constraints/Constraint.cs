using System.Collections.Generic;
using UnityEngine;

public abstract partial class Entity {

	internal void AddConstraint(Constraint c) {
		usedInConstraints.Add(c);
	}

	internal void RemoveConstraint(Constraint c) {
		usedInConstraints.Remove(c);
	}
}

public class Constraint : SketchObject {

	LineCanvas canvas;
	bool firstDrawn = false;
	public bool changed;
	protected override GameObject gameObject { get { return canvas.gameObject; } }
	List<Entity> entities = new List<Entity>();

	protected T AddEntity<T>(T e) where T : Entity {
		e.AddConstraint(this);
		entities.Add(e);
		return e;
	}

	public Constraint(Sketch sk) : base(sk) {
		sk.AddConstraint(this);
		var go = new GameObject("constraint");
		canvas = go.AddComponent<LineCanvas>();
	}

	public override void Destroy() {
		if(isDestroyed) return;
		base.Destroy();
		foreach(var e in entities) {
			e.RemoveConstraint(this);
		}
		GameObject.Destroy(canvas.gameObject);
	}

	protected override void OnDestroy() {

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
			behaviour.Update();
		}
	}
	//public bool changed;
	ConstraintBehaviour behaviour;
	protected override GameObject gameObject { get { return behaviour.text.gameObject; } }

	public ValueConstraint(Sketch sk) : base(sk) {
		behaviour = GameObject.Instantiate(EntityConfig.instance.constraint);
		behaviour.constraint = this;
	}

	protected override void OnDestroy() {
		GameObject.Destroy(behaviour.gameObject);
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
		return ValueToLabel(value.value);
	}

	public void SetValue(double v) {
		value.value = LabelToValue(v);
	}

	public virtual double ValueToLabel(double value) {
		return value;
	}

	public virtual double LabelToValue(double label) {
		return label;
	}

	protected virtual bool OnSatisfy() {
		EquationSystem sys = new EquationSystem();
		sys.AddParameter(value);
		sys.AddEquations(equations);
		return sys.Solve() == EquationSystem.SolveResult.OKAY;
	}

	public bool Satisfy() {
		return OnSatisfy();
	}
}

