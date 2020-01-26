using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ExpressionData {
	
	public SketchObject obj { get; private set; }

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

	public ExpressionData(SketchObject sko, Param p0 = null) {
		obj = sko;
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
        { "acos",	Exp.Op.ACos },
        { "asin",	Exp.Op.Cos },
        { "exp",	Exp.Op.Exp },
        { "sinh",	Exp.Op.Sinh },
        { "cosh",	Exp.Op.Cosh },
        { "sfres",	Exp.Op.SFres },
        { "cfres",	Exp.Op.CFres },
    };
    
    Dictionary <char, Exp.Op> operators = new Dictionary<char, Exp.Op> {
        { '+', Exp.Op.Add },
        { '-', Exp.Op.Sub },
        { '=', Exp.Op.Equal },
        { '~', Exp.Op.Drag },
        { '*', Exp.Op.Mul },
        { '/', Exp.Op.Div },
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
			" (a * ((b + c) + (d + e)) * 3 + (f - 1) * 5)) ",
			"a / b * c + 1"
		};

		foreach(var e in exps) {
			var parser = new ExpParser(e);
			var exp = parser.Parse();
			Debug.Log("src: \"" + e + "\" -> \"" + exp.ToString() + "\"");
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
				Debug.Log("result fail: get \"" + exp.Eval() + "\" excepted: \"" + e.Value + "\"");
			}
		}

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
        if(!IsAlpha(next)) return false;
        var start = index;
        while(HasNext() && (IsAlpha(next) || IsDigit(next))) index++;
        alphas = toParse.Substring(start, index - start);
		return true;
    }
    
    Exp.Op GetFunction(string name) {
        if(functions.ContainsKey(name)) {
            return functions[name];
        }
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
                    Exp a = ParseExp(0);
                    Exp b = null;
                    if(SkipIf(',')) {
                        b = ParseExp(0);
                    }
                    Skip(')');
					if(func == Exp.Op.Atan2 && b == null) {
						error("second function argument execpted");
					}
                    return new Exp(func, a, b);
                } else error("function arguments execpted");
            }

			var constant = GetConstant(alphas);
			if(constant != null) return constant;

            var param = GetParam(alphas);
            if(param == null) {
                param = new Param(alphas);
                newParameters.Add(param);
            }
            return new Exp(param);
        }
		error("valid operand excepted");
		return null;
    }
    
    int OrderOf(Exp.Op op) {
        switch(op) {
			case Exp.Op.Equal:
			case Exp.Op.Drag:
				return 1;
            case Exp.Op.Add:
            case Exp.Op.Sub:
                return 2;
            case Exp.Op.Mul:
            case Exp.Op.Div:
                return 3;
            default:
                return 0;
        }
    }
	
	Exp.Op CheckOp() {
		if(operators.ContainsKey(next)) {
			var result = operators[next];
			return result;
		}
		return Exp.Op.Undefined;
	}

	Exp.Op ParseOp() {
		var result = CheckOp();
		if(result != Exp.Op.Undefined)index++;
		return result;
	}

	bool HasNext() {
		return index < toParse.Length;
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
		return Exp.Op.Undefined;
	}
	    
    Exp ParseExp(int minOrder) {

		var uop = ParseUnary();
				
		Exp a = null;
        if(SkipIf('(')) {
            a = ParseExp(0);
            Skip(')');
        } else {
			a = ParseValue();
		}
		if(uop != Exp.Op.Undefined) {
			a = new Exp(uop, a, null);
		}
        
		while(HasNext() && next != ')' && next != ',')  {
			var op = CheckOp();
			if(op == Exp.Op.Undefined) error("operator execpted");
			var curOrder = OrderOf(op);
			if(curOrder <= minOrder) {
				return a;
			}
			index++;

			var b = ParseExp(curOrder);
        
			a = new Exp(op, a, b);
		}
		return a;
    }
    
	public Exp Parse() {
		try {
			var result = ParseExp(0);
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
