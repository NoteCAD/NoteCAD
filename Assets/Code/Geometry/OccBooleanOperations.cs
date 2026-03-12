#if !UNITY_WEBGL
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Csg;
using g3;
using UnityEngine;

namespace NoteCAD.Geometry {

	/// <summary>
	/// Boolean operations backed by the native OpenCASCADE (OCCT) library.
	/// Only available on native binary builds (Windows, macOS, Linux).
	/// Automatically falls back to the managed CSG library when the native
	/// plugin cannot be loaded or an operation fails.
	/// </summary>
	public class OccBooleanOperations : IBooleanOperations {

		// ------------------------------------------------------------------ //
		// Native DLL name – the compiled plugin must be named NativeOCC
		// (Unity resolves to NativeOCC.dll / libNativeOCC.so / NativeOCC.bundle)
		// ------------------------------------------------------------------ //
		private const string DllName = "NativeOCC";

		// ------------------------------------------------------------------ //
		// P/Invoke declarations – see Plugins/NativeOCC/OccWrapper.h for the
		// matching C API exported by the native plugin.
		// ------------------------------------------------------------------ //

		/// <summary>
		/// Performs a Boolean operation on two triangle meshes using OCCT.
		/// </summary>
		/// <param name="operation">0 = Union, 1 = Difference, 2 = Intersection</param>
		/// <param name="aVertices">Flat float array: x0,y0,z0, x1,y1,z1, …</param>
		/// <param name="aVertexCount">Number of vertices in mesh A</param>
		/// <param name="aTriangles">Flat int array: i0,i1,i2, i3,i4,i5, …</param>
		/// <param name="aTriangleCount">Number of triangles in mesh A</param>
		/// <param name="bVertices">Same layout for mesh B</param>
		/// <param name="bVertexCount">Number of vertices in mesh B</param>
		/// <param name="bTriangles">Flat int array for mesh B</param>
		/// <param name="bTriangleCount">Number of triangles in mesh B</param>
		/// <param name="outVertices">Native pointer to the result vertex data (caller must free)</param>
		/// <param name="outVertexCount">Number of vertices in the result mesh</param>
		/// <param name="outTriangles">Native pointer to the result triangle data (caller must free)</param>
		/// <param name="outTriangleCount">Number of triangles in the result mesh</param>
		/// <returns>0 on success, non-zero on failure</returns>
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		private static extern int OCC_BooleanOp(
			int operation,
			[In] float[] aVertices, int aVertexCount,
			[In] int[] aTriangles, int aTriangleCount,
			[In] float[] bVertices, int bVertexCount,
			[In] int[] bTriangles, int bTriangleCount,
			out IntPtr outVertices, out int outVertexCount,
			out IntPtr outTriangles, out int outTriangleCount
		);

		/// <summary>Releases memory allocated by the native plugin.</summary>
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		private static extern void OCC_FreeMemory(IntPtr ptr);

		/// <summary>
		/// Returns the OCCT version string (e.g. "7.7.0").
		/// Can be used to verify that the plugin loaded correctly.
		/// </summary>
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr OCC_GetVersion();

		// ------------------------------------------------------------------ //
		// Availability check
		// ------------------------------------------------------------------ //

		private static bool? _available;
		private static readonly CsgBooleanOperations _fallback = new CsgBooleanOperations();

		/// <summary>
		/// Returns true when the native NativeOCC plugin is present and functional.
		/// The result is cached after the first check.
		/// </summary>
		public static bool IsAvailable() {
			if(_available.HasValue) return _available.Value;
			try {
				OCC_GetVersion();
				_available = true;
			} catch(Exception ex) {
				Debug.LogWarning($"[NativeOCC] Plugin not available: {ex.Message}. " +
				                 "Boolean operations will use the managed CSG fallback.");
				_available = false;
			}
			return _available.Value;
		}

		// ------------------------------------------------------------------ //
		// IBooleanOperations implementation
		// ------------------------------------------------------------------ //

		public Solid Union(Solid a, Solid b) => Perform(BoolOp.Union, a, b);
		public Solid Difference(Solid a, Solid b) => Perform(BoolOp.Difference, a, b);
		public Solid Intersection(Solid a, Solid b) => Perform(BoolOp.Intersection, a, b);

