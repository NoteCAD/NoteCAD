
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

	public static ExpVector operator+(ExpVector a, ExpVector b) { return new ExpVector(a.x + b.x, a.y + b.y, a.z + b.z); } 
	public static ExpVector operator-(ExpVector a, ExpVector b) { return new ExpVector(a.x - b.x, a.y - b.y, a.z - b.z); } 
	public static ExpVector operator*(ExpVector a, ExpVector b) { return new ExpVector(a.x * b.x, a.y * b.y, a.z * b.z); } 
	public static ExpVector operator/(ExpVector a, ExpVector b) { return new ExpVector(a.x / b.x, a.y / b.y, a.z / b.z); } 

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

	public static Exp Distance(ExpVector a, ExpVector b) {
		return (b - a).Magnitude();
	}

	public Exp Magnitude() {
		return Exp.Sqrt(Exp.Sqr(x) + Exp.Sqr(y) + Exp.Sqr(z));
	}

	public Vector3 Eval() {
		return new Vector3((float)x.Eval(), (float)y.Eval(), (float)z.Eval());
	}

}