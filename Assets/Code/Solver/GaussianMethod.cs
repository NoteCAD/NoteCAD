using System;
using UnityEngine;

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
		UnityEngine.Profiling.Profiler.BeginSample("GaussianMethod.Rank");
		//var time = Time.realtimeSinceStartup;
		var rows = A.GetLength(0);
		var cols = A.GetLength(1);

		int rank = 0;
		double[] rowsLength = new double[rows];

		for(int i = 0; i < rows; i++) {
			for(int ii = 0; ii < i; ii++) {
				if(rowsLength[ii] <= rankEpsilon) continue;

				double sum = 0;
				for(int j = 0; j < cols; j++) {
					if (A[ii, j] == 0 || A[i, j] == 0) continue;
					sum += A[ii, j] * A[i, j];
				}
				if (sum == 0.0) continue;
				double coeff = sum / rowsLength[ii];
				for(int j = 0; j < cols; j++) {
					//if (A[ii, j] == 0) continue;
					A[i, j] -= A[ii, j] * coeff;
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

		UnityEngine.Profiling.Profiler.EndSample();
		//Debug.Log($"GaussianMethod.Rank({rank}) time " + (Time.realtimeSinceStartup - time) * 1000);
		return rank;
	}

	public static void Solve(double[,] A, double[] B, ref double[] X) {

		UnityEngine.Profiling.Profiler.BeginSample("GaussianMethod.Solve");
		//var time = Time.realtimeSinceStartup;
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
				if(coef == 0.0) continue;
				/*
				for(int c = r; c < cols; c++) {
					A[rr, c] -= A[r, c] * coef;
				}
				*/
				
				// unrolled version works a little bit faster (20-30%)
				const int u = 16;
				int c = r;
				int loop = (cols - r) / u;
				int left = (cols - r) % u;

				while(loop-- != 0) {
					A[rr, c +  0] -= A[r, c +  0] * coef;
					A[rr, c +  1] -= A[r, c +  1] * coef;
					A[rr, c +  2] -= A[r, c +  2] * coef;
					A[rr, c +  3] -= A[r, c +  3] * coef;
					A[rr, c +  4] -= A[r, c +  4] * coef;
					A[rr, c +  5] -= A[r, c +  5] * coef;
					A[rr, c +  6] -= A[r, c +  6] * coef;
					A[rr, c +  7] -= A[r, c +  7] * coef;
					A[rr, c +  8] -= A[r, c +  8] * coef;
					A[rr, c +  9] -= A[r, c +  9] * coef;
					A[rr, c + 10] -= A[r, c + 10] * coef;
					A[rr, c + 11] -= A[r, c + 11] * coef;
					A[rr, c + 12] -= A[r, c + 12] * coef;
					A[rr, c + 13] -= A[r, c + 13] * coef;
					A[rr, c + 14] -= A[r, c + 14] * coef;
					A[rr, c + 15] -= A[r, c + 15] * coef;
					c += u;
				}

				switch(left) {
					case 15: A[rr, c + 14] -= A[r, c + 14] * coef; goto case 14;
					case 14: A[rr, c + 13] -= A[r, c + 13] * coef; goto case 13;
					case 13: A[rr, c + 12] -= A[r, c + 12] * coef; goto case 12;
					case 12: A[rr, c + 11] -= A[r, c + 11] * coef; goto case 11;
					case 11: A[rr, c + 10] -= A[r, c + 10] * coef; goto case 10;
					case 10: A[rr, c +  9] -= A[r, c +  9] * coef; goto case 9;
					case  9: A[rr, c +  8] -= A[r, c +  8] * coef; goto case 8;
					case  8: A[rr, c +  7] -= A[r, c +  7] * coef; goto case 7;
					case  7: A[rr, c +  6] -= A[r, c +  6] * coef; goto case 6;
					case  6: A[rr, c +  5] -= A[r, c +  5] * coef; goto case 5;
					case  5: A[rr, c +  4] -= A[r, c +  4] * coef; goto case 4;
					case  4: A[rr, c +  3] -= A[r, c +  3] * coef; goto case 3;
					case  3: A[rr, c +  2] -= A[r, c +  2] * coef; goto case 2;
					case  2: A[rr, c +  1] -= A[r, c +  1] * coef; goto case 1;
					case  1: A[rr, c +  0] -= A[r, c +  0] * coef; goto case 0;
					case  0: break;
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
		//Debug.Log("GaussianMethod.Solve time " + (Time.realtimeSinceStartup - time) * 1000);
	}

}