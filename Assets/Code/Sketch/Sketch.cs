using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;
using System.Xml;

public class Sketch : MonoBehaviour {
	List<Entity> entities = new List<Entity>();
	List<Constraint> constraints = new List<Constraint>();
	public static Sketch instance;
	public Text resultText;
	public Canvas canvas;
	public GameObject labelParent;
	bool sysDirty;
	EquationSystem sys = new EquationSystem();
	Exp dragX;
	Exp dragY;

	SketchObject hovered_;
	public SketchObject hovered {
		get {
			return hovered_;
		}
		set {
			if(hovered_ == value) return;
			if(hovered_ != null) {
				hovered_.isHovered = false;
			}
			hovered_ = value;
			if(hovered_ != null) {
				hovered_.isHovered = true;
			}
		}
	}

	private void Start() {
		instance = this;
	}

	public void SetDrag(Exp dragX, Exp dragY) {
		if(this.dragX != dragX) {
			if(this.dragX != null) sys.RemoveEquation(this.dragX);
			this.dragX = dragX;
			if(dragX != null) sys.AddEquation(dragX);
		}
		if(this.dragY != dragY) {
			if(this.dragY != null) sys.RemoveEquation(this.dragY);
			this.dragY = dragY;
			if(dragY != null) sys.AddEquation(dragY);
		}
	} 

	public void AddEntity(Entity e) {
		if(entities.Contains(e)) return;
		entities.Add(e);
		sysDirty = true;
	}

	public Entity GetEntity(Guid guid) {
		return entities.Find(e => e.guid == guid);
	}

	public void AddConstraint(Constraint c) {
		if(constraints.Contains(c)) return;
			constraints.Add(c);
		sysDirty = true;
	}

	void UpdateSystem() {
		if(!sysDirty) return;
		sys.Clear();
		foreach(var e in entities) {
			sys.AddParameters(e.parameters);
			sys.AddEquations(e.equations);
		}
		foreach(var c in constraints) {
			sys.AddParameters(c.parameters);
			sys.AddEquations(c.equations);
		}
		sysDirty = false;
	}

	private void Update() {
		UpdateSystem();
		var result = sys.Solve();
		resultText.text = result.ToString();
	}

	private void LateUpdate() {
		foreach(var c in constraints) {
			c.Draw();
		}
		MarkUnchanged();
	}

	public void MarkUnchanged() {
		foreach(var e in entities) {
			foreach(var p in e.parameters) {
				p.changed = false;
			}
		}
		foreach(var c in constraints) {
			foreach(var p in c.parameters) {
				p.changed = false;
			}
			c.changed = false;
		}
	}

	void GenerateLoops() {
		var all = entities.OfType<ISegmentaryEntity>().ToList();
		var first = all.FirstOrDefault();
		var current = first;
		PointEntity prev = null;
		List<Entity> loop = new List<Entity>();
		while(current != null) {
			var entity = current as Entity;
			loop.Add(entity);
			var connected = current.end.constraints
				.OfType<PointsCoincident>()
				.Select(pc => pc.GetOtherPoint(current.end))
				.Where(p => p != prev);
			//prev = current;
			//current = connected.FirstOrDefault();
		}

	}

	public string WriteXml() {
		var text = new StringWriter();
		var xml = new XmlTextWriter(text);
		xml.Formatting = Formatting.Indented;
		xml.IndentChar = '\t';
		xml.Indentation = 1;
		xml.WriteStartDocument();
		xml.WriteStartElement("sketch");
		xml.WriteStartElement("entities");
		foreach(var e in entities) {
			if(e.parent != null) continue;
			e.Write(xml);
		}
		xml.WriteEndElement();

		xml.WriteStartElement("constraints");
		foreach(var c in constraints) {
			c.Write(xml);
		}
		xml.WriteEndElement();

		xml.WriteEndElement();
		return text.ToString();
	}

	public void ReadXml(string str) {
		var xml = new XmlDocument();
		xml.LoadXml(str);

		Type[] types = { typeof(Sketch) };
		object[] param = { this };
		foreach(XmlNode nodeKind in xml.DocumentElement.ChildNodes) {
			if(nodeKind.Name == "entities") {
				foreach(XmlNode node in nodeKind.ChildNodes) {
					if(node.Name != "entity") continue;
					var type = node.Attributes["type"].Value;
					var entity = Type.GetType(type).GetConstructor(types).Invoke(param) as Entity;
					entity.Read(node);
				}
			}
			if(nodeKind.Name == "constraints") {
				foreach(XmlNode node in nodeKind.ChildNodes) {
					if(node.Name != "constraint") continue;
					var type = node.Attributes["type"].Value;
					var t = Type.GetType(type).GetConstructor(types);
					var constraint = Type.GetType(type).GetConstructor(types).Invoke(param) as Constraint;
					constraint.Read(node);
				}
			}
		}
	}

	public void Remove(SketchObject sko) {
		if(sko.sketch != this) {
			Debug.Log("Can't remove this constraint!");
			return;
		}
		if(hovered == sko) {
			hovered = null;
		}
		if(sko is Constraint) {
			var c = sko as Constraint;
			if(constraints.Remove(c)) {
				c.Destroy();
				sysDirty = true;
			} else {
				Debug.Log("Can't remove this constraint!");
			}
		}
		if(sko is Entity) {
			var e = sko as Entity;
			if(entities.Remove(e)) {
				e.Destroy();
				sysDirty = true;
			} else {
				Debug.Log("Can't remove this entity!");
			}
		}
	}

}
