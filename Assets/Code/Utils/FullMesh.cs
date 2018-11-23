using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public struct Pair<A, B> {
	public A a;
	public B b;

	public Pair(A a_, B b_) {
		a = a_;
		b = b_;
	}
}

public class MeshCheck {
		
	float EPSILON = 1e-4f;
			
	class Vertex {
		public Vector3 pos;
		public int index;
	};
			
	class Edge {
		public Vertex a;
		public Vertex b;
		public List<Triangle> triangles = new List<Triangle>();

		public void removeTriangle(Triangle triangle) {
			int i = triangles.IndexOf(triangle);
			if(i < 0) {
				//Debug.Log("dont remove triangle from edge\n");
				return;
			}
			triangles.RemoveAt(i);
		}

		public bool ContainsVertices(Vertex va, Vertex vb) {
			return a == va && b == vb || a == vb && b == va;
		}
	};
			
	class Triangle {
		public Vertex[] vertices = new Vertex[3];
		public Edge[] edges = new Edge[3];

		public Vector3 normal {
			get {
				return Vector3.Cross(vertices[0].pos - vertices[1].pos, vertices[2].pos - vertices[1].pos).normalized;
			}
		}
	};
			
	List<Vertex> vertices = new List<Vertex>();
	List<Triangle> triangles = new List<Triangle>();
	Dictionary<Pair<Vertex , Vertex>, Edge> edges = new Dictionary<Pair<Vertex, Vertex>, Edge>();

	public Mesh ToUnitySmoothMesh() {

		Mesh mesh = new Mesh();
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

		var verts = new List<Vector3>(triangles.Count * 3);
		var norms = new List<Vector3>(triangles.Count * 3);
		var vertexTriangles = new List<List<Triangle>>(vertices.Count);

		// make vertices indexing
		// initialize vertex-to-triangle array
		for(int i = 0; i < vertices.Count; i++) {
			vertices[i].index = i;
			vertexTriangles.Add(new List<Triangle>());
		}

		// populate vertex-to-triangle table
		for(int i = 0; i < triangles.Count; i++) {
			Triangle t = triangles[i];
			for(var j = 0; j < 3; j++) {
				vertexTriangles[t.vertices[j].index].Add(t);
			}
		}

		for(int i = 0; i < triangles.Count * 3; i++) {
			norms.Add(Vector3.zero);
			verts.Add(Vector3.zero);
		}

		for(int i = 0; i < triangles.Count; i++) {
			Triangle t = triangles[i];
			for(int j = 0; j < 3; j++) {
				Vertex v = t.vertices[j];
				verts[i * 3 + j] = v.pos;
				for(int k = 0; k < vertexTriangles[v.index].Count; k++) {
					norms[i * 3 + j] += vertexTriangles[v.index][k].normal;
				}
			}
		}

		for(int i = 0; i < norms.Count; i++) {
			norms[i] = norms[i].normalized;
		}

		var indices = new int[verts.Count];
		for(int i = 0; i < indices.Length; i++) indices[i] = i;

		mesh.SetVertices(verts);
		mesh.SetNormals(norms);
		mesh.SetIndices(indices, MeshTopology.Triangles, 0);
		mesh.RecalculateBounds();
		//mesh.RecalculateNormals();
		//mesh.RecalculateTangents();
		return mesh;
	}

	public Mesh ToUnityMesh() {

		Mesh mesh = new Mesh();
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

		var verts = new List<Vector3>();

		foreach(var tri in triangles) {
			for(var i = 0; i < 3; i++) {
				verts.Add(tri.vertices[i].pos);
			}
		}

		var indices = new int[verts.Count];
		for(int i = 0; i < indices.Length; i++) indices[i] = i;

		mesh.SetVertices(verts);
		mesh.SetIndices(indices, MeshTopology.Triangles, 0);
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();
		return mesh;
	}

	public Mesh ToUnityWatertightMesh() {

		Mesh mesh = new Mesh();
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

		for(int i = 0; i < vertices.Count; i++) {
			vertices[i].index = i;
		}

		var verts = vertices.Select(v => v.pos).ToList();

		var indices = new int[triangles.Count * 3];
		for(int i = 0; i < triangles.Count; i++) {
			for(var j = 0; j < 3; j++) {
				indices[i * 3 + j] = triangles[i].vertices[j].index;
			}
		}

		mesh.SetVertices(verts);
		mesh.SetIndices(indices, MeshTopology.Triangles, 0);
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();
		return mesh;
	}
			
