using System;
using System.IO;

namespace Csg
{
	public static class Formats
	{
		static readonly IFormatProvider icult = System.Globalization.CultureInfo.InvariantCulture;

		public static string ToStlString(this Solid csg, string name)
		{
			var w = new StringWriter();
			WriteStl(csg, name, w);
			return w.ToString();
		}

		public static void WriteStl(this Solid csg, string name, TextWriter writer)
		{
			writer.Write("solid ");
			writer.WriteLine(name);
			foreach (var p in csg.Polygons)
			{
				WriteStl(p, writer);
			}
			writer.Write("endsolid ");
			writer.WriteLine(name);
		}

		public static void WriteStl(this Polygon polygon, TextWriter writer)
		{
			if (polygon.Vertices.Count >= 3)
			{
				var firstVertexStl = polygon.Vertices[0].ToStlString();
				for (var i = 0; i < polygon.Vertices.Count - 2; i++)
				{
					writer.WriteLine("facet normal " + polygon.Plane.Normal.ToStlString());
					writer.WriteLine("outer loop");
					writer.WriteLine(firstVertexStl);
					writer.WriteLine(polygon.Vertices[i + 1].ToStlString());
					writer.WriteLine(polygon.Vertices[i + 2].ToStlString());
					writer.WriteLine("endloop");
					writer.WriteLine("endfacet");
				}
			}
		}

		public static string ToStlString(this Vector3D vector)
		{
			return string.Format(icult, "{0} {1} {2}", vector.X, vector.Y, vector.Z);
		}

		public static string ToStlString(this Vertex vertex)
		{
			return string.Format(icult, "vertex {0} {1} {2}", vertex.Pos.X, vertex.Pos.Y, vertex.Pos.Z);
		}
	}
}

