using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class ExpressionData {
	
	public SketchObject obj { get; private set; }
	public bool isEquation { get; set; }

	string source_ = "";
	public string source {
		get {
			return source_;
		}

		set {
			source_ = value;
			if(source_ == "") {
				expression = Exp.zero;
			} else {
				var parser = new ExpParser(this);
				expression = parser.Parse();
				foreach(var p in parser.newParameters) {
					obj.sketch.AddParameter(p);
				}
			}
			Debug.Log("exp = " + expression.ToString());
		}
	}

	public bool Exist() {
		return source != "" && source != null;
	}
	
	Exp exp;
	public Exp expression {
		private set {
			exp = value;
			obj.sketch.MarkDirtySketch(topo: true, entities: true);
		}
		get {
			return exp;
		}
	}

	public List<Param> parameters { get; private set; }

	public ExpressionData(SketchObject sko, bool equation, Param p0 = null) {
		obj = sko;
		isEquation = equation;
		if(p0 != null) {
			parameters = new List<Param>();
			parameters.Add(p0);
		}
	}

}

public class ExpParser {
    
    Dictionary <string, Exp.Op> functions = new Dictionary<string, Exp.Op> {
        { "sin",	Exp.Op.Sin },
        { "cos",	Exp.Op.Cos },
        { "atan2",	Exp.Op.Atan2 },
        { "sqr",	Exp.Op.Sqr },
        { "sqrt",	Exp.Op.Sqrt },
        { "abs",	Exp.Op.Abs },
        { "sign",	Exp.Op.Sign },
        { "norm",	Exp.Op.Norm },
        { "acos",	Exp.Op.ACos },
        { "asin",	Exp.Op.Cos },
        { "exp",	Exp.Op.Exp },
        { "sinh",	Exp.Op.Sinh },
        { "cosh",	Exp.Op.Cosh },
        { "sfres",	Exp.Op.SFres },
        { "cfres",	Exp.Op.CFres },
        { "if",		Exp.Op.If },
    };
    
    Dictionary <string, Exp.Op> operators = new Dictionary<string, Exp.Op> {
        { "+", Exp.Op.Add },
        { "-", Exp.Op.Sub },
        { "=", Exp.Op.Equal },
        { ">=", Exp.Op.GEqual },
        { "<=", Exp.Op.LEqual },
        { ">", Exp.Op.Greater },
        { "<", Exp.Op.Less },
        { "~", Exp.Op.Drag },
        { "*", Exp.Op.Mul },
        { "/", Exp.Op.Div },
    };

    Dictionary <string, double> constants = new Dictionary<string, double> {
		{ "pi", Math.PI },
		{ "e", Math.E },
	};
   
    string toParse;
    int index = 0;
    
    public List<Param> parameters = new List<Param>();
    public List<Param> newParameters = new List<Param>();
    
	public static void Test() {
		List<string> exps = new List<string> {
			"a + b",
			"  a  - -b",
			"43 + d * c",
			"2.3 * d + c ",
			"(a * b) + c ",
			"a * (b + c)",
			" a * b + c * (d + e) * f - 1 ",
			" a * (b + c) * (d + e) * (f - 1) ",
			"((a * ((b + c) + (d + e)) * 3 + (f - 1) * 5))",
			"a / b * c + 1"
		};

		foreach(var e in exps) {
			var parser = new ExpParser(e);
			try {
				var exp = parser.Parse();
				Debug.Log("src: \"" + e + "\" -> \"" + exp.ToString() + "\"");
			} catch (Exception except) {
				Debug.LogError("can't parse src: \"" + e + "\" with error " + except.ToString());
			}
		}

		Dictionary<string, double> results = new Dictionary<string, double> {
			{ "2 * 3", 6.0 },
			{ "2 + 1", 3.0 },
			{ "-2 + 2", 0.0 },
			{ "+2 - -2", 4.0 },
			{ "-2 * -2", 4.0 },
			{ "+1 * +2", 2.0 },
			{ "2 + 3 * 6", 20.0 },
			{ "2 + (3 * 6)", 20.0 },
			{ "(2 + 3) * 6", 30.0 },
			{ "((2 + 3) * (6))", 30.0 },
			{ "cos(0)", 1.0 },
			{ "sqr(cos(2)) + sqr(sin(2))", 1.0 },
			{ "pi", Math.PI },
			{ "e", Math.E },
		};

		foreach(var e in results) {
			var parser = new ExpParser(e.Key);
			var exp = parser.Parse();
			Debug.Log("src: \"" + e + "\" -> \"" + exp.ToString() + "\" = " + exp.Eval().ToStr());
			if(exp.Eval() != e.Value) {
				Debug.LogError("result fail: get \"" + exp.Eval() + "\" excepted: \"" + e.Value + "\"");
			}
		}

	}