		/// <summary>
		/// Assembly merges two solids without boolean intersection.
		/// This is handled by the managed CSG library on all platforms because
		/// OCCT does not have a direct equivalent.
		/// </summary>
		public Solid Assembly(Solid a, Solid b) => _fallback.Assembly(a, b);

		// ------------------------------------------------------------------ //
		// Internal helpers
		// ------------------------------------------------------------------ //

		/// Values must stay in sync with the OCC_OP_* constants in OccWrapper.h.
		private enum BoolOp { Union = 0, Difference = 1, Intersection = 2 }

		private static Solid Perform(BoolOp op, Solid a, Solid b) {
			if(!IsAvailable()) {
				switch(op) {
					case BoolOp.Union:        return _fallback.Union(a, b);
					case BoolOp.Difference:   return _fallback.Difference(a, b);
					default:                  return _fallback.Intersection(a, b);
				}
			}

			try {
				var (aVerts, aTris) = ExtractMesh(a);
				var (bVerts, bTris) = ExtractMesh(b);

				int res = OCC_BooleanOp(
					(int)op,
					aVerts, aVerts.Length / 3,
					aTris,  aTris.Length  / 3,
					bVerts, bVerts.Length / 3,
					bTris,  bTris.Length  / 3,
					out IntPtr outVerts, out int outVertCount,
					out IntPtr outTris,  out int outTriCount
				);

				if(res != 0) {
					Debug.LogWarning($"[NativeOCC] {op} returned error code {res}, using CSG fallback.");
					return FallbackOp(op, a, b);
				}

				// Copy result buffers back to managed memory before freeing.
				var vertices  = new float[outVertCount * 3];
				var triangles = new int[outTriCount  * 3];
				Marshal.Copy(outVerts, vertices,  0, vertices.Length);
				Marshal.Copy(outTris,  triangles, 0, triangles.Length);
				OCC_FreeMemory(outVerts);
				OCC_FreeMemory(outTris);

				return BuildSolid(vertices, triangles);
			} catch(Exception ex) {
				Debug.LogWarning($"[NativeOCC] {op} threw an exception: {ex.Message}. Using CSG fallback.");
				return FallbackOp(op, a, b);
			}
		}

		private static Solid FallbackOp(BoolOp op, Solid a, Solid b) {
			switch(op) {
				case BoolOp.Union:      return _fallback.Union(a, b);
				case BoolOp.Difference: return _fallback.Difference(a, b);
				default:                return _fallback.Intersection(a, b);
			}
		}

		/// <summary>
		/// Converts a <see cref="Csg.Solid"/> into flat vertex and triangle index arrays
		/// suitable for passing to the native plugin.
		/// </summary>
		private static (float[] vertices, int[] triangles) ExtractMesh(Solid solid) {
			var verts = new List<float>();
			var tris  = new List<int>();

			foreach(var polygon in solid.Polygons) {
				if(polygon.Vertices.Count < 3) continue;

				// Fan-triangulate the polygon.
				int baseIdx = verts.Count / 3;
				foreach(var v in polygon.Vertices) {
					verts.Add((float)v.Pos.X);
					verts.Add((float)v.Pos.Y);
					verts.Add((float)v.Pos.Z);
				}
				for(int i = 1; i < polygon.Vertices.Count - 1; i++) {
					tris.Add(baseIdx);
					tris.Add(baseIdx + i);
					tris.Add(baseIdx + i + 1);
				}
			}

			return (verts.ToArray(), tris.ToArray());
		}

		/// <summary>
		/// Builds a <see cref="Csg.Solid"/> from flat vertex and triangle index arrays
		/// returned by the native plugin.
		/// </summary>
		private static Solid BuildSolid(float[] vertices, int[] triangles) {
			var polygons = new List<Polygon>();
			for(int i = 0; i < triangles.Length / 3; i++) {
				var polyVerts = new List<Vertex>();
				for(int j = 0; j < 3; j++) {
					int idx = triangles[i * 3 + j];
					polyVerts.Add(new Vertex(new Vector3D(
						vertices[idx * 3 + 0],
						vertices[idx * 3 + 1],
						vertices[idx * 3 + 2]
					)));
				}
				polygons.Add(new Polygon(polyVerts, null));
			}
			return Solid.FromPolygons(polygons);
		}
	}

}
#endif
