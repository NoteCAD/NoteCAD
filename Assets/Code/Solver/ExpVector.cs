
using System;
using UnityEngine;

public class ExpVector {
	public Exp x;
	public Exp y;
	public Exp z;

	public ExpVector(Exp x, Exp y, Exp z) {
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public static implicit operator ExpVector(Vector3 v) {
		return  new ExpVector(v.x, v.y, v.z);
	}

	public static ExpVector operator+(ExpVector a, ExpVector b) { return new ExpVector(a.x + b.x, a.y + b.y, a.z + b.z); } 
	public static ExpVector operator-(ExpVector a, ExpVector b) { return new ExpVector(a.x - b.x, a.y - b.y, a.z - b.z); } 
	public static ExpVector operator*(ExpVector a, ExpVector b) { return new ExpVector(a.x * b.x, a.y * b.y, a.z * b.z); } 
	public static ExpVector operator/(ExpVector a, ExpVector b) { return new ExpVector(a.x / b.x, a.y / b.y, a.z / b.z); } 

	public static ExpVector operator-(ExpVector b) { return new ExpVector(-b.x, -b.y, -b.z); } 

	public static ExpVector operator*(Exp a, ExpVector b) { return new ExpVector(a * b.x, a * b.y, a * b.z); }
	public static ExpVector operator*(ExpVector a, Exp b) { return new ExpVector(a.x * b, a.y * b, a.z * b); }

	public static ExpVector operator/(Exp a, ExpVector b) { return new ExpVector(a / b.x, a / b.y, a / b.z); }
	public static ExpVector operator/(ExpVector a, Exp b) { return new ExpVector(a.x / b, a.y / b, a.z / b); }

	public static Exp Dot(ExpVector a, ExpVector b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
	public static ExpVector Cross(ExpVector a, ExpVector b) {
		return new ExpVector(
			a.y * b.z - b.y * a.z,
			a.z * b.x - b.z * a.x,
			a.x * b.y - b.x * a.y
		);
	}

	public static Exp PointLineDistance(ExpVector point, ExpVector l0, ExpVector l1) {
		var d = l0 - l1;
		return Cross(d, l0 - point).Magnitude() / d.Magnitude();
	}

	public static float PointLineDistance(Vector3 point, Vector3 l0, Vector3 l1) {
		var d = l0 - l1;
		return Vector3.Cross(d, l0 - point).magnitude / d.magnitude;
	}

	public static ExpVector ProjectPointToLine(ExpVector p, ExpVector l0, ExpVector l1) {
		var d = l1 - l0;
		var t = Dot(d, p - l0) / Dot(d, d);
		return l0 + d * t;
	}

	public static Vector3 ProjectPointToLine(Vector3 p, Vector3 l0, Vector3 l1) {
		var d = l1 - l0;
		var t = Vector3.Dot(d, p - l0) / Vector3.Dot(d, d);
		return l0 + d * t;
	}

	public Exp Magnitude() {
		return Exp.Sqrt(Exp.Sqr(x) + Exp.Sqr(y) + Exp.Sqr(z));
	}

	public ExpVector Normalized() {
		return this / Magnitude();
	}

	public Vector3 Eval() {
		return new Vector3((float)x.Eval(), (float)y.Eval(), (float)z.Eval());
	}

	public bool ValuesEquals(ExpVector o, double eps) {
		return Math.Abs(x.Eval() - o.x.Eval()) < eps &&
			   Math.Abs(y.Eval() - o.y.Eval()) < eps &&
			   Math.Abs(z.Eval() - o.z.Eval()) < eps;
	}

	public static ExpVector RotateAround(ExpVector point, ExpVector axis, ExpVector origin, Exp angle) {
		var a = axis.Normalized();
		var c = Exp.Cos(angle);
		var s = Exp.Sin(angle);
		var u = new ExpVector(c + (1.0 - c) * a.x * a.x, (1.0 - c) * a.y * a.x + s * a.z, (1 - c) * a.z * a.x - s * a.y);
		var v = new ExpVector((1.0 - c) * a.x * a.y - s * a.z, c + (1.0 - c) * a.y * a.y, (1.0 - c) * a.z * a.y + s * a.x);
		var n = new ExpVector((1.0 - c) * a.x * a.z + s * a.y, (1.0 - c) * a.y * a.z - s * a.x,	c + (1 - c) * a.z * a.z);
		var p = point - origin;
		return p.x * u + p.y * v + p.z * n + origin;
	}

	public static Vector3 RotateAround(Vector3 point, Vector3 axis, Vector3 origin, float angle) {
		var a = axis.normalized;
		var c = Mathf.Cos(angle);
		var s = Mathf.Sin(angle);
		var u = new Vector3(c + (1 - c) * a.x * a.x, (1 - c) * a.y * a.x + s * a.z, (1 - c) * a.z * a.x - s * a.y);
		var v = new Vector3((1 - c) * a.x * a.y - s * a.z, c + (1 - c) * a.y * a.y, (1 - c) * a.z * a.y + s * a.x);
		var n = new Vector3((1 - c) * a.x * a.z + s * a.y, (1 - c) * a.y * a.z - s * a.x, c + (1 - c) * a.z * a.z);
		var p = point - origin;
		return p.x * u + p.y * v + p.z * n + origin;
	}

}