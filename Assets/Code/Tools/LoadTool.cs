using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LoadTool : Tool, IPointerDownHandler {

	protected override void OnActivate() {
		StopTool();
	}

	public void OnPointerDown(PointerEventData eventData) {
		NoteCADJS.LoadData(DataLoaded);
	}

	void DataLoaded(string data) {
		GetComponentInChildren<Text>().text = data;
	}
}
