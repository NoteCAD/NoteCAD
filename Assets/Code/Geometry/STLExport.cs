using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class STLExport {
	public static string ExportSTL(this Mesh mesh) {
		var builder = new StringBuilder();
		builder.Append(string.Format("solid {0}\n", mesh.name));
		for(int i = 0; i < mesh.subMeshCount; i++) {
			var indices = mesh.GetIndices(i);
			var vertices = mesh.vertices;
			for(int j = 0; j < indices.Length / 3; j++) {
				var v = new Vector3[] {
					vertices[indices[j * 3 + 0]],
					vertices[indices[j * 3 + 1]],
					vertices[indices[j * 3 + 2]]
				};
				builder.Append(string.Format(" facet normal {0} {1} {2}\n", 0f, 0f, 0f));
				builder.Append("  outer loop\n");
				for(int k = 0; k < 3; k++) {
					builder.Append(string.Format("   vertex {0} {1} {2}\n", v[k].x, v[k].y, v[k].z));
				}
				builder.Append("  endloop\n");
				builder.Append(" endfacet\n");
			}
		}
		builder.Append(string.Format("endsolid {0}\n", mesh.name));
		return builder.ToString();
	}

	public static string ExportOBJ(this Mesh mesh) {
		var builder = new StringBuilder();
		for(int i = 0; i < mesh.subMeshCount; i++) {
			var indices = mesh.GetIndices(i);
			var vertices = mesh.vertices;
			for(int j = 0; j < vertices.Length; j++) {
				var v = vertices[j];
				builder.Append(string.Format("v {0} {1} {2}\n", v.x.ToStr(), v.y.ToStr(), v.z.ToStr()));
			}

			var normals = mesh.normals;
			for(int j = 0; j < normals.Length; j++) {
				var n = normals[j];
				builder.Append(string.Format("vn {0} {1} {2}\n", n.x.ToStr(), n.y.ToStr(), n.z.ToStr()));
			}

			for(int j = 0; j < indices.Length / 3; j++) {
				builder.Append("f ");
				for(int k = 0; k < 3; k++) {
					if(k > 0) builder.Append(" ");
					builder.Append(string.Format("{0}//{0}", indices[j * 3 + k] + 1));
				}
				builder.Append("\n");
			}
		}
		return builder.ToString();
	}
}
