using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;
using System.Xml;
using System.Collections;
using NoteCAD;

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
	public List<Param> parameters = new List<Param>();
	public IEnumerable<Param> userParameters => parameters;

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

	public void AddParameter(Param p) {
		parameters.Add(p);
		MarkDirtySketch(topo:true);
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

	public static double hoverRadius {
		get {
			return 5.0f * (Screen.dpi / 100.0f);
		}
	}
	public SketchObject Hover(Vector3 mouse, Camera camera, Matrix4x4 tf, HoverFilter filter, ref double objDist) {
		double min = -1.0;
		SketchObject hoveredObject = null;
		foreach(var en in entities) {
			var e = en.Value;
			if(!e.isVisible) continue;
			if(!e.isSelectable) continue;
			if(filter != null && !filter(e)) continue;
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
			if(filter != null && !filter(c)) continue;
			var dist = c.Select(Input.mousePosition, camera, tf);
			if(dist < 0.0) continue;
			if(dist > hoverRadius) continue;
			candidates.Add(c, dist);
			if(min >= 0.0 && dist >= min) continue;
			min = dist;
			hoveredObject = c;
		}

		if(hoveredObject is Constraint) {
			if(candidates.Count > 1) {
				for(int i = 0; i < candidates.Count; i++) {
					var current = candidates.ElementAt(i).Key;
					if(!DetailEditor.instance.IsSelected(current)) continue;
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
			foreach(var p in e.Value.allParameters) {
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
		return GenerateLoops(entities.Values);
	}

	public static List<List<Entity>> GenerateLoops(IEnumerable<Entity> entities) {

		List<List<Entity>> loops = new List<List<Entity>>();
		List<Entity> loop = new List<Entity>();
		var all = entities.Where(e => !e.isConstruction).OfType<ISegmentaryEntity>().ToList();

		// process singleton loops
		for (int i = 0; i < all.Count; i++) {
			var e = all[i];
			if (e.begin.GetConicidentPoints().Contains(e.end)) {
				all.RemoveAt(i--);
				loop.Add(e as Entity);
				loops.Add(loop);
				loop = new();
			}
		}

		// remove branches
		var branchFound = false;
		do {
			var notConnected = all.Where(e =>
				Enumerable.Repeat(e.begin, 1).Concat(Enumerable.Repeat(e.end, 1))
				.Any(p => p.GetConicidentPoints()
					.Where(p => p.parent != null).Select(p => p.parent).OfType<ISegmentaryEntity>()
					.All(oe => e == oe || !all.Contains(oe))
				)
			).ToList();
			branchFound = notConnected.Any();
			if (branchFound) {
				all.RemoveAll(e => notConnected.Contains(e));
			}
		} while(branchFound);

		var first = all.FirstOrDefault();
		var current = first;
		PointEntity prevPoint = null;

		while(current != null && all.Count > 0) {
			if(!all.Remove(current)) {
				break;
			}
			loop.Add(current as Entity);
			var points = new List<PointEntity> { current.begin, current.end };
			bool found = false;

			// function entity should generate loop when begin and end point is coincident
			//if (current.begin.GetPosition() == current.end.GetPosition()) {
			//	found = true;
			//} else {

				for(int i = 0; i < points.Count; i++) {
					var point = points[i];
					// find coincident points to the entity points begin and end
					var connected1 = point.GetConicidentPoints();
					// connection of entities can be through simple point, so add "just points"
					// for further inspecting
					var justPoints = connected1.Where(p => p.parent == null);
					points.AddRange(justPoints);

					// inspect not-previous points with not-null parents and
					// where point is ending of its parent
					// and parent should not be already used in loop or equal to the first
					var connected = connected1
						.Where(p => p != null && p != prevPoint)
						.Where(p => p.parent != null && p.parent.IsEnding(p) && (p.parent == first || !loop.Contains(p.parent)))
						.Select(p => p.parent).OfType<ISegmentaryEntity>()
						.Where(e => e == first || all.Contains(e))
						.ToList();
					if(connected.Any()) {
						current = connected.First();
						found = true;
						prevPoint = point;
						break;
					} else {
						bool stop = true;
					}
				}
			//}
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

		// add ILoopEntities as separate loops
		loops.AddRange(entities
			.Where(e => !e.isConstruction)
			.OfType<ILoopEntity>()
			.Select(e => Enumerable.Repeat(e as Entity, 1).ToList())
		);
		return loops;
	}

	public static List<List<Vector3>> GetPolygons(List<List<IEntity>> loops, ref List<List<IdPath>> ids) {
		if(ids != null) ids.Clear();
		var result = new List<List<Vector3>>();
		if(loops == null) return result;

		foreach(var loop in loops) {
			var polygon = new List<Vector3>();
			List<IdPath> idPolygon = null;
			if(ids != null) idPolygon = new List<IdPath>();

			Action<IEnumerable<Vector3>, IEntity> AddToPolygon = (points, entity) => {
				polygon.AddRange(points);
				if(idPolygon != null) {
					idPolygon.AddRange(Enumerable.Repeat(entity.id, points.Count()));
				}
			};

			for(int i = 0; i < loop.Count; i++) {
				if(loop[i] is ISegmentaryEntity) {
					var cur = loop[i] as ISegmentaryEntity;
					var segmentPoints = cur.segmentPoints.First();
					var next = loop[(i + 1) % loop.Count] as ISegmentaryEntity;
					if(!next.begin.IsCoincidentWith(cur.begin) && !next.end.IsCoincidentWith(cur.begin)) {
						AddToPolygon(segmentPoints, loop[i]);
					} else
					if(!next.begin.IsCoincidentWith(cur.end) && !next.end.IsCoincidentWith(cur.end)) {
						AddToPolygon(segmentPoints.Reverse(), loop[i]);
					} else if(next.begin.IsCoincidentWith(cur.end)) {
						AddToPolygon(segmentPoints, loop[i]);
					} else
					if(i % 2 == 0) {
						AddToPolygon(segmentPoints, loop[i]);
					} else {
						AddToPolygon(segmentPoints.Reverse(), loop[i]);
					}
				} else
				if(loop[i] is ILoopEntity) {
					var cur = loop[i] as ILoopEntity;
					foreach(var lp in cur.loopPoints) {
						polygon = new List<Vector3>();
						if(ids != null) idPolygon = new List<IdPath>();
						AddToPolygon(lp, loop[i]);
						if(polygon.Count > 0) {
							polygon.RemoveAt(polygon.Count - 1);
							if(idPolygon != null) idPolygon.RemoveAt(idPolygon.Count - 1);
						}
						if(polygon.Count < 3) continue;
						if(!Triangulation.IsClockwise(polygon)) {
							polygon.Reverse();
							if(idPolygon != null) idPolygon.Reverse();
						}
						result.Add(polygon);
						if(ids != null) ids.Add(idPolygon);
					}
					polygon = null;
					continue;
				} else {
					continue;
				}
				if(polygon != null && polygon.Count > 0) {
					polygon.RemoveAt(polygon.Count - 1);
					if(idPolygon != null) idPolygon.RemoveAt(idPolygon.Count - 1);
				}
			}
			if(polygon == null) continue;
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

	// Ray-casting point-in-polygon test (2D, uses x/y coordinates only).
	// Returns true if point is strictly inside the polygon.
	static bool PointInPolygon2D(Vector3 point, List<Vector3> polygon) {
		bool inside = false;
		int j = polygon.Count - 1;
		for(int i = 0; i < polygon.Count; i++) {
			// Only consider edges whose y-range straddles the test point's y.
			// The division is safe because the condition ensures the two y-values differ.
			if((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
				point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x) {
				inside = !inside;
			}
			j = i;
		}
		return inside;
	}

	public static List<(List<Vector3> outer, List<List<Vector3>> holes, List<IdPath> outerIds, List<List<IdPath>> holeIds)>
	GroupPolygons(List<List<Vector3>> polygons, List<List<IdPath>> ids) {
		var result = new List<(List<Vector3>, List<List<Vector3>>, List<IdPath>, List<List<IdPath>>)>();
		if(polygons == null || polygons.Count == 0) return result;

		var depths = new int[polygons.Count];
		for(int i = 0; i < polygons.Count; i++) {
			if(polygons[i].Count == 0) continue;
			for(int j = 0; j < polygons.Count; j++) {
				if(i == j || polygons[j].Count == 0) continue;
				if(PointInPolygon2D(polygons[i][0], polygons[j])) depths[i]++;
			}
		}

		for(int i = 0; i < polygons.Count; i++) {
			if(depths[i] % 2 != 0) continue;
			var outer = polygons[i];
			var outerIds_ = (ids != null && i < ids.Count) ? ids[i] : null;
			var holes = new List<List<Vector3>>();
			var holeIds_ = new List<List<IdPath>>();

			for(int j = 0; j < polygons.Count; j++) {
				if(j == i || depths[j] != depths[i] + 1 || polygons[j].Count == 0) continue;
				if(!PointInPolygon2D(polygons[j][0], outer)) continue;
				// Keep holes in their original CW orientation (same as outer polygons).
				// The extrusion/revolve wall helpers (ShouldReverseWinding) rely on CW holes
				// to produce inward-facing normals on the void surfaces.
				holes.Add(polygons[j]);
				if(ids != null && j < ids.Count) {
					holeIds_.Add(ids[j]);
				}
			}
			result.Add((outer, holes, outerIds_, holeIds_));
		}
		return result;
	}

	public void Write(Writer xml, Func<SketchObject, bool> filter = null) {
		if(parameters.Count > 0) {
			xml.WriteBeginArray("parameters");
			foreach(var p in parameters) {
				xml.WriteBeginArrayElement("param");
				xml.WriteAttribute("name", p.name);
				xml.WriteAttribute("value", p.value);
				xml.WriteEndArrayElement();
			}
			xml.WriteEndArray();
		}

		if(entities.Count > 0) {
			xml.WriteBeginArray("entities");
			foreach(var en in entities) {
				var e = en.Value;
				if(filter != null && !filter(e as SketchObject) || filter == null && e.parent != null) continue;
				e.Write(xml);
			}
			xml.WriteEndArray();
		}

		if(constraints.Count > 0) {
			xml.WriteBeginArray("constraints");
			foreach(var c in constraints.Values) {
				if(filter != null && !filter(c as SketchObject)) continue;
				c.Write(xml);
			}
			xml.WriteEndArray();
		}
	}

	public Dictionary<Id, Id> idMapping = null;

	public void Read(XmlNode xml, bool remap = false) {

		if(remap) {
			idMapping = new Dictionary<Id, Id>();
		}
		foreach(XmlNode nodeKind in xml.ChildNodes) {
			if(nodeKind.Name == "entities") {
				var objects = new Dictionary<SketchObject, XmlNode>();
				foreach(XmlNode node in nodeKind.ChildNodes) {
					if(node.Name != "entity") continue;
					var type = node.Attributes["type"].Value;
					var entity = Entity.New(type, this);
					entity.Read(node);
					objects[entity] = node;
				}

				foreach(var obj in objects) {
					obj.Key.AfterRead(obj.Value);
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
					Id id;
					// if remapping, we need to allocate new guid
					if(idMapping != null) {
						id = idGenerator.New();
					} else {
						id = idGenerator.Create(node.Attributes["id"].Value);
					}
					var constraint = Constraint.New(typeName, this, id);
					if(constraint == null) continue;
					constraint.Read(node);
				}
			}
			if(nodeKind.Name == "parameters") {
				foreach(XmlNode node in nodeKind.ChildNodes) {
					if(node.Name != "param") continue;
					var name = node.Attributes["name"].Value;
					var value = node.Attributes["value"].Value.ToDouble();
					var param = new Param(name, value);
					AddParameter(param);
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
			foreach(var itr in e.GetIntersections(entity)) {
				intersection = itr;
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
			if (!c.enabled) {
				continue;
			}
			system.AddParameters(c.parameters);
			system.AddEquations(c.equations);
		}
		system.AddParameters(parameters);
	}

	public override ICADObject GetChild(Id guid) {
		var e = GetEntity(guid);
		if(e != null) return e;
		return GetConstraint(guid);
	}

	public Bounds calculateBounds() {
		var points = entities.SelectMany(e => e.Value.SegmentsInPlane(null).SelectMany(p => p)).ToArray();
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

	public bool HasNonSolvable() {
		return constraintList.OfType<ValueConstraint>().Any(c => !c.solvable);
	}

	/// <summary>
	/// New contour detection algorithm:
	/// 1) Intersect all entities between each other (split edges at crossing points).
	/// 2) Build a planar subdivision graph (DCEL) and extract closed contours.
	/// Hole detection (step 3 of the algorithm) is handled by <see cref="GroupPolygons"/>.
	/// </summary>
	public static List<List<Vector3>> GetPolygons(IEnumerable<Entity> entities, ref List<List<IdPath>> ids) {
		if(ids != null) ids.Clear();

		var allEntities = entities.Where(e => !e.isConstruction).ToList();
		if(allEntities.Count == 0) return new List<List<Vector3>>();

		// Collect all individual line segments from every entity, tagged by entity index.
		var rawEdges = new List<(Vector3 a, Vector3 b, int entityIdx)>();
		for(int ei = 0; ei < allEntities.Count; ei++) {
			var e = allEntities[ei];
			if(!(e is ISegmentProvider sp)) continue;
			foreach(var loop in sp.segmentPoints) {
				Vector3 prev = Vector3.zero;
				bool first = true;
				foreach(var pt in loop) {
					if(!first) rawEdges.Add((prev, pt, ei));
					first = false;
					prev = pt;
				}
			}
		}

		if(rawEdges.Count == 0) return new List<List<Vector3>>();

		// Step 1: Intersect all entities – split edges at pairwise intersection points.
		var splitEdges = SplitEdgesAtIntersections(rawEdges);

		// Step 2: Build planar subdivision graph and extract closed contours (polygonize).
		var graph = new PlanarGraph();
		foreach(var (a, b, _) in splitEdges) {
			graph.AddEdge(a, b);
		}
		// Remove dangling edges (degree-1 nodes) before face extraction.
		// Dead-end segment tails can never be part of a closed contour and they
		// cause the DCEL traversal to produce non-simple (self-touching) pseudo-faces.
		graph.PruneDanglingEdges();
		graph.SortEdges();
		return graph.ExtractFaces();
		// Note: hole detection (step 3) is handled by GroupPolygons.
	}

	// Split a flat list of directed segments at every pairwise intersection point
	// between segments that originate from different entities.
	// Handles both proper crossings and T-junctions (an endpoint of one segment
	// lying on the interior of another segment from a different entity).
	// Note: eps (1e-5) is smaller than PlanarGraph.NodeEps (1e-4) intentionally:
	// the tighter tolerance prevents false-positive intersections while the looser
	// node-merge tolerance absorbs floating-point accumulation from the intersection math.
	static List<(Vector3 a, Vector3 b, int entityIdx)> SplitEdgesAtIntersections(
		List<(Vector3 a, Vector3 b, int entityIdx)> edges) {

		const float eps = 1e-5f;
		var splits = new List<List<(float t, Vector3 pos)>>(edges.Count);
		for(int i = 0; i < edges.Count; i++) splits.Add(new List<(float, Vector3)>());

		for(int i = 0; i < edges.Count; i++) {
			for(int j = i + 1; j < edges.Count; j++) {
				if(edges[i].entityIdx == edges[j].entityIdx) continue; // same entity – skip

				// Proper crossing: both parameters strictly interior to their segments.
				var itr = Vector3.zero;
				if(GeomUtils.isSegmentsCrossed(edges[i].a, edges[i].b,
						edges[j].a, edges[j].b, ref itr, eps) == GeomUtils.Cross.INTERSECTION) {
					var di = edges[i].b - edges[i].a;
					float liSq = di.sqrMagnitude;
					float ti = liSq > eps * eps ? Vector3.Dot(itr - edges[i].a, di) / liSq : 0.5f;
					var dj = edges[j].b - edges[j].a;
					float ljSq = dj.sqrMagnitude;
					float tj = ljSq > eps * eps ? Vector3.Dot(itr - edges[j].a, dj) / ljSq : 0.5f;
					if(ti > eps && ti < 1f - eps) splits[i].Add((ti, itr));
					if(tj > eps && tj < 1f - eps) splits[j].Add((tj, itr));
				}

				// T-junctions: an endpoint of one segment lies on the interior of the other.
				TryAddSplitAtEndpoint(edges[i], edges[j].a, splits[i], eps);
				TryAddSplitAtEndpoint(edges[i], edges[j].b, splits[i], eps);
				TryAddSplitAtEndpoint(edges[j], edges[i].a, splits[j], eps);
				TryAddSplitAtEndpoint(edges[j], edges[i].b, splits[j], eps);
			}
		}

		var result = new List<(Vector3, Vector3, int)>();
		for(int i = 0; i < edges.Count; i++) {
			splits[i].Sort((a, b) => a.t.CompareTo(b.t));
			// Deduplicate split points with nearly equal t values.
			var deduped = new List<(float t, Vector3 pos)>();
			foreach(var sp in splits[i]) {
				if(deduped.Count == 0 || sp.t - deduped[deduped.Count - 1].t > eps)
					deduped.Add(sp);
			}
			Vector3 prev = edges[i].a;
			foreach(var (_, pos) in deduped) {
				if((pos - prev).sqrMagnitude > eps * eps)
					result.Add((prev, pos, edges[i].entityIdx));
				prev = pos;
			}
			if((edges[i].b - prev).sqrMagnitude > eps * eps)
				result.Add((prev, edges[i].b, edges[i].entityIdx));
		}
		return result;
	}

	// If point P lies strictly on the interior of segment (edge.a, edge.b), add it as a split.
	static void TryAddSplitAtEndpoint(
		(Vector3 a, Vector3 b, int entityIdx) edge,
		Vector3 P,
		List<(float t, Vector3 pos)> splitList,
		float eps) {
		var d = edge.b - edge.a;
		float lenSq = d.sqrMagnitude;
		if(lenSq < eps * eps) return;
		var ap = P - edge.a;
		float t = Vector3.Dot(ap, d) / lenSq;
		if(t <= eps || t >= 1f - eps) return;  // not strictly interior
		var closest = edge.a + d * t;
		if((closest - P).sqrMagnitude > eps * eps) return;  // not on the segment line
		splitList.Add((t, P));
	}
}

// ---------------------------------------------------------------------------
// Planar graph (DCEL) used for intersection-based contour detection.
// ---------------------------------------------------------------------------

/// <summary>
/// Planar subdivision graph built from directed half-edges.
/// Faces are extracted by following the DCEL "next edge" relation:
/// for half-edge A→B the next edge in the CW (interior) face is the outgoing
/// edge from B that comes just before the twin (B→A) in the CCW-sorted list
/// of edges at B.
/// </summary>
class PlanarGraph {
	// NodeEps is intentionally larger than the edge-split epsilon (1e-5) so that
	// intersection points computed via floating-point arithmetic are reliably snapped
	// to the same graph node even after accumulated rounding errors.
	const float NodeEps = 1e-4f;
	static readonly float NodeEpsSq = NodeEps * NodeEps;

	readonly List<PGNode> nodes = new List<PGNode>();
	readonly List<PGHalfEdge> halfEdges = new List<PGHalfEdge>();

	PGNode FindOrCreate(Vector3 pos) {
		// Only x and y are compared: the planar graph operates in the 2D sketch plane (z ≈ 0).
		foreach(var n in nodes) {
			float dx = n.pos.x - pos.x, dy = n.pos.y - pos.y;
			if(dx * dx + dy * dy < NodeEpsSq) return n;
		}
		var node = new PGNode { pos = pos };
		nodes.Add(node);
		return node;
	}

	/// <summary>Add a directed edge (and its twin) between positions a and b.</summary>
	public void AddEdge(Vector3 a, Vector3 b) {
		var na = FindOrCreate(a);
		var nb = FindOrCreate(b);
		if(na == nb) return; // degenerate (zero-length) edge

		// Reject duplicate edges so the DCEL remains valid.
		if(na.outgoing.Any(e => e.to == nb)) return;

		var fwd = new PGHalfEdge { from = na, to = nb };
		var rev = new PGHalfEdge { from = nb, to = na };
		fwd.twin = rev;
		rev.twin = fwd;
		na.outgoing.Add(fwd);
		nb.outgoing.Add(rev);
		halfEdges.Add(fwd);
		halfEdges.Add(rev);
	}

	/// <summary>
	/// Iteratively remove degree-1 nodes (dead-end / dangling edges) from the graph.
	/// A node with only one outgoing edge can never be part of a closed face.
	/// After pruning, every remaining node has degree ≥ 2 and participates in a cycle.
	/// Uses a queue for O(n) amortized performance.
	/// </summary>
	public void PruneDanglingEdges() {
		// Seed the queue with all current leaf nodes.
		var queue = new Queue<PGNode>();
		foreach(var node in nodes) {
			if(node.outgoing.Count == 1) queue.Enqueue(node);
		}
		while(queue.Count > 0) {
			var node = queue.Dequeue();
			if(node.outgoing.Count != 1) continue; // may have been updated already
			var fwd = node.outgoing[0];
			var rev = fwd.twin;
			var neighbour = fwd.to;
			// Remove the reverse half-edge from the neighbour's outgoing list.
			neighbour.outgoing.Remove(rev);
			halfEdges.Remove(fwd);
			halfEdges.Remove(rev);
			node.outgoing.Clear();
			// The neighbour may now be a leaf too.
			if(neighbour.outgoing.Count == 1) queue.Enqueue(neighbour);
		}
	}

	/// <summary>Sort the outgoing edges at every node by angle (CCW from east).</summary>
	public void SortEdges() {
		foreach(var node in nodes) {
			node.outgoing.Sort((a, b) => {
				float angA = Mathf.Atan2(a.to.pos.y - a.from.pos.y, a.to.pos.x - a.from.pos.x);
				float angB = Mathf.Atan2(b.to.pos.y - b.from.pos.y, b.to.pos.x - b.from.pos.x);
				return angA.CompareTo(angB);
			});
		}
	}

	// Returns the next half-edge in the CW face that contains h on its boundary.
	PGHalfEdge NextEdge(PGHalfEdge h) {
		var outgoing = h.to.outgoing;
		int idx = outgoing.IndexOf(h.twin);
		if(idx < 0) return null;
		return outgoing[(idx - 1 + outgoing.Count) % outgoing.Count];
	}

	/// <summary>
	/// Traverse all unvisited half-edges and collect the interior faces.
	/// The "previous-in-CCW" DCEL traversal yields CCW interior faces and CW exterior faces.
	/// Interior (CCW) faces are reversed to CW and returned; the CW exterior face is discarded.
	/// Non-simple polygons (those that visit the same node twice) are also discarded.
	/// </summary>
	public List<List<Vector3>> ExtractFaces() {
		var result = new List<List<Vector3>>();
		// globalVisited tracks half-edges that are already owned by a confirmed face.
		// Half-edges from invalid traversals are NOT added here (except the starting edge),
		// so they remain available as starting points for other valid faces.
		var globalVisited = new HashSet<PGHalfEdge>();

		foreach(var start in halfEdges) {
			if(globalVisited.Contains(start)) continue;

			var poly = new List<Vector3>();
			// localEdges accumulates the edges of the current traversal.
			// They are only promoted to globalVisited when the face is confirmed.
			var localEdges = new HashSet<PGHalfEdge>();
			var faceNodes = new HashSet<PGNode>();
			var h = start;
			bool valid = true;
			int limit = halfEdges.Count + 1; // guard against infinite loops

			do {
				if(--limit <= 0) { valid = false; break; }
				// Edge already belongs to a confirmed face – this traversal is invalid.
				if(globalVisited.Contains(h)) { valid = false; break; }
				// Intra-traversal edge cycle (not back to start) – invalid.
				if(!localEdges.Add(h)) { valid = false; break; }
				// A node visited twice in one face means a non-simple (self-touching) polygon.
				if(!faceNodes.Add(h.from)) { valid = false; break; }
				poly.Add(h.from.pos);
				h = NextEdge(h);
				if(h == null) { valid = false; break; }
			} while(h != start);

			if(!valid || poly.Count < 3) {
				// Invalid traversal: only consume the starting edge so this start is not retried.
				// Leave other traversed edges available for future (potentially valid) traversals.
				globalVisited.Add(start);
				continue;
			}

			// Confirmed face: mark every edge in it as globally consumed.
			foreach(var e in localEdges) globalVisited.Add(e);

			// The DCEL "previous-in-CCW" traversal yields CCW interior faces and CW exterior faces.
			// Discard the CW exterior/infinite face; reverse interior (CCW) faces to CW
			// because the rest of the pipeline (GroupPolygons, TriangulateWithHoles) expects CW.
			if(Triangulation.IsClockwise(poly)) continue;
			poly.Reverse();
			result.Add(poly);
		}
		return result;
	}
}

class PGNode {
	public Vector3 pos;
	public readonly List<PGHalfEdge> outgoing = new List<PGHalfEdge>();
}

class PGHalfEdge {
	public PGNode from;
	public PGNode to;
	public PGHalfEdge twin;
}
