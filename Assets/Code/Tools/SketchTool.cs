using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SketchTool : Tool {
	IEntity p;
	IEntity u;
	IEntity v;

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		if(sko == null) return;
		if(p == null) {
			p = sko as IEntity;
		} else if(u == null) {
			u = sko as IEntity;
		} else if(v == null) {
			v = sko as IEntity;
			StopTool();
			var feature = new SketchFeature();
			feature.u = u;
			feature.v = v;
			feature.p = p;
			feature.source = DetailEditor.instance.activeFeature;
			DetailEditor.instance.AddFeature(feature); 
			DetailEditor.instance.ActivateFeature(feature);
		}
	}

	protected override void OnDeactivate() {
		u = null;
		v = null;
		p = null;
	}

	protected override void OnActivate() {
	}
}
