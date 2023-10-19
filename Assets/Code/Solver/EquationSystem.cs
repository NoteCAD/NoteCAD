using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EquationSystem  {

	public enum SolveResult {
		OKAY,
		DIDNT_CONVEGE,
		REDUNDANT,
		POSTPONE
	}

	bool isDirty = true;

	public bool IsDirty { get { return isDirty; } }
	public int maxSteps = 20;
	public int dragSteps = 3;
	public bool revertWhenNotConverged = true;

	Exp[,] J;
	List<int>[] nzColumns;
	List<int>[] nzRows;
	double[,] A;
	double[,] AAT;
	double[] B;
	double[] X;
	double[] Z;
	double[] oldParamValues;

	List<Exp> sourceEquations = new List<Exp>();
	List<Param> parameters = new List<Param>();

	List<Exp> equations = new List<Exp>();
	List<Param> currentParams = new List<Param>();

	public IEnumerable<Exp> equationsList => sourceEquations.AsEnumerable();

	Dictionary<Param, Param> subs;

	public void AddEquation(Exp eq) {
		sourceEquations.Add(eq);
		isDirty = true;
	}

	public void AddEquation(ExpVector v) {
		sourceEquations.Add(v.x);
		sourceEquations.Add(v.y);
		sourceEquations.Add(v.z);
		isDirty = true;
	}

	public void AddEquations(IEnumerable<Exp> eq) {
		sourceEquations.AddRange(eq);
		isDirty = true;
	}

	public void RemoveEquation(Exp eq) {
		sourceEquations.Remove(eq);
		isDirty = true;
	}

	public void AddParameter(Param p) {
		parameters.Add(p);
		isDirty = true;
	}

	public void AddParameters(IEnumerable<Param> p) {
		parameters.AddRange(p);
		isDirty = true;
	}

	public void RemoveParameter(Param p) {
		parameters.Remove(p);
		isDirty = true;
	}

	public int CurrentParamsCount() {
		return currentParams.Count;
	}

	public int CurrentEquationsCount() {
		return equations.Count;
	}

	public void Eval(ref double[] B, bool clearDrag) {
		for(int i = 0; i < equations.Count; i++) {
			if(clearDrag && equations[i].IsDrag()) {
				B[i] = 0.0;
				continue;
			}
			B[i] = equations[i].Eval();
		}
	}

	public bool IsConverged(bool checkDrag, bool printNonConverged = false) {
		for(int i = 0; i < equations.Count; i++) {
			if(!checkDrag && equations[i].IsDrag()) {
				continue;
			}
			if(Math.Abs(B[i]) < GaussianMethod.epsilon) continue;
			if(printNonConverged) {
				//Debug.Log("Not converged: " + equations[i].ToString());
				continue;
			}
			return false;
		}
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

	Exp[,] WriteJacobian(List<Exp> equations, List<Param> parameters) {
		UnityEngine.Profiling.Profiler.BeginSample("WriteJacobian");
		//var time = Time.realtimeSinceStartup;
		var depends = equations.Select(eq => eq.DependOnParams()).ToList();
		var allDepends = depends.SelectMany(eq => eq).ToHashSet();
		var allParameters = parameters.ToHashSet();

		int removedCount = 0;
		for(int i = 0; i < parameters.Count; i++) {
			if (!allDepends.Contains(parameters[i])) {
				removedCount++;
				parameters.RemoveAt(i--);
			}
		}
		for(int i = 0; i < equations.Count; i++) {
			if (depends[i].All(d => !allParameters.Contains(d))) {
				removedCount++;
				equations.RemoveAt(i--);
			}
		}
		Debug.Log($"removed params + equs {removedCount}");

		int rows = equations.Count;
		int cols = parameters.Count;
		nzColumns = new List<int>[cols];
		for(int c = 0; c < cols; c++) {
			nzColumns[c] = new();
		}
		nzRows = new List<int>[rows];
		for(int r = 0; r < rows; r++) {
			nzRows[r] = new();
		}


		var J = new Exp[equations.Count, parameters.Count];
		for(int r = 0; r < equations.Count; r++) {
			var eq = equations[r];
			var depend = depends[r];
			for(int c = 0; c < parameters.Count; c++) {
				var u = parameters[c];
				
				if (!depend.Contains(u))
				{
					J[r, c] = Exp.zero;
					continue;
				}
				J[r, c] = eq.Deriv(u);
				nzColumns[c].Add(r);
				nzRows[r].Add(c);
			}
		}
		//Debug.Log("WriteJacobian time " + (Time.realtimeSinceStartup - time) * 1000);
		UnityEngine.Profiling.Profiler.EndSample();
		return J;
	}

	public bool HasDragged() {
		return equations.Any(e => e.IsDrag());
	}

	public void EvalJacobian(Exp[,] J, ref double[,] A, bool clearDrag) {
		UpdateDirty();
		UnityEngine.Profiling.Profiler.BeginSample("EvalJacobian");
		//var time = Time.realtimeSinceStartup;

		int rows = J.GetLength(0);
		int cols = J.GetLength(1);
		for(int r = 0; r < rows; r++) {
			for(int c = 0; c < cols; c++) {
				A[r, c] = 0.0;
			}
			if(clearDrag && equations[r].IsDrag()) {
				continue;
			}
			foreach(int c in nzRows[r]) {
				A[r, c] = J[r, c].Eval();
			}
		}
		UnityEngine.Profiling.Profiler.EndSample();
		//Debug.Log("EvalJacobian time " + (Time.realtimeSinceStartup - time) * 1000);
	}

	public void SolveLeastSquares(double[,] A, double[] B, ref double[] X) {

		// A^T * A * X = A^T * B
		var rows = A.GetLength(0);
		var cols = A.GetLength(1);

		UnityEngine.Profiling.Profiler.BeginSample("SolveLeastSquares: A^T * A");
		//var time = Time.realtimeSinceStartup;

		for(int r = 0; r < rows; r++) {
			for(int c = 0; c < rows; c++) {
				AAT[r, c] = 0.0;
			}
		}

		double Ari = 0.0;
		for(int i = 0; i < cols; i++) {
			for(int r = 0; r < rows; r++) {
				Ari = A[r, i];
				if (Ari == 0.0) continue;
				/*
				for(int c = 0; c < rows; c++) {
					if(A[c, i] == 0) continue;
					AAT[r, c] += Ari * A[c, i];
				}
				*/

				const int u = 16;
				int loop = rows / u;
				int left = rows % u;
				int c = 0;

				while(loop-- != 0) {
					if(A[c +  0, i] != 0) AAT[r, c +  0] += Ari * A[c +  0, i];
					if(A[c +  1, i] != 0) AAT[r, c +  1] += Ari * A[c +  1, i];
					if(A[c +  2, i] != 0) AAT[r, c +  2] += Ari * A[c +  2, i];
					if(A[c +  3, i] != 0) AAT[r, c +  3] += Ari * A[c +  3, i];
					if(A[c +  4, i] != 0) AAT[r, c +  4] += Ari * A[c +  4, i];
					if(A[c +  5, i] != 0) AAT[r, c +  5] += Ari * A[c +  5, i];
					if(A[c +  6, i] != 0) AAT[r, c +  6] += Ari * A[c +  6, i];
					if(A[c +  7, i] != 0) AAT[r, c +  7] += Ari * A[c +  7, i];
					if(A[c +  8, i] != 0) AAT[r, c +  8] += Ari * A[c +  8, i];
					if(A[c +  9, i] != 0) AAT[r, c +  9] += Ari * A[c +  9, i];
					if(A[c + 10, i] != 0) AAT[r, c + 10] += Ari * A[c + 10, i];
					if(A[c + 11, i] != 0) AAT[r, c + 11] += Ari * A[c + 11, i];
					if(A[c + 12, i] != 0) AAT[r, c + 12] += Ari * A[c + 12, i];
					if(A[c + 13, i] != 0) AAT[r, c + 13] += Ari * A[c + 13, i];
					if(A[c + 14, i] != 0) AAT[r, c + 14] += Ari * A[c + 14, i];
					if(A[c + 15, i] != 0) AAT[r, c + 15] += Ari * A[c + 15, i];

					c += u;
				}

				switch(left) {
					case 15: AAT[r, c + 14] += Ari * A[c + 14, i]; goto case 14;
					case 14: AAT[r, c + 13] += Ari * A[c + 13, i]; goto case 13;
					case 13: AAT[r, c + 12] += Ari * A[c + 12, i]; goto case 12;
					case 12: AAT[r, c + 11] += Ari * A[c + 11, i]; goto case 11;
					case 11: AAT[r, c + 10] += Ari * A[c + 10, i]; goto case 10;
					case 10: AAT[r, c +  9] += Ari * A[c +  9, i]; goto case 9;
					case  9: AAT[r, c +  8] += Ari * A[c +  8, i]; goto case 8;
					case  8: AAT[r, c +  7] += Ari * A[c +  7, i]; goto case 7;
					case  7: AAT[r, c +  6] += Ari * A[c +  6, i]; goto case 6;
					case  6: AAT[r, c +  5] += Ari * A[c +  5, i]; goto case 5;
					case  5: AAT[r, c +  4] += Ari * A[c +  4, i]; goto case 4;
					case  4: AAT[r, c +  3] += Ari * A[c +  3, i]; goto case 3;
					case  3: AAT[r, c +  2] += Ari * A[c +  2, i]; goto case 2;
					case  2: AAT[r, c +  1] += Ari * A[c +  1, i]; goto case 1;
					case  1: AAT[r, c +  0] += Ari * A[c +  0, i]; goto case 0;
					case  0: break;	
				}

			}
		}

		//Debug.Log("AAT time " + (Time.realtimeSinceStartup - time) * 1000);
		UnityEngine.Profiling.Profiler.EndSample();

		//time = Time.realtimeSinceStartup;
		GaussianMethod.Solve(AAT, B, ref Z);
		//Debug.Log("GaussianMethod time " + (Time.realtimeSinceStartup - time) * 1000);

		for(int c = 0; c < cols; c++) {
			double sum = 0.0;
			for(int r = 0; r < rows; r++) {
				sum += Z[r] * A[r, c];
			}
			X[c] = sum;
		}

	}

	public void Clear() {
		parameters.Clear();
		currentParams.Clear();
		equations.Clear();
		sourceEquations.Clear();
		isDirty = true;
		UpdateDirty();
	}

	public bool TestRank(out int dof) {
		EvalJacobian(J, ref A, clearDrag:false);
		int rank = GaussianMethod.Rank(A);
		dof = A.GetLength(1) - rank;
		return rank == A.GetLength(0);
	}

	void UpdateDirty() {
		if(isDirty) {
			equations = sourceEquations.Select(e => e.DeepClone()).ToList();
			currentParams = parameters.ToList();
			/*
			foreach(var e in equations) {
				e.ReduceParams(currentParams);
			}*/
			//currentParams = parameters.Where(p => equations.Any(e => e.IsDependOn(p))).ToList();
			subs = SolveBySubstitution();

			J = WriteJacobian(equations, currentParams);
			A = new double[J.GetLength(0), J.GetLength(1)];
			B = new double[equations.Count];
			X = new double[currentParams.Count];
			Z = new double[A.GetLength(0)];
			AAT = new double[A.GetLength(0), A.GetLength(0)];
			oldParamValues = new double[parameters.Count];
			isDirty = false;
			dofChanged = true;
		}
	}

	void BackSubstitution(Dictionary<Param, Param> subs) {
		if(subs == null) return;
		for(int i = 0; i < parameters.Count; i++) {
			var p = parameters[i];
			if(!subs.ContainsKey(p)) continue;
			p.value = subs[p].value;
		}
	}

	Dictionary<Param, Param> SolveBySubstitution() {
		var subs = new Dictionary<Param, Param>();
		var newParams = new HashSet<Param>(currentParams);
		UnityEngine.Profiling.Profiler.BeginSample("SolveBySubstitution");
		//var time = Time.realtimeSinceStartup;

		Param getLastSubstitution(Param p) {
			Param current = p;
			while(subs.ContainsKey(current)) {
				current = subs[current];
				// to break the loops
				if(current == p) {
					// break the loop;
					subs.Remove(current);
					break;
				}
			}
			return current;
		}

		for (int i = 0; i < equations.Count; i++) {
			var eq = equations[i];
			if(!eq.IsSubstitionForm()) continue;
			
			// b замещаем на a
			var a = eq.a.param;
			var b = eq.b.param;

			if(a == b) {
				equations.RemoveAt(i--);
				continue;
			}

			if(Math.Abs(a.value - b.value) > GaussianMethod.epsilon) continue;
			if(!newParams.Contains(b)) {
				var t = a;
				a = b;
				b = t;
			}
			
			// берем  последнее замещение параметра b
			// это делается для того, чтобы сформировать цепочку замещений
			Param last = getLastSubstitution(b);
			
			// замещаем параметром a и метим как замещенный
			subs[last] = a;
			
			// если a замещено
			if(subs.ContainsKey(a)) {
				// берем последнее замещение вхолостую, 
				// тем самым разбиваем циклы если они вдруг появились
				getLastSubstitution(a);
			}
			equations.RemoveAt(i--);
			newParams.Remove(b);
		}

		currentParams = newParams.ToList();

		var backSubs = new Dictionary<Param, Param>();
		foreach(var p in subs.Keys) {
			var last = getLastSubstitution(p);
			if(last == p) continue;
			backSubs[p] = last;
		}

		// замещаем все параметры во всех уравнениях последними замещениями в цепочке замещений
		for(int i = 0; i < equations.Count; i++) {
			var eq = equations[i];
			var depends = eq.DependOnParams();
			foreach(var p in depends) {
				if(backSubs.TryGetValue(p, out var sub)) {
					eq.Substitute(p, sub);
				}
			}
		}

		UnityEngine.Profiling.Profiler.EndSample();
		//Debug.Log("SolveBySubstitution time " + (Time.realtimeSinceStartup - time) * 1000);
		return backSubs;
	}

	public string stats { get; private set; }
	public bool dofChanged { get; private set; }

	public SolveResult Solve() {
		dofChanged = false;
		UpdateDirty();
		StoreParams();
		try {
			int steps = 0;
			do {
				bool isDragStep = steps <= dragSteps;
				Eval(ref B, clearDrag: !isDragStep);
				/*
				if(steps > 0) {
					BackSubstitution(subs);
					return SolveResult.POSTPONE;
				}
				*/
				if(IsConverged(checkDrag: isDragStep)) {
					if(steps > 0) {
						dofChanged = true;
						Debug.Log(String.Format("solved {0} equations with {1} unknowns in {2} steps", equations.Count, currentParams.Count, steps));
					}
					stats = String.Format("eqs:{0}\nunkn: {1}", equations.Count, currentParams.Count);
					BackSubstitution(subs);
					return SolveResult.OKAY;
				}
				EvalJacobian(J, ref A, clearDrag: !isDragStep);
				SolveLeastSquares(A, B, ref X);
				for(int i = 0; i < currentParams.Count; i++) {
					currentParams[i].value -= X[i];
				}
			} while(steps++ <= maxSteps);
			IsConverged(checkDrag: false, printNonConverged: true);
			if(revertWhenNotConverged) {
				RevertParams();
				dofChanged = false;
			}
		} catch (Exception e) {
			Debug.LogError(e.Message);
		}
		return SolveResult.DIDNT_CONVEGE;
	}
}
