using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class ExportSTLTool : Tool {

	protected override void OnActivate() {
		StopTool();
		var data = DetailEditor.instance.ExportSTL(); 
		NoteCADJS.SaveData(data, "NoteCADExport.stl", "stl");
	}
}
