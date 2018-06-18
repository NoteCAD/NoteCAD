using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ImportSTLTool : Tool, IPointerDownHandler {

	protected override void OnActivate() {
		StopTool();
	}

	public void OnPointerDown(PointerEventData eventData) {
		NoteCADJS.LoadBinaryData(DataLoaded);
	}

	void DataLoaded(byte[] data) {
		var feature = new MeshImportFeature(data);
		feature.source = DetailEditor.instance.activeFeature;
		DetailEditor.instance.AddFeature(feature); 
		DetailEditor.instance.ActivateFeature(feature);
	}
}
