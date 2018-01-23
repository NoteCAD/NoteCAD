using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointsCoincidentTool : Tool {

	PointEntity p0;

	protected override void OnMouseDown(Vector3 pos, ICADObject entity) {
		if(entity == null) return;
		if(!(entity is PointEntity)) return;
		var p = entity as PointEntity;
		if(p0 != null) {
			var c = new PointsCoincident(DetailEditor.instance.currentSketch.GetSketch()	, p0, p);
			p0 = null;
		} else {
			p0 = p;
		}
	}

	protected override void OnDeactivate() {
		p0 = null;
	}

	protected override string OnGetDescription() {
		return "hover and click two different points to constrain them to be coincident.";
	}

}
