using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Csg;
using g3;
using System;

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

	public static void DrawTriangulation(List<List<Vector3>> polygons, LineCanvas canvas) {
		foreach(var p in polygons) {
			var pv = new List<Vector3>(p);
			/*var triangles = */Triangulation.Triangulate(pv, canvas);
		}
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

	public static Solid CreateSolidExtrusion(List<List<Entity>> entitiyLoops, float extrude, UnityEngine.Matrix4x4 tf, IdPath feature) {
		var ids = new List<List<Id>>();
		var polygons = Sketch.GetPolygons(entitiyLoops, ref ids);
		bool inversed = extrude < 0f;
		
		var polys = new List<Polygon>();

		Func<Vector3, Vector3> TF = v => tf.MultiplyPoint(v);

		for(int pi = 0; pi < polygons.Count; pi++) {
			var p = polygons[pi];
			var pid = ids[pi];
			var pv = new List<Vector3>(p);
			var triangles = Triangulation.Triangulate(pv);

			IdPath polyId = feature.With(new Id(-1)); 
			bool invComp = true;
			var shift = Vector3.zero;
			var extrudeVector = Vector3.forward * extrude;

			for(int side = 0; side < 2; side++) {
				for(int i = 0; i < triangles.Count / 3; i++) {
					var polygonVertices = new List<Vertex>();
					for(int j = 0; j < 3; j++) {
						polygonVertices.Add(TF(triangles[i * 3 + j] + shift).ToVertex());
					}
					if(inversed == invComp) polygonVertices.Reverse();
					polys.Add(new Polygon(polygonVertices, polyId));
				}
				polyId = feature.With(new Id(-2)); 
				invComp = false;
				shift = extrudeVector;
			}

			Dictionary<Id, IdPath> paths = new Dictionary<Id, IdPath>();
			for(int i = 0; i < p.Count; i++) {
				var polygonVertices = new List<Vertex>();
				polygonVertices.Add(TF(p[i]).ToVertex());
				polygonVertices.Add(TF(p[(i + 1) % p.Count]).ToVertex());
				polygonVertices.Add(TF((p[(i + 1) % p.Count] + extrudeVector)).ToVertex());
				polygonVertices.Add(TF((p[i] + extrudeVector)).ToVertex());
				if(!inversed) polygonVertices.Reverse();
				IdPath curPath = null;
				if(!paths.ContainsKey(pid[i])) {
					curPath = feature.With(pid[i]);
					paths.Add(pid[i], curPath);
				} else {
					curPath = paths[pid[i]];
				}
				polys.Add(new Polygon(polygonVertices, curPath));
			}
		}
		return Solid.FromPolygons(polys);
	}

	public static Solid CreateSolidRevolve(List<List<Entity>> entitiyLoops, float angle, float helixStep, Vector3 axis, Vector3 origin,  float angleStep, UnityEngine.Matrix4x4 tf, IdPath feature) {
		var ids = new List<List<Id>>();
		var polygons = Sketch.GetPolygons(entitiyLoops, ref ids);
		bool isHelix = (Math.Abs(helixStep) > 1e-6);
		if(!isHelix && Mathf.Abs(angle) > 360f) angle = Mathf.Sign(angle) * 360f;
		bool inversed = angle < 0f;
		int subdiv = (int)Mathf.Ceil(Math.Abs(angle) / angleStep);
		//var drot = UnityEngine.Matrix4x4.Translate(origin) * UnityEngine.Matrix4x4.Rotate(Quaternion.AngleAxis(angle / subdiv, axis)) * UnityEngine.Matrix4x4.Translate(-origin);

		Func<float, Vector3, Vector3> PointOn = (float a, Vector3 point) => {
			var ax = axis;
			var axn = axis.normalized;
			var t = a / 360.0f;
			var o = origin;
			var prj = ExpVector.ProjectPointToLine(point, o, o + ax);
			var ra = Mathf.Atan2(helixStep / 4.0f, (point - prj).magnitude);
			var res = ExpVector.RotateAround(point, point - prj, o, ra);
			res = ExpVector.RotateAround(res, ax, o, a * Mathf.PI / 180.0f);
			return res + axn * t * helixStep;
		};

		var polys = new List<Polygon>();
		Func<Vector3, Vector3> TF = v => tf.MultiplyPoint(v);

		for(int pi = 0; pi < polygons.Count ; pi++) {
			var p = polygons[pi];
			var pid = ids[pi];
			var pv = new List<Vector3>(p);
			var triangles = Triangulation.Triangulate(pv);

			IdPath polyId = feature.With(new Id(-1)); 
			bool invComp = true;

			if(Math.Abs(Math.Abs(angle) - 360f) > 1e-6 || isHelix) {
				float a = 0f;
				for(int side = 0; side < 2; side++) {
					for(int i = 0; i < triangles.Count / 3; i++) {
						var polygonVertices = new List<Vertex>();
						for(int j = 0; j < 3; j++) {
							polygonVertices.Add(PointOn(a, TF(triangles[i * 3 + j])).ToVertex());
						}
						if(inversed == invComp) polygonVertices.Reverse();
						polys.Add(new Polygon(polygonVertices, polyId));
					}
					polyId = feature.With(new Id(-2)); 
					invComp = false;
					a = angle;
				}
			}

			Dictionary<Id, IdPath> paths = new Dictionary<Id, IdPath>();
			for(int i = 0; i < p.Count; i++) {
				float a = 0f;
				float da = angle / subdiv;
				for(int j = 0; j < subdiv; j++) {
					var polygonVertices = new List<Vertex>();
					polygonVertices.Add(PointOn(a, TF(p[(i + 1) % p.Count])).ToVertex());
					polygonVertices.Add(PointOn(a + da, TF(p[(i + 1) % p.Count])).ToVertex());
					polygonVertices.Add(PointOn(a + da, TF(p[i])).ToVertex());
					polygonVertices.Add(PointOn(a, TF(p[i])).ToVertex());
					a += da;
					if(!inversed) polygonVertices.Reverse();
					IdPath curPath = null;
					if(!paths.ContainsKey(pid[i])) {
						curPath = feature.With(pid[i]);
						paths.Add(pid[i], curPath);
					} else {
						curPath = paths[pid[i]];
					}

					if(isHelix) {
						var verts = new List<Vertex>();
						verts.Add(polygonVertices[0]);
						verts.Add(polygonVertices[1]);
						verts.Add(polygonVertices[2]);
						polys.Add(new Polygon(verts, curPath));

						verts = new List<Vertex>();
						verts.Add(polygonVertices[0]);
						verts.Add(polygonVertices[2]);
						verts.Add(polygonVertices[3]);
						polys.Add(new Polygon(verts, curPath));
					} else {
						polys.Add(new Polygon(polygonVertices, curPath));
					}
				}
			}
		}
		return Solid.FromPolygons(polys);
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

	public static Vector3d ToVector3d(this Vector3 v) {
		return new Vector3d(v.x, v.y, v.z);
	}

	public static Vector3 ToVector3(this Vector3d v) {
		return new Vector3((float)v.x, (float)v.y, (float)v.z);
	}

	public static Solid ToSolid(this Mesh mesh, UnityEngine.Matrix4x4 tf) {
		var indices = mesh.GetIndices(0);
		var polygons = new List<Polygon>();
		var mverts = mesh.vertices;
		for(int i = 0; i < indices.Length / 3; i++) {
			var vertices = new List<Vertex>();
			for(int j = 0; j < 3; j++) {
				var v = mverts[indices[i * 3 + j]];
				v = tf.MultiplyPoint(v);
				vertices.Add(v.ToVertex());
			}
			polygons.Add(new Polygon(vertices, -1));
		}
		return Solid.FromPolygons(polygons);
	}

	public static object Raytrace(this Solid solid, Ray ray) {
		double min = -1;
		Polygon res = null;
		for(int i = 0; i < solid.Polygons.Count; i++) {
			var polygon = solid.Polygons[i];
			var a = polygon.Vertices[0].Pos;
			for (var j = 0; j < polygon.Vertices.Count - 2; j++) {
				var b = polygon.Vertices[j + 1].Pos;
				var c = polygon.Vertices[j + 2].Pos;
				double t = -1;
				if(RaytraceTriangle(ray, a, b, c, ref t, true, false)) {
					if(min < 0 || min > t) {
						min = t;
						res = polygon;
					}
				}
			}
		}
		if(res != null) return res.userData;
		return null;
	}
	/*
	public static bool RaytraceTriangle(Ray ray, Vector3 vert0, Vector3 vert1, Vector3 vert2, ref float intsPoint, bool fs, bool fd) {

		// Idea: Tomas Moeller and Ben Trumbore
		// in Fast, Minimum Storage Ray/Triangle Intersection

		// Find vectors for two edges sharing vert0
		Vector3 rayDir = ray.direction;
		Vector3 edge1 = vert1 - vert0;
		Vector3 edge2 = vert2 - vert0;

		// Begin calculating determinant - also used to calculate U parameter
		Vector3 pvec = Vector3.Cross(rayDir, edge2);

		// If determinant is near zero, ray lies in plane of triangle
		float det = Vector3.Dot(edge1, pvec);

		//
		if(det < 1e-6) return false;
		float inv_det = 1.0f / det;

		// Calculate distance from vert0 to ray origin
		Vector3 tvec = ray.origin - vert0;

		// Calculate U parameter and test bounds
		float u = Vector3.Dot(tvec, pvec) * inv_det;
		if (u < 0.0f || u > 1.0f) return false;

		// Prepare to test V parameter
		Vector3 qvec = Vector3.Cross(tvec, edge1);

		// Calculate V parameter and test bounds
		float v = Vector3.Dot(rayDir, qvec) * inv_det;
		if (v < 0.0f || u + v > 1.0f) return false;

		// Calculate t, ray intersects triangle
		float t = Vector3.Dot(edge2, qvec) * inv_det;

		// Calculate intersection point and test ray length and direction
		if ((fs && t < 0.0f) || (fd && t > 1.0f)) return false;

		intsPoint = t;

		return true;
	}
	*/

	public static bool RaytraceTriangle(Ray ray, Vector3D vert0, Vector3D vert1, Vector3D vert2, ref double intsPoint, bool fs, bool fd) {

		// Idea: Tomas Moeller and Ben Trumbore
		// in Fast, Minimum Storage Ray/Triangle Intersection

		// Find vectors for two edges sharing vert0
		var rayDir = ray.direction.ToVector3D();
		var edge1 = vert1 - vert0;
		var edge2 = vert2 - vert0;

		// Begin calculating determinant - also used to calculate U parameter
		var pvec = rayDir.Cross(edge2);

		// If determinant is near zero, ray lies in plane of triangle
		var det = edge1.Dot(pvec);

		//
		if(det < 1e-6) return false;
		var inv_det = 1.0f / det;

		// Calculate distance from vert0 to ray origin
		var tvec = ray.origin.ToVector3D() - vert0;

		// Calculate U parameter and test bounds
		var u = tvec.Dot(pvec) * inv_det;
		if (u < 0.0f || u > 1.0f) return false;

		// Prepare to test V parameter
		var qvec = tvec.Cross(edge1);

		// Calculate V parameter and test bounds
		var v = rayDir.Dot(qvec) * inv_det;
		if (v < 0.0f || u + v > 1.0f) return false;

		// Calculate t, ray intersects triangle
		var t = edge2.Dot(qvec) * inv_det;

		// Calculate intersection point and test ray length and direction
		if ((fs && t < 0.0f) || (fd && t > 1.0f)) return false;

		intsPoint = t;

		return true;
	}

	public static void FromSolid(this Mesh mesh, Solid solid, object selected) {
		var vertices = new List<Vector3>();

		foreach(var polygon in solid.Polygons) {
			if(polygon.Vertices.Count < 3) continue;
			if(polygon.userData != selected) continue;
			var first = polygon.Vertices[0];
			for (var i = 0; i < polygon.Vertices.Count - 2; i++) {
				vertices.Add(first.Pos.ToVector3());
				vertices.Add(polygon.Vertices[i + 1].Pos.ToVector3());
				vertices.Add(polygon.Vertices[i + 2].Pos.ToVector3());
			}
		}
		var indices = new int[vertices.Count];
		for(int i = 0; i < indices.Length; i++) indices[i] = i;

		mesh.Clear();
		if(vertices.Count < 1) return;
		mesh.SetVertices(vertices);
		mesh.SetIndices(indices, MeshTopology.Triangles, 0);
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();
	}

	public static void FromSolid(this Mesh mesh, Solid solid) {
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
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
			polygons.Add(new Polygon(vertices, p.userData));
		}
		return Solid.FromPolygons(polygons);
	}

	public static DMesh3 ToDMesh3(this Mesh mesh) {
		DMesh3 result = new DMesh3();

		var indices = mesh.GetIndices(0);
		var verts = mesh.vertices;

		for(int i = 0; i < verts.Length; i++) {
			result.AppendVertex(verts[i]);
		}

		for(int i = 0; i < indices.Length / 3; i++) {
			result.AppendTriangle(
				indices[i * 3 + 0],
				indices[i * 3 + 1],
				indices[i * 3 + 2]
			);
		}
		return result;
	}

}
