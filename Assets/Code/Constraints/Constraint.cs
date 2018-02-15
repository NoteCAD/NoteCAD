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
	protected override GameObject gameObject { get { return null; } }
	//List<Entity> entities = new List<Entity>();
	List<IdPath> ids = new List<IdPath>();

	protected void AddEntity<T>(T e) where T : IEntity {
		if(e is Entity) (e as Entity).AddConstraint(this);
		//ientities.Add(e);
		ids.Add(e.id);
		//return e;
	}

	public Constraint(Sketch sk) : base(sk) {
		sk.AddConstraint(this);
	}

	public override void Destroy() {
		if(isDestroyed) return;
		base.Destroy();
		for(int i = 0; i < ids.Count; i++) {
			var ent = GetEntity(i) as Entity;
			if(ent == null) continue;
			ent.RemoveConstraint(this);
		}
	}

	protected override void OnDestroy() {

	}
	
	public override void Write(XmlTextWriter xml) {
		xml.WriteStartElement("constraint");
		xml.WriteAttributeString("type", this.GetType().Name);
		base.Write(xml);
		foreach(var id in ids) {
			xml.WriteStartElement("entity");
			xml.WriteAttributeString("path", id.ToString());
			xml.WriteEndElement();
		}
		xml.WriteEndElement();
	}

	public override void Read(XmlNode xml) {
		ids.Clear();
		foreach(XmlNode node in xml.ChildNodes) {
			if(node.Name != "entity") continue;
			var path = IdPath.From(node.Attributes["path"].Value);
			var e = sketch.feature.detail.GetObjectById(path) as IEntity;
			AddEntity(e);
		}
		base.Read(xml);
	}

	public IEntity GetEntity(int i) {
		return sketch.feature.GetObjectById(ids[i]) as IEntity;
	}

	protected void SetEntity(int i, IEntity e) {
		var ent = GetEntity(i) as Entity;
		if(ent != null) {
			ent.RemoveConstraint(this);
		}
		ids[i] = e.id;
		ent = GetEntity(i) as Entity;
		if(ent != null) {
			ent.AddConstraint(this);
		}
		changed = true;
	}

	public override bool IsChanged() {
		return base.IsChanged() || changed;
	}

	public bool ReplaceEntity(IEntity before, IEntity after) {
		bool result = false;
		var beforeId = before.id;
		for(int i = 0; i < ids.Count; i++) {
			if(ids[i] != beforeId) continue;
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
		return OnGetBasis() * sketch.plane.GetTransform();
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

	protected override double OnSelect(Vector3 mouse, Camera camera, Matrix4x4 tf) {
		var pp = camera.WorldToScreenPoint(tf.MultiplyPoint(behaviour.gameObject.transform.position));
		pp.z = 0f;
		mouse.z = 0f;
		var dist = (pp - mouse).magnitude - 5;
		if(dist < 0.0) return 0.0;
		return dist;
	}
}

