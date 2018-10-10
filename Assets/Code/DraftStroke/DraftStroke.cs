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
		public int queue = -1;
		public bool depthTest = true;
		//float stippleWidth;
		//bool pixel;
	}

	public StrokeStyle[] styles = new StrokeStyle[0]; 
	public GameObject parent;

	struct Line {
		public Vector3 a;
		public Vector3 b;
	}

	class Lines {
		public List<Line> lines = new List<Line>();
		public List<GameObject> objects = new List<GameObject>();
		public List<Mesh> meshes = new List<Mesh>();
		public bool dirty;
		public StrokeStyle style;
		public Material material;

		public Lines(StrokeStyle s) {
			style = s;
		}
		public void AddLine(Vector3 a, Vector3 b) {
			lines.Add(new Line { a = a, b = b } );
			dirty = true;
		}
		
		public void ClearMeshes() {
			foreach(var m in meshes) {
				Destroy(m);
			}
			foreach(var m in objects) {
				DestroyImmediate(m.GetComponent<MeshRenderer>().material);
				Destroy(m);
			}
			objects.Clear();
			meshes.Clear();
			dirty = true;
		}

		public void Clear() {
			lines.Clear();
			ClearMeshes();
		}
	}
	
	public Material material;
	public Material materialDepthOff;

	Mesh CreateMesh(Lines lines, StrokeStyle ss) {
		var go = new GameObject(lines.style.name);
		go.transform.SetParent(parent != null ? parent.transform : gameObject.transform, false);
		var mr = go.AddComponent<MeshRenderer>();
		mr.material = ss.depthTest ? material : materialDepthOff;
		lines.material = mr.material;
		var mf = go.AddComponent<MeshFilter>();
		var mesh = new Mesh();
		mesh.name = "lines";
		mf.mesh = mesh;
		lines.objects.Add(go);
		lines.meshes.Add(mesh);
		return mesh;
	}

	void FillMesh(Lines li) {
		li.ClearMeshes();
		var lines = li.lines;
		if(lines.Count == 0) return;
		int maxLines = 64000 / 4;
		int meshesCount = lines.Count / maxLines + 1;
		int lineStartIndex = 0;
		for(int mi = 0; mi < meshesCount; mi++) {
			var curLinesCount = (mi == meshesCount - 1) ? lines.Count % maxLines : maxLines; 
			var vertices = new Vector3[curLinesCount * 4];
			var tangents = new Vector4[curLinesCount * 4];
			var normals = new Vector3[curLinesCount * 4];
			var indices = new int[curLinesCount * 6];
			int curV = 0;

			var t0 = new Vector4(-1.0f, -1.0f);
			var t1 = new Vector4(-1.0f, +1.0f);
			var t2 = new Vector4(+1.0f, -1.0f);
			var t3 = new Vector4(+1.0f, +1.0f);

			for(int i = 0; i < curLinesCount; i++) {
				var l = lines[i + lineStartIndex];
				var t = l.b - l.a;
				vertices[curV] = l.a;
				tangents[curV] = t0;
				normals[curV] = t;
				curV++;

				vertices[curV] = l.a;
				tangents[curV] = t1;
				normals[curV] = t;
				curV++;

				vertices[curV] = l.b;
				tangents[curV] = t2;
				normals[curV] = t;
				curV++;

				vertices[curV] = l.b;
				tangents[curV] = t3;
				normals[curV] = t;
				curV++;

				indices[i * 6 + 0] = i * 4 + 0;
				indices[i * 6 + 1] = i * 4 + 1;
				indices[i * 6 + 2] = i * 4 + 2;

				indices[i * 6 + 3] = i * 4 + 3;
				indices[i * 6 + 4] = i * 4 + 2;
				indices[i * 6 + 5] = i * 4 + 1;
			}
			var mesh = CreateMesh(li, li.style);
			mesh.vertices = vertices;
			mesh.tangents = tangents;
			mesh.normals = normals;
			mesh.SetIndices(indices, MeshTopology.Triangles, 0, true);
			//mesh.RecalculateBounds();
			lineStartIndex += curLinesCount;
		}
	}

	Dictionary<StrokeStyle, Lines> lines = new Dictionary<StrokeStyle, Lines>();

	public void DrawToGraphics(Matrix4x4 tf) {
		for(var li = lines.GetEnumerator(); li.MoveNext(); ) {
			var material = li.Current.Value.material;
			for(int i = 0; i < li.Current.Value.meshes.Count; i++) {
				var mesh = li.Current.Value.meshes[i];
				Graphics.DrawMesh(mesh, tf, material, 0);
			}
		}
	}

	public static double getPixelSize() {
		return 1.0 / Camera.main.nonJitteredProjectionMatrix.GetColumn(0).magnitude / (double)Camera.main.pixelWidth * 2.0;
	}

	public void UpdateDirty() {
		var pixel = getPixelSize();
		Vector4 dir = Camera.main.transform.forward.normalized;
		Vector4 right = Camera.main.transform.right.normalized;
		foreach(var l in lines) {
			var style = l.Key;
			if(l.Value.dirty) {
				FillMesh(l.Value);
				l.Value.dirty = false;
			}
			foreach(var m in l.Value.objects) {
				var material = m.GetComponent<MeshRenderer>().material;
				material.SetFloat("_Pixel", (float)pixel);
				material.SetVector("_CamDir", dir);
				material.SetVector("_CamRight", right);
				material.SetFloat("_Width", style.width);
				material.SetColor("_Color", style.color);
				material.renderQueue = style.queue;
			}
		}
	}

	StrokeStyle currentStyle = new StrokeStyle();
	Lines currentLines = null;

	public StrokeStyle GetStyle(string name) {
		return styles.First(s => s.name == name);
	}

	public void SetStyle(string name) {
		currentStyle = styles.First(s => s.name == name);
		if(!lines.ContainsKey(currentStyle)) {
			lines[currentStyle] = new Lines(currentStyle);
		}
		currentLines = lines[currentStyle];
	}

	public void DrawLine(Vector3 a, Vector3 b) {
		currentLines.AddLine(a, b);
	}

	private void LateUpdate() {
		UpdateDirty();
	}

	public void Clear() {
		foreach(var l in lines) {
			l.Value.Clear();
		}
	}

	public void ClearStyle(string name) {
		foreach(var l in lines) {
			if(l.Key.name != name) continue;
			l.Value.Clear();
		}
	}
}
