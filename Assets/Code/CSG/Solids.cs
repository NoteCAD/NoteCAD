using System;
using System.Collections.Generic;
using System.Linq;

namespace Csg
{
	public static class Solids
	{
		public static Solid Cube(CubeOptions options)
		{
			var c = options.Center;
			var r = options.Radius.Abs; // negative radii make no sense
			if (r.X == 0.0 || r.Y == 0.0 || r.Z == 0.0)
				return new Solid();
			var result = Solid.FromPolygons(cubeData.Select(info =>
			{
				//var normal = new Vector3D(info[1]);
				//var plane = new Plane(normal, 1);
				var vertices = info[0].Select(i =>
				{
					var pos = new Vector3D(
						c.X + r.X * (2 * ((i & 1) != 0 ? 1 : 0) - 1),
							c.Y + r.Y * (2 * ((i & 2) != 0 ? 1 : 0) - 1),
							c.Z + r.Z * (2 * ((i & 4) != 0 ? 1 : 0) - 1));
					return new Vertex(pos);
				});
				return new Polygon(vertices.ToList());
			}).ToList());
			return result;
		}

		public static Solid Cube(double size = 1, bool center = false)
		{
			var r = new Vector3D(size / 2, size / 2, size / 2);
			var c = center ? new Vector3D(0, 0, 0) : r;
			return Solids.Cube(new CubeOptions { Radius = r, Center = c });
		}

		public static Solid Cube(double size, Vector3D center)
		{
			var r = new Vector3D(size / 2, size / 2, size / 2);
			var c = center;
			return Solids.Cube(new CubeOptions { Radius = r, Center = c });
		}

		public static Solid Cube(Vector3D size, bool center = false)
		{
			var r = size / 2;
			var c = center ? new Vector3D(0, 0, 0) : r;
			return Solids.Cube(new CubeOptions { Radius = r, Center = c });
		}

		public static Solid Cube(Vector3D size, Vector3D center)
		{
			var r = size / 2;
			var c = center;
			return Solids.Cube(new CubeOptions { Radius = r, Center = c });
		}

		public static Solid Cube(double width, double height, double depth, bool center = false)
		{
			var r = new Vector3D(width/2, height/2, depth/2);
			var c = center ? new Vector3D(0, 0, 0) : r;
			return Solids.Cube(new CubeOptions { Radius = r, Center = c });
		}

		public static Solid Sphere(SphereOptions options)
		{
			var center = options.Center;
			var radius = Math.Abs(options.Radius);
			if (radius == 0.0)
				return new Solid();
			var resolution = options.Resolution;
			var xvector = options.XAxis * radius;
			var yvector = options.YAxis * radius;
			var zvector = options.ZAxis * radius;
			if (resolution < 4) resolution = 4;
			var qresolution = resolution / 4;
			var prevcylinderpoint = new Vector3D(0,0,0);
			var polygons = new List<Polygon>();
			for (var slice1 = 0; slice1 <= resolution; slice1++)
			{
				var angle = Math.PI * 2.0 * slice1 / resolution;
				var cylinderpoint = xvector * (Math.Cos(angle)) + (yvector * (Math.Sin(angle)));
				if (slice1 > 0)
				{
					double prevcospitch = 0, prevsinpitch = 0;
					for (var slice2 = 0; slice2 <= qresolution; slice2++)
					{
						var pitch = 0.5 * Math.PI * (double)slice2 / qresolution;
						var cospitch = Math.Cos(pitch);
						var sinpitch = Math.Sin(pitch);
						if (slice2 > 0)
						{
							var vertices = new List<Vertex>();
							vertices.Add(new Vertex(center + (prevcylinderpoint * (prevcospitch) - (zvector * (prevsinpitch)))));
							vertices.Add(new Vertex(center + (cylinderpoint * (prevcospitch) - (zvector * (prevsinpitch)))));
							if (slice2 < qresolution)
							{
								vertices.Add(new Vertex(center + (cylinderpoint * (cospitch) - (zvector * (sinpitch)))));
							}
							vertices.Add(new Vertex(center + (prevcylinderpoint * (cospitch) - (zvector * (sinpitch)))));
							polygons.Add(new Polygon(vertices));
							vertices = new List<Vertex>();
							vertices.Add(new Vertex(center + (prevcylinderpoint * (prevcospitch) + (zvector * (prevsinpitch)))));
							vertices.Add(new Vertex(center + (cylinderpoint * (prevcospitch) + (zvector * (prevsinpitch)))));
							if (slice2 < qresolution)
							{
								vertices.Add(new Vertex(center + (cylinderpoint * (cospitch) + (zvector * (sinpitch)))));
							}
							vertices.Add(new Vertex(center + (prevcylinderpoint * (cospitch) + (zvector * (sinpitch)))));
							vertices.Reverse();
							polygons.Add(new Polygon(vertices));
						}
						prevcospitch = cospitch;
						prevsinpitch = sinpitch;
					}
				}
				prevcylinderpoint = cylinderpoint;
			}
			var result = Solid.FromPolygons(polygons);
			return result;
		}

		public static Solid Sphere(double r = 1, bool center = true)
		{
			var c = center ? new Vector3D(0, 0, 0) : new Vector3D(r, r, r);
			return Solids.Sphere(new SphereOptions { Radius = r, Center = c });
		}

		public static Solid Sphere(double r, Vector3D center)
		{
			return Solids.Sphere(new SphereOptions { Radius = r, Center = center });
		}

