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

	static Exp[,] WriteJacobian(List<Exp> equations, List<Param> parameters) {
		var J = new Exp[equations.Count, parameters.Count];
		for(int r = 0; r < equations.Count; r++) {
			var eq = equations[r];
			for(int c = 0; c < parameters.Count; c++) {
				var u = parameters[c];
				J[r, c] = eq.Deriv(u);
				/*
				if(!J[r, c].IsZeroConst()) {
					Debug.Log(J[r, c].ToString() + "\n");
				}
				*/
			}
		}
		return J;
	}

	public bool HasDragged() {
		return equations.Any(e => e.IsDrag());
	}

	public void EvalJacobian(Exp[,] J, ref double[,] A, bool clearDrag) {
		UpdateDirty();
		UnityEngine.Profiling.Profiler.BeginSample("EvalJacobian");
		for(int r = 0; r < J.GetLength(0); r++) {
			if(clearDrag && equations[r].IsDrag()) {
				for(int c = 0; c < J.GetLength(1); c++) {
					A[r, c] = 0.0;
				}
				continue;
			}
			for(int c = 0; c < J.GetLength(1); c++) {
				A[r, c] = J[r, c].Eval();
			}
		}
		UnityEngine.Profiling.Profiler.EndSample();
	}

	public void SolveLeastSquares(double[,] A, double[] B, ref double[] X) {

		// A^T * A * X = A^T * B
		var rows = A.GetLength(0);
		var cols = A.GetLength(1);

		UnityEngine.Profiling.Profiler.BeginSample("SolveLeastSquares: A^T * A");
		var time = Time.realtimeSinceStartup;
		for(int r = 0; r < rows; r++) {
			for(int c = 0; c < rows; c++) {
				double sum = 0.0;
				for(int i = 0; i < cols; i++) {
					if(A[c, i] == 0 || A[r, i] == 0) continue;
					sum += A[r, i] * A[c, i];
				}
				AAT[r, c] = sum;
			}
		}
		//Debug.Log("AAT time " + (Time.realtimeSinceStartup - time) * 1000);
		UnityEngine.Profiling.Profiler.EndSample();

		GaussianMethod.Solve(AAT, B, ref Z);

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

		for(int i = 0; i < equations.Count; i++) {
			var eq = equations[i];
			if(!eq.IsSubstitionForm()) continue;
			var a = eq.GetSubstitutionParamA();
			var b = eq.GetSubstitutionParamB();
			if(Math.Abs(a.value - b.value) > GaussianMethod.epsilon) continue;
			if(!currentParams.Contains(b)) {
				var t = a;
				a = b;
				b = t;
			}
			// TODO: Check errors
			//if(!parameters.Contains(b)) {
			//	continue;
			//}

			foreach(var k in subs.Keys.ToList()) {
				if(subs[k] == b) {
					subs[k] = a;
				}
			}
			subs[b] = a;
			equations.RemoveAt(i--);
			currentParams.Remove(b);

			for(int j = 0; j < equations.Count; j++) {
				equations[j].Substitute(b, a);
			}
		}
		return subs;
	}

	public string stats { get; private set; }
	public bool dofChanged { get; private set; }

	public SolveResult Solve() {
		dofChanged = false;
		UpdateDirty();
		StoreParams();
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
		return SolveResult.DIDNT_CONVEGE;
	}
}
