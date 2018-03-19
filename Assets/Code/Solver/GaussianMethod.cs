using System;

public static class GaussianMethod {

	public const double epsilon = 1e-10;
	public const double rankEpsilon = 1e-8;


	public static string Print<T>(this T[,] A) {
		string result = "";
		for(int r = 0; r < A.GetLength(0); r++) {
			for(int c = 0; c < A.GetLength(1); c++) {
				result += A[r, c].ToString() + " ";
			}
			result += "\n";
		}
		return result;
	}

	public static string Print<T>(this T[] A) {
		string result = "";
		for(int r = 0; r < A.GetLength(0); r++) {
			result += A[r].ToString() + "\n";
		}
		return result;
	}


	public static int Rank(double[,] A) {
		var rows = A.GetLength(0);
		var cols = A.GetLength(1);

		int rank = 0;
		double[] rowsLength = new double[rows];

		for(int i = 0; i < rows; i++) {
			for(int ii = 0; ii < i; ii++) {
				if(rowsLength[ii] <= rankEpsilon) continue;

				double sum = 0;
				for(int j = 0; j < cols; j++) {
					sum += A[ii, j] * A[i, j];
				}
				for(int j = 0; j < cols; j++) {
					A[i, j] -= A[ii, j] * sum / rowsLength[ii];
				}
			}

			double len = 0;
			for(int j = 0; j < cols; j++) {
				len += A[i, j] * A[i, j];
			}
			if(len > rankEpsilon) {
				rank++;
			}
			rowsLength[i] = len;
		}

		return rank;
	}

	public static void Solve(double[,] A, double[] B, ref double[] X) {

		UnityEngine.Profiling.Profiler.BeginSample("GaussianMethod.Solve");
		var rows = A.GetLength(0);
		var cols = A.GetLength(1);
		double t = 0.0;

		for(int r = 0; r < rows; r++) {

			var mr = r;
			double max = 0.0;
			for(int rr = r; rr < rows; rr++) {
				if(Math.Abs(A[rr, r]) <= max) continue;
				max = Math.Abs(A[rr, r]);
				mr = rr;
			}

			if(max < epsilon) continue;

			for(int c = 0; c < cols; c++) {
				t = A[r, c];
				A[r, c] = A[mr, c];
				A[mr, c] = t;
			}

			t = B[r];
			B[r] = B[mr];
			B[mr] = t;

			// normalize
			/*
			double scale = A[r, r];
			for(int c = 0; c < cols; c++) {
				A[r, c] /= scale;
			}
			B[r] /= scale;
			*/

			// 
			for(int rr = r + 1; rr < rows; rr++) {
				double coef = A[rr, r] / A[r, r];
				for(int c = 0; c < cols; c++) {
					A[rr, c] -= A[r, c] * coef;
				}
				B[rr] -= B[r] * coef;
			}
		}

		for(int r = rows - 1; r >= 0; r--) {
			if(Math.Abs(A[r, r]) < epsilon) continue;
			double xx = B[r] / A[r, r];
			for(int rr = rows - 1; rr > r; rr--) {
				xx -= X[rr] * A[r, rr] / A[r, r];
			}
			X[r] = xx;
		}
		UnityEngine.Profiling.Profiler.EndSample();
	}

}