		public static Solid Cylinder(CylinderOptions options)
		{
			var s = options.Start;
			var e = options.End;
			var r = Math.Abs(options.RadiusStart);
			var rEnd = Math.Abs(options.RadiusEnd);
			var rStart = r;
			var alpha = options.SectorAngle;
			alpha = alpha > 360 ? alpha % 360 : alpha;

			if ((rEnd == 0) && (rStart == 0))
			{
				return new Solid();
			}
			if (s.Equals(e))
			{
				return new Solid();
			}

			var slices = options.Resolution;
			var ray = e - (s);
			var axisZ = ray.Unit;
			var axisX = axisZ.RandomNonParallelVector().Unit;

			var axisY = axisX.Cross(axisZ).Unit;
			axisX = axisZ.Cross(axisY).Unit;
			var start = new Vertex(s);
			var end = new Vertex(e);
			var polygons = new List<Polygon>();

			Func<double, double, double, Vertex> point = (stack, slice, radius) =>
			{
				var angle = slice * Math.PI * alpha / 180;
				var outp = axisX * (Math.Cos(angle)) + (axisY * (Math.Sin(angle)));
				var pos = s + (ray * (stack)) + (outp * (radius));
				return new Vertex(pos);
			};

			if (alpha > 0)
			{
				for (var i = 0; i < slices; i++)
				{
					double t0 = (double)i / slices;
					double t1 = (double)(i + 1) / slices;
					if (rEnd == rStart)
					{
						polygons.Add(new Polygon(start, point(0, t0, rEnd), point(0, t1, rEnd)));
						polygons.Add(new Polygon(point(0, t1, rEnd), point(0, t0, rEnd), point(1, t0, rEnd), point(1, t1, rEnd)));
						polygons.Add(new Polygon(end, point(1, t1, rEnd), point(1, t0, rEnd)));
					}
					else {
						if (rStart > 0)
						{
							polygons.Add(new Polygon(start, point(0, t0, rStart), point(0, t1, rStart)));
							polygons.Add(new Polygon(point(0, t0, rStart), point(1, t0, rEnd), point(0, t1, rStart)));
						}
						if (rEnd > 0)
						{
							polygons.Add(new Polygon(end, point(1, t1, rEnd), point(1, t0, rEnd)));
							polygons.Add(new Polygon(point(1, t0, rEnd), point(1, t1, rEnd), point(0, t1, rStart)));
						}
					}
				}
	            if (alpha < 360) {
	                polygons.Add(new Polygon(start, end, point(0, 0, rStart)));
	                polygons.Add(new Polygon(point(0, 0, rStart), end, point(1, 0, rEnd)));
	                polygons.Add(new Polygon(start, point(0, 1, rStart), end));
	                polygons.Add(new Polygon(point(0, 1, rStart), point(1, 1, rEnd), end));
	            }
			}
			var result = Solid.FromPolygons(polygons);
	        return result;
	    }

		public static Solid Cylinder(double r, double h, bool center = false)
		{
			var start = center ? new Vector3D(0, -h / 2, 0) : new Vector3D(0, 0, 0);
			var end = center ? new Vector3D(0, h / 2, 0) : new Vector3D(0, h, 0);
			return Cylinder(new CylinderOptions { Start = start, End = end, RadiusStart = r, RadiusEnd = r, });
		}

		public static Solid Union(params Solid[] csgs)
		{
			if (csgs.Length == 0)
			{
				return new Solid();
			}
			else if (csgs.Length == 1)
			{
				return csgs[0];
			}
			else
			{
				var head = csgs[0];
				var rest = csgs.Skip(1).ToArray();
				return head.Union(rest);
			}
		}

		public static Solid Difference(params Solid[] csgs)
		{
			if (csgs.Length == 0)
			{
				return new Solid();
			}
			else if (csgs.Length == 1)
			{
				return csgs[0];
			}
			else
			{
				var head = csgs[0];
				var rest = csgs.Skip(1).ToArray();
				return head.Substract(rest);
			}
		}

		public static Solid Intersection(params Solid[] csgs)
		{
			if (csgs.Length == 0 || csgs.Length == 1)
			{
				return new Solid();
			}
			else
			{
				var head = csgs[0];
				var rest = csgs.Skip(1).ToArray();
				return head.Intersect(rest);
			}
		}

		static readonly int[][][] cubeData =
			{
				new[] {
					new[] { 0, 4, 6, 2 },
					new[] { -1, 0, 0 }
				},
				new[] {
					new[] {1, 3, 7, 5},
					new[] {+1, 0, 0}
				},
				new[] {
					new[] {0, 1, 5, 4},
					new[] {0, -1, 0},
				},
				new[] {
					new[] {2, 6, 7, 3},
					new[] { 0, +1, 0}
				},
				new[] {
					new[] {0, 2, 3, 1},
					new[] { 0, 0, -1}
				},
				new[] {
					new[] {4, 5, 7, 6},
					new[] { 0, 0, +1}
				}
			};
	}

	public class CubeOptions
	{
		public Vector3D Center;
		public Vector3D Radius = new Vector3D(1, 1, 1);
	}

	public class SphereOptions
	{
		public Vector3D XAxis = new Vector3D(1, 0, 0);
		public Vector3D YAxis = new Vector3D(0, -1, 0);
		public Vector3D ZAxis = new Vector3D(0, 0, 1);
		public Vector3D Center;
		public double Radius = 1;
		public int Resolution = Solid.DefaultResolution3D;
	}

	public class CylinderOptions
	{
		public Vector3D Start;
		public Vector3D End;
		public double RadiusStart = 1;
		public double RadiusEnd = 1;
		public double SectorAngle = 360;
		public int Resolution = Solid.DefaultResolution3D;
	}
}

