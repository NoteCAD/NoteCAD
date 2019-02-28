using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;
using System.Xml;
using System.Collections;

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
		if(plane == null) return Matrix4x4.identity;
		return UnityExt.Basis(plane.u, plane.v, plane.n, plane.o);
	}

	public static ExpVector FromPlane(this IPlane plane, ExpVector pt) {
		if(plane == null) return pt;
		return plane.o + (ExpVector)plane.u * pt.x + (ExpVector)plane.v * pt.y + (ExpVector)plane.n * pt.z;
	}

	public static Vector3 FromPlane(this IPlane plane, Vector3 pt) {
		if(plane == null) return pt;
		return plane.o + plane.u * pt.x + plane.v * pt.y + plane.n * pt.z;
	}

	public static ExpVector DirFromPlane(this IPlane plane, ExpVector dir) {
		if(plane == null) return dir;
		return (ExpVector)plane.u * dir.x + (ExpVector)plane.v * dir.y + (ExpVector)plane.n * dir.z;
	}

	public static Vector3 DirFromPlane(this IPlane plane, Vector3 dir) {
		if(plane == null) return dir;
		return plane.u * dir.x + plane.v * dir.y + plane.n * dir.z;
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

	public static ExpVector DirToPlane(this IPlane plane, ExpVector dir) {
		if(plane == null) return dir;
		ExpVector result = new ExpVector(0, 0, 0);
		result.x = ExpVector.Dot(dir, plane.u);
		result.y = ExpVector.Dot(dir, plane.v);
		result.z = ExpVector.Dot(dir, plane.n);
		return result;
	}

	public static Vector3 DirToPlane(this IPlane plane, Vector3 dir) {
		if(plane == null) return dir;
		Vector3 result = new Vector3(0, 0, 0);
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

	public static Vector3 ToFrom(this IPlane to, Vector3 pt, IPlane from) {
		return to.ToPlane(from.FromPlane(pt));
	}

	public static IEnumerable<Vector3> ToFrom(this IPlane to, IEnumerable<Vector3> points, IPlane from) {
		return to.ToPlane(from.FromPlane(points));
	}

	public static ExpVector DirToFrom(this IPlane to, ExpVector pt, IPlane from) {
		return to.DirToPlane(from.DirFromPlane(pt));
	}

}

public class Sketch : CADObject  {
	Dictionary<Id, Entity> entities = new Dictionary<Id, Entity>();
	Dictionary<Id, Constraint> constraints = new Dictionary<Id, Constraint>();
	Feature feature_;

	public Feature feature {
		get {
			return feature_;
		}

		set {
			feature_ = value;
			if(feature_ != null && guid_ == Id.Null) {
				//guid_ = feature.idGenerator.New();
				guid_ = new Id(-1);
			}
		}
	}
	public IPlane plane;

	public bool is3d = false;

	public IEnumerable<Entity> entityList {
		get {
			return entities.Values.AsEnumerable();
		}
	}

	public IEnumerable<Constraint> constraintList {
		get {
			return constraints.Values.AsEnumerable();
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
		if(entities.ContainsKey(e.guid)) return;
		entities.Add(e.guid, e);
		MarkDirtySketch(topo:true, entities:true);
	}

	public Entity GetEntity(Id guid) {
		Entity result = null;
		entities.TryGetValue(guid, out result);
		return result;
	}

	public Constraint GetConstraint(Id guid) {
		Constraint result = null;
		constraints.TryGetValue(guid, out result);
		return result;
	}

	public void AddConstraint(Constraint c) {
		if(constraints.ContainsKey(c.guid)) return;
		constraints.Add(c.guid, c);
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

	public void MarqueeSelect(Rect rect, bool wholeObject, Camera camera, Matrix4x4 tf, ref List<ICADObject> result) {
		foreach(var en in entities) {
			var e = en.Value;
			if(!e.isSelectable) continue;
			if(e.MarqueeSelect(rect, wholeObject, camera, tf)) {
				result.Add(e);
			}
		}

		foreach(var c in constraints.Values) {
			if(!c.isSelectable) continue;
			if(c.MarqueeSelect(rect, wholeObject, camera, tf)) {
				result.Add(c);
			}
		}
	}

	public static double hoverRadius = 5.0;
	public SketchObject Hover(Vector3 mouse, Camera camera, Matrix4x4 tf, ref double objDist) {
		double min = -1.0;
		SketchObject hoveredObject = null;
		foreach(var en in entities) {
			var e = en.Value;
			if(!e.isVisible) continue;
			if(!e.isSelectable) continue;
			var dist = e.Select(Input.mousePosition, camera, tf);
			if(dist < 0.0) continue;
			if(dist > hoverRadius) continue;
			if(min >= 0.0 && dist > min) continue;
			min = dist;
			hoveredObject = e;
		}

		Dictionary<Constraint, double> candidates = new Dictionary<Constraint, double>();
		foreach(var c in constraints.Values) {
			if(!c.isVisible) continue;
			if(!c.isSelectable) continue;
			var dist = c.Select(Input.mousePosition, camera, tf);
			if(dist < 0.0) continue;
			if(dist > hoverRadius) continue;
			if(min >= 0.0 && dist >= min) continue;
			min = dist;
			hoveredObject = c;
			candidates.Add(c, dist);
		}

		if(hoveredObject is Constraint) {
			if(candidates.Count > 0) {
				for(int i = 0; i < candidates.Count; i++) {
					var current = candidates.ElementAt(i).Key;
					if(DetailEditor.instance.selection.All(id => id.ToString() != current.id.ToString())) continue;
					var next = candidates.ElementAt((i + 1) % candidates.Count);
					objDist = next.Value;
					return next.Key;
				}
			}
		}
		objDist = min;
		return hoveredObject;
	}

	public bool IsConstraintsChanged() {
		return constraintsChanged || constraints.Values.Any(c => c.IsChanged());
	}

	public bool IsEntitiesChanged() {
		return entitiesChanged || entities.Any(e => e.Value.IsChanged());
	}

	public void MarkUnchanged() {
		foreach(var e in entities) {
			foreach(var p in e.Value.parameters) {
				p.changed = false;
			}
		}
		foreach(var c in constraints.Values) {
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
		var all = entities.Values.OfType<ISegmentaryEntity>().ToList();
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
					.Select(p => p.GetOtherPoint(point) as PointEntity)
					.Where(p => p != null && p != prevPoint)
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
		loops.AddRange(entities.Values.OfType<ILoopEntity>().Select(e => Enumerable.Repeat(e as Entity, 1).ToList()));
		return loops;
	}

	public static List<List<Vector3>> GetPolygons(List<List<Entity>> loops, ref List<List<Id>> ids) {
		if(ids != null) ids.Clear();
		var result = new List<List<Vector3>>();
		if(loops == null) return result;

		foreach(var loop in loops) {
			var polygon = new List<Vector3>();
			List<Id> idPolygon = null;
			if(ids != null) idPolygon = new List<Id>();

			Action<IEnumerable<Vector3>, Entity> AddToPolygon = (points, entity) => {
				polygon.AddRange(points);
				if(idPolygon != null) {
					idPolygon.AddRange(Enumerable.Repeat(entity.guid, points.Count()));
				}
			};

			for(int i = 0; i < loop.Count; i++) {
				if(loop[i] is ISegmentaryEntity) {
					var cur = loop[i] as ISegmentaryEntity;
					var next = loop[(i + 1) % loop.Count] as ISegmentaryEntity;
					if(!next.begin.IsCoincidentWith(cur.begin) && !next.end.IsCoincidentWith(cur.begin)) {
						AddToPolygon(cur.segmentPoints, loop[i]);
					} else 
					if(!next.begin.IsCoincidentWith(cur.end) && !next.end.IsCoincidentWith(cur.end)) {
						AddToPolygon(cur.segmentPoints.Reverse(), loop[i]);
					} else if(next.begin.IsCoincidentWith(cur.end)) {
						AddToPolygon(cur.segmentPoints, loop[i]);
					} else
					if(i % 2 == 0) {
						AddToPolygon(cur.segmentPoints, loop[i]);
					} else {
						AddToPolygon(cur.segmentPoints.Reverse(), loop[i]);
					}
				} else
				if(loop[i] is ILoopEntity) {
					var cur = loop[i] as ILoopEntity;
					AddToPolygon(cur.loopPoints, loop[i]);
				} else {
					continue;
				}
				if(polygon.Count > 0) {
					polygon.RemoveAt(polygon.Count - 1);
					if(idPolygon != null) idPolygon.RemoveAt(idPolygon.Count - 1);
				}
			}
			if(polygon.Count < 3) continue;
			if(!Triangulation.IsClockwise(polygon)) {
				polygon.Reverse();
				if(idPolygon != null) idPolygon.Reverse();
			}
			result.Add(polygon);
			if(ids != null) ids.Add(idPolygon);
		}
		return result;
	}

	public void Write(XmlTextWriter xml, Func<SketchObject, bool> filter = null) {
		if(entities.Count > 0) {
			xml.WriteStartElement("entities");
			foreach(var en in entities) {
				var e = en.Value;
				if(filter != null && !filter(e as SketchObject) || filter == null && e.parent != null) continue;
				e.Write(xml);
			}
			xml.WriteEndElement();
		}

		if(constraints.Count > 0) {
			xml.WriteStartElement("constraints");
			foreach(var c in constraints.Values) {
				if(filter != null && !filter(c as SketchObject)) continue;
				c.Write(xml);
			}
			xml.WriteEndElement();
		}
	}

	public Dictionary<Id, Id> idMapping = null;

	public void Read(XmlNode xml, bool remap = false) {

		if(remap) {
			idMapping = new Dictionary<Id, Id>();
		}

		foreach(XmlNode nodeKind in xml.ChildNodes) {
			if(nodeKind.Name == "entities") {
				foreach(XmlNode node in nodeKind.ChildNodes) {
					if(node.Name != "entity") continue;
					var type = node.Attributes["type"].Value;
					var entity = Entity.New(type, this);
					entity.Read(node);
				}
				var oldEntities = entities.Values.ToList();
				entities.Clear();
				foreach(var e in oldEntities) {
					entities.Add(e.guid, e);
				}
			}
			if(nodeKind.Name == "constraints") {
				foreach(XmlNode node in nodeKind.ChildNodes) {
					if(node.Name != "constraint") continue;
					var typeName = node.Attributes["type"].Value;
					var constraint = Constraint.New(typeName, this);
					constraint.Read(node);
				}
				var oldConstraints = constraints.Values.ToList();
				constraints.Clear();
				foreach(var c in oldConstraints) {
					constraints.Add(c.guid, c);
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
			if(constraints.Remove(c.guid)) {
				c.Destroy();
				MarkDirtySketch(topo:c is PointsCoincident, constraints:true);
				constraintsTopologyChanged = true;
			} else {
				Debug.Log("Can't remove this constraint!");
			}
		}
		if(sko is Entity) {
			var e = sko as Entity;
			if(entities.Remove(e.guid)) {
				e.Destroy();
				MarkDirtySketch(topo:true, entities:true);
			} else {
				Debug.Log("Can't remove this entity!");
			}
		}
	}

	public void Clear() {
		while(entities.Count > 0) {
			entities.First().Value.Destroy();
		}
		while(constraints.Count > 0) {
			constraints.First().Value.Destroy();
		}
		MarkDirtySketch(topo:true, entities:true, constraints:true, loops:true);
	}

	public bool IsCrossed(Entity entity, ref Vector3 intersection) {
		foreach(var en in entities) {
			var e = en.Value;
			if(e == entity) continue;
			if(e.IsCrossed(entity, ref intersection)) {
				return true;
			}
		}
		return false;
	}

	public void ReplaceEntityInConstraints(Entity before, Entity after) {
		foreach(var c in constraints.Values) {
			if(c.ReplaceEntity(before, after)) {
				MarkDirtySketch(constraints:true, topo:c is PointsCoincident);
			}
		}
	}

	public void GenerateEquations(EquationSystem system) {
		foreach(var en in entities) {
			var e = en.Value;
			system.AddParameters(e.parameters);
			system.AddEquations(e.equations);
		}
		foreach(var c in constraints.Values) {
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
		var points = entities.SelectMany(e => e.Value.SegmentsInPlane(null)).ToArray();
		if(points.Length == 0) return new Bounds();
		return GeometryUtility.CalculateBounds(points, Matrix4x4.identity);
	}

	public PointEntity GetOtherPointByPoint(PointEntity point, float eps) {
		Vector3 pos = point.pos;
		foreach(var en in entities) {
			var e = en.Value;
			if(e.type != IEntityType.Point) continue;
			var pt = e as PointEntity;
			if(pt == point) continue;
			if((pt.pos - point.pos).sqrMagnitude > eps * eps) continue;
			return pt;
		}
		return null;
	}
}
