using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SketchTool : Tool {
	protected override void OnActivate() {
		StopTool();
		var feature = new SketchFeature();
		DetailEditor.instance.AddFeature(feature); 
		DetailEditor.instance.ActivateFeature(feature);
	}
}
