using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        toParse = str;
    }

	public void SetString(string str) {
		toParse = str;
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

	void SkipSpaces() {
		if(!HasNext()) return;
        while(HasNext() && IsSpace(next)) index++;
    }
    
    Param GetParam(string name) {
        return parameters.Find(p => p.name == name);
    }
    
    void Skip(char c) {
        SkipSpaces();
		if(!HasNext() || next != c) {
			error("\"" + c + "\" excepted!");
		}
        index++;
    }
    
    bool SkipIf(char c) {
        SkipSpaces();
		if(!HasNext() || next != c) {
			return false;
		}
        index++;
		return true;
    }
    
    bool ParseDigits(ref double digits) {
        SkipSpaces();
		if(!HasNext()) error("operand exepted");
        if(!IsDigit(next)) return false;
        var start = index;
        while(HasNext() && (IsDigit(next) || IsDelimiter(next))) index++;
        var str = toParse.Substring(start, index - start);
        digits = str.ToDouble();
        return true;
    }
    
    bool ParseAlphas(ref string alphas) {
        SkipSpaces();
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
			str.Insert(index, "?");
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
		bool braced = false;
        
        string alphas = "";
        if(ParseAlphas(ref alphas)) {
            var func = GetFunction(alphas);
            if(func != Exp.Op.Undefined) {
                if(SkipIf('(')) {
                    Exp a = ParseExp(ref braced);
                    Exp b = null;
                    if(SkipIf(',')) {
                        b = ParseExp(ref braced);
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
                parameters.Add(param);
            }
            return new Exp(param);
        }
		error("valid operand excepted");
		return null;
    }
    
    int OrderOf(Exp.Op op) {
        switch(op) {
            case Exp.Op.Add:
            case Exp.Op.Sub:
                return 1;
            case Exp.Op.Mul:
            case Exp.Op.Div:
                return 2;
            default:
                return 0;
        }
    }
	
	Exp.Op ParseOp() {
		SkipSpaces();
		if(operators.ContainsKey(next)) {
			var result = operators[next];
			index++;
			return result;
		}
		return Exp.Op.Undefined;
	}

	bool HasNext() {
		return index < toParse.Length;
	}

	Exp.Op ParseUnary() {
		SkipSpaces();
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
	    
    Exp ParseExp(ref bool braced) {

		var uop = ParseUnary();
				
		Exp a = null;
		bool aBraced = false;
        if(SkipIf('(')) {
			bool br = false;
            a = ParseExp(ref br);
            Skip(')');
			aBraced = true;
        } else {
			a = ParseValue();
		}
		if(uop != Exp.Op.Undefined && uop != Exp.Op.Pos) {
			a = new Exp(uop, a, null);
		}
        
		SkipSpaces();
		if(!HasNext() || next == ')' || next == ',') {
			braced = aBraced;
			return a;
		}
        
		var op = ParseOp();
		if(op == Exp.Op.Undefined) error("operator execpted");
        
		bool bBraced = false;
		var b = ParseExp(ref bBraced);
        
		if(!bBraced && b.HasTwoOperands() && OrderOf(op) > OrderOf(b.op)) {
			b.a = new Exp(op, a, b.a);
			return b;
		}
	    return new Exp(op, a, b);
    }
    
	public Exp Parse() {
		try {
			bool braced = false;
			return ParseExp(ref braced);
		} catch (System.Exception) {
			return null;
		}
	}
    
}
