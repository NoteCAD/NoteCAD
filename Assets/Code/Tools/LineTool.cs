using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineTool : Tool {

	LineEntity current;

	protected override void OnMouseDown(Vector3 pos, Entity entity) {
		var newLine = new LineEntity(Sketch.instance);
		newLine.p0.SetPosition(pos);
		newLine.p1.SetPosition(pos);

		if(current != null) {
			current.p1.SetPosition(pos);
			new PointsCoincident(current.sketch, current.p1, newLine.p0);
		}
		current = newLine;
	}

	protected override void OnMouseMove(Vector3 pos, Entity entity) {
		if(current != null) {
			current.p1.SetPosition(pos);
		}
	}

	protected override void OnDeactivate() {
		current = null;
	}

}
