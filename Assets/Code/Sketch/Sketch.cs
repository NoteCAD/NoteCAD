using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;
using System.Xml;
using System.Collections;

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
	List<List<Entity>> loops = new List<List<Entity>>();

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

	IEnumerator LoadWWWFile(string url) {
		WWW www = new WWW(url);
		yield return www;
		ReadXml(www.text);
	}

	private void Start() {
		instance = this;
		if(NoteCADJS.GetParam("filename") != "") {
			var uri = new Uri(Application.absoluteURL);
			var url = "http://" + uri.Host + ":" + uri.Port + "/Files/" + NoteCADJS.GetParam("filename");
			StartCoroutine(LoadWWWFile(url));
		}
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
		loops = GenerateLoops();
		CreateLoops();
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
		string result = sys.Solve().ToString();
		result += "\n" + sys.stats;
		resultText.text = result.ToString();
	}

	private void LateUpdate() {
		foreach(var c in constraints) {
			c.Draw();
		}
		foreach(var e in entities) {
			e.Draw();
		}
		if(loops.Any(l => l.Any(e => e.IsChanged()))) {
			CreateLoops();
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

	List<List<Entity>> GenerateLoops() {
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

	bool IsClockwise(List<Vector3> points) {
		int minIndex = 0;
		for(int i = 1; i < points.Count; i++) {
			if(points[minIndex].y < points[i].y) continue;
			minIndex = i;
		}
		var a = points[(minIndex - 1 + points.Count) % points.Count];
		var b = points[minIndex];
		var c = points[(minIndex + 1) % points.Count];
		return Triangulation.IsConvex(a, b, c);
	}

	List<List<Vector3>> GetPolygons(List<List<Entity>> loops) {
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
			if(!IsClockwise(polygon)) {
				polygon.Reverse();
			}
			result.Add(polygon);
		}
		return result;
	}

	Mesh CreateMesh(List<List<Vector3>> polygons, float extrude) {
		var capacity = polygons.Sum(p => (p.Count - 2) * 3 + p.Count) * 2;
		var vertices = new List<Vector3>(capacity);
		var indices = new List<int>(capacity);
		foreach(var p in polygons) {
			var pv = new List<Vector3>(p);
			var triangles = Triangulation.Triangulate(pv);
			var start = vertices.Count;
			vertices.AddRange(triangles);
			for(int i = 0; i < triangles.Count; i++) {
				indices.Add(i + start);
			}
			var extrudeVector = Vector3.forward * extrude;
			var striangles = triangles.Select(pt => pt + extrudeVector).ToList();
			start = vertices.Count;
			vertices.AddRange(striangles);
			for(int i = 0; i < striangles.Count / 3; i++) {
				indices.Add(start + i * 3 + 1);
				indices.Add(start + i * 3 + 0);
				indices.Add(start + i * 3 + 2);
			}

			start = vertices.Count();

			if(extrude < 0f) {
				for(int i = 0; i < p.Count; i++) {
					vertices.Add(p[i]);
					vertices.Add(p[(i + 1) % p.Count]);
					vertices.Add(p[i] + extrudeVector);

					vertices.Add(p[(i + 1) % p.Count]);
					vertices.Add(p[(i + 1) % p.Count] + extrudeVector);
					vertices.Add(p[i] + extrudeVector);
				}
			} else {
				for(int i = 0; i < p.Count; i++) {
					vertices.Add(p[i]);
					vertices.Add(p[i] + extrudeVector);
					vertices.Add(p[(i + 1) % p.Count]);

					vertices.Add(p[(i + 1) % p.Count]);
					vertices.Add(p[i] + extrudeVector);
					vertices.Add(p[(i + 1) % p.Count] + extrudeVector);
				}
			}
			for(int i = 0; i < p.Count * 6; i++) {
				indices.Add(start + i);
			}
		}
		var result = new Mesh();
		result.SetVertices(vertices);
		result.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
		result.RecalculateBounds();
		result.RecalculateNormals();
		result.RecalculateTangents();
		return result;
	}

	List<GameObject> loopsObjects = new List<GameObject>();
	Mesh mainMesh;

	void CreateLoops() {
		foreach(var obj in loopsObjects) {
			Destroy(obj);
		}
		loopsObjects.Clear();
		var itr = new Vector3();
		foreach(var loop in loops) {
			loop.ForEach(e => e.isError = false);
			foreach(var e0 in loop) {
				foreach(var e1 in loop) {
					if(e0 == e1) continue;
					var cross = e0.IsCrossed(e1, ref itr);
					e0.isError = e0.isError || cross;
					e1.isError = e1.isError || cross;
				}
			}
		}
		var polygons = GetPolygons(loops.Where(l => l.All(e => !e.isError)).ToList());
		mainMesh = CreateMesh(polygons, 5f);
		var go = new GameObject();
		var mf = go.AddComponent<MeshFilter>();
		var mr = go.AddComponent<MeshRenderer>();
		mf.mesh = mainMesh;
		mr.material = EntityConfig.instance.meshMaterial;
		loopsObjects.Add(go);
	}

	public string ExportSTL() {
		if(mainMesh == null) {
			mainMesh = new Mesh();
		}
		return mainMesh.ExportSTL();
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
		Clear();
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

	public void Clear() {
		while(entities.Count > 0) {
			entities[0].Destroy();
		}
		sysDirty = true;
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
				sysDirty = true;
			}
		}
	}
}