	static ExpParser() {
		Test();
	}

    public ExpParser(string str) {
        SetString(str);
    }

    public ExpParser(ExpressionData data) {
        SetString(data.source);
		if(data.parameters != null) {
			parameters.AddRange(data.parameters);
		}
		parameters.AddRange(data.obj.sketch.userParameters);
    }

	string Normalize(string str) {
		if(str == null) return "";
		return str.Replace(" ", "").Replace("\t", "");
	}

	public void SetString(string str) {
		toParse = Normalize(str);
		index = 0;
	}
    
    char next {
		get {
			return toParse[index];
		}
	}

	bool IsSpace(char c) {
		return Char.IsWhiteSpace(c);
	}

	bool IsDigit(char c) {
		return Char.IsDigit(c);
	}

	bool IsDelimiter(char c) {
		return c == '.';
	}

	bool IsAlpha(char c) {
		return Char.IsLetter(c);
	}
    
    Param GetParam(string name) {
        return parameters.Find(p => p.name == name);
    }
    
    void Skip(char c) {
		if(!HasNext() || next != c) {
			error("\"" + c + "\" excepted!");
		}
        index++;
    }
    
    bool SkipIf(char c) {
		if(!HasNext() || next != c) {
			return false;
		}
        index++;
		return true;
    }
    
    bool ParseDigits(ref double digits) {
		if(!HasNext()) error("operand exepted");
        if(!IsDigit(next)) return false;
        var start = index;
        while(HasNext() && (IsDigit(next) || IsDelimiter(next))) index++;
        var str = toParse.Substring(start, index - start);
        digits = str.ToDouble();
        return true;
    }
    
    bool ParseAlphas(ref string alphas) {
		if(!HasNext()) error("operand exepted");
        if(!IsAlpha(next) && next != '_') return false;
        var start = index;
        while(HasNext() && (IsAlpha(next) || IsDigit(next) || next == '_')) index++;
        alphas = toParse.Substring(start, index - start);
		return true;
    }
    
    Exp.Op GetFunction(string name) {
        if(functions.ContainsKey(name)) {
            return functions[name];
        }
		var cus = CustomFunction.GetFunction(name);
		if(cus != null) return cus.Op;
        return Exp.Op.Undefined;
    }

    Exp GetConstant(string name) {
        if(constants.ContainsKey(name)) {
            return constants[name];
        }
        return null;
    }
    
	void error(string error = "") {
		var str = toParse;
		if(index < str.Length) {
			str = str.Insert(index, "?");
		}
		var msg = error + " (error in \"" + str + "\")";
		Debug.Log(msg);
		throw new System.Exception(msg);
	}
    
