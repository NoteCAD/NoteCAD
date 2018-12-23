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
			eid.path.Add(new Id(v0, v1));
			return eid;
		}
	}

	public IPlane plane {
		get {
			return null;
		}
	}
	
	ExpVector p0 { get { return feature.basis.TransformPosition(feature.hitMesh.Mesh.GetVertex(v0).ToVector3()); } }
	ExpVector p1 { get { return feature.basis.TransformPosition(feature.hitMesh.Mesh.GetVertex(v1).ToVector3()); } }

	public IEnumerable<ExpVector> points {
		get {
			yield return p0;
			yield return p1;
		}
	}

	public IEnumerable<Vector3> segments {
		get {
			yield return feature.transform.MultiplyPoint3x4(feature.hitMesh.Mesh.GetVertex(v0).ToVector3());
			yield return feature.transform.MultiplyPoint3x4(feature.hitMesh.Mesh.GetVertex(v1).ToVector3());
		}
	}

	public ExpVector PointOn(Exp t) {
		return p0 + (p1 - p0) * t;
	}

	public ExpVector TangentAt(Exp t) {
		return p1 - p0;
	}

	public Exp Length() {
		return (p1 - p0).Magnitude();
	}

	public Exp Radius() {
		return null;
	}

	public ExpVector Center() {
		return null;
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
			eid.path.Add(new Id(v0, -1));
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
		return this.GetLineP0(null);
	}

	public ExpVector TangentAt(Exp t) {
		return null;
	}

	public Exp Length() {
		return null;
	}

	public Exp Radius() {
		return null;
	}

	public ExpVector Center() {
		return null;
	}

}

[Serializable]
public class MeshImportFeature : MeshFeature {
	[NonSerialized]
	public Mesh mesh = new Mesh();

	[NonSerialized]
	public MeshCheck meshCheck = new MeshCheck();

	[NonSerialized]
	public DMeshAABBTree3 hitMesh;

	GameObject go;
	List<Pair<Vector3, Vector3>> edges;

	[NonSerialized]
	public bool useThreshold_ = false;
	public bool useThreshold {
		get {
			return useThreshold_;
		}
		set {
			if(useThreshold_ == value) return;
			useThreshold_ = value;
			if(useThreshold_ == true) {
				meshCheck.setMesh(mesh);
			}
			MarkDirty();
			edges = null;
		}
	}

	[NonSerialized]
	float thresholdAngle_ = 25f;
	public float thresholdAngle {
		get {
			return thresholdAngle_;
		}
		set {
			thresholdAngle_ = value;
			MarkDirty();
			edges = null;
		}
	}

	public ExpBasis basis { get; private set; }

	public MeshImportFeature() {
		basis = new ExpBasis();
	}

	public MeshImportFeature(byte[] data) {
		MemoryStream ms = new MemoryStream(data);
		var meshes = Parabox.STL.pb_Stl_Importer.Import(ms);
		mesh = meshes[0];
		useThreshold = (mesh.GetIndexCount(0) < 5000);
		if(meshes.Length > 1) {
			Debug.LogWarning("Imported " + meshes.Length + " meshes, but used only one");
		}

		//meshCheck.setMesh(mesh);
		//mesh = meshCheck.ToUnityWatertightMesh();
		basis = new ExpBasis();
		hitMesh = new DMeshAABBTree3(mesh.ToDMesh3(), true);
	}

	public override GameObject gameObject {
		get {
			return go;
		}
	}

	public override ICADObject GetChild(Id guid) {
		if(guid.value == -1) return sketch;
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

	new LineCanvas canvas;

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
			if(canvas == null) {
				go = new GameObject("MeshImportFeature");
				canvas = GameObject.Instantiate(EntityConfig.instance.lineCanvas, go.transform);
			} else {
				canvas.Clear();
			}
			canvas.SetStyle("entities");
			if(useThreshold) {
				edges = meshCheck.GenerateEdges(thresholdAngle);
			} else {
				edges = new List<Pair<Vector3, Vector3>>();
				var indices = mesh.GetIndices(0);
				var vertices = mesh.vertices;
				for(int i = 0; i < indices.Length / 3; i++) {
					for(int j = 0; j < 3; j++) {
						edges.Add(new Pair<Vector3, Vector3>(vertices[indices[i * 3 + j]], vertices[indices[i * 3 + (j + 1) % 3]]));
					}
				}
			}
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
		StringBuilder sb = new StringBuilder();
		var indices = mesh.GetIndices(0);
		var verts = mesh.vertices;
		for(int i = 0; i < indices.Length; i++) {
			if(i != 0) sb.Append(" ");
			sb.Append(verts[indices[i]].ToStr());
		}
		xml.WriteAttributeString("basis", basis.ToString());
		xml.WriteAttributeString("mesh", sb.ToString());
		xml.WriteAttributeString("useThreshold", useThreshold.ToString());
	}

	protected override void OnReadMeshFeature(XmlNode xml) {
		basis.FromString(xml.Attributes["basis"].Value);
		if(xml.Attributes["useThreshold"] != null) useThreshold = Convert.ToBoolean(xml.Attributes["useThreshold"].Value);
		var strVerts = xml.Attributes["mesh"].Value.Split(' ');
		var verts = new Vector3[strVerts.Length / 3];
		for(int i = 0; i < strVerts.Length / 3; i++) {
			verts[i].x = strVerts[i * 3 + 0].ToFloat();
			verts[i].y = strVerts[i * 3 + 1].ToFloat();
			verts[i].z = strVerts[i * 3 + 2].ToFloat();
		}
		var indices = new int[verts.Length];
		for(int i = 0; i < indices.Length; i++) {
			indices[i] = i;
		}
		mesh.Clear();
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		mesh.vertices = verts;
		mesh.SetIndices(indices, MeshTopology.Triangles, 0);
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();
		meshCheck.setMesh(mesh);
		hitMesh = new DMeshAABBTree3(mesh.ToDMesh3(), true);
	}

	public UnityEngine.Matrix4x4 transform {
		get {
			return basis.matrix;
		}
	}
	protected override ICADObject OnHover(Vector3 mouse, Camera camera, UnityEngine.Matrix4x4 tf, ref double dist) {
		var tris = new List<int>();
		var fullTf = tf * transform;
		var invFullTf = fullTf.inverse;
		var ray = camera.ScreenPointToRay(mouse);
		ray.origin = invFullTf.MultiplyPoint3x4(ray.origin);
		ray.direction = invFullTf.MultiplyVector(ray.direction);
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
				if(useThreshold && !meshCheck.IsEdgeSharp(v0, v1, thresholdAngle)) continue;
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
	