using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using NoteCAD;

public class ImageTool : Tool, IPointerDownHandler {

	protected override void OnActivate() {
		StopTool();
	}

	public void OnPointerDown(PointerEventData eventData) {
		NoteCADJS.LoadBinaryData(DataLoaded, "");
	}

	void DataLoaded(byte[] data) {
		if (data == null || data.Length == 0) return;

		editor.PushUndo();
		var sk = DetailEditor.instance.currentSketch;
		if (sk == null) {
			editor.PopUndo();
			return;
		}

		var imageData = Convert.ToBase64String(data);
		var img = SpawnEntity(new ImageEntity(sk.GetSketch()));
		img.imageData = imageData;

		var center = Tool.CenterPos;
		float halfWidth = 1.0f;
		float halfHeight = (float)(halfWidth * img.aspectRatio);
		img.p[0].pos = center + new Vector3(-halfWidth, -halfHeight, 0f);
		img.p[1].pos = center + new Vector3(halfWidth, -halfHeight, 0f);
		img.UpdatePoints();
	}

	protected override string OnGetDescription() {
		return "click to load an image file and place it in the sketch";
	}
}
