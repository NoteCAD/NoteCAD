using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ButtonContent : MonoBehaviour {

	public Image image;
	public Text text;

	private void Start() {
		text.horizontalOverflow = HorizontalWrapMode.Wrap;
	}
	void Update () {
		var tool = GetComponentInParent<Tool>();
		if(tool == null) return;
		if(image.sprite != tool.icon) image.sprite = tool.icon;
		var richText = tool.GetRichText();
		if(text.text != richText) text.text = richText;
	}
}
