using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceTool : Tool {


	PointEntity p0;

	protected override void OnMouseDown(Vector3 pos, Entity entity) {
		if(entity == null) return;

		if(p0 == null && entity is LineEntity) {
			var line = entity as LineEntity;
			new PointsDistance(line.sketch, line.p0, line.p1);
		}

		if(!(entity is PointEntity)) return;
		var p = entity as PointEntity;
		if(p0 != null) {
			var c = new PointsDistance(entity.sketch, p0, p);
			p0 = null;
		} else {
			p0 = p;
		}
	}

	protected override void OnDeactivate() {
		p0 = null;
	}
}
