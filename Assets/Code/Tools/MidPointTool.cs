using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MidPointTool : Tool {

	IEntity p0;

	protected override void OnMouseDown(Vector3 pos, ICADObject ico) {
		IEntity entity = ico as IEntity;
		if(entity == null) return;
		if(p0 != null) {
			if(entity.type != IEntityType.Point) {
				editor.PushUndo();
				var pOn = new PointOn(DetailEditor.instance.currentSketch.GetSketch(), p0, entity);
				pOn.reference = false;
				pOn.SetValue(0.5);
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
		return "hover and click point and arbitarary entity";
	}

}
