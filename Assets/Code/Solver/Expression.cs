using System;

public class Param {
	public string name;
	public double value;

	public Param(string name) {
		this.name = name;
	}
}

public class Exp {

	public enum Op {
		Const,
		Param,
		Add,
		Sub,
		Mul,
		Div,
		Sin,
		Cos,
		ACos,
		ASin,
		Sqrt,
		Sqr,
		Atan2,
		Abs,
		Sign,
		Neg,
		//Pow,
	}

	public static readonly Exp zero = new Exp(0.0);
	public static readonly Exp one  = new Exp(1.0);
	public static readonly Exp mOne = new Exp(-1.0);
	public static readonly Exp two  = new Exp(2.0);

	Op op;

	Exp a;
	Exp b;
	Param param;
	double value;

	Exp() { }

	public Exp(double value) {
		this.value = value;
		this.op = Op.Const;
	}

	public static implicit operator Exp(Param param) {
		Exp result = new Exp();
		result.param = param;
		result.op = Op.Param;
		return result;
	}

	public static implicit operator Exp(double value) {
		if(value == 0.0) return zero;
		if(value == 1.0) return one;
		Exp result = new Exp();
		result.value = value;
		result.op = Op.Const;
		return result;
	}

	Exp(Op op, Exp a, Exp b) {
		this.a = a;
		this.b = b;
		this.op = op;
	}

	static public Exp operator+(Exp a, Exp b) { return new Exp(Op.Add, a, b); }
	static public Exp operator-(Exp a, Exp b) { return new Exp(Op.Sub, a, b); }
	static public Exp operator*(Exp a, Exp b) { return new Exp(Op.Mul, a, b); }
	static public Exp operator/(Exp a, Exp b) { return new Exp(Op.Div, a, b); }
	//static public Exp operator^(Exp a, Exp b) { return new Exp(Op.Pow, a, b); }

	static public Exp operator-(Exp a) { return new Exp(Op.Neg, a, null); }

	static public Exp Sin  (Exp x) { return new Exp(Op.Sin,   x, null); }
	static public Exp Cos  (Exp x) { return new Exp(Op.Cos,   x, null); }
	static public Exp ACos (Exp x) { return new Exp(Op.ACos,  x, null); }
	static public Exp ASin (Exp x) { return new Exp(Op.ASin,  x, null); }
	static public Exp Sqrt (Exp x) { return new Exp(Op.Sqrt,  x, null); }
	static public Exp Sqr  (Exp x) { return new Exp(Op.Sqr,   x, null); }
	static public Exp Abs  (Exp x) { return new Exp(Op.Abs,   x, null); }
	static public Exp Sign (Exp x) { return new Exp(Op.Sign,  x, null); }
	static public Exp Atan2(Exp x, Exp y) { return new Exp(Op.Atan2, x, y); }
	static public Exp Pow  (Exp x, Exp y) { return new Exp(Op.Sign,  x, y); }

	public double Eval() {
		switch(op) {
			case Op.Const:	return value;
			case Op.Param:	return param.value;
			case Op.Add:	return a.Eval() + b.Eval();
			case Op.Sub:    return a.Eval() - b.Eval();
			case Op.Mul:    return a.Eval() * b.Eval();
			case Op.Div:    return a.Eval() / b.Eval();
			case Op.Sin:    return Math.Sin(a.Eval());
			case Op.Cos:    return Math.Cos(a.Eval());
			case Op.ACos:	return Math.Acos(a.Eval());
			case Op.ASin:	return Math.Asin(a.Eval());
			case Op.Sqrt:	return Math.Sqrt(a.Eval());
			case Op.Sqr:    {  double av = a.Eval(); return av * av; }
			case Op.Atan2:	return Math.Atan2(a.Eval(), b.Eval());
			case Op.Abs:	return Math.Abs(a.Eval());
			case Op.Sign:	return Math.Sign(a.Eval());
			case Op.Neg:	return -a.Eval();
			//case Op.Pow:    return Math.Pow(a.Eval(), b.Eval());
		}
		return 0.0;
	}

	public bool IsUnary() {
		switch(op) {
			case Op.Const:
			case Op.Param:
			case Op.Sin:
			case Op.Cos:
			case Op.ACos:
			case Op.ASin:
			case Op.Sqrt:
			case Op.Sqr:
			case Op.Abs:
			case Op.Sign:
			case Op.Neg:
				return true;
		}
		return false;
	}

	string Quoted() {
		if(IsUnary()) return Print();
		return "(" + Print() + ")";
	}

	public string Print() {
		switch(op) {
			case Op.Const:	return value.ToString();
			case Op.Param:	return param.name;
			case Op.Add:	return a.Print() + " + " + b.Print();
			case Op.Sub:	return a.Print() + " - " + b.Quoted();
			case Op.Mul:    return a.Quoted() + " * " + b.Quoted();
			case Op.Div:    return a.Quoted() + " / " + b.Quoted();
			case Op.Sin:    return "sin(" + a.Print() + ")";
			case Op.Cos:    return "cos(" + a.Print() + ")";
			case Op.ASin:	return "asin(" + a.Print() + ")";
			case Op.ACos:	return "acos(" + a.Print() + ")";
			case Op.Sqrt:	return "sqrt(" + a.Print() + ")";
			case Op.Sqr:	return a.Quoted() + " ^ 2";
			case Op.Abs:	return "abs(" + a.Print() + ")";
			case Op.Sign:	return "sign(" + a.Print() + ")";
			case Op.Atan2:	return "atan2(" + a.Print() + ", " + b.Print() + ")";
			case Op.Neg:	return "-" + a.Quoted();
			//case Op.Pow:	return Quoted(a) + " ^ " + Quoted(b);
		}
		return "";
	}

	public Exp Deriv(Param p) {
		return d(p);
	}

	Exp d(Param p) {
		switch(op) {
			case Op.Const:	return zero;
			case Op.Param:	return (param == p) ? one : zero;
			case Op.Add:	return a.d(p) + b.d(p);
			case Op.Sub:	return a.d(p) - b.d(p);
			case Op.Mul:    return a.d(p) * b + a * b.d(p);
			case Op.Div:    return (a.d(p) * b - a * b.d(p)) / Sqr(b);
			case Op.Sin:    return a.d(p) * Cos(a);
			case Op.Cos:    return a.d(p) * -Sin(a);
			case Op.ASin:	return a.d(p) * one / Sqrt(one - Sqr(a));
			case Op.ACos:	return a.d(p) * mOne / Sqrt(one - Sqr(a));
			case Op.Sqrt:	return a.d(p) * one / (two * Sqrt(a));
			case Op.Sqr:	return a.d(p) * two * a;
			case Op.Abs:	return a.d(p) * Sign(a);
			case Op.Sign:	return zero;
			case Op.Atan2:	return (a * b.d(p) - b * a.d(p)) / (Sqr(a) + Sqr(b));
		}
		return zero;
	}
}