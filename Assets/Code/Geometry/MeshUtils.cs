using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Csg;

public static class MeshUtils {

	public static void CreateMeshRegion(List<List<Vector3>> polygons, ref Mesh mesh) {
		var capacity = polygons.Sum(p => p.Count - 2) * 3;
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
			for(int i = 0; i < triangles.Count / 3; i++) {
				indices.Add(start + i * 3 + 0);
				indices.Add(start + i * 3 + 2);
				indices.Add(start + i * 3 + 1);
			}
		}
		mesh.name = "polygons";
		mesh.SetVertices(vertices);
		mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();

	}

	public static void CreateMeshExtrusion(List<List<Vector3>> polygons, float extrude, ref Mesh mesh) {
		var capacity = polygons.Sum(p => (p.Count - 2) * 3 + p.Count) * 2;
		var vertices = new List<Vector3>(capacity);
		var indices = new List<int>(capacity);
		bool inversed = extrude < 0f;
		foreach(var p in polygons) {
			var pv = new List<Vector3>(p);
			var triangles = Triangulation.Triangulate(pv);
			var start = vertices.Count;
			vertices.AddRange(triangles);
			if(!inversed) {
				for(int i = 0; i < triangles.Count; i++) {
					indices.Add(i + start);
				}
			} else {
				for(int i = 0; i < triangles.Count / 3; i++) {
					indices.Add(start + i * 3 + 0);
					indices.Add(start + i * 3 + 2);
					indices.Add(start + i * 3 + 1);
				}
			}
			var extrudeVector = Vector3.forward * extrude;
			var striangles = triangles.Select(pt => pt + extrudeVector).ToList();
			start = vertices.Count;
			vertices.AddRange(striangles);
			if(inversed) {
				for(int i = 0; i < striangles.Count; i++) {
					indices.Add(i + start);
				}
			} else {
				for(int i = 0; i < striangles.Count / 3; i++) {
					indices.Add(start + i * 3 + 0);
					indices.Add(start + i * 3 + 2);
					indices.Add(start + i * 3 + 1);
				}
			}

			start = vertices.Count();

			if(inversed) {
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
		mesh.Clear();
		mesh.name = "extrusion";
		mesh.SetVertices(vertices);
		mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();
	}
	
	public static Vector3D ToVector3D(this Vector3 v) {
		return new Vector3D(v.x, v.y, v.z);
	}

	public static Vertex ToVertex(this Vector3 v) {
		return new Vertex(v.ToVector3D());
	}

	public static Vector3 ToVector3(this Vector3D v) {
		return new Vector3((float)v.X, (float)v.Y, (float)v.Z);
	}

	public static Solid ToSolid(this Mesh mesh, UnityEngine.Matrix4x4 tf) {
		var indices = mesh.GetIndices(0);
		var polygons = new List<Polygon>();
		for(int i = 0; i < indices.Length / 3; i++) {
			var vertices = new List<Vertex>();
			for(int j = 0; j < 3; j++) {
				var v = mesh.vertices[indices[i * 3 + j]];
				v = tf.MultiplyPoint(v);
				vertices.Add(v.ToVertex());
			}
			polygons.Add(new Polygon(vertices));
		}
		return Solid.FromPolygons(polygons);
	}

	public static void FromSolid(this Mesh mesh, Solid solid) {
		var vertices = new List<Vector3>();

		foreach(var polygon in solid.Polygons) {
			if (polygon.Vertices.Count < 3) continue;
			var first = polygon.Vertices[0];
			for (var i = 0; i < polygon.Vertices.Count - 2; i++) {
				vertices.Add(first.Pos.ToVector3());
				vertices.Add(polygon.Vertices[i + 1].Pos.ToVector3());
				vertices.Add(polygon.Vertices[i + 2].Pos.ToVector3());
			}
		}
		var indices = new int[vertices.Count];
		for(int i = 0; i < indices.Length; i++) indices[i] = i;

		mesh.SetVertices(vertices);
		mesh.SetIndices(indices, MeshTopology.Triangles, 0);
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();
	}

	public static Solid Clone(this Solid solid) {
		var polygons = new List<Polygon>();
		foreach(var p in solid.Polygons) {
			var vertices = new List<Vertex>();
			foreach(var v in p.Vertices) {
				vertices.Add(new Vertex(v.Pos));
			}
			polygons.Add(new Polygon(vertices));
		}
		return Solid.FromPolygons(polygons);
	}

}
