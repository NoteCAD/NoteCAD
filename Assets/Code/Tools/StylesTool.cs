using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class StylesTool : Tool {

	protected override void OnActivate() {
		Inspect(EntityConfig.instance.lineCanvas.strokeStyles.styles);
	}

	protected override string OnGetDescription() {
		return "Styles";
	}
}
