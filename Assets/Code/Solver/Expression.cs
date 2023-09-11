using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Param {
	public string name;
	[NonSerialized] public bool reduceable = true;
	private double v;
	[NonSerialized] public bool changed;
	//public bool solvable = true;

	public double value {
		get { return v; }
		set {
			if(v == value) return;
			changed = true;
			v = value;
		}
	}

	public Exp exp { get; private set; }

	public Param(string name, bool reduceable = true) {
		this.name = name;
		this.reduceable = reduceable;
		exp = new Exp(this);
	}

	public Param(string name, double value) {
		this.name = name;
		this.value = value;
		exp = new Exp(this);
	}

}

public abstract class CustomFunction {

	static Dictionary<Exp.Op, CustomFunction> customs = new Dictionary<Exp.Op, CustomFunction>();
	static Dictionary<string, CustomFunction> functionNames = new Dictionary<string, CustomFunction>();
	public static Dictionary<string, CustomFunction> operatorNames { get; private set; }

	public static void Add(CustomFunction f) {
		if(f.IsFunction) {
			customs.Add(f.Op, f);
			functionNames.Add(f.Name, f);
		} else {
			customs.Add(f.Op, f);
			operatorNames.Add(f.Name, f);
		}
	}

	public static CustomFunction Get(Exp.Op op) {
		if(!customs.ContainsKey(op)) return null;
		return customs[op];
	}

	public static CustomFunction GetFunction(string name) {
		if(!functionNames.ContainsKey(name)) return null;
		return functionNames[name];
	}

	public static CustomFunction GetOperator(string name) {
		if(!operatorNames.ContainsKey(name)) return null;
		return operatorNames[name];
	}

	public abstract double Eval(Exp exp);
	public abstract bool EvalBool(Exp exp);
	public abstract Exp Deriv(Exp exp, Param p);
	public abstract string AsString(Exp exp);
	public abstract int ArgCount { get; }
	public abstract int Order { get; }
	public abstract bool IsFunction { get; }
	public abstract string Name { get; }
	public abstract Exp.Op Op { get; }

	static CustomFunction() {
		operatorNames = new Dictionary<string, CustomFunction>();
		Add(new CustomFunctions.And());
		Add(new CustomFunctions.Or());
		Add(new CustomFunctions.BoolNot());
		Add(new CustomFunctions.Ceil());
		Add(new CustomFunctions.Floor());
		Add(new CustomFunctions.Round());
		Add(new CustomFunctions.RoundUp());
		Add(new CustomFunctions.RoundDown());
	}
}

public class Exp {

	public enum Op {
		Undefined,
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
		Pos,
		Drag,
		Exp,
		Sinh,
		Cosh,
		SFres,
		CFres,
		Equal,
		LEqual,
		GEqual,
		Greater,
		Less,
		Norm,

		// Not Solvable
		If,
		And,
		Or,
		Not,
		Round,
		RoundUp,
		RoundDown,
		Ceil,
		Floor,

		//Pow,
	}

	public static readonly Exp zero = new Exp(0.0);
	public static readonly Exp one  = new Exp(1.0);
	public static readonly Exp mOne = new Exp(-1.0);
	public static readonly Exp two  = new Exp(2.0);

	public Op op;

	public Exp a;
	public Exp b;
	public Exp c;

	public Param param;
	public double value;

	Exp() { }

	public Exp(double value) {
		this.value = value;
		this.op = Op.Const;
	}

	internal Exp(Param p) {
		this.param = p;
		this.op = Op.Param;
	}

	public static implicit operator Exp(Param param) {
		return param.exp;
	}

	public static implicit operator Exp(double value) {
		if(value == 0.0) return zero;
		if(value == 1.0) return one;
		Exp result = new Exp();
		result.value = value;
		result.op = Op.Const;
		return result;
	}

	public Exp(Op op, Exp a, Exp b) {
		this.a = a;
		this.b = b;
		this.op = op;
	}

	public Exp(Op op, Exp a, Exp b, Exp c) {
		this.a = a;
		this.b = b;
		this.c = c;
		this.op = op;
	}

	static public Exp operator+(Exp a, Exp b) {
		if(a.IsZeroConst()) return b;
		if(b.IsZeroConst()) return a;
		if(b.op == Op.Neg) return a - b.a;
		if(b.op == Op.Pos) return a + b.a;
		return new Exp(Op.Add, a, b);
	}

