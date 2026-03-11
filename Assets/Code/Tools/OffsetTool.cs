using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NoteCAD;

public class OffsetTool : Tool {

	OffsetTool() {
		enableHoverFilter = true;
	}

	protected override bool OnTryHover(Constraint c) {
		return false;
	}

	protected override bool OnTryHover(IEntity e) {
		return e is ISegmentaryEntity || e is ILoopEntity;
	}

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		//click = pos;
		var entity = sko as IEntity;
		if(entity == null) return;

		editor.PushUndo();
		var offset = SpawnEntity(new OffsetEntity(DetailEditor.instance.currentSketch.GetSketch()));
		offset.source = entity;
		offset.p0.SetPosition(offset.PointOn(0.0).Eval());
		offset.p1.SetPosition(offset.PointOn(1.0).Eval());
	}

	protected override string OnGetDescription() {
		return "click an entity to create offset";
	}

}
