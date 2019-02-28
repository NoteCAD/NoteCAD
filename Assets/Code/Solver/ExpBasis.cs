using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class ExpBasis {
	Param px, py, pz;
	Param ux, uy, uz;
	Param vx, vy, vz;
	Param nx, ny, nz;

	public ExpVector u { get; private set; }
	public ExpVector v { get; private set; }
	public ExpVector n { get; private set; }
	public ExpVector p { get; private set; }

	public ExpBasis() {
		px = new Param("ux", 0.0);
		py = new Param("uy", 0.0);
		pz = new Param("uz", 0.0);

		ux = new Param("ux", 1.0);
		uy = new Param("uy", 0.0);
		uz = new Param("uz", 0.0);

		vx = new Param("vx", 0.0);
		vy = new Param("vy", 1.0);
		vz = new Param("vz", 0.0);

		nx = new Param("nx", 0.0);
		ny = new Param("ny", 0.0);
		nz = new Param("nz", 1.0);

		p = new ExpVector(px, py, pz);
		u = new ExpVector(ux, uy, uz);
		v = new ExpVector(vx, vy, vz);
		n = new ExpVector(nx, ny, nz);
	}

	public Matrix4x4 matrix {
		get {
			return UnityExt.Basis(u.Eval(), v.Eval(), n.Eval(), p.Eval());
		}
	}

	public IEnumerable<Param> parameters {
		get {
			yield return ux;
			yield return uy;
			yield return uz;
			yield return vx;
			yield return vy;
			yield return vz;
			yield return nx;
			yield return ny;
			yield return nz;
			yield return px;
			yield return py;
			yield return pz;
		}
	}

	public override string ToString() {
		string result = "";
		foreach(var p in parameters) {
			result += p.value.ToStr() + " ";
		}
		return result;
	}

	public void FromString(string str) {
		char[] sep = { ' ' };
		var values = str.Split(sep, StringSplitOptions.RemoveEmptyEntries);
		int i = 0;
		foreach(var p in parameters) {
			p.value = values[i].ToDouble();
			i++;
		}
	}

	public ExpVector TransformPosition(ExpVector pos) {
		return pos.x * u + pos.y * v + pos.z * n + p;
	}

	public ExpVector TransformDirection(ExpVector dir) {
		return dir.x * u + dir.y * v + dir.z * n;
	}

	public void GenerateEquations(EquationSystem sys) {
		sys.AddParameters(parameters);

		sys.AddEquation(u.Magnitude() - 1.0);
		sys.AddEquation(v.Magnitude() - 1.0);

		var cross = ExpVector.Cross(u, v);
		var dot = ExpVector.Dot(u, v);
		sys.AddEquation(Exp.Atan2(cross.Magnitude(), dot) - Math.PI / 2);
		sys.AddEquation(n - ExpVector.Cross(u, v));
	}

	public bool changed {
		get {
			return parameters.Any(p => p.changed);
		}
	}

	public void markUnchanged() {
		parameters.ForEach(pp => pp.changed = false);
	}
}

public class ExpBasis2d {
	Param px, py;
	Param ux, uy;
	Param vx, vy;

	public ExpVector u { get; private set; }
	public ExpVector v { get; private set; }
	public ExpVector p { get; private set; }

	public ExpBasis2d() {
		px = new Param("ux", 0.0);
		py = new Param("uy", 0.0);

		ux = new Param("ux", 1.0);
		uy = new Param("uy", 0.0);

		vx = new Param("vx", 0.0);
		vy = new Param("vy", 1.0);

		p = new ExpVector(px, py, 0.0);
		u = new ExpVector(ux, uy, 0.0);
		v = new ExpVector(vx, vy, 0.0);
	}

	public void SetPosParams(Param x, Param y) {
		px = x;
		py = y;
		p.x = px;
		p.y = py;
	}

	/*
	public Matrix4x4 matrix {
		get {
			return UnityExt.Basis(u.Eval(), v.Eval(), n.Eval(), p.Eval());
		}
	}*/

	public IEnumerable<Param> parameters {
		get {
			yield return ux;
			yield return uy;
			yield return vx;
			yield return vy;
			yield return px;
			yield return py;
		}
	}

	public override string ToString() {
		string result = "";
		foreach(var p in parameters) {
			result += p.value.ToStr() + " ";
		}
		return result;
	}

	public void FromString(string str) {
		char[] sep = { ' ' };
		var values = str.Split(sep, StringSplitOptions.RemoveEmptyEntries);
		int i = 0;
		foreach(var p in parameters) {
			p.value = values[i].ToDouble();
			i++;
		}
	}

	public ExpVector TransformPosition(ExpVector pos) {
		return pos.x * u + pos.y * v + p;
	}

	public ExpVector TransformDirection(ExpVector dir) {
		return dir.x * u + dir.y * v;
	}

	public void GenerateEquations(EquationSystem sys) {
		sys.AddParameters(parameters);
		sys.AddEquations(equations);
	}

	public IEnumerable<Exp> equations {
		get {
			yield return u.Magnitude() - 1.0;
			yield return v.Magnitude() - 1.0;
			var cross = ExpVector.Cross(u, v);
			var dot = ExpVector.Dot(u, v);
			yield return Exp.Atan2(cross.Magnitude(), dot) - Math.PI / 2;
		}
	}

	public bool changed {
		get {
			return parameters.Any(p => p.changed);
		}
	}

	public void markUnchanged() {
		parameters.ForEach(pp => pp.changed = false);
	}
}

