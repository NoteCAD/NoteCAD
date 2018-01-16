using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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

}
