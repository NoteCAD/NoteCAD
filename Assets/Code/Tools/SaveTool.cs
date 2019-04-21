using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;

public class SaveTool : Tool {

	[Serializable]
	class Options {
		SaveTool tool;
		public Options(SaveTool t) {
			tool = t;
		}
		public string specifyFilename;
		[RuntimeInspectorNamespace.RuntimeInspectorButton("Save", false, RuntimeInspectorNamespace.ButtonVisibility.InitializedObjects)]
		void Save() {
			if(specifyFilename != "") {
				tool.editor.GetDetail().name = specifyFilename;
				tool.Save();
			}
		}
	}
	
	Options options;

	public SaveTool() {
		options = new Options(this);
	}

	public void Save() {
		var data = DetailEditor.instance.WriteXml(); 
		NoteCADJS.SaveData(data, editor.GetDetail().name + ".xml", "xml");
		StopTool();
	}

	protected override void OnActivate() {
		if(editor.GetDetail().name == "") {
			options.specifyFilename = "";
			Inspect(options);
		}
	}
}
