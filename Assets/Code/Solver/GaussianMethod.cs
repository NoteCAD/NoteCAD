using System;

public static class GaussianMethod {

	public const double epsilon = 1e-10;


	public static string Print(this double[,] A) {
		string result = "";
		for(int r = 0; r < A.GetLength(0); r++) {
			for(int c = 0; c < A.GetLength(1); c++) {
				result += A[r, c].ToString() + " ";
			}
			result += "\n";
		}
		return result;
	}

	public static string Print(this double[] A) {
		string result = "";
		for(int r = 0; r < A.GetLength(0); r++) {
			result += A[r].ToString() + "\n";
		}
		return result;
	}

	public static void Solve(double[,] A, double[] B, ref double[] X) {

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
			B[mr] = B[r];
			B[r] = t;

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
	}

}