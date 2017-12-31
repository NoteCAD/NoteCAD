using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class DetailEditor : MonoBehaviour {

	public static DetailEditor instance;

	private Detail detail;
	public Sketch currentSketch;
	public GameObject labelParent;
	public Text resultText;

	IEnumerator LoadWWWFile(string url) {
		WWW www = new WWW(url);
		yield return www;
		detail.ReadXml(www.text);
	}

	private void Start() {
		instance = this;
		New();
		if(NoteCADJS.GetParam("filename") != "") {
			var uri = new Uri(Application.absoluteURL);
			var url = "http://" + uri.Host + ":" + uri.Port + "/Files/" + NoteCADJS.GetParam("filename");
			StartCoroutine(LoadWWWFile(url));
		}
	}

	private void Update() {
		detail.Update();
	}

	private void LateUpdate() {
		GC.Collect();
	}

	public void New() {
		if(detail != null) {
			detail.Clear();
		}
		detail = new Detail();
		currentSketch = new Sketch();
		detail.AddFeature(currentSketch);
	}

	public void ReadXml(string xml) {
		detail.ReadXml(xml);
		currentSketch = detail.features.LastOrDefault(f => f is Sketch) as Sketch;
	}

	public string WriteXml() {
		return detail.WriteXml();
	}

}
