using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinearArrayTool : Tool {
	protected override void OnActivate() {
		StopTool();
		if(DetailEditor.instance.currentWorkplane == null) return;
		editor.PushUndo();
		var feature = new LinearArrayFeature();
		feature.source = DetailEditor.instance.currentWorkplane;
		DetailEditor.instance.AddFeature(feature); 
		DetailEditor.instance.ActivateFeature(feature);
	}

}
