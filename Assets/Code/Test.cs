using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Exp paramExp = new Param("x");
		var expression = (new Exp(1.0) + 5.0 - 10.0) * 2.0 + Exp.Cos(paramExp + 3 * 2 - 6) * (2 + paramExp);
		Debug.Log(expression.Print() + " = " + expression.Eval().ToString());
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
