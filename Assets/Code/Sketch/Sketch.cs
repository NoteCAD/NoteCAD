using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;
using System.Xml;
using System.Collections;

interface ISketch {
	IEnumerable<IEntity> entities { get; }
	IEntity Hover(Vector3 mouse, Camera camera, Matrix4x4 transform, ref double dist);
}

public class Sketch : CADObject, ISketch  {
	List<Entity> entities = new List<Entity>();
	List<Constraint> constraints = new List<Constraint>();
	public Feature feature;

	IEnumerable<IEntity> ISketch.entities {
		get {
			foreach(var e in entities) yield return e;
		}
	}

	public IEnumerable<Entity> entityList {
		get {
			return entities.AsEnumerable();
		}
	}

	public IEnumerable<Constraint> constraintList {
		get {
			return constraints.AsEnumerable();
		}
	}

	Guid guid_ = Guid.NewGuid();
	public override Guid guid {
		get {
			return guid_;
		}

		protected set {
			guid_ = value;
		}
	}

	public override CADObject parentObject {
		get {
			return feature;
		}
	}

	public void AddEntity(Entity e) {
		if(entities.Contains(e)) return;
		entities.Add(e);
		MarkDirtySketch(topo:true, entities:true);
	}

	public Entity GetEntity(Guid guid) {
		return entities.Find(e => e.guid == guid);
	}

	public void AddConstraint(Constraint c) {
		if(constraints.Contains(c)) return;
		constraints.Add(c);
		MarkDirtySketch(topo:c is PointsCoincident, constraints:true);
		constraintsTopologyChanged = true;
	}

	public bool constraintsTopologyChanged = true;
	public bool constraintsChanged = true;
	public bool entitiesChanged = true;
	public bool loopsChanged = true;
	public bool topologyChanged = true;

	public bool IsDirty() {
		return constraintsTopologyChanged || constraintsChanged || entitiesChanged || loopsChanged || topologyChanged;
	}

	public void MarkDirtySketch(bool topo = false, bool constraints = false, bool entities = false, bool loops = false) {
		topologyChanged = topologyChanged || topo;
		constraintsChanged = constraintsChanged || constraints;
		entitiesChanged = entitiesChanged || entities;
		loopsChanged = loopsChanged || loops;
	}

	IEntity ISketch.Hover(Vector3 mouse, Camera camera, Matrix4x4 tf, ref double objDist) {
		return Hover(mouse, camera, tf, ref objDist) as IEntity;
	}

	public SketchObject Hover(Vector3 mouse, Camera camera, Matrix4x4 tf, ref double objDist) {
		double min = -1.0;
		SketchObject hoveredObject = null;
		foreach(var e in entities) {
			if(!e.isSelectable) continue;
			var dist = e.Select(Input.mousePosition, camera, tf);
			if(dist < 0.0) continue;
			if(dist > 5.0) continue;
			if(min >= 0.0 && dist > min) continue;
			min = dist;
			hoveredObject = e;
		}
		foreach(var c in constraints) {
			if(!c.isSelectable) continue;
			var dist = c.Select(Input.mousePosition, camera, tf);
			if(dist < 0.0) continue;
			if(dist > 5.0) continue;
			if(min >= 0.0 && dist > min) continue;
			min = dist;
			hoveredObject = c;
		}
		objDist = min;
		return hoveredObject;
	}

	public bool IsConstraintsChanged() {
		return constraintsChanged || constraints.Any(c => c.IsChanged());
	}

	public bool IsEntitiesChanged() {
		return entitiesChanged || entities.Any(e => e.IsChanged());
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
		constraintsTopologyChanged = false;
		constraintsChanged = false;
		entitiesChanged = false;
		loopsChanged = false;
		topologyChanged = false;
	}