	static public Exp operator-(Exp a, Exp b) {
		if(a.IsZeroConst()) return -b;
		if(b.IsZeroConst()) return a;
		return new Exp(Op.Sub, a, b);
	}

	static public Exp operator*(Exp a, Exp b) {
		if(a.IsZeroConst()) return zero;
		if(b.IsZeroConst()) return zero;
		if(a.IsOneConst()) return b;
		if(b.IsOneConst()) return a;
		if(a.IsMinusOneConst()) return -b;
		if(b.IsMinusOneConst()) return -a;
		if(a.IsConst() && b.IsConst()) return a.value * b.value;
		return new Exp(Op.Mul, a, b);
	}

	static public Exp operator/(Exp a, Exp b) {
		if(b.IsOneConst()) return a;
		if(a.IsZeroConst()) return zero;
		if(b.IsMinusOneConst()) return -a;
		return new Exp(Op.Div, a, b);
	}
	//static public Exp operator^(Exp a, Exp b) { return new Exp(Op.Pow, a, b); }

	static public Exp operator-(Exp a) {
		if(a.IsZeroConst()) return a;
		if(a.IsConst()) return -a.value;
		if(a.op == Op.Neg) return a.a;
		return new Exp(Op.Neg, a, null);
	}

	// https://www.hindawi.com/journals/mpe/2018/4031793/
	public static double CFres(double x) {
		
		var PI = Math.PI;
		var ax = Math.Abs(x);
		var ax2 = ax * ax;
		var ax3 = ax2 * ax;
		var x3 = x * x * x;
		/*
		return (
			-Math.Sin(PI * ax2 / 2.0) / 
			(PI * (x + 20.0 * PI * Math.Exp(-200.0 * PI * Math.Sqrt(ax))))

			+ 8.0 / 25.0 * (1.0 - Math.Exp(-69.0 / 100.0     * PI * x3))
			+ 2.0 / 25.0 * (1.0 - Math.Exp(-9.0 / 2.0        * PI * ax2))
			+ 1.0 / 10.0 * (1.0 - Math.Exp(-1.55294068198794 * PI * x ))
		) * Math.Sign(x);
		
		*/
		return Math.Sign(x) * (
			 1.0 / 2.0 + ((1 + 0.926 * ax) / (2 + 1.792 * ax + 3.104 * ax2)) * Math.Sin(Math.PI * ax2 / 2)
			-(1 / (2 + 4.142 * ax + 3.492 * ax2 + 6.67 * ax3)) * Math.Cos(Math.PI * ax2 / 2)
		);
	}

	public static double SFres(double x) {
		
		var PI = Math.PI;
		var ax = Math.Abs(x);
		var ax2 = ax * ax;
		var ax3 = ax2 * ax;

		/*
		return (
			-Math.Cos(PI * ax2 / 2.0) / 
			(PI * (ax + 16.7312774552827 * PI * Math.Exp(-1.57638860756614 * PI * Math.Sqrt(ax))))

			+ 8.0 / 25.0 * (1.0 - Math.Exp(-0.608707749430681 * PI * ax3))
			+ 2.0 / 25.0 * (1.0 - Math.Exp(-1.71402838165388  * PI * ax2))
			+ 1.0 / 10.0 * (1.0 - Math.Exp(-9.0 / 10.0        * PI * ax ))
		) * Math.Sign(x);
		*/
		return Math.Sign(x) * (
			1.0 / 2.0 - ((1 + 0.926 * ax) / (2 + 1.792 * ax + 3.104 * ax2)) * Math.Cos(Math.PI * ax2 / 2)
			-(1 / (2 + 4.142 + 3.492 * ax2 + 6.67 * ax3)) * Math.Sin(Math.PI * ax2 / 2)
		);
	}

	
	static public Exp Sin	(Exp x) { return new Exp(Op.Sin,	x, null); }
	static public Exp Cos	(Exp x) { return new Exp(Op.Cos,	x, null); }
	static public Exp ACos	(Exp x) { return new Exp(Op.ACos,	x, null); }
	static public Exp ASin	(Exp x) { return new Exp(Op.ASin,	x, null); }
	static public Exp Sqrt	(Exp x) { return new Exp(Op.Sqrt,	x, null); }
	static public Exp Sqr	(Exp x) { return new Exp(Op.Sqr,	x, null); }
	static public Exp Abs	(Exp x) { return new Exp(Op.Abs,	x, null); }
	static public Exp Sign	(Exp x) { return new Exp(Op.Sign,	x, null); }
	static public Exp Norm	(Exp x) { return new Exp(Op.Norm,	x, null); }
	static public Exp Atan2	(Exp x, Exp y) { return new Exp(Op.Atan2, x, y); }
	static public Exp Expo	(Exp x) { return new Exp(Op.Exp,	x, null); }
	static public Exp Sinh	(Exp x) { return new Exp(Op.Sinh,	x, null); }
	static public Exp Cosh	(Exp x) { return new Exp(Op.Cosh,	x, null); }
	static public Exp SFres	(Exp x) { return new Exp(Op.SFres,	x, null); }
	static public Exp CFres	(Exp x) { return new Exp(Op.CFres,	x, null); }
	//static public Exp Pow  (Exp x, Exp y) { return new Exp(Op.Pow,   x, y); }

