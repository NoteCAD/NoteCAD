using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using NoteCAD;

[Serializable]
public class EquationConstraint : ValueConstraint {
	
	public EquationConstraint(Sketch sk) : base(sk) {
		expression.isEquation = true;
	}
	public EquationConstraint(Sketch sk, Id id) : base(sk, id) {
		expression.isEquation = true;
	}

	public override ValueUnits units => ValueUnits.ARBITRARY;
}
