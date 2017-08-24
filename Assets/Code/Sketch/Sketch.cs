using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Sketch : MonoBehaviour {
	List<Entity> entities = new List<Entity>();
	public static Sketch instance;
	public Text distanceText;
	public Text matrixText;
	public Text resultText;
	NewtonSolver solver = new NewtonSolver();
	EquationSystem sys = new EquationSystem();

	private void Start() {
		instance = this;
		/*
		double[,] A = {
			{ 3.01,  2, -5 },
			{ 3.01,  2, -5 },
			{ 2, -1,  3 },
			{ 1,  2, -1 },
			{ 1,  2, -1 },
		};

		double[] B = {
			-1,
			-1,
			 13,
			 8.9,
			 9.1,
		};

		double[] X = new double[3];

		NewtonSolver.SolveLeastSquares(A, B, ref X);
		matrixText.text = A.Print();
		resultText.text = X.Print();
		*/
		PointEntity[] p = new PointEntity[4];
		for(int i = 0; i < p.Length; i++) {
			p[i] = CreatePoint();
			sys.unknowns.Add(p[i].x);
			sys.unknowns.Add(p[i].y);
			p[i].x.value = Random.value * 10.0;
			p[i].y.value = Random.value * 10.0;
		}
		
		sys.equations.Add((p[0].GetPositionExp() - p[1].GetPositionExp()).Magnitude() - 5.0);
		sys.equations.Add((p[1].GetPositionExp() - p[2].GetPositionExp()).Magnitude() - 10.0);
		sys.equations.Add((p[2].GetPositionExp() - p[3].GetPositionExp()).Magnitude() - 5.0);
		sys.equations.Add((p[2].GetPositionExp() - p[3].GetPositionExp()).Magnitude() - 5.0);
		sys.equations.Add(DirCos(p[0].PE() - p[1].PE(), p[2].PE() - p[1].PE()));
		sys.equations.Add(DirCos(p[1].PE() - p[2].PE(), p[3].PE() - p[2].PE()));

		//sys.equations.Add((p[3].GetPositionExp() - p[1].GetPositionExp()).Magnitude() - 10.0);
	}

	Exp DirCos(ExpVector a, ExpVector b) {
		return ExpVector.Dot(a, b) / (a.Magnitude() * b.Magnitude());
	}

	public PointEntity CreatePoint() {
		return new PointEntity(this);
	}

	public void CreateLine() {
		new LineEntity(this);
	}

	public void AddEntity(Entity e) {
		entities.Add(e);
	}

	private void Update() {
		solver.Solve(sys);
		if(entities.Count > 1 && entities[0] is PointEntity && entities[1] is PointEntity) {
			var p0 = entities[0] as PointEntity;
			var p1 = entities[1] as PointEntity;

			var distance = ExpVector.Distance(p0.GetPositionExp(), p1.GetPositionExp());
			distanceText.text = distance.Eval().ToString();
		}
	}
}