	public Exp Drag(Exp to) {
		return new Exp(Op.Drag, this, to);
	}

	public double Eval() {
		switch(op) {
			case Op.Const:	return value;
			case Op.Param:	return param.value;
			case Op.Add:	return a.Eval() + b.Eval();
			case Op.Equal:
			case Op.Drag:
			case Op.Sub:	return a.Eval() - b.Eval();
			
			case Op.Less:
			case Op.LEqual: {
				var av = a.Eval();
				var bv = b.Eval();
				return Math.Abs(av - bv) - (bv - av);
			}
			
			case Op.Greater:
			case Op.GEqual: {
				var av = a.Eval();
				var bv = b.Eval();
				return Math.Abs(av - bv) - (av - bv);
			}
							 
			case Op.Mul:	return a.Eval() * b.Eval();
			case Op.Div: {
					var bv = b.Eval();
					if(Math.Abs(bv) < 1e-10) {
						//Debug.Log("Division by zero");
						bv = 1.0;
					}
					return a.Eval() / bv;
			}
			case Op.Sin:	return Math.Sin(a.Eval());
			case Op.Cos:	return Math.Cos(a.Eval());
			case Op.ACos:	return Math.Acos(a.Eval());
			case Op.ASin:	return Math.Asin(a.Eval());
			case Op.Sqrt:	return Math.Sqrt(a.Eval());
			case Op.Sqr:	{  double av = a.Eval(); return av * av; }
			case Op.Atan2:	return Math.Atan2(a.Eval(), b.Eval());
			case Op.Abs:	return Math.Abs(a.Eval());
			case Op.Sign:	return Math.Sign(a.Eval());
			case Op.Norm:   return a.Eval();
			case Op.Neg:	return -a.Eval();
			case Op.Pos:	return a.Eval();
			case Op.Exp:	return Math.Exp(a.Eval());
			case Op.Sinh:	return Math.Sinh(a.Eval());
			case Op.Cosh:	return Math.Cosh(a.Eval());
			case Op.SFres:	return SFres(a.Eval());
			case Op.CFres:	return CFres(a.Eval());
			case Op.If:		return a.EvalBool() ? b.Eval() : c.Eval();
			//case Op.Pow:	return Math.Pow(a.Eval(), b.Eval());
		}
		var cus = CustomFunction.Get(op);
		if(cus == null) return 0.0;
		return cus.Eval(this);
	}

	public bool EvalBool() {
		switch(op) {
			case Op.Add:	return a.EvalBool() || b.EvalBool();
			case Op.Equal:
			case Op.Drag:	return a.EvalBool() == b.EvalBool();
			case Op.Sub:	return a.EvalBool() != b.EvalBool();
			
			case Op.Less:	return a.Eval() <  b.Eval();
			case Op.LEqual: return a.Eval() <= b.Eval();
			case Op.Greater:return a.Eval() >  b.Eval();
			case Op.GEqual: return a.Eval() >= b.Eval();
			
			case Op.Mul:	return a.EvalBool() && b.EvalBool();
			case Op.Div:	return a.EvalBool() && !b.EvalBool();
			case Op.Neg:	return !a.EvalBool();
			case Op.Pos:	return a.EvalBool();
			case Op.If:		return a.EvalBool();
		}
		var cus = CustomFunction.Get(op);
		if(cus == null) return false;
		return cus.EvalBool(this);
	}