	public List<List<Entity>> GenerateLoops() {
		var all = entities.OfType<ISegmentaryEntity>().ToList();
		var first = all.FirstOrDefault();
		var current = first;
		PointEntity prevPoint = null;
		ISegmentaryEntity prev = null;
		List<Entity> loop = new List<Entity>();
		List<List<Entity>> loops = new List<List<Entity>>();
		while(current != null && all.Count > 0) {
			if(!all.Remove(current)) {
				break;
			}
			loop.Add(current as Entity);
			var points = new List<PointEntity> { current.begin, current.end };
			bool found = false;
			foreach(var point in points) {
				var connected = point.constraints
					.OfType<PointsCoincident>()
					.Select(p => p.GetOtherPoint(point))
					.Where(p => p != prevPoint)
					.Where(p => p.parent.IsEnding(p))
					.Select(p => p.parent)
					.OfType<ISegmentaryEntity>();
				if(connected.Any()) {
					prev = current;
					current = connected.First() as ISegmentaryEntity;
					found = true;
					prevPoint = point;
					break;
				}
			}
			if(!found || current == first) {
				if(found && current == first) {
					loops.Add(loop);
				}
				loop = new List<Entity>();
				first = all.FirstOrDefault();
				current = first;
				continue;
			}
		}
		loops.AddRange(entities.OfType<ILoopEntity>().Select(e => Enumerable.Repeat(e as Entity, 1).ToList()));
		return loops;
	}

	public static List<List<Vector3>> GetPolygons(List<List<Entity>> loops) {
		var result = new List<List<Vector3>>();
		if(loops == null) return result;
		foreach(var loop in loops) {
			var polygon = new List<Vector3>();
			for(int i = 0; i < loop.Count; i++) {
				if(loop[i] is ISegmentaryEntity) {
					var cur = loop[i] as ISegmentaryEntity;
					var next = loop[(i + 1) % loop.Count] as ISegmentaryEntity;
					if(!next.begin.IsCoincidentWith(cur.begin) && !next.end.IsCoincidentWith(cur.begin)) {
						polygon.AddRange(cur.segmentPoints);
					} else 
					if(!next.begin.IsCoincidentWith(cur.end) && !next.end.IsCoincidentWith(cur.end)) {
						polygon.AddRange(cur.segmentPoints.Reverse());
					} else if(next.begin.IsCoincidentWith(cur.end)) {
						polygon.AddRange(cur.segmentPoints);
					} else
					if(i % 2 == 0) {
						polygon.AddRange(cur.segmentPoints);
					} else {
						polygon.AddRange(cur.segmentPoints.Reverse());
					}
				} else
				if(loop[i] is ILoopEntity) {
					var cur = loop[i] as ILoopEntity;
					polygon.AddRange(cur.loopPoints);
				} else {
					continue;
				}
				polygon.RemoveAt(polygon.Count - 1);
			}
			if(polygon.Count < 3) continue;
			if(!Triangulation.IsClockwise(polygon)) {
				polygon.Reverse();
			}
			result.Add(polygon);
		}
		return result;
	}

	public void Write(XmlTextWriter xml) {
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
	}

	public void Read(XmlNode xml) {
		Type[] types = { typeof(Sketch) };
		object[] param = { this };
		foreach(XmlNode nodeKind in xml.ChildNodes) {
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
		if(DetailEditor.instance.hovered == sko) {
			DetailEditor.instance.hovered = null;
		}
		if(sko is Constraint) {
			var c = sko as Constraint;
			if(constraints.Remove(c)) {
				c.Destroy();
				MarkDirtySketch(topo:c is PointsCoincident, constraints:true);
			} else {
				Debug.Log("Can't remove this constraint!");
			}
		}
		if(sko is Entity) {
			var e = sko as Entity;
			if(entities.Remove(e)) {
				e.Destroy();
				MarkDirtySketch(topo:true, entities:true);
			} else {
				Debug.Log("Can't remove this entity!");
			}
		}
	}

	public void Clear() {
		while(entities.Count > 0) {
			entities[0].Destroy();
		}
		MarkDirtySketch(topo:true, entities:true, constraints:true, loops:true);
	}

	public bool IsCrossed(Entity entity, ref Vector3 intersection) {
		foreach(var e in entities) {
			if(e == entity) continue;
			if(e.IsCrossed(entity, ref intersection)) {
				return true;
			}
		}
		return false;
	}

	public void ReplaceEntityInConstraints(Entity before, Entity after) {
		foreach(var c in constraints) {
			if(c.ReplaceEntity(before, after)) {
				MarkDirtySketch(constraints:true, topo:c is PointsCoincident);
			}
		}
	}

	public void GenerateEquations(EquationSystem system) {
		foreach(var e in entities) {
			system.AddParameters(e.parameters);
			system.AddEquations(e.equations);
		}
		foreach(var c in constraints) {
			system.AddParameters(c.parameters);
			system.AddEquations(c.equations);
		}
	}

	public override CADObject GetChild(Guid guid) {
		var e = GetEntity(guid);
		if(e != null) return e;
		return constraints.Find(c => c.guid == guid);
	}
}