    Exp ParseValue() {
        
        double digits = 0.0;
        if(ParseDigits(ref digits)) {
            return new Exp(digits);
        }
        
        string alphas = "";
        if(ParseAlphas(ref alphas)) {
            var func = GetFunction(alphas);
            if(func != Exp.Op.Undefined) {
                if(SkipIf('(')) {
                    Exp a = ParseMain();
                    Exp b = null;
					Exp c = null;
                    if(SkipIf(',')) {
                        b = ParseMain();
                    }
                    if(SkipIf(',')) {
                        c = ParseMain();
                    }
                    Skip(')');
					if((func == Exp.Op.Atan2 || func == Exp.Op.If) && b == null) {
						error("second function argument execpted");
					}
					if(func == Exp.Op.If && c == null) {
						error("third function argument execpted");
					}
                    return new Exp(func, a, b, c);
                } else error("function arguments execpted");
            }

			var constant = GetConstant(alphas);
			if(constant != null) return constant;

            var param = GetParam(alphas);
            if(param == null) {
                param = new Param(alphas);
				//param.solvable = false;
                newParameters.Add(param);
            }
            return new Exp(param);
        }
		error("valid operand excepted");
		return null;
    }
    
	IEnumerable<KeyValuePair<string, Exp.Op>> allOperators => operators.Concat(CustomFunction.operatorNames.Select(on => new KeyValuePair<string, Exp.Op>(on.Key, on.Value.Op)));

	Exp.Op CheckOp(Func<Exp.Op, bool> filter = null) {
		foreach(var op in allOperators) {
			if(filter != null && !filter(op.Value)) continue;
			if(!HasNext(op.Key.Length)) continue;
			if(op.Key == toParse.Substring(index, op.Key.Length)) {
				return op.Value;
			}
		}
		return Exp.Op.Undefined;
	}

	void SkipOp(Exp.Op op) {
		index += allOperators.First(o => o.Value == op).Key.Length;
	}

	Exp.Op ParseOp() {
		var result = CheckOp();
		if(result != Exp.Op.Undefined) SkipOp(result);
		return result;
	}

	bool HasNext(int length = 1) {
		return index + length - 1 < toParse.Length;
	}

	Exp.Op ParseUnary() {
		if(next == '+') {
			index++;
			return Exp.Op.Pos;
		}
		if(next == '-') {
			index++;
			return Exp.Op.Neg;
		}
		return CheckOp(op => {
			var cus = CustomFunction.Get(op);
			return cus != null && cus.ArgCount == 1;
		});
	}
	
	Exp ParseMain() {
				
		Exp a = ParseExp(0);
		while(HasNext() && next != ')' && next != ',')  {
			var op = CheckOp();
			if(op == Exp.Op.Undefined) error("operator expected");
			//var curSingle = Exp.IsEquation(op);
			//if(curSingle && single) error("operators like \"" + operators.First(o => o.Value == op).Key + "\" can be used only one time per expression");
			//single = curSingle;
			var curOrder = Exp.OrderOf(op);
			SkipOp(op);
			var b = ParseExp(curOrder);
			a = new Exp(op, a, b);
		}
		return a;
	}

    Exp ParseExp(int minOrder) {

		var uop = ParseUnary();
				
		Exp a = null;
        if(SkipIf('(')) {
            a = ParseMain();
            Skip(')');
        } else {
			a = ParseValue();
		}
		if(uop != Exp.Op.Undefined) {
			a = new Exp(uop, a, null);
		}
        
		bool single = false;
		while(HasNext() && next != ')' && next != ',')  {
			var op = CheckOp();
			if(op == Exp.Op.Undefined) error("operator expected");
			//var curSingle = Exp.IsEquation(op);
			//if(curSingle && single) error("operators like \"" + operators.First(o => o.Value == op).Key + "\" can be used only one time per expression");
			//single = curSingle;
			var curOrder = Exp.OrderOf(op);
			if(curOrder <= minOrder) {
				return a;
			}
			SkipOp(op);

			var b = ParseExp(curOrder);
        
			a = new Exp(op, a, b);
		}
		return a;
    }
    
	public Exp Parse() {
		try {
			var result = ParseMain();
			if(HasNext()) {
				error("unexcepted token");
			}
			/*
			var resStr = Normalize(result.ToString());
			if(resStr != toParse) {
				Debug.LogErrorFormat("Result expression isn't equal to source\nres: {0}\nsrc: {1}", resStr, toParse);
			}
			*/
			return result;

		} catch (System.Exception) {
			return null;
		}
	}

  }
