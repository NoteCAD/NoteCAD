using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EqualTool : Tool {

	IEntity l0;
	ValueConstraint c0;

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
			new Equal(DetailEditor.instance.currentSketch.GetSketch(), l0, entity);
			l0 = null;
		} else {
			l0 = entity;
		}
	}

	protected override void OnDeactivate() {
		l0 = null;
		c0 = null;
	}

	protected override string OnGetDescription() {
		return "hover and click two different entities or two angle constraints.";
	}

}
