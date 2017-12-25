using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DraftStroke : MonoBehaviour {

	class DashStyle {
		float[] dashes;
	}

	[System.Serializable]
	public class StrokeStyle {
		//DashStyle dash;
		public string name;
		public Color color = Color.white;
		public float width = 1f;
		//float stippleWidth;
		//bool pixel;
	}

	public StrokeStyle[] styles = new StrokeStyle[0]; 

	struct Line {
		public Vector3 a;
		public Vector3 b;
	}

	class Lines {
		public List<Line> lines = new List<Line>();
		public Mesh mesh;
		public MeshRenderer renderer;
		public bool dirty;

		public void AddLine(Vector3 a, Vector3 b) {
			lines.Add(new Line { a = a, b = b } );
			dirty = true;
		}
	}
	
	public Material material;

	void CreateMesh(Lines lines) {
		var go = new GameObject();
		go.transform.parent = gameObject.transform;
		var mr = go.AddComponent<MeshRenderer>();
		mr.material = material;
		var mf = go.AddComponent<MeshFilter>();
		var mesh = new Mesh();
		mesh.name = "lines";
		mf.mesh = mesh;
		lines.mesh = mesh;
		lines.renderer = mr;
	}

	void FillMesh(Mesh mesh, List<Line> lines) {
		mesh.Clear();
		if(lines.Count == 0) return;
		var vertices = new List<Vector3>(lines.Count * 4);
		var tangents = new List<Vector4>(lines.Count * 4);
		var normals = new List<Vector3>(lines.Count * 4);
		var indices = new int[lines.Count * 6];
		for(int i = 0; i < lines.Count; i++) {
			var l = lines[i];
			var t = (l.b - l.a).normalized;
			vertices.Add(l.a);
			tangents.Add(new Vector4(-1.0f, -1.0f));
			normals.Add(t);

			vertices.Add(l.a);
			tangents.Add(new Vector4(-1.0f, +1.0f));
			normals.Add(t);

			vertices.Add(l.b);
			tangents.Add(new Vector4(+1.0f, -1.0f));
			normals.Add(t);

			vertices.Add(l.b);
			tangents.Add(new Vector4(+1.0f, +1.0f));
			normals.Add(t);

			indices[i * 6 + 0] = i * 4 + 0;
			indices[i * 6 + 1] = i * 4 + 1;
			indices[i * 6 + 2] = i * 4 + 2;

			indices[i * 6 + 3] = i * 4 + 3;
			indices[i * 6 + 4] = i * 4 + 2;
			indices[i * 6 + 5] = i * 4 + 1;
		}
		mesh.SetVertices(vertices);
		mesh.SetTangents(tangents);
		mesh.SetNormals(normals);
		mesh.SetIndices(indices, MeshTopology.Triangles, 0, true);
		mesh.RecalculateBounds();
	}

	Dictionary<StrokeStyle, Lines> lines = new Dictionary<StrokeStyle, Lines>();

	void UpdateDirty() {
		var pixel = (Camera.main.ScreenToWorldPoint(new Vector3(1f, 0f, 0f)) - Camera.main.ScreenToWorldPoint(Vector3.zero)).magnitude;
		Vector4 dir = Camera.main.transform.forward;
		foreach(var l in lines) {
			if(l.Value.mesh == null) {
				CreateMesh(l.Value);
			}
			var material = l.Value.renderer.material;
			var style = l.Key;
			material.SetFloat("_Pixel", pixel);
			material.SetVector("_CamDir", dir);
			material.SetFloat("_Width", style.width);
			material.SetColor("_Color", style.color);
			if(l.Value.dirty) {
				FillMesh(l.Value.mesh, l.Value.lines);
				l.Value.dirty = false;
			}
		}
	}

	StrokeStyle currentStyle = new StrokeStyle();

	public void SetStyle(string name) {
		currentStyle = styles.First(s => s.name == name);
		if(!lines.ContainsKey(currentStyle)) {
			lines[currentStyle] = new Lines();
		}
	}

	public void DrawLine(Vector3 a, Vector3 b) {
		lines[currentStyle].AddLine(a, b);
	}

	private void LateUpdate() {
		UpdateDirty();
	}

	public void Clear() {
		foreach(var l in lines) {
			l.Value.lines.Clear();
			l.Value.dirty = true;
		}
	}
}
