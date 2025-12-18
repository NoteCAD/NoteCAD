using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;

public class NewTool : Tool {

	[Serializable]
	class NewToolOptions {
		Tool tool;
		public NewToolOptions(Tool t) {
			tool = t;
		}
		public string newFilename = "";
		[RuntimeInspectorNamespace.RuntimeInspectorButton("Create", false, RuntimeInspectorNamespace.ButtonVisibility.InitializedObjects)]
		void Create() {
			if(newFilename != "") {
				tool.StopTool();
				tool.editor.New();
				tool.editor.GetDetail().name = newFilename;
			}
		}
	}

	NewToolOptions options;
	public NewTool() {
		options = new NewToolOptions(this);
	}

	protected override void OnActivate() {
		options.newFilename = "";
		Inspect(options);
	}
}
