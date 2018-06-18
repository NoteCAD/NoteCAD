using Csg;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEngine;
using gs;
using g3;


class MeshEdgeEntity : IEntity {
	MeshImportFeature feature;
	int v0;
	int v1;

	IEntityType IEntity.type { get { return IEntityType.Line; } }

	public MeshEdgeEntity(MeshImportFeature f, int v0, int v1) {
		feature = f;
		this.v0 = v0;
		this.v1 = v1;
	}

	public IdPath id {
		get {
			var eid = feature.id;
			eid.path.Insert(0, new Id(v0, v1));
			return eid;
		}
	}

	public IPlane plane {
		get {
			return null;
		}
	}

	public IEnumerable<ExpVector> points {
		get {
			yield return feature.hitMesh.Mesh.GetVertex(v0).ToVector3() + feature.pos;
			yield return feature.hitMesh.Mesh.GetVertex(v1).ToVector3() + feature.pos;
		}
	}

	public IEnumerable<Vector3> segments {
		get {
			yield return feature.hitMesh.Mesh.GetVertex(v0).ToVector3();
			yield return feature.hitMesh.Mesh.GetVertex(v1).ToVector3();
		}
	}

	public ExpVector PointOn(Exp t) {
		throw new NotImplementedException();
	}
}

class MeshPointEntity : IEntity {
	MeshImportFeature feature;
	int v0;

	IEntityType IEntity.type { get { return IEntityType.Point; } }

	public MeshPointEntity(MeshImportFeature f, int v0) {
		feature = f;
		this.v0 = v0;
	}

	public IdPath id {
		get {
			var eid = feature.id;
			eid.path.Insert(0, new Id(v0, -1));
			return eid;
		}
	}

	public IPlane plane {
		get {
			return null;
		}
	}

	public IEnumerable<ExpVector> points {
		get {
			yield return feature.hitMesh.Mesh.GetVertex(v0).ToVector3();
		}
	}

	public IEnumerable<Vector3> segments {
		get {
			yield return feature.hitMesh.Mesh.GetVertex(v0).ToVector3();
		}
	}

	public ExpVector PointOn(Exp t) {
		throw new NotImplementedException();
	}
}

public class MeshImportFeature : MeshFeature {
	public Mesh mesh = new Mesh();
	public MeshCheck meshCheck = new MeshCheck();
	public DMeshAABBTree3 hitMesh;
	GameObject go;
	List<Pair<Vector3, Vector3>> edges = new List<Pair<Vector3, Vector3>>();

	public Param x { get; private set; }
	public Param y { get; private set; }
	public Param z { get; private set; }
	public ExpVector pos { get; private set; }

	public MeshImportFeature(byte[] data) {
		MemoryStream ms = new MemoryStream(data);
		meshCheck.setMesh(Parabox.STL.pb_Stl_Importer.Import(ms)[0]);
		mesh = meshCheck.ToUnityWatertightMesh();
		hitMesh = new DMeshAABBTree3(mesh.ToDMesh3(), true);
		edges = meshCheck.GenerateEdges();
		x = new Param("x");
		y = new Param("y");
		z = new Param("z");
		pos = new ExpVector(x, y, z);
	}

	public override GameObject gameObject {
		get {
			return go;
		}
	}

	public override ICADObject GetChild(Id guid) {
		if(guid.second == -1) return new MeshPointEntity(this, (int)guid.value);
		return new MeshEdgeEntity(this, (int)guid.value, (int)guid.second);
	}

	protected override void OnUpdate() {
		if(x.changed || y.changed || z.changed) {
			MarkDirty();
		}
		x.changed = false;
		y.changed = false;
		z.changed = false;
	}

	protected override Solid OnGenerateMesh() {
		var solid = mesh.ToSolid(UnityEngine.Matrix4x4.Translate(pos.Eval()));
		return solid;
	}

	LineCanvas canvas;

	Vector3 CalculateNormal(int tri, int e, Vector3[] vert, int[] tris) {
		var v0 = vert[tris[tri + e]];
		var v1 = vert[tris[tri + (e + 1) % 3]];
		var v2 = vert[tris[tri + (e + 2) % 3]];
		return Vector3.Cross(v0 - v1, v2 - v1).normalized;
	}

	Vector3 Snap(Vector3 v, Vector3[] vert) {
		for(int i = 0; i < vert.Length; i++) {
			if((v - vert[i]).sqrMagnitude < 1e-9) return vert[i];
		}
		return v;
	}

	protected override void OnGenerateEquations(EquationSystem sys) {
		sys.AddParameter(x);
		sys.AddParameter(y);
		sys.AddParameter(z);
		//sketch.GenerateEquations(sys);
		//base.OnGenerateEquations(sys);
	}


