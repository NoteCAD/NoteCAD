using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

public class DraftStroke : MonoBehaviour {
	public StrokeStyles strokeStyles;
	public GameObject parent;

	Camera _camera;
	Camera Camera => _camera != null ? _camera : (_camera = Camera.main);

	struct Line {
		public Vector3 a;
		public Vector3 b;

		public void Swap() {
			Vector3 t = a;
			a = b;
			b = t;
		}
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
			lines.Add(new Line { a = a, b = b });
			dirty = true;
		}

		public void ClearMeshes() {
			foreach (var m in meshes) {
				DestroyImmediate(m);
			}
			foreach (var m in objects) {
				DestroyImmediate(m.GetComponent<MeshRenderer>().sharedMaterial);
				DestroyImmediate(m);
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

	public DraftStroke() {
		SetStyle(new StrokeStyle());
	}

	public Material material;
	public Material materialDepthOff;
	
	Mesh CreateMesh(Lines lines, StrokeStyle ss) {
		var go = new GameObject(lines.style.name);
		go.transform.SetParent(parent != null ? parent.transform : gameObject.transform, false);
		var mr = go.AddComponent<MeshRenderer>();
		mr.sharedMaterial = ss.depthTest ? material : materialDepthOff;
		mr.sharedMaterial = new Material(ss.depthTest ? Shader.Find("NoteCAD/DraftLines") : Shader.Find("NoteCAD/DraftLinesDepthOff"));
		lines.material = mr.sharedMaterial;
		var mf = go.AddComponent<MeshFilter>();
		var mesh = new Mesh();
		mesh.name = "lines";
		mf.sharedMesh = mesh;
		lines.objects.Add(go);
		lines.meshes.Add(mesh);
		return mesh;
	}

	void FillMesh(Lines li) {
		li.ClearMeshes();
		var lines = li.lines;
		if (lines.Count == 0) return;
		int maxLines = 64000 / 4;
		int meshesCount = lines.Count / maxLines + 1;
		int lineStartIndex = 0;
		for (int mi = 0; mi < meshesCount; mi++) {
			var curLinesCount = (mi == meshesCount - 1) ? lines.Count % maxLines : maxLines;
			var vertices = new Vector3[curLinesCount * 4];
			var tangents = new Vector4[curLinesCount * 4];
			var normals = new Vector3[curLinesCount * 4];
			var coords = new Vector2[curLinesCount * 4];
			var indices = new int[curLinesCount * 6];
			int curV = 0;

			var t0 = new Vector4(-1.0f, -1.0f);
			var t1 = new Vector4(-1.0f, +1.0f);
			var t2 = new Vector4(+1.0f, -1.0f);
			var t3 = new Vector4(+1.0f, +1.0f);
			float phase = 0f;

			for (int i = 0; i < curLinesCount; i++) {
				var l = lines[i + lineStartIndex];
				bool needZeroPhase = false;
				if (i < curLinesCount - 1) {
					var nl = lines[i + 1 + lineStartIndex];
					if (l.b == nl.b) {
						nl.Swap();
						lines[i + 1 + lineStartIndex] = nl;
					} else
					if (l.a == nl.b) {
						nl.Swap();
						lines[i + 1 + lineStartIndex] = nl;
						l.Swap();
					} else
					if (l.a == nl.a) {
						l.Swap();
					}
					if (l.b != nl.a) needZeroPhase = true;
				}
				var t = l.b - l.a;
				vertices[curV] = l.a;
				tangents[curV] = t0;
				normals[curV] = t;
				coords[curV] = new Vector2(phase, -1f);
				curV++;

				vertices[curV] = l.a;
				tangents[curV] = t1;
				normals[curV] = t;
				coords[curV] = new Vector2(phase, 1f);
				curV++;

				phase += t.magnitude;

				vertices[curV] = l.b;
				tangents[curV] = t2;
				normals[curV] = t;
				coords[curV] = new Vector2(phase, -1f);
				curV++;

				vertices[curV] = l.b;
				tangents[curV] = t3;
				normals[curV] = t;
				coords[curV] = new Vector2(phase, 1f);
				curV++;

				indices[i * 6 + 0] = i * 4 + 0;
				indices[i * 6 + 1] = i * 4 + 1;
				indices[i * 6 + 2] = i * 4 + 2;

				indices[i * 6 + 3] = i * 4 + 3;
				indices[i * 6 + 4] = i * 4 + 2;
				indices[i * 6 + 5] = i * 4 + 1;

				if (needZeroPhase) {
					//Debug.LogFormat("clear phase {0} {1}", l.b.ToStr(), nl.a.ToStr());
					phase = 0f;
				}

			}
			var mesh = CreateMesh(li, li.style);
			mesh.vertices = vertices;
			mesh.tangents = tangents;
			mesh.normals = normals;
			mesh.uv = coords;
			mesh.SetIndices(indices, MeshTopology.Triangles, 0, true);
			//mesh.RecalculateBounds();
			lineStartIndex += curLinesCount;
		}
	}

	Dictionary<StrokeStyle, Lines> lines = new Dictionary<StrokeStyle, Lines>();

	public void DrawToGraphics(Matrix4x4 tf) {
		for (var li = lines.GetEnumerator(); li.MoveNext();) {
			var material = li.Current.Value.material;
			for (int i = 0; i < li.Current.Value.meshes.Count; i++) {
				var mesh = li.Current.Value.meshes[i];
				Graphics.DrawMesh(mesh, tf, material, 0);
			}
		}
	}
	
	public double getPixelSize() {
		//var transformScale = transform.localToWorldMatrix.GetColumn(0).magnitude;
		//return 1.0 / (Camera.nonJitteredProjectionMatrix.GetColumn(0).magnitude * transformScale) / (double)Camera.pixelWidth * 2.0 * (Screen.dpi / 120f);
		return getGlobalPixelSize();
	}

	public static double dpiScale() {
		return Screen.dpi / 120f;
	}
	
	public static double getGlobalPixelSize() {
		return 1.0 / Camera.main.nonJitteredProjectionMatrix.GetColumn(0).magnitude / (double)Camera.main.pixelWidth * 2.0 * dpiScale();
	}

	public void UpdateDirty()
	{
		var pixel = getPixelSize();
		// These seem unnecesary. Dir is not used in shader
		// Right is used but in a special case, not sure how necessary.
		Vector4 dir = Camera.transform.forward.normalized;
		Vector4 right = Camera.transform.right.normalized;
		foreach (var l in lines) {
			var style = l.Key;
			if (l.Value.dirty) {
				FillMesh(l.Value);
				l.Value.dirty = false;
			}
			foreach (var m in l.Value.objects) {
				// meshrenderer.Material peroperty internally creates a defensive copy
				// sharedMaterial does not leak new objects
				// this changeg is bad if material is actually shared
				// Alexey: Actually I wish to inherit material here, not to use sharedMaterial
				// since I need to change current instance parameters. Ofc it can be done through
				// using material property block.

				// You are creating one unique material per Lines object, so both .material and
				// .sharedMaterial should return the same instance, but .material will check 
				// to see if anyother objects share this material. I still would rather use
				// .sharedMaterial and guarantees you're not leaking materials

				var material = m.GetComponent<MeshRenderer>().sharedMaterial;
				material.SetFloat("_Pixel", (float)pixel);
				material.SetFloat("_DpiScale", (float)dpiScale());
				material.SetVector("_CamDir", dir);
				material.SetVector("_CamRight", right);
				material.SetFloat("_Width", (float)(style.width * style.scale(pixel)));
				material.SetFloat("_StippleWidth", (float)(style.dashesScale(pixel)));
				material.SetFloat("_PatternLength", style.GetPatternLength());
				material.SetColor("_Color", style.color);
				material.SetTexture("_MainTex", DashAtlas.GetAtlas(style.dashes));
				material.renderQueue = style.queue;
			}
		}
	}

	StrokeStyle currentStyle = new StrokeStyle();
	Lines currentLines = null;

	public StrokeStyle GetStyle(string name) {
		return strokeStyles.styles.First(s => s.name == name);
	}

	public void SetStyle(StrokeStyle style) {
		currentStyle = style;
		//currentStyle = strokeStyles.styles.First(s => s.name == name);
		if (!lines.ContainsKey(currentStyle)) {
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
		foreach (var l in lines) {
			l.Value.Clear();
		}
		lines.Clear();
	}

	public void ClearStyle(string name) {
		foreach (var l in lines) {
			if (l.Key.name != name) continue;
			l.Value.Clear();
		}
	}
}