	public bool IsZeroConst()		{ return op == Op.Const && value ==  0.0; }
	public bool IsOneConst()		{ return op == Op.Const && value ==  1.0; }
	public bool IsMinusOneConst()	{ return op == Op.Const && value == -1.0; }
	public bool IsConst()			{ return op == Op.Const; }
	public bool IsDrag()			{ return op == Op.Drag; }

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
			case Op.Norm:
			case Op.Neg:
			case Op.Pos:
			case Op.Exp:
			case Op.Cosh:
			case Op.Sinh:
			case Op.CFres:
			case Op.SFres:
				return true;
		}
		var cus = CustomFunction.Get(op);
		if(cus != null) return cus.ArgCount == 1;
		return false;
	}

	public bool IsAdditive() {
		switch(op) {
			case Op.Equal:
			case Op.Drag:
			case Op.Sub:
			case Op.Add:
			case Op.Or:
				return true;
		}
		return false;
	}

    public static int OrderOf(Op op) {
        switch(op) {
			case Exp.Op.Equal:
			case Exp.Op.LEqual:
			case Exp.Op.GEqual:
			case Exp.Op.Greater:
			case Exp.Op.Less:
			case Exp.Op.Drag:
				return 1;
            case Exp.Op.Add:
            case Exp.Op.Sub:
                return 2;
            case Exp.Op.Mul:
            case Exp.Op.Div:
                return 3;
        }
		var cus = CustomFunction.Get(op);
		if(cus != null) return cus.Order;
		return 0;
    }

	public string Quoted() {
		if(IsUnary()) return ToString();
		return "(" + ToString() + ")";
	}

	public string QuotedAdd() {
		if(!IsAdditive()) return ToString();
		return "(" + ToString() + ")";
	}

	public override string ToString() {
		switch(op) {
			case Op.Const:	return value.ToStr();
			case Op.Param:	return param.name;
			case Op.Add:	return a.ToString() + " + " + b.ToString();
			case Op.Sub:	return a.ToString() + " - " + b.QuotedAdd();
			case Op.Mul:	return a.QuotedAdd() + " * " + b.QuotedAdd();
			case Op.Div:	return a.QuotedAdd() + " / " + b.Quoted();
			case Op.Sin:	return "sin(" + a.ToString() + ")";
			case Op.Cos:	return "cos(" + a.ToString() + ")";
			case Op.ASin:	return "asin(" + a.ToString() + ")";
			case Op.ACos:	return "acos(" + a.ToString() + ")";
			case Op.Sqrt:	return "sqrt(" + a.ToString() + ")";
			case Op.Sqr:	return a.Quoted() + " ^ 2";
			case Op.Abs:	return "abs(" + a.ToString() + ")";
			case Op.Sign:	return "sign(" + a.ToString() + ")";
			case Op.Norm:	return "norm(" + a.ToString() + ")";
			case Op.Atan2:	return "atan2(" + a.ToString() + ", " + b.ToString() + ")";
			case Op.Neg:	return "-" + a.Quoted();
			case Op.Pos:	return "+" + a.Quoted();
			case Op.Equal:  return a.ToString() + " = " + b.ToString();
			case Op.GEqual: return a.ToString() + " >= " + b.ToString();
			case Op.LEqual: return a.ToString() + " <= " + b.ToString();
			case Op.Greater:return a.ToString() + " > " + b.ToString();
			case Op.Less:   return a.ToString() + " < " + b.ToString();
			case Op.Drag:   return a.ToString() + " ~ " + b.ToString();
			case Op.Exp:	return "exp(" + a.ToString() + ")";
			case Op.Sinh:	return "sinh(" + a.ToString() + ")";
			case Op.Cosh:	return "cosh(" + a.ToString() + ")";
			case Op.SFres:	return "sfres(" + a.ToString() + ")";
			case Op.CFres:	return "cfres(" + a.ToString() + ")";
			case Op.If:		return "if(" + a.ToString() + ", " + b.ToString() + ", " + c.ToString() + ")";
			//case Op.Pow:	return Quoted(a) + " ^ " + Quoted(b);
		}
		var cus = CustomFunction.Get(op);
		if(cus == null) return "!?!";
		return CustomFunction.Get(op).AsString(this);
	}

	public bool IsDependOn(Param p) {
		if(op == Op.Param) return param == p;
		if(a != null) {
			if(b != null) {
				return a.IsDependOn(p) || b.IsDependOn(p);
			}
			return a.IsDependOn(p);
		}
		return false;
	}

	public Exp Deriv(Param p) {
		return d(p);
	}

	Exp d(Param p) {
		switch(op) {
			case Op.Const:	return zero;
			case Op.Param:	return (param == p) ? one : zero;
			case Op.Add:	return a.d(p) + b.d(p);
			case Op.Equal:
			case Op.Drag:
			case Op.Sub:	return a.d(p) - b.d(p);
			
			case Op.GEqual:
			case Op.Greater: return (Abs(a - b) - Norm(a - b)).d(p);

			case Op.LEqual:
			case Op.Less: return (Abs(a - b) - Norm(b - a)).d(p);

			case Op.Mul:	return a.d(p) * b + a * b.d(p);
			case Op.Div:	return (a.d(p) * b - a * b.d(p)) / Sqr(b);
			case Op.Sin:	return a.d(p) * Cos(a);
			case Op.Cos:	return a.d(p) * -Sin(a);
			case Op.ASin:	return a.d(p) / Sqrt(one - Sqr(a));
			case Op.ACos:	return a.d(p) * mOne / Sqrt(one - Sqr(a));
			case Op.Sqrt:	return a.d(p) / (two * Sqrt(a));
			case Op.Sqr:	return a.d(p) * two * a;
			case Op.Abs:	return a.d(p) * Sign(a);
			case Op.Sign:	return zero;
			case Op.Norm:	return Sign(Abs(a)) * a.d(p);
			case Op.Neg:    return -a.d(p);
			case Op.Atan2:	return (b * a.d(p) - a * b.d(p)) / (Sqr(a) + Sqr(b));
			case Op.Exp:	return a.d(p) * Expo(a);
			case Op.Sinh:	return a.d(p) * Cosh(a);
			case Op.Cosh:	return a.d(p) * Sinh(a);
			case Op.SFres:	return a.d(p) * Sin(Math.PI * Sqr(a) / 2.0);
			case Op.CFres:	return a.d(p) * Cos(Math.PI * Sqr(a) / 2.0);
			case Op.Pos:	return a.d(p);
			case Op.If:		return new Exp(Op.If, a, b.d(p), c.d(p));
		}
		var cus = CustomFunction.Get(op);
		if(cus == null) return 0.0;
		return cus.Deriv(this, p);
	}

	public bool IsSubstitionForm() {
		return op == Op.Sub && a.op == Op.Param && b.op == Op.Param;
	}

	public Param GetSubstitutionParamA() {
		if(!IsSubstitionForm()) return null;
		return a.param;
	}

	public Param GetSubstitutionParamB() {
		if(!IsSubstitionForm()) return null;
		return b.param;
	}

	public void Substitute(Param pa, Param pb) {
		if(a != null) {
			a.Substitute(pa, pb);
			if(b != null) {
				b.Substitute(pa, pb);
			}
		} else
		if(op == Op.Param && param == pa) {
			param = pb;
		}
	}

	public void Substitute(Param p, Exp e) {
		if(a != null) {
			a.Substitute(p, e);
			if(b != null) {
				b.Substitute(p, e);
				if(c != null) {
					c.Substitute(p, e);
				}
			}
		} else
		if(op == Op.Param && param == p) {
			op = e.op;
			a = e.a;
			b = e.b;
			param = e.param;
			value = e.value;
		}
	}

	public void Walk(Action<Exp> action) {
		action(this);
		if(a != null) {
			action(a);
			if(b != null) {
				action(b);
				if(c != null) {
					action(c);
				}
			}
		}
	}

	public Exp DeepClone() {
		Exp result = new Exp();
		result.op = op;
		result.param = param;
		result.value = value;
		if(a != null) {
			result.a = a.DeepClone();
			if(b != null) {
				result.b = b.DeepClone();
				if(c != null) {
					result.c = c.DeepClone();
				}
			}
		}
		return result;
	}

	public bool ParamsChanged() {
		bool changed = false;
		Walk(e => {
			changed = changed || e.op == Op.Param && e.param.changed;
		});
		return changed;
	}
	
	public void ReduceParams(List<Param> pars) {
		if(op == Op.Param) {
			if(param.reduceable && !pars.Contains(param)) {
				value = Eval();
				op = Op.Const;
				param = null;
			}
			return;
		}

		if(a != null) {
			a.ReduceParams(pars);
			if(b != null) b.ReduceParams(pars);
			if(a.IsConst() && (b == null || b.IsConst())) {
				value = Eval();
				op = Op.Const;
				a = null;
				b = null;
				param = null;
			}
		}

	}

	public bool HasTwoOperands() {
		return a != null && b != null;
	}

	public Op GetOp() {
		return op;
	}

	public static bool IsEquation(Exp.Op op) {
        switch(op) {
			case Exp.Op.Equal:
			case Exp.Op.LEqual:
			case Exp.Op.GEqual:
			case Exp.Op.Greater:
			case Exp.Op.Less:
			case Exp.Op.Drag:
				return true;
		}
		return false;
	}

	public bool IsEquation() {
		return IsEquation(op);
	}

}

