using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LoadTool : Tool, IPointerDownHandler {

	#if XOR_ENCRYPTED
		string extension = "ncad";
	#else
		string extension = "xml";
	#endif
	protected override void OnActivate() {
		StopTool();
	}

	public void OnPointerDown(PointerEventData eventData) {
		NoteCADJS.LoadData(DataLoaded, extension);
	}

	void DataLoaded(string data) {
		editor.PushUndo();
		DetailEditor.instance.ReadXml(data);
	}
}
