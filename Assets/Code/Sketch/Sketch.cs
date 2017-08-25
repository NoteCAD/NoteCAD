using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Sketch : MonoBehaviour {
	List<Entity> entities = new List<Entity>();
	public static Sketch instance;
	public Text distanceText;
	public Text matrixText;
	public Text resultText;
	EquationSystem sys = new EquationSystem();

	private void Start() {
		instance = this;
		PointEntity[] pr = null;
		for(int i = 0; i < 10; i++) {
			var nr = CreateRectangle(new Vector3(i * 5, 0, 0));
			if(pr != null) {
				sys.AddEquation(new Exp(pr[1].x) - nr[0].x);
				sys.AddEquation(new Exp(pr[1].y) - nr[0].y);
			}
			pr = nr;
		}
	}

	PointEntity[] CreateRectangle(Vector3 pos) {
		var p = new PointEntity[4];
		for(int i = 0; i < p.Length; i++) {
			p[i] = CreatePoint();
			sys.AddParameter(p[i].x);
			sys.AddParameter(p[i].y);
			p[i].x.name = "x" + i.ToString();
			p[i].y.name = "y" + i.ToString();
		}

		p[0].SetPosition(new Vector3(0, 0, 0));
		p[1].SetPosition(new Vector3(5, 0, 0));
		p[2].SetPosition(new Vector3(5, 10, 0));
		p[3].SetPosition(new Vector3(0, 10, 0));
		for(int i = 0; i < p.Length; i++) {
			p[i].SetPosition(p[i].GetPosition() + pos);
		}
		
		sys.AddEquation((p[0].GetPositionExp() - p[1].GetPositionExp()).Magnitude() - 5.0);
		sys.AddEquation((p[1].GetPositionExp() - p[2].GetPositionExp()).Magnitude() - 10.0);
		sys.AddEquation((p[2].GetPositionExp() - p[3].GetPositionExp()).Magnitude() - 5.0);
		sys.AddEquation(DirCos(p[0].PE() - p[1].PE(), p[2].PE() - p[1].PE()));
		sys.AddEquation(DirCos(p[1].PE() - p[2].PE(), p[3].PE() - p[2].PE()));
		return p;
	}


	Exp DirCos(ExpVector a, ExpVector b) {
		return ExpVector.Dot(a, b) / (a.Magnitude() * b.Magnitude());
	}

	float DirCos(Vector3 a, Vector3 b) {
		return Vector3.Dot(a, b) / (a.magnitude * b.magnitude);
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
		var result = sys.Solve();
		resultText.text = result.ToString();
		if(entities.Count > 1 && entities[0] is PointEntity && entities[1] is PointEntity) {
			var p0 = entities[0] as PointEntity;
			var p1 = entities[1] as PointEntity;

			var distance = ExpVector.Distance(p0.GetPositionExp(), p1.GetPositionExp());
			distanceText.text = distance.Eval().ToString();
		}
	}
}
