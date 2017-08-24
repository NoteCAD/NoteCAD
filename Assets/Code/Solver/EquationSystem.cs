using System;
using System.Collections.Generic;
using UnityEngine;

public class EquationSystem  {

	public enum SolveResult {
		OKAY,
		DIDNT_CONVEGE,
		REDUNDANT
	}

	bool isDirty = true;
	public int maxSteps = 20;

	Exp[,] J;
	double[,] A;
	double[] B;
	double[] X;
	double[] oldParamValues;

	List<Exp> equations = new List<Exp>();
	List<Param> parameters = new List<Param>();

	public void AddEquation(Exp eq) {
		equations.Add(eq);
		isDirty = true;
	}

	public void AddParameter(Param p) {
		parameters.Add(p);
		isDirty = true;
	}

	public void Eval(ref double[] B) {
		UnityEngine.Profiling.Profiler.BeginSample("EvalB");
		for(int i = 0; i < equations.Count; i++) {
			B[i] = equations[i].Eval();
		}
		UnityEngine.Profiling.Profiler.EndSample();
	}

	public bool IsConverged() {
		UnityEngine.Profiling.Profiler.BeginSample("IsConverged");
		for(int i = 0; i < equations.Count; i++) {
			if(Math.Abs(equations[i].Eval()) < GaussianMethod.epsilon) continue;
			UnityEngine.Profiling.Profiler.EndSample();
			return false;
		}
		UnityEngine.Profiling.Profiler.EndSample();
		return true;
	}

	void StoreParams() {
		for(int i = 0; i < parameters.Count; i++) {
			oldParamValues[i] = parameters[i].value;
		}
	}

	void RevertParams() {
		for(int i = 0; i < parameters.Count; i++) {
			parameters[i].value = oldParamValues[i];
		}
	}

	static Exp[,] WriteJacobian(List<Exp> equations, List<Param> parameters) {
		var J = new Exp[equations.Count, parameters.Count];
		for(int r = 0; r < equations.Count; r++) {
			var eq = equations[r];
			for(int c = 0; c < parameters.Count; c++) {
				var u = parameters[c];
				J[r, c] = eq.Deriv(u);
				if(!J[r, c].IsZeroConst()) {
					Debug.Log(J[r, c].ToString() + "\n");
				}
			}
		}
		return J;
	}

	public static void EvalJacobian(Exp[,] J, ref double[,] A) {
		UnityEngine.Profiling.Profiler.BeginSample("EvalJacobian");
		for(int r = 0; r < J.GetLength(0); r++) {
			for(int c = 0; c < J.GetLength(1); c++) {
				A[r, c] = J[r, c].Eval();
			}
		}
		UnityEngine.Profiling.Profiler.EndSample();
	}

	public static void SolveLeastSquares(double[,] A, double[] B, ref double[] X) {

		// A^T * A * X = A^T * B
		var rows = A.GetLength(0);
		var cols = A.GetLength(1);

		var AAT = new double[rows, rows];

		UnityEngine.Profiling.Profiler.BeginSample("SolveLeastSquares: A^T * A");
		for(int r = 0; r < rows; r++) {
			for(int c = 0; c < rows; c++) {
                double sum = 0.0;
				for(int i = 0; i < cols; i++) {
					sum += A[r, i] * A[c, i];
				}
                AAT[r, c] = sum;
			}
		}
		UnityEngine.Profiling.Profiler.EndSample();

        var Z = new double[rows];
        GaussianMethod.Solve(AAT, B, ref Z);

        for(int c = 0; c < cols; c++) {
            double sum = 0.0;
            for(int r = 0; r < rows; r++) {
                sum += Z[r] * A[r, c];
            }
            X[c] = sum;
        }

	}

	public SolveResult Solve() {

		if(isDirty) {
			J = WriteJacobian(equations, parameters);
			A = new double[J.GetLength(0), J.GetLength(1)];
			B = new double[equations.Count];
			X = new double[parameters.Count];
			oldParamValues = new double[parameters.Count];
			isDirty = false;
		}

		StoreParams();
		int steps = 0;
		do {
			EvalJacobian(J, ref A);
			Eval(ref B);
			SolveLeastSquares(A, B, ref X);
			for(int i = 0; i < parameters.Count; i++) {
				parameters[i].value -= X[i];
			}
			if(IsConverged()) {
				return SolveResult.OKAY;
			}
		} while(steps++ <= maxSteps);
		RevertParams();
		return SolveResult.DIDNT_CONVEGE;
	}
}
