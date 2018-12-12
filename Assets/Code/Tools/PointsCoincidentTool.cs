using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointsCoincidentTool : Tool {

	IEntity p0;

	protected override void OnMouseDown(Vector3 pos, ICADObject ico) {
		IEntity entity = ico as IEntity;
		if(entity == null) return;
		if(p0 != null) {
			if(entity.type == IEntityType.Point) {
				PointEntity pt0 = p0 as PointEntity;
				PointEntity pt1 = entity as PointEntity;

				if(!p0.IsSameAs(entity) && (pt0 == null || pt1 == null || !pt0.IsCoincidentWith(pt1))) {
					editor.PushUndo();
					new PointsCoincident(DetailEditor.instance.currentSketch.GetSketch(), p0, entity);
				}
			} else {
				editor.PushUndo();
				new PointOn(DetailEditor.instance.currentSketch.GetSketch(), p0, entity);
			}
			p0 = null;
		} else if(entity.type == IEntityType.Point) {
			p0 = entity;
		}
	}

	protected override void OnDeactivate() {
		p0 = null;
	}

	protected override string OnGetDescription() {
		return "hover and click point and arbitrary entity for constraining them to be coincident.";
	}

}
