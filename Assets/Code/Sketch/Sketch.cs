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

public interface IPlane {
	Vector3 u { get; }
	Vector3 v { get; }
	Vector3 n { get; }
	Vector3 o { get; }
}

public static class IPlaneUtils {

	public static Quaternion GetRotation(this IPlane plane) {
		return Quaternion.LookRotation(plane.n, plane.v);
	}

	public static Matrix4x4 GetTransform(this IPlane plane) {
		var result = Matrix4x4.identity;
		result.SetColumn(0, plane.u);
		result.SetColumn(1, plane.v);
		result.SetColumn(2, plane.n);
		Vector4 pos = new Vector4(plane.o.x, plane.o.y, plane.o.z, 1);
		result.SetColumn(3, pos);
		return result;
	}

	public static ExpVector FromPlane(this IPlane plane, ExpVector pt) {
		if(plane == null) return pt;
		return plane.o + (ExpVector)plane.u * pt.x + (ExpVector)plane.v * pt.y + (ExpVector)plane.n * pt.z;
	}

	public static Vector3 FromPlane(this IPlane plane, Vector3 pt) {
		if(plane == null) return pt;
		return plane.o + plane.u * pt.x + plane.v * pt.y + plane.n * pt.z;
	}

	public static IEnumerable<Vector3> FromPlane(this IPlane plane, IEnumerable<Vector3> points) {
		if(plane == null) return points;

		var pu = plane.u;
		var pv = plane.v;
		var pn = plane.n;
		var po = plane.o;

		return points.Select(pt => po + pu * pt.x + pv * pt.y + pn * pt.z);
	}

	public static ExpVector ToPlane(this IPlane plane, ExpVector pt) {
		if(plane == null) return pt;
		ExpVector result = new ExpVector(0, 0, 0);
		var dir = pt - plane.o;
		result.x = ExpVector.Dot(dir, plane.u);
		result.y = ExpVector.Dot(dir, plane.v);
		result.z = ExpVector.Dot(dir, plane.n);
		return result;
	}

	public static Vector3 ToPlane(this IPlane plane, Vector3 pt) {
		if(plane == null) return pt;
		Vector3 result = new Vector3(0, 0, 0);
		var dir = pt - plane.o;
		result.x = Vector3.Dot(dir, plane.u);
		result.y = Vector3.Dot(dir, plane.v);
		result.z = Vector3.Dot(dir, plane.n);
		return result;
	}

	public static Vector3 projectVectorInto(this IPlane plane, Vector3 val) {
		if(plane == null) return val;
		Vector3 r = val - plane.o;
		float up = Vector3.Dot(r, plane.u);
		float vp = Vector3.Dot(r, plane.v);
		return plane.u * up + plane.v * vp + plane.o;
	}

	public static IEnumerable<Vector3> ToPlane(this IPlane plane, IEnumerable<Vector3> points) {
		if(plane == null) return points;

		var pu = plane.u;
		var pv = plane.v;
		var pn = plane.n;
		var po = plane.o;

		return points.Select(pt => {
			var dir = pt - po;
			return new Vector3(
				Vector3.Dot(dir, pu),
				Vector3.Dot(dir, pv),
				Vector3.Dot(dir, pn)
			);
		});
	}

	public static ExpVector ToFrom(this IPlane to, ExpVector pt, IPlane from) {
		return to.ToPlane(from.FromPlane(pt));
	}

	public static IEnumerable<Vector3> ToFrom(this IPlane to, IEnumerable<Vector3> points, IPlane from) {
		return to.ToPlane(from.FromPlane(points));
	}

}

public class Sketch : CADObject, ISketch  {
	List<Entity> entities = new List<Entity>();
	List<Constraint> constraints = new List<Constraint>();
	Feature feature_;

	public Feature feature {
		get {
			return feature_;
		}

		set {
			feature_ = value;
			if(feature_ != null && guid_ == Id.Null) {
				guid_ = feature.idGenerator.New();
			}
		}
	}
	public IPlane plane;

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

	public IdGenerator idGenerator = new IdGenerator();
	Id guid_;
	public override Id guid {
		get {
			return guid_;
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

	public Entity GetEntity(Id guid) {
		for(int i = 0; i < entities.Count(); i++) {
			if(entities[i].guid == guid) {
				return entities[i];
			}
		}
		return null;
	}

	public Constraint GetConstraint(Id guid) {
		for(int i = 0; i < constraints.Count(); i++) {
			if(constraints[i].guid == guid) {
				return constraints[i];
			}
		}
		return null;
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
		constraintsTopologyChanged = constraintsTopologyChanged || constraints;
		entitiesChanged = entitiesChanged || entities;
		loopsChanged = loopsChanged || loops;
	}

	IEntity ISketch.Hover(Vector3 mouse, Camera camera, Matrix4x4 tf, ref double objDist) {
		return Hover(mouse, camera, tf, ref objDist) as IEntity;
	}
	public static double hoverRadius = 5.0;
	public SketchObject Hover(Vector3 mouse, Camera camera, Matrix4x4 tf, ref double objDist) {
		double min = -1.0;
		SketchObject hoveredObject = null;
		foreach(var e in entities) {
			if(!e.isSelectable) continue;
			var dist = e.Select(Input.mousePosition, camera, tf);
			if(dist < 0.0) continue;
			if(dist > hoverRadius) continue;
			if(min >= 0.0 && dist > min) continue;
			min = dist;
			hoveredObject = e;
		}
		foreach(var c in constraints) {
			if(!c.isSelectable) continue;
			var dist = c.Select(Input.mousePosition, camera, tf);
			if(dist < 0.0) continue;
			if(dist > Sketch.hoverRadius) continue;
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
					.Where(p => p.parent != null && p.parent.IsEnding(p))
					.Select(p => p.parent)
					.OfType<ISegmentaryEntity>();
				if(connected.Any()) {
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
				constraintsTopologyChanged = true;
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
		foreach(var c in constraints) {
			c.Destroy();
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

	public override ICADObject GetChild(Id guid) {
		var e = GetEntity(guid);
		if(e != null) return e;
		return GetConstraint(guid);
	}

	public Bounds calculateBounds() {
		var points = entities.SelectMany(e => e.SegmentsInPlane(null)).ToArray();
		if(points.Length == 0) return new Bounds();
		return GeometryUtility.CalculateBounds(points, Matrix4x4.identity);
	}
}
