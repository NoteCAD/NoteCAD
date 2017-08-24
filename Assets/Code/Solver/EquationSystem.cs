using System;
using System.Collections.Generic;

public class EquationSystem  {
	public List<Exp> equations = new List<Exp>();
	public List<Param> unknowns = new List<Param>();

	public void Eval(ref double[] B) {
		for(int i = 0; i < equations.Count; i++) {
			B[i] = equations[i].Eval();
		}
	}

	public bool IsConverged() {
		for(int i = 0; i < equations.Count; i++) {
			if(equations[i].Eval() < GaussianMethod.epsilon) continue;
			return false;
		}
		return true;
	}

}