namespace CustomFunctions {
	
	abstract class BoolBin : CustomFunction {
		public override int ArgCount => 2;
		public override bool IsFunction => false;
		public override string AsString(Exp exp) { return exp.a.QuotedAdd() + Name + exp.b.QuotedAdd(); }
		public override Exp Deriv(Exp exp, Param p) { return 0.0; }
		public override double Eval(Exp exp) { return 0.0; }
	}

	abstract class Func : CustomFunction {
		public override int ArgCount => 1;
		public override int Order => 0;
		public override bool IsFunction => true;
		public override string AsString(Exp exp) { return Name + "(" + exp.a.ToString() + ")"; }
		public override bool EvalBool(Exp exp) { return Math.Abs(Eval(exp)) > 0.0; }
		public override Exp Deriv(Exp exp, Param p) { return 0.0; }
	}

	class And : BoolBin {
		public override string Name => "&";
		public override int Order => -1;
		public override Exp.Op Op => Exp.Op.And;
		public override bool EvalBool(Exp exp) {
			return exp.a.EvalBool() && exp.b.EvalBool();
		}
	}

	class Or : BoolBin {
		public override string Name => "|";
		public override int Order => -2;
		public override Exp.Op Op => Exp.Op.Or;
		public override bool EvalBool(Exp exp) { return exp.a.EvalBool() || exp.b.EvalBool(); }
	}

