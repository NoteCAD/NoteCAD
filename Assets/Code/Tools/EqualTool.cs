using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EqualTool : Tool {

	LineEntity l0;

	protected override void OnMouseDown(Vector3 pos, SketchObject sko) {
		var entity = sko as Entity;
		if(entity == null) return;

		if(!(entity is LineEntity)) return;
		var l = entity as LineEntity;
		if(l0 != null) {
			new EqualLineLine(entity.sketch, l0, l);
			l0 = null;
		} else {
			l0 = l;
		}
	}

	protected override void OnDeactivate() {
		l0 = null;
	}

}
