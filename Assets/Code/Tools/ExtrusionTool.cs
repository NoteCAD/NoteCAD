using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExtrusionTool : Tool {
	protected override void OnActivate() {
		StopTool();
		if(DetailEditor.instance.currentSketch == null) return;
		var feature = new ExtrusionFeature();
		feature.source = DetailEditor.instance.currentSketch;
		DetailEditor.instance.AddFeature(feature); 
		DetailEditor.instance.ActivateFeature(feature);
	}
}
