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

	private void Start() {
		instance = this;

		double[,] A = {
			{ 3,  2, -5 },
			{ 2, -1,  3 },
			{ 1,  2, -1 }
		};

		double[] B = {
			-1,
			 13,
			 9
		};

		double[] X = new double[3];

		GaussianMethod.Solve(A, B, ref X);
		matrixText.text = A.Print();
		resultText.text = X.Print();

	}

	public void CreatePoint() {
		new PointEntity(this);
	}

	public void CreateLine() {
		new LineEntity(this);
	}

	public void AddEntity(Entity e) {
		entities.Add(e);
	}

	private void Update() {
		if(entities.Count > 1 && entities[0] is PointEntity && entities[1] is PointEntity) {
			var p0 = entities[0] as PointEntity;
			var p1 = entities[1] as PointEntity;

			var distance = ExpVector.Distance(p0.GetPositionExp(), p1.GetPositionExp());
			distanceText.text = distance.Eval().ToString();
		}
	}
}
