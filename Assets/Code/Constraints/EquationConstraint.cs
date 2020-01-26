using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

[Serializable]
public class EquationConstraint : ValueConstraint {
	
	public EquationConstraint(Sketch sk) : base(sk) { }
	public EquationConstraint(Sketch sk, Id id) : base(sk, id) { }

	public override IEnumerable<Exp> equations {
		get {
			if(expression.Exist() && !reference) {
				yield return expression.expression;
			}
		}
	}

	public override ValueUnits units => ValueUnits.ARBITRARY;
}