	protected override void OnUpdateDirty() {
		GameObject.Destroy(go);
		go = new GameObject("ImportMeshFeature");
		canvas = GameObject.Instantiate(EntityConfig.instance.lineCanvas, go.transform);
		canvas.SetStyle("entities");
		foreach(var edge in edges) {
			canvas.DrawLine(edge.a, edge.b);
		}
		meshCheck.drawErrors(canvas);

		go.transform.position = pos.Eval();

		/*

		var vertices = mesh.vertices;
		var triangles = mesh.GetTriangles(0);
		var edges = new HashSet<KeyValuePair<Vector3, Vector3>>();
		var antiEdges = new HashSet<KeyValuePair<Vector3, Vector3>>();
		var tris = new Dictionary<KeyValuePair<Vector3, Vector3>, int>();
		for(int i = 0; i < triangles.Length / 3; i++) {
			for(int j = 0; j < 3; j++) {
				var edge = new KeyValuePair<Vector3, Vector3>(
					Snap(vertices[triangles[i * 3 + j]], vertices),
					Snap(vertices[triangles[i * 3 + (j + 1) % 3]], vertices)
				);
				if(edge.Key == edge.Value) continue;
				tris[edge] =  i * 3 + j;
				if(edges.Contains(edge)) continue;
				if(antiEdges.Contains(edge)) continue;
				var antiEdge = new KeyValuePair<Vector3, Vector3>(edge.Value, edge.Key);
				edges.Add(edge);
				antiEdges.Add(antiEdge);
			}
		}

		foreach(var edge in edges) {
			var anti = new KeyValuePair<Vector3, Vector3>(edge.Value, edge.Key);
			if(tris.ContainsKey(edge) && tris.ContainsKey(anti)) {
				var tri0 = tris[edge];
				var edg0 = tri0 % 3;
				tri0 -= edg0;
				var n0 = CalculateNormal(tri0, edg0, vertices, triangles);

				var tri1 = tris[anti];
				var edg1 = tri1 % 3;
				tri1 -= edg1;
				var n1 = CalculateNormal(tri1, edg1, vertices, triangles);

				if((n0 - n1).magnitude < 1e-4) continue;
			} else continue;
			canvas.DrawLine(edge.Key, edge.Value);
		}
		*/
		go.SetActive(visible);
	}

	protected override void OnShow(bool state) {
		if(go != null) {
			go.SetActive(state);
		}
	}

	protected override void OnClear() {
		GameObject.Destroy(go);
	}

	protected override void OnWriteMeshFeature(XmlTextWriter xml) {
	}

	protected override void OnReadMeshFeature(XmlNode xml) {
	}

	protected override ICADObject OnHover(Vector3 mouse, Camera camera, UnityEngine.Matrix4x4 tf, ref double dist) {
		var ray = camera.ScreenPointToRay(mouse);
		var tris = new List<int>();
		hitMesh.FindAllHitTriangles(new Ray3d(ray.origin.ToVector3d(), ray.direction.ToVector3d().Normalized), tris);

		double min = -1.0;
		int hoverV0 = -1;
		int hoverV1 = -1;

		foreach(var ti in tris) {
			var t = hitMesh.Mesh.GetTriangle(ti);
			for(int i = 0; i < 3; i++) {
				var v0 = hitMesh.Mesh.GetVertex(t[i]).ToVector3();
				if(!HoverPoint(mouse, camera, ref min, v0)) continue;
				hoverV0 = t[i];
				hoverV1 = -1;
			}
		}

		if(hoverV0 != -1) {
			dist = min;
			return new MeshPointEntity(this, hoverV0);
		}

		foreach(var ti in tris) {
			var t = hitMesh.Mesh.GetTriangle(ti);
			for(int i = 0; i < 3; i++) {
				var v0 = hitMesh.Mesh.GetVertex(t[i]).ToVector3();
				var v1 = hitMesh.Mesh.GetVertex(t[(i + 1) % 3]).ToVector3();
				if(!meshCheck.IsEdgeSharp(v0, v1)) continue;
				if(!HoverSegment(mouse, camera, ref min, v0, v1)) continue;
				hoverV0 = t[i];
				hoverV1 = t[(i + 1) % 3];
			}
		}

		if(hoverV0 != -1) {
			dist = min;
			return new MeshEdgeEntity(this, hoverV0, hoverV1);
		}

		/*
		var sk = source as SketchFeature;
		k
		double d0 = -1;
		var r0 = sk.Hover(mouse, camera, tf, ref d0);
		if(!(r0 is Entity)) r0 = null;

		UnityEngine.Matrix4x4 move = UnityEngine.Matrix4x4.Translate(Vector3.forward * (float)extrude.value);
		double d1 = -1;
		var r1 = sk.Hover(mouse, camera, tf * move, ref d1);
		if(!(r1 is Entity)) r1 = null;

		if(r1 != null && (r0 == null || d1 < d0)) {
			r0 = new ExtrudedEntity(r1 as Entity, this, 1);
			d0 = d1;
		} else if(r0 != null) {
			r0 = new ExtrudedEntity(r0 as Entity, this, 0);
		}

		var points = sk.GetSketch().entityList.OfType<PointEntity>();
		var dir = extrusionDir.Eval();
		double min = -1.0;
		PointEntity hover = null;
		var sktf = tf * sk.GetTransform();
		foreach(var p in points) {
			Vector3 pp = sktf.MultiplyPoint(p.pos);
			var p0 = camera.WorldToScreenPoint(pp);
			var p1 = camera.WorldToScreenPoint(pp + dir);
			double d = GeomUtils.DistancePointSegment2D(mouse, p0, p1);
			if(d > Sketch.hoverRadius) continue;
			if(min >= 0.0 && d > min) continue;
			min = d;
			hover = p;
		}

		if(hover != null && (r0 == null || d0 > min)) {
			dist = min;
			return new ExtrudedPointEntity(hover, this);
		}

		if(r0 != null) {
			dist = d0;
			return r0;
		}
		*/
		return null;
	}

}
	