using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class HelpTool : Tool {

	protected override void OnActivate() {
	}

	protected override string OnGetDescription() {
		return "Use middle mouse button for panning camera, scroll for zooming, right button for rotating the camera. For making solid bodies, please draw closed contours of primitives, then you can create extrusion and sketches in different workplanes. For cancelling current tool you can press Escape or right mouse button.";
	}
}
