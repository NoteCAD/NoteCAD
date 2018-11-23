using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using g3;
using gs;
using System.IO;
using gs.info;
using System.Text;

public class SliceTool : Tool {
	string message;

	protected override string OnGetDescription() {
		return message;
	}

	protected override void OnActivate() {
		StopTool();
		editor.PushUndo();
		var feature = new SliceFeature(DetailEditor.instance.mesh);
		feature.source = DetailEditor.instance.activeFeature;
		DetailEditor.instance.AddFeature(feature); 
		DetailEditor.instance.ActivateFeature(feature);
	}
}
