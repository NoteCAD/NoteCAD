using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EquationSystem  {

	public enum SolveResult {
		OKAY,
		DIDNT_CONVEGE,
		REDUNDANT,
		POSTPONE,
		INTERNAL_FAILURE,
		JUMP
	}

	bool isDirty = true;

	public bool IsDirty { get { return isDirty; } }
	public int maxSteps = 20;
	public int dragSteps = 3;
	public bool revertWhenNotConverged = true;
	public bool avoidJumping = true;
	public double jumpFactor = 20.0;

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
	public IEnumerable<Param> parametersList => parameters.AsEnumerable();

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
	
	double GetMaxParamChange() {
		double result = 0.0;
		for(int i = 0; i < parameters.Count; i++) {
			result = Math.Max(Math.Abs(parameters[i].value - oldParamValues[i]), result);
		}
		return result;
	}

	Exp[,] WriteJacobian(List<Exp> equations, List<Param> parameters) {
		UnityEngine.Profiling.Profiler.BeginSample("WriteJacobian");
		//var time = Time.realtimeSinceStartup;
		var depends = equations.Select(eq => eq.DependOnParams()).ToList();
		/*
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
		*/

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
				var v = J[r, c].Eval();
				if (double.IsNaN(v)) {
					// for some reason, may be it will help to jump out of NaN
					// while testing it showed up better behaviour
					v = 1.0;
				}
				A[r, c] = v;
			}
		}
		UnityEngine.Profiling.Profiler.EndSample();
		//Debug.Log("EvalJacobian time " + (Time.realtimeSinceStartup - time) * 1000);
	}

	public void MakeAAT(double[,] A, double[,] AAT) {
		// A^T * A * X = A^T * B
		var rows = A.GetLength(0);
		var cols = A.GetLength(1);
		UnityEngine.Profiling.Profiler.BeginSample("MakeAAT: A^T * A");
		//var time = Time.realtimeSinceStartup;

		//Array.Clear(AAT, 0, AAT.Length);
		
		for(int r = 0; r < rows; r++) {
			for(int c = 0; c < rows; c++) {
				AAT[r, c] = 0.0;
			}
		}

		for(int i = 0; i < cols; i++) {
			var nzColumn = nzColumns[i];
			foreach(int r in nzColumn) {
				foreach(int c in nzColumn) {
					AAT[r, c] += A[r, i] * A[c, i];
				}
			}
		}

		//Debug.Log($"MakeAAT({rows}x{cols}) time " + (Time.realtimeSinceStartup - time) * 1000);
		UnityEngine.Profiling.Profiler.EndSample();
	}

	public void SolveLeastSquares(double[,] A, double[] B, ref double[] X) {

		MakeAAT(A, AAT);
		GaussianMethod.Solve(AAT, B, ref Z);

		var rows = A.GetLength(0);
		var cols = A.GetLength(1);

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
			// не можем замещать, так как не имеем параметра под контролем системы уравнений
			if(!newParams.Contains(b)) {
				continue;
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
		double deviation = 0.0;
		try {
			int steps = 0;
			do {
				bool isDragStep = steps <= dragSteps;
				Eval(ref B, clearDrag: !isDragStep);
				if (steps == 0 && avoidJumping) {
					deviation = B.Aggregate(0.0, (a, b) => Math.Max(a, Math.Abs(b)));
				}
				/*
				if(steps > 0) {
					BackSubstitution(subs);
					return SolveResult.POSTPONE;
				}
				*/
				if(IsConverged(checkDrag: isDragStep)) {
					if(avoidJumping) {
						var maxChange = GetMaxParamChange();
						if(maxChange > jumpFactor * deviation) {
							Debug.Log(String.Format("check jumping: inital deviation: {0}, max param change {1}", deviation, maxChange));
							if(revertWhenNotConverged) {
								RevertParams();
								dofChanged = false;
							}
							return SolveResult.JUMP;

						}
					}
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
					if (double.IsNaN(X[i])) {
						continue;
					}
					if(X[i] > jumpFactor * deviation) {
						bool stop = true;
					}
					currentParams[i].value -= X[i];
				}
			} while(steps++ <= maxSteps);
			//IsConverged(checkDrag: false, printNonConverged: true);
			if(revertWhenNotConverged) {
				RevertParams();
				dofChanged = false;
			}
		} catch (Exception e) {
			Debug.LogError(e.Message);
			if(revertWhenNotConverged) {
				RevertParams();
				dofChanged = false;
			}
			return SolveResult.INTERNAL_FAILURE;
		}
		return SolveResult.DIDNT_CONVEGE;
	}
}
