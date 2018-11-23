using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RevolveTool : Tool {
	IEntity p;
	IEntity axis;

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		if(sko == null) return;
		var entity = sko as IEntity;
		if(entity == null) return;
		if(p == null) {
			if(entity.type == IEntityType.Point) {
				p = entity;
			}
		} else if(axis == null) {
			if(entity.type == IEntityType.Line) {
				axis = entity;
				StopTool();
				editor.PushUndo();
				var feature = new RevolveFeature();
				DetailEditor.instance.AddFeature(feature); 
				feature.axis = axis;
				feature.origin = p;
				feature.source = DetailEditor.instance.activeFeature;
				DetailEditor.instance.ActivateFeature(feature);
			}
		}
	}

	protected override void OnDeactivate() {
		axis = null;
		p = null;
	}

	protected override void OnActivate() {
		if(DetailEditor.instance.currentWorkplane == null) {
			StopTool();
		}
	}

	protected override string OnGetDescription() {
		return "click some point first and then click line to define an axis. The point will be origin of the axis, and line will be direction of an axis.";
	}

}
