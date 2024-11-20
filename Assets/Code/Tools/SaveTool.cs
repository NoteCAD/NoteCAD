using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;

public class SaveTool : Tool {

	public enum FileFormat {
		Xml,
		Json,
		NCAD,
//		Binary
	};

	[Serializable]
	class Options {
		public FileFormat format = FileFormat.Xml;
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
			case FileFormat.Xml: {
				var data = DetailEditor.instance.WriteXml(encrypt: false);
				NoteCADJS.SaveData(data, editor.GetDetail().name + ".xml", "xml");
			} break;
			case FileFormat.Json: {
				var data = DetailEditor.instance.GetDetail().WriteJsonAsString();
				NoteCADJS.SaveData(data, editor.GetDetail().name + ".json", "json");
			} break;
			case FileFormat.NCAD: {
				var data = DetailEditor.instance.WriteXml(encrypt: false);
				NoteCADJS.SaveData(data, editor.GetDetail().name + ".ncad", "ncad");
			} break;
			/*
			case FileFormat.Binary: {
				var data = DetailEditor.instance.GetDetail().WriteXmlAsBinary();
				NoteCADJS.SaveBinaryData(data, editor.GetDetail().name + ".notecad", "notecad");
			} break;
			*/
		}
		StopTool();
	}

	protected override void OnActivate() {
		options.specifyFilename = editor.GetDetail().name;
		Inspect(options);
	}
}
