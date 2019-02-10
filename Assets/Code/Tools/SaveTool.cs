using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class SaveTool : Tool {

	protected override void OnActivate() {
		StopTool();
		var data = DetailEditor.instance.WriteXml(); 
		NoteCADJS.SaveData(data, "NoteCADFile.xml", "xml");
	}
}