	class BoolNot : CustomFunction {
		public override int ArgCount => 1;
		public override bool IsFunction => false;
		public override int Order => 0;
		public override string Name => "!";
		public override Exp.Op Op => Exp.Op.Not;
		public override string AsString(Exp exp) { return Name + exp.a.Quoted(); }
		public override Exp Deriv(Exp exp, Param p) { return 0.0; }
		public override double Eval(Exp exp) { return 0.0; }
		public override bool EvalBool(Exp exp) { return !exp.a.EvalBool(); }
	}


	class Floor : Func {
		public override string Name => "floor";
		public override Exp.Op Op => Exp.Op.Floor;
		public override double Eval(Exp exp) { return Math.Floor(exp.a.Eval()); }
	}

	class Ceil : Func {
		public override string Name => "ceil";
		public override Exp.Op Op => Exp.Op.Ceil;
		public override double Eval(Exp exp) { return Math.Ceiling(exp.a.Eval()); }
	}

	class Round : Func {
		public override string Name => "round";
		public override Exp.Op Op => Exp.Op.Round;
		public override double Eval(Exp exp) { return Math.Round(exp.a.Eval()); }
	}

	class RoundUp : Func {
		public override int ArgCount => 2;
		public override string Name => "roundUp";
		public override Exp.Op Op => Exp.Op.RoundUp;
		public override double Eval(Exp exp) {
			var a = exp.a.Eval();
			var b = exp.b.Eval();
			return Math.Round(Math.Ceiling(a / b) * b);
		}
	}

	class RoundDown : Func {
		public override int ArgCount => 2;
		public override string Name => "roundDown";
		public override Exp.Op Op => Exp.Op.RoundDown;
		public override double Eval(Exp exp) {
			var a = exp.a.Eval();
			var b = exp.b.Eval();
			return Math.Round(Math.Floor(a / b) * b);
		}
	}
}