using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class FullscreenTool : Tool {

	protected override void OnActivate() {
		Screen.fullScreen = !Screen.fullScreen;
		StopTool();
	}
}
