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
			current.p1.isSelectable = true;
			current.p0.isSelectable = true;
			current.isSelectable = true;
			new PointsCoincident(current.sketch, current.p1, newLine.p0);
		}
		current = newLine;
		current.isSelectable = false;
		current.p0.isSelectable = false;
		current.p1.isSelectable = false;
	}

	protected override void OnMouseMove(Vector3 pos, Entity entity) {
		if(current != null) {
			current.p1.SetPosition(pos);
		}
	}

	protected override void OnDeactivate() {
		if(current != null) {
			current.isSelectable = true;
			current.p0.isSelectable = true;
			current.p1.isSelectable = true;
		}
		current = null;
	}

}
