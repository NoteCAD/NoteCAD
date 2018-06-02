using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using g3;
using gs;
using System.IO;
using gs.info;
using System.Text;

public class SaveGCodeTool : Tool {
	string message;

	protected override string OnGetDescription() {
		return message;
	}

	protected override void OnActivate() {
		if(!(DetailEditor.instance.activeFeature is SliceFeature)) {
			message = "SliceFeature should be activated!";
			return;
		}
		message = "";
		StopTool();
		var feature = DetailEditor.instance.activeFeature as SliceFeature;

		var data = feature.GenerateGCode();
		NoteCADJS.SaveData(data, "NoteCAMFile.gcode");
	}
}
