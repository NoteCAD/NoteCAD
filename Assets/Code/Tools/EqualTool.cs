using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EqualTool : Tool {

	IEntity l0;
	ValueConstraint c0;

	EqualTool() {
		enableHoverFilter = true;
	}

	protected override bool OnTryHover(Constraint c) {
		return l0 == null && c is AngleConstraint && c != c0;
	}

	protected override bool OnTryHover(IEntity e) {
		return c0 == null && (e.Radius() != null || e.Length() != null) && !e.IsSameAs(l0);
	}

	[System.Serializable]
	class Options { 
		public bool preserveRatio = false;
	}

	Options options = new Options();

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		var vc = sko as AngleConstraint;
		if(vc != null) {
			if(c0 == null) {
				c0 = vc;
			} else {
				editor.PushUndo();
				//c0.reference = true;
				vc.reference = true;
				new EqualValue(DetailEditor.instance.currentSketch.GetSketch(), c0, vc);
				c0 = null;
			}
		}
		if(c0 != null) return;
		var entity = sko as IEntity;
		if(entity == null) return;

		if(entity.Radius() == null && entity.Length() == null) return;
		if(l0 != null) {
			editor.PushUndo();
			var c = new Equal(DetailEditor.instance.currentSketch.GetSketch(), l0, entity);
			if(options.preserveRatio) c.Satisfy();
			l0 = null;
		} else {
			l0 = entity;
		}
	}

	protected override void OnActivate() {
		Inspect(options);
	}

	protected override void OnDeactivate() {
		l0 = null;
		c0 = null;
	}

	protected override string OnGetDescription() {
		return "hover and click two different entities or two angle constraints.";
	}

}