	void splitEdges() {
		Edge split = null;
		for(int j = 0; j < vertices.Count; j++) {
			var vertex = vertices[j];
			do {
				split = null;
				for(var it = edges.Values.GetEnumerator(); it.MoveNext();) {
					var edge = it.Current;
					if(GeomUtils.DistancePointSegment3D(vertex.pos, edge.a.pos, edge.b.pos) > EPSILON) continue;
					if(edge.a == vertex) continue;
					if(edge.b == vertex) continue;
					split = edge;
					break;
				}
						
				if(split != null) {
					removeEdge(split);
					while(split.triangles.Count > 0) {
						//Debug.Log("split triangels " + split.triangles.Count);
						Triangle triangle = split.triangles[0];
						//foreach(Edge edge in triangle.edges) {
						//	if(edge == split) continue;
						//	addTriangle(edge.a, edge.b, vertex);
						//}
						for(int i = 0; i < 3; i++) {
							Vertex a = triangle.vertices[i];
							Vertex b = triangle.vertices[(i + 1) % 3];
							if(split.ContainsVertices(a, b)) {
								Vertex c = triangle.vertices[(i + 2) % 3];
								addTriangle(a, vertex, c);
								addTriangle(vertex, b, c);
								break;
							}
						}
						removeTriangle(triangle);
						if(split.triangles.Count > 0 && split.triangles[0] == triangle) {
							//Debug.Log("split triangele not died!!!!!\n");
							split.removeTriangle(triangle);
						}
					}
				}
			} while(split != null);
		}
			
	}
			
	Vertex getVertex(Vector3 v) {
		for(int i = 0; i < vertices.Count; i++) {
			var vx = vertices[i];
			if((vx.pos - v).sqrMagnitude > EPSILON * EPSILON) continue;
			//if(vx.pos.x != v.x || vx.pos.y != v.y || vx.pos.z != v.z) continue;
			return vx;
		}
		return null;
	}

	Vertex addVertex(Vector3 v) {
		Vertex result = getVertex(v);
		if(result != null) return result;
		
		result = new Vertex();
		result.pos = v;
		vertices.Add(result);				
		return result;
	}
			
	void removeTriangle(Triangle triangle) {
		int i = triangles.IndexOf(triangle);
		if(i < 0) {
			//Debug.Log("dont removeTriangle\n");
			return;
		}
		for(int j = 0; j < triangle.edges.Length; j++) {
			triangle.edges[j].removeTriangle(triangle);
		}
		triangles.RemoveAt(i);
	}
			
	void removeEdge(Vertex v0, Vertex v1) {
		edges.Remove(new Pair<Vertex, Vertex>(v0, v1));
		edges.Remove(new Pair<Vertex, Vertex>(v1, v0));
	}
			
	void removeEdge(Edge edge) {
		removeEdge(edge.a, edge.b);
	}

	Edge getEdge(Vertex v0, Vertex v1) {
		Edge result = null;
		edges.TryGetValue(new Pair<Vertex, Vertex >(v0, v1), out result);
		if(result != null) return result;
		edges.TryGetValue(new Pair<Vertex, Vertex >(v1, v0), out result);
		return result;
	}

	Edge addEdge(Vertex v0, Vertex v1) {
		Edge result = getEdge(v0, v1);
		if(result != null) return result;
				
		result = new Edge();
		result.a = v0;
		result.b = v1;
		edges.Add(new Pair<Vertex, Vertex>(v0, v1), result);
		return result;
	}
			
	void addTriangle(Vector3 a, Vector3 b, Vector3 c) {
		addTriangle(addVertex(a), addVertex(b), addVertex(c));
	}
			
	void addTriangle(Vertex a, Vertex b, Vertex c) {
		if(a == b || a == c || b == c) {
			//Debug.Log("bad triangle\n");
			return;
		}
				
		Triangle t = new Triangle();
				
		// set triangle vertices
		t.vertices[0] = a;
		t.vertices[1] = b;
		t.vertices[2] = c;
				
		// add edges
		for(int i = 0; i < 3; i++) {
			Edge e = addEdge(t.vertices[i], t.vertices[(i + 1) % 3]);
			e.triangles.Add(t);
			t.edges[i] = e;
		}
		triangles.Add(t);
	}
	
