using System.Collections.Generic;

public class Constraint : SketchObject {
	public Constraint(Sketch sk) : base(sk) {
		sk.AddConstraint(this);
	}
}

public class ValueConstraint : Constraint {

	protected Param value = new Param("value");
	public bool reference;

	public ValueConstraint(Sketch sk) : base(sk) {
	}

	public override IEnumerable<Param> parameters {
		get {
			if(!reference) yield break;
			yield return value;
		}
	}

}

