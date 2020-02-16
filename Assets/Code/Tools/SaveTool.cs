using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;

public class SaveTool : Tool {

	public enum FileFormat {
		Text,
		Binary
	};

	[Serializable]
	class Options {
		/*public */FileFormat format = FileFormat.Text;
		SaveTool tool;
		public Options(SaveTool t) {
			tool = t;
		}
		public string specifyFilename;
		[RuntimeInspectorNamespace.RuntimeInspectorButton("Save", false, RuntimeInspectorNamespace.ButtonVisibility.InitializedObjects)]
		void Save() {
			if(specifyFilename != "") {
				tool.editor.GetDetail().name = specifyFilename;
				tool.Save(format);
			}
		}
	}
	
	Options options;

	public SaveTool() {
		options = new Options(this);
	}

	public void Save(FileFormat format) {
		switch(format) {
			case FileFormat.Text: {
				var data = DetailEditor.instance.WriteXml();
				NoteCADJS.SaveData(data, editor.GetDetail().name + ".ncad", "ncad");
			} break;
			case FileFormat.Binary: {
				var data = DetailEditor.instance.GetDetail().WriteXmlAsBinary();
				NoteCADJS.SaveBinaryData(data, editor.GetDetail().name + ".notecad", "notecad");
			} break;
		}
		StopTool();
	}

	protected override void OnActivate() {
		if(editor.GetDetail().name == "") {
			options.specifyFilename = "";
			Inspect(options);
		} else {
			Save(FileFormat.Text);
		}
	}
}
