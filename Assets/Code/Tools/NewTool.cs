using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class NewTool : Tool {

	protected override void OnActivate() {
		StopTool();
		DetailEditor.instance.New();
	}
}
