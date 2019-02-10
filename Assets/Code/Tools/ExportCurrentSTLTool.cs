using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class ExportCurrentSTLTool : Tool {

	protected override void OnActivate() {
		StopTool();
		var data = DetailEditor.instance.ExportCurrentSTL();
		if(data == "") return;
		NoteCADJS.SaveData(data, "NoteCADExport.stl", "stl");
	}
}