	/*
	public void saveSTL(const char *path) {
		FILE *f = fopen(path, "wb");
				
		char str[80];
		memset(str, 0, sizeof(str));
		strcpy(str, "GlobeXY::CAD STereoLithography Mesh Export.");
		fwrite(str, 1, 80, f);
			
		int32_t n = triangles.size();
		fwrite(&n, 4, 1, f);
			
		for(Triangle t : triangles) {
					
			Vec3 a(t.vertices[0].pos);
			Vec3 b(t.vertices[1].pos);
			Vec3 c(t.vertices[2].pos);
					
			// normal
			Vec3 n = normalize(cross(b - a, c - b));
			fwrite(&n.x, 4, 1, f);
			fwrite(&n.y, 4, 1, f);
			fwrite(&n.z, 4, 1, f);
					
			// a
			fwrite(&a.x, 4, 1, f);
			fwrite(&a.y, 4, 1, f);
			fwrite(&a.z, 4, 1, f);
					
			// b
			fwrite(&b.x, 4, 1, f);
			fwrite(&b.y, 4, 1, f);
			fwrite(&b.z, 4, 1, f);
					
			// c
			fwrite(&c.x, 4, 1, f);
			fwrite(&c.y, 4, 1, f);
			fwrite(&c.z, 4, 1, f);
					
			// attr
			uint16_t attr = 0;
			fwrite(&attr, 2, 1, f);
		}
				
		fclose(f);
	}
	*/
			
	public int getNumErrors() {
		return getNumOpenEdges() + getNumMultiEdges();
	}
			
	public int getNumOpenEdges(){
		int num_errors = 0;
		foreach(Edge e in edges.Values) {
			if(e.triangles.Count == 1) num_errors++;
		}
		return num_errors;
	}
			
	public int getNumMultiEdges() {
		int num_errors = 0;
		foreach(Edge e in edges.Values) {
			if(e.triangles.Count > 2) num_errors++;
		}
		return num_errors;
	}
			
	public void clear() {
		triangles.Clear();				
		edges.Clear();
		vertices.Clear();
	}
			
	public void setMesh(Mesh mesh) {
		clear();
				
		Vector3[] vertices = mesh.vertices;
		int[] triangles = mesh.GetTriangles(0);
		int n = triangles.Length / 3;
		for(int i = 0; i < n; ++i) {
			int ai = triangles[i * 3 + 0];
			int bi = triangles[i * 3 + 1];
			int ci = triangles[i * 3 + 2];
					
			Vector3 a = vertices[ai];
			Vector3 b = vertices[bi];
			Vector3 c = vertices[ci];
					
			addTriangle(a, b, c);
		}
		splitEdges();
	}
			
	Vector3 CalculateNormal(int tri, int e, List<Vector3> vert, List<int> tris) {
		var v0 = vert[tris[tri + e]];
		var v1 = vert[tris[tri + (e + 1) % 3]];
		var v2 = vert[tris[tri + (e + 2) % 3]];
		return Vector3.Cross(v0 - v1, v2 - v1).normalized;
	}

	Vector3 Snap(Vector3 v, List<Vector3> vert) {
		for(int i = 0; i < vert.Count; i++) {
			if((v - vert[i]).sqrMagnitude <= EPSILON * EPSILON) return vert[i];
		}
		return v;
	}

	bool IsNormalsSharp(Vector3 n0, Vector3 n1, float angle) {
		return Vector3.Dot(n0, n1) <= Mathf.Cos(angle * Mathf.PI / 180f);
	}

	bool IsEdgeSharp(Edge edge, float angle) {
		Vector3 prevNormal = Vector3.zero;
		bool first = true;
		foreach(var tri in edge.triangles) {
			if(first) {
				prevNormal = tri.normal;
				first = false;
				continue;
			}
			if(IsNormalsSharp(tri.normal, prevNormal, angle)) {
				return true;
			}
		}
		return false;
	}

	public bool IsEdgeSharp(Vector3 p0, Vector3 p1, float angle) {
		var v0 = getVertex(p0);
		if(v0 == null) return false;
		var v1 = getVertex(p1);
		if(v1 == null) return false;
		var edge = getEdge(v0, v1);
		if(edge == null) return false;
		return IsEdgeSharp(edge, angle);
	}

	public List<Pair<Vector3, Vector3>> GenerateEdges(float angle) {
		List<Pair<Vector3, Vector3>> result = new List<Pair<Vector3, Vector3>>();
		foreach(var edge in edges.Values) {
			if(!IsEdgeSharp(edge, angle)) continue;
			result.Add(new Pair<Vector3, Vector3>(edge.a.pos, edge.b.pos));
		}
		return result;
	}

	public void drawErrors(LineCanvas renderer) {
		int i = 0;
				
		int num_errors = 0;
		foreach(Edge e in edges.Values) {
			if(e.triangles.Count == 2) continue;
			i++;
			num_errors++;
			//switch(e.triangles.size()) {
			switch(e.triangles.Count) {
				case 1: 
					renderer.SetStyle("error");
					break;
				case 2: {
					renderer.SetStyle("entities");
					break;
				}
				default:
					renderer.SetStyle("hovered");
					break;
			}
			renderer.DrawLine(e.a.pos, e.b.pos);
		}
		if(num_errors != 0) {
			//Debug.Log("Achtung!!! STL check num_errors = %d\n", num_errors);
		}
	}
		
};

