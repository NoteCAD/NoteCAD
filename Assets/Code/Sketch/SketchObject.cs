using System.Collections.Generic;

public class SketchObject {

	Sketch sk;
	public Sketch sketch { get { return sk; } }

	public SketchObject(Sketch sketch) {
		sk = sketch;
	}

	public virtual IEnumerable<Param> parameters { get { yield break; } }
	public virtual IEnumerable<Exp> equations { get { yield break; } }
}
