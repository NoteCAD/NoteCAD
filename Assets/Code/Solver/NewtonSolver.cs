using System;
using UnityEngine;

public class NewtonSolver {

	public enum Result {
		OKAY,
		DIDNT_CONVEGE,
		REDUNDANT
	}

	public int maxSteps = 20;

	public static Exp[,] WriteJacobian(EquationSystem sys) {
		var J = new Exp[sys.equations.Count, sys.unknowns.Count];
		for(int r = 0; r < sys.equations.Count; r++) {
			var eq = sys.equations[r];
			for(int c = 0; c < sys.unknowns.Count; c++) {
				var u = sys.unknowns[c];
				J[r, c] = eq.Deriv(u);
			}
		}
		return J;
	}

	public static void EvalJacobian(Exp[,] J, ref double[,] A) {
		for(int r = 0; r < J.GetLength(0); r++) {
			for(int c = 0; c < J.GetLength(1); c++) {
				A[r, c] = J[r, c].Eval();
			}
		}
	}

	public static void SolveLeastSquares(double[,] A, double[] B, ref double[] X) {

		// A^T * A * X = A^T * B
		var rows = A.GetLength(0);
		var cols = A.GetLength(1);

		var AAT = new double[rows, rows];

		for(int r = 0; r < rows; r++) {
			for(int c = 0; c < rows; c++) {
                double sum = 0.0;
				for(int i = 0; i < cols; i++) {
					sum += A[r, i] * A[c, i];
				}
                AAT[r, c] = sum;
			}
		}
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

	public Result Solve(EquationSystem sys) {

		var J = WriteJacobian(sys);
		Debug.Log(J.Print());
		var A = new double[J.GetLength(0), J.GetLength(1)];
		var B = new double[sys.equations.Count];
		var X = new double[sys.unknowns.Count];

		sys.Eval(ref B);
		int steps = 0;
		do {
			EvalJacobian(J, ref A);
			sys.Eval(ref B);
			SolveLeastSquares(A, B, ref X);
			for(int i = 0; i < sys.unknowns.Count; i++) {
				sys.unknowns[i].value -= X[i];
			}
			if(sys.IsConverged()) {
				return Result.OKAY;
			}
		} while(steps++ <= maxSteps);
		return Result.DIDNT_CONVEGE;
	}

}