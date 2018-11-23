using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplitTool : Tool {
	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		if(sko == null) return;
		if(!(sko is Entity)) return;
		if(!(sko is ISegmentaryEntity || sko is ILoopEntity)) return;
		editor.PushUndo();
		var e = sko as Entity;
		var part = e.Split(pos);
		if(e is ISegmentaryEntity) {
			var s0 = e as ISegmentaryEntity;
			var s1 = part as ISegmentaryEntity;
			e.sketch.ReplaceEntityInConstraints(s0.end, s1.end);
			new PointsCoincident(e.sketch, s0.end, s1.begin);
		}
	}
}
