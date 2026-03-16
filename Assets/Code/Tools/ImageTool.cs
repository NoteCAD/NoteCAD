using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NoteCAD;

public class ImageTool : Tool {

	ImageEntity img;
	byte[] pendingBytes;
	Texture2D pendingTexture;
	string pendingName;

	[System.Serializable]
	class ImageToolOptions {
		public ImageEntity.ScaleMode scaleMode = ImageEntity.ScaleMode.Scale;
	}

	ImageToolOptions options = new ImageToolOptions();

	ImageTool() {
		enableHoverFilter = true;
	}

	protected override bool OnTryHover(Constraint c) {
		return false;
	}

	protected override bool OnTryHover(IEntity e) {
		return CanConstrainCoincident(e);
	}

	protected override void OnActivate() {
		pendingBytes = null;
		pendingTexture = null;
		pendingName = null;
		options.scaleMode = ImageEntity.ScaleMode.Scale;
		Inspect(options);
		NoteCADJS.LoadBinaryData(OnImageLoaded, "png,jpg,bmp");
	}

	void OnImageLoaded(byte[] bytes) {
		if(bytes == null || bytes.Length == 0) return;
		pendingBytes = bytes;
		pendingTexture = new Texture2D(2, 2);
		pendingTexture.LoadImage(bytes);
		pendingName = "image";
		Inspect(options);
	}

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		if(pendingTexture == null) return;

		if(img != null) {
			AutoConstrainCoincident(img.p[3], sko as IEntity);
			img.isSelectable = true;
			foreach(var pt in img.p) pt.isSelectable = true;
			img = null;
			StopTool();
		} else {
			editor.PushUndo();
			var sk = DetailEditor.instance.currentSketch;
			if(sk == null) return;
			img = SpawnEntity(new ImageEntity(sk.GetSketch()));
			img.scaleMode = options.scaleMode;
			img.SetImage(pendingTexture, pendingBytes, pendingName);
			img.p[0].pos = pos;
			img.p[3].pos = pos + Vector3.up * 0.1f;
			img.UpdatePoints();
			AutoConstrainCoincident(img.p[0], sko as IEntity);
			foreach(var pt in img.p) pt.isSelectable = false;
			img.isSelectable = false;
		}
	}

	protected override void OnMouseMove(Vector3 pos, ICADObject entity) {
		if(img != null) {
			img.p[3].pos = pos;
			img.UpdatePoints();
		}
	}

	protected override void OnDeactivate() {
		if(img != null) {
			img.Destroy();
			editor.PopUndo();
			img = null;
		}
	}

	protected override string OnGetDescription() {
		return "Load an image file and click to place it in the sketch";
	}

}
