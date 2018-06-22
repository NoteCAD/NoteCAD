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
			yield return feature.basis.TransformPosition(feature.hitMesh.Mesh.GetVertex(v0).ToVector3());
			yield return feature.basis.TransformPosition(feature.hitMesh.Mesh.GetVertex(v1).ToVector3());
		}
	}

	public IEnumerable<Vector3> segments {
		get {
			yield return feature.transform.MultiplyPoint3x4(feature.hitMesh.Mesh.GetVertex(v0).ToVector3());
			yield return feature.transform.MultiplyPoint3x4(feature.hitMesh.Mesh.GetVertex(v1).ToVector3());
		}
	}

	public ExpVector PointOn(Exp t) {
		throw new NotImplementedException();
	}
}

class MeshVertexEntity : IEntity {
	MeshImportFeature feature;
	int v0;

	IEntityType IEntity.type { get { return IEntityType.Point; } }

	public MeshVertexEntity(MeshImportFeature f, int v0) {
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
			yield return feature.basis.TransformPosition(feature.hitMesh.Mesh.GetVertex(v0).ToVector3());
		}
	}

	public IEnumerable<Vector3> segments {
		get {
			yield return feature.transform.MultiplyPoint3x4(feature.hitMesh.Mesh.GetVertex(v0).ToVector3());
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
	List<Pair<Vector3, Vector3>> edges;

	public ExpBasis basis { get; private set; }

	public MeshImportFeature(byte[] data) {
		MemoryStream ms = new MemoryStream(data);
		mesh = Parabox.STL.pb_Stl_Importer.Import(ms)[0];
		meshCheck.setMesh(mesh);
		//mesh = meshCheck.ToUnityWatertightMesh();
		basis = new ExpBasis();
	}

	public override GameObject gameObject {
		get {
			return go;
		}
	}

	public override ICADObject GetChild(Id guid) {
		if(guid.second == -1) return new MeshVertexEntity(this, (int)guid.value);
		return new MeshEdgeEntity(this, (int)guid.value, (int)guid.second);
	}

	protected override void OnUpdate() {
		if(basis.changed) {
			MarkDirty();
			basis.markUnchanged();
		}
	}

	protected override Solid OnGenerateMesh() {
		var solid = mesh.ToSolid(basis.matrix);
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
		basis.GenerateEquations(sys);
		//sketch.GenerateEquations(sys);
		//base.OnGenerateEquations(sys);
	}


	protected override void OnUpdateDirty() {
		if(edges == null) {
			go = new GameObject("ImportMeshFeature");
			canvas = GameObject.Instantiate(EntityConfig.instance.lineCanvas, go.transform);
			canvas.SetStyle("entities");
			edges = meshCheck.GenerateEdges();
			foreach(var edge in edges) {
				canvas.DrawLine(edge.a, edge.b);
			}
			meshCheck.drawErrors(canvas);
		}
		go.transform.SetMatrix(basis.matrix);
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

	public UnityEngine.Matrix4x4 transform {
		get {
			return basis.matrix;
		}
	}
	bool initialized = false;
	protected override ICADObject OnHover(Vector3 mouse, Camera camera, UnityEngine.Matrix4x4 tf, ref double dist) {
		var tris = new List<int>();
		var fullTf = tf * transform;
		var invFullTf = fullTf.inverse;
		var ray = camera.ScreenPointToRay(mouse);
		ray.origin = invFullTf.MultiplyPoint3x4(ray.origin);
		ray.direction = invFullTf.MultiplyVector(ray.direction);
		if(hitMesh == null) {
			if(!initialized) {
				initialized = true;
				return null;
			}
			hitMesh = new DMeshAABBTree3(mesh.ToDMesh3(), true);
		}
		hitMesh.FindAllHitTriangles(new Ray3d(ray.origin.ToVector3d(), ray.direction.ToVector3d().Normalized), tris);

		double min = -1.0;
		int hoverV0 = -1;
		int hoverV1 = -1;

		foreach(var ti in tris) {
			var t = hitMesh.Mesh.GetTriangle(ti);
			for(int i = 0; i < 3; i++) {
				var v0 = fullTf.MultiplyPoint3x4(hitMesh.Mesh.GetVertex(t[i]).ToVector3());
				if(!HoverPoint(mouse, camera, ref min, v0)) continue;
				hoverV0 = t[i];
				hoverV1 = -1;
			}
		}

		if(hoverV0 != -1) {
			dist = min;
			return new MeshVertexEntity(this, hoverV0);
		}

		foreach(var ti in tris) {
			var t = hitMesh.Mesh.GetTriangle(ti);
			for(int i = 0; i < 3; i++) {
				var v0 = hitMesh.Mesh.GetVertex(t[i]).ToVector3();
				var v1 = hitMesh.Mesh.GetVertex(t[(i + 1) % 3]).ToVector3();
				if(!meshCheck.IsEdgeSharp(v0, v1)) continue;
				if(!HoverSegment(mouse, camera, ref min, fullTf.MultiplyPoint3x4(v0), fullTf.MultiplyPoint3x4(v1))) continue;
				hoverV0 = t[i];
				hoverV1 = t[(i + 1) % 3];
			}
		}

		if(hoverV0 != -1) {
			dist = min;
			return new MeshEdgeEntity(this, hoverV0, hoverV1);
		}

		return null;
	}

}
	