using System.Collections.Generic;
using System.Xml;
using System;
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
	}

	public override void Destroy() {
		if(isDestroyed) return;
		base.Destroy();
		foreach(var e in entities) {
			e.RemoveConstraint(this);
		}
	}

	protected override void OnDestroy() {

	}
	
	public override void Write(XmlTextWriter xml) {
		xml.WriteStartElement("constraint");
		xml.WriteAttributeString("type", this.GetType().Name);
		base.Write(xml);
		foreach(var e in entities) {
			xml.WriteStartElement("entity");
			xml.WriteAttributeString("guid", e.guid.ToString());
			xml.WriteEndElement();
		}
		xml.WriteEndElement();
	}

	public override void Read(XmlNode xml) {
		foreach(XmlNode node in xml.ChildNodes) {
			if(node.Name != "entity") continue;
			var guid = new Guid(node.Attributes["guid"].Value);
			var entity = sketch.GetEntity(guid);
			AddEntity(entity);
		}
		base.Read(xml);
	}

	public Entity GetEntity(int i) {
		return entities[i];
	}

	protected void SetEntity(int i, Entity e) {
		entities[i].RemoveConstraint(this);
		entities[i] = e;
		entities[i].AddConstraint(this);
		changed = true;
	}

	public override bool IsChanged() {
		return base.IsChanged() || changed;
	}

	public bool ReplaceEntity(Entity before, Entity after) {
		bool result = false;
		for(int i = 0; i < entities.Count; i++) {
			if(entities[i] != before) continue;
			SetEntity(i, after);
			result = true;
		}
		return result;
	}
}

public class ValueConstraint : Constraint {

	protected Param value = new Param("value");
	public bool reference;
	Vector3 position_;
	public Vector3 pos {
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
		pos += delta;
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

	protected override void OnWrite(XmlTextWriter xml) {
		xml.WriteAttributeString("x", pos.x.ToString());
		xml.WriteAttributeString("y", pos.y.ToString());
		xml.WriteAttributeString("z", pos.z.ToString());
		xml.WriteAttributeString("value", GetValue().ToString());
	}

	public override void Read(XmlNode xml) {
		base.Read(xml);
		Vector3 pos;
		pos.x = float.Parse(xml.Attributes["x"].Value);
		pos.y = float.Parse(xml.Attributes["y"].Value);
		pos.z = float.Parse(xml.Attributes["z"].Value);
		this.pos = pos;
		SetValue(double.Parse(xml.Attributes["value"].Value));
	}
}

