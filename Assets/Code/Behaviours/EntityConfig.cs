using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using SharpFont;

public class EntityConfig : MonoBehaviour {

	public static EntityConfig instance;
	public Text labelPrefab;
	public Material meshMaterial;
	public Material loopMaterial;
	public Material imageMaterial;
	public LineCanvas lineCanvas;
	public StrokeStyles styles;
	public Font systemFont;

	public TextAsset font;
	public TextAsset translation;
	public FontFace fontFace;

	void Start() {
		instance = this;
		fontFace = new SharpFont.FontFace(new MemoryStream(font.bytes));
		Trans.FromCSV(translation.text);
	}
}
