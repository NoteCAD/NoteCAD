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
		var feature = DetailEditor.instance.activeFeature as SliceFeature;

		message = "Generating GCode...";
		StartCoroutine(feature.GenerateGCode(
			progress => {
				message = progress.stage + " " + progress.current + "/" + progress.total + "(" + Mathf.Floor((float)progress.current / progress.total * 100f) + "%)";
				//Debug.Log(message);
			},
			data => {
				NoteCADJS.SaveData(data, "NoteCAMFile.gcode", "gcode");
				message = "";
				StopTool();
			}
		));
	}
}
