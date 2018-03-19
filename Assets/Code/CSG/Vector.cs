using System;

namespace Csg
{
	public struct Vector3D : IEquatable<Vector3D>
	{
		public double X, Y, Z;

		public Vector3D(double x, double y, double z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public bool Equals(Vector3D a)
		{
#pragma warning disable RECS0018 // Comparison of floating point numbers with equality operator
			return X == a.X && Y == a.Y && Z == a.Z;
#pragma warning restore RECS0018 // Comparison of floating point numbers with equality operator
		}

		public double Length
		{
			get { return Math.Sqrt(X * X + Y * Y + Z * Z); }
		}

		public double DistanceToSquared(Vector3D a)
		{
			var dx = X - a.X;
			var dy = Y - a.Y;
			var dz = Z - a.Z;
			return dx * dx + dy * dy + dz * dz;
		}

		public double Dot(Vector3D a)
		{
			return X * a.X + Y * a.Y + Z * a.Z;
		}

		public Vector3D Cross(Vector3D a)
		{
			return new Vector3D(
				Y * a.Z - Z * a.Y,
				Z * a.X - X * a.Z,
				X * a.Y - Y * a.X);
		}

		public Vector3D Unit
		{
			get
			{
				var d = Length;
				return new Vector3D(X / d, Y / d, Z / d);
			}
		}

		public Vector3D Negated
		{
			get
			{
				return new Vector3D(-X, -Y, -Z);
			}
		}

		public Vector3D Abs
		{
			get
			{
				return new Vector3D(Math.Abs(X), Math.Abs(Y), Math.Abs(Z));
			}
		}

		public Vector3D Min(Vector3D other)
		{
			return new Vector3D(Math.Min(X, other.X), Math.Min(Y, other.Y), Math.Min(Z, other.Z));
		}

		public Vector3D Max(Vector3D other)
		{
			return new Vector3D(Math.Max(X, other.X), Math.Max(Y, other.Y), Math.Max(Z, other.Z));
		}

		public static Vector3D operator +(Vector3D a, Vector3D b)
		{
			return new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
		}
		public static Vector3D operator -(Vector3D a, Vector3D b)
		{
			return new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
		}
		public static Vector3D operator *(Vector3D a, Vector3D b)
		{
			return new Vector3D(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
		}
		public static Vector3D operator *(Vector3D a, double b)
		{
			return new Vector3D(a.X * b, a.Y * b, a.Z * b);
		}
		public static Vector3D operator /(Vector3D a, double b)
		{
			return new Vector3D(a.X / b, a.Y / b, a.Z / b);
		}
		public static Vector3D operator *(Vector3D a, Matrix4x4 b)
		{
			return b.LeftMultiply1x3Vector(a);
		}

		public override string ToString()
		{
			return string.Format(System.Globalization.CultureInfo.InvariantCulture, "[{0:0.0}, {1:0.0}, {2:0.0}]", X, Y, Z);
		}

		public Vector3D RandomNonParallelVector()
		{
			var abs = Abs;
			if ((abs.X <= abs.Y) && (abs.X <= abs.Z))
			{
				return new Vector3D(1, 0, 0);
			}
			else if ((abs.Y <= abs.X) && (abs.Y <= abs.Z))
			{
				return new Vector3D(0, 1, 0);
			}
			else {
				return new Vector3D(0, 0, 1);
			}
		}
	}

	public struct Vector2D : IEquatable<Vector2D>
	{
		public double X, Y;

		public Vector2D(double x, double y)
		{
			X = x;
			Y = y;
		}

		public bool Equals(Vector2D a)
		{
#pragma warning disable RECS0018 // Comparison of floating point numbers with equality operator
			return X == a.X && Y == a.Y;
#pragma warning restore RECS0018 // Comparison of floating point numbers with equality operator
		}

		public double Length
		{
			get { return Math.Sqrt(X * X + Y * Y); }
		}

		public double DistanceTo(Vector2D a)
		{
			var dx = X - a.X;
			var dy = Y - a.Y;
			return Math.Sqrt(dx * dx + dy * dy);
		}

		public double Dot(Vector2D a)
		{
			return X * a.X + Y * a.Y;
		}

		public Vector2D Unit
		{
			get
			{
				var d = Length;
				return new Vector2D(X / d, Y / d);
			}
		}

		public Vector2D Negated { get { return new Vector2D(-X, -Y); } }

		public Vector2D Normal { get { return new Vector2D(Y, -X); } }

		public static Vector2D operator +(Vector2D a, Vector2D b)
		{
			return new Vector2D(a.X + b.X, a.Y + b.Y);
		}
		public static Vector2D operator -(Vector2D a, Vector2D b)
		{
			return new Vector2D(a.X - b.X, a.Y - b.Y);
		}
		public static Vector2D operator *(Vector2D a, Vector2D b)
		{
			return new Vector2D(a.X * b.X, a.Y * b.Y);
		}
		public static Vector2D operator *(Vector2D a, double b)
		{
			return new Vector2D(a.X * b, a.Y * b);
		}
		public static Vector2D operator /(Vector2D a, double b)
		{
			return new Vector2D(a.X / b, a.Y / b);
		}

		public override string ToString()
		{
			return string.Format(System.Globalization.CultureInfo.InvariantCulture, "[{0:0.0}, {1:0.0}]", X, Y);
		}
	}

	public class BoundingBox
	{
		public readonly Vector3D Min;
		public readonly Vector3D Max;
		public BoundingBox(Vector3D min, Vector3D max)
		{
			Min = min;
			Max = max;
		}
		public BoundingBox At(Vector3D position, Vector3D size)
		{
			return new BoundingBox(position, position + size);
		}
		public BoundingBox(double dx, double dy, double dz)
		{
			Min = new Vector3D(-dx / 2, -dy / 2, -dz / 2);
			Max = new Vector3D(dx / 2, dy / 2, dz / 2);
		}
		public Vector3D Size { get { return Max - Min; } }
		public Vector3D Center { get {  return (Min + Max) / 2; } }
		public static BoundingBox operator +(BoundingBox a, Vector3D b)
		{
			return new BoundingBox(a.Min + b, a.Max + b);
		}
		public bool Intersects(BoundingBox b)
		{
			if (Max.X < b.Min.X) return false;
			if (Max.Y < b.Min.Y) return false;
			if (Max.Z < b.Min.Z) return false;
			if (Min.X > b.Max.X) return false;
			if (Min.Y > b.Max.Y) return false;
			if (Min.Z > b.Max.Z) return false;
			return true;
		}
		//public override string ToString() => $"{Center}, s={Size}";
	}

	public class BoundingSphere
	{
		public Vector3D Center;
		public double Radius;
	}

	public class OrthoNormalBasis
	{
		public readonly Vector3D U;
		public readonly Vector3D V;
		public readonly Plane Plane;
		public readonly Vector3D PlaneOrigin;
		public OrthoNormalBasis(Plane plane)
		{
			var rightvector = plane.Normal.RandomNonParallelVector();
			V = plane.Normal.Cross(rightvector).Unit;
			U = V.Cross(plane.Normal);
			Plane = plane;
			PlaneOrigin = plane.Normal * plane.W;
		}
		public Vector2D To2D(Vector3D vec3)
		{
			return new Vector2D(vec3.Dot(U), vec3.Dot(V));
		}
		public Vector3D To3D(Vector2D vec2)
		{
			return PlaneOrigin + U * vec2.X + V * vec2.Y;
		}
	}

	public class Line2D
	{
		readonly Vector2D normal;
		//readonly double w;
		public Line2D(Vector2D normal, double w)
		{
			var l = normal.Length;
			w *= l;
			normal = normal * (1.0 / l);
			this.normal = normal;
			//this.w = w;
		}
		public Vector2D Direction { get { return normal.Normal; } }
		public static Line2D FromPoints(Vector2D p1, Vector2D p2)
		{
			var direction = p2 - (p1);
			var normal = direction.Normal.Negated.Unit;
			var w = p1.Dot(normal);
			return new Line2D(normal, w);
		}
	}

	public class Matrix4x4
	{
		readonly double[] elements;

		public bool IsMirroring = false;

		public Matrix4x4(double[] els)
		{
			elements = els;
		}

		public Matrix4x4()
			: this(new double[16])
		{
		}

		public double[] Elements { get { return elements; } }

		public static Matrix4x4 Scaling(Vector3D vec)
		{
			var els = new[] {
				vec.X, 0, 0, 0, 0, vec.Y, 0, 0, 0, 0, vec.Z, 0, 0, 0, 0, 1
			};
			return new Matrix4x4(els);
		}

		public static Matrix4x4 Translation(Vector3D vec)
		{
			var els = new[] {
				1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, vec.X, vec.Y, vec.Z, 1
			};
			return new Matrix4x4(els);
		}

		public static Matrix4x4 RotationX(double degrees)
		{
			var radians = degrees * Math.PI * (1.0 / 180.0);
			var cos = Math.Cos(radians);
			var sin = Math.Sin(radians);
			var els = new double[] {
				1, 0, 0, 0, 0, cos, sin, 0, 0, -sin, cos, 0, 0, 0, 0, 1
			};
			return new Matrix4x4(els);
		}

		public static Matrix4x4 RotationY(double degrees)
		{
			var radians = degrees * Math.PI * (1.0 / 180.0);
			var cos = Math.Cos(radians);
			var sin = Math.Sin(radians);
			var els = new double[] {
				cos, 0, -sin, 0, 0, 1, 0, 0, sin, 0, cos, 0, 0, 0, 0, 1
			};
			return new Matrix4x4(els);
		}

		public static Matrix4x4 RotationZ(double degrees)
		{
			var radians = degrees * Math.PI * (1.0 / 180.0);
			var cos = Math.Cos(radians);
			var sin = Math.Sin(radians);
			var els = new double[] {
				cos, sin, 0, 0, -sin, cos, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1
			};
			return new Matrix4x4(els);
		}

		public Vector3D LeftMultiply1x3Vector(Vector3D v)
		{
			var v0 = v.X;
			var v1 = v.Y;
			var v2 = v.Z;
			var v3 = 1;
			var x = v0 * this.elements[0] + v1 * this.elements[4] + v2 * this.elements[8] + v3 * this.elements[12];
			var y = v0 * this.elements[1] + v1 * this.elements[5] + v2 * this.elements[9] + v3 * this.elements[13];
			var z = v0 * this.elements[2] + v1 * this.elements[6] + v2 * this.elements[10] + v3 * this.elements[14];
			var w = v0 * this.elements[3] + v1 * this.elements[7] + v2 * this.elements[11] + v3 * this.elements[15];
			// scale such that fourth element becomes 1:
			if (w != 1)
			{
				var invw = 1.0 / w;
				x *= invw;
				y *= invw;
				z *= invw;
			}
			return new Vector3D(x, y, z);
		}

		public static Matrix4x4 operator * (Matrix4x4 l, Matrix4x4 m)
		{
			// cache elements in local variables, for speedup:
			var this0  = l.elements[0];
			var this1  = l.elements[1];
			var this2  = l.elements[2];
			var this3  = l.elements[3];
			var this4  = l.elements[4];
			var this5  = l.elements[5];
			var this6  = l.elements[6];
			var this7  = l.elements[7];
			var this8  = l.elements[8];
			var this9  = l.elements[9];
			var this10 = l.elements[10];
			var this11 = l.elements[11];
			var this12 = l.elements[12];
			var this13 = l.elements[13];
			var this14 = l.elements[14];
			var this15 = l.elements[15];
			var m0 = m.elements[0];
			var m1 = m.elements[1];
			var m2 = m.elements[2];
			var m3 = m.elements[3];
			var m4 = m.elements[4];
			var m5 = m.elements[5];
			var m6 = m.elements[6];
			var m7 = m.elements[7];
			var m8 = m.elements[8];
			var m9 = m.elements[9];
			var m10 = m.elements[10];
			var m11 = m.elements[11];
			var m12 = m.elements[12];
			var m13 = m.elements[13];
			var m14 = m.elements[14];
			var m15 = m.elements[15];

			var result = new double[16];
			result[0] = this0 * m0 + this1 * m4 + this2 * m8 + this3 * m12;
			result[1] = this0 * m1 + this1 * m5 + this2 * m9 + this3 * m13;
			result[2] = this0 * m2 + this1 * m6 + this2 * m10 + this3 * m14;
			result[3] = this0 * m3 + this1 * m7 + this2 * m11 + this3 * m15;
			result[4] = this4 * m0 + this5 * m4 + this6 * m8 + this7 * m12;
			result[5] = this4 * m1 + this5 * m5 + this6 * m9 + this7 * m13;
			result[6] = this4 * m2 + this5 * m6 + this6 * m10 + this7 * m14;
			result[7] = this4 * m3 + this5 * m7 + this6 * m11 + this7 * m15;
			result[8] = this8 * m0 + this9 * m4 + this10 * m8 + this11 * m12;
			result[9] = this8 * m1 + this9 * m5 + this10 * m9 + this11 * m13;
			result[10] = this8 * m2 + this9 * m6 + this10 * m10 + this11 * m14;
			result[11] = this8 * m3 + this9 * m7 + this10 * m11 + this11 * m15;
			result[12] = this12 * m0 + this13 * m4 + this14 * m8 + this15 * m12;
			result[13] = this12 * m1 + this13 * m5 + this14 * m9 + this15 * m13;
			result[14] = this12 * m2 + this13 * m6 + this14 * m10 + this15 * m14;
			result[15] = this12 * m3 + this13 * m7 + this14 * m11 + this15 * m15;
			return new Matrix4x4(result);
		}
	}
}

