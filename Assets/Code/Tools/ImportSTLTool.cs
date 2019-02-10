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
		NoteCADJS.LoadBinaryData(DataLoaded, "stl");
	}

	void DataLoaded(byte[] data) {
		editor.PushUndo();
		var feature = new MeshImportFeature(data);
		feature.source = DetailEditor.instance.activeFeature;
		feature.operation = CombineOp.Assembly;
		DetailEditor.instance.AddFeature(feature); 
		DetailEditor.instance.ActivateFeature(feature);
	}
}
