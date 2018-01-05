using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class DetailEditor : MonoBehaviour {

	public static DetailEditor instance;

	private Detail detail;
	public GameObject labelParent;
	public Text resultText;
	public GameObject featuresContent;
	public FeatureUI featureUIPrefab;
	public List<FeatureUI> featuresUI;
	public Color pressedColor;
	Mesh mesh;

	bool meshDirty = true;

	LineCanvas canvas;


	SketchObject hovered_;
	public SketchObject hovered {
		get {
			return hovered_;
		}
		set {
			if(hovered_ == value) return;
			if(hovered_ != null) {
				hovered_.isHovered = false;
			}
			hovered_ = value;
			if(hovered_ != null) {
				hovered_.isHovered = true;
			}
		}
	}

	public SketchFeature currentSketch {
		get {
			return activeFeature as SketchFeature;
		}
		set {
			ActivateFeature(value);
		}
	}
	Feature activeFeature;
	
	IEnumerator LoadWWWFile(string url) {
		WWW www = new WWW(url);
		yield return www;
		detail.ReadXml(www.text);
	}

	private void Start() {
		instance = this;
		var go = new GameObject("DetailMesh");
		var mf = go.AddComponent<MeshFilter>();
		var mr = go.AddComponent<MeshRenderer>();
		mesh = new Mesh();
		mesh.name = "detail";
		mf.mesh = mesh;
		mr.material = EntityConfig.instance.meshMaterial;
		New();
		if(NoteCADJS.GetParam("filename") != "") {
			var uri = new Uri(Application.absoluteURL);
			var url = "http://" + uri.Host + ":" + uri.Port + "/Files/" + NoteCADJS.GetParam("filename");
			StartCoroutine(LoadWWWFile(url));
		}
		canvas = GameObject.Instantiate(EntityConfig.instance.lineCanvas);
	}

	void UpdateFeatures() {
		for(int i = featuresUI.Count - 1; i >= 0; i--) {
			Destroy(featuresUI[i].gameObject);
		}
		featuresUI.Clear();
		foreach(var f in detail.features) {
			var ui = Instantiate(featureUIPrefab, featuresContent.transform);
			ui.feature = f;
			featuresUI.Add(ui);
		}
		ActivateFeature(activeFeature);
	}

	private void Update() {
		detail.Update();
		detail.MarkDirty();
		detail.UpdateDirty();
		if(meshDirty) {
			meshDirty = false;
			mesh.Clear();
			var instances = new List<CombineInstance>();
			foreach(var f in detail.features) {
				if(f is MeshFeature) {
					var instance = new CombineInstance();
					instance.mesh = (f as MeshFeature).GenerateMesh();
					instances.Add(instance);
				}
				if(f == activeFeature) break;
			}
			mesh.CombineMeshes(instances.ToArray(), mergeSubMeshes:true, useMatrices:false);
		}
		double dist = -1.0;
		hovered = detail.Hover(Input.mousePosition, Camera.main, ref dist);

		canvas.ClearStyle("hovered");
		if(hovered != null) {
			canvas.SetStyle("hovered");
			hovered.Draw(canvas);
		}

	}

	private void LateUpdate() {
		GC.Collect();
	}

	public void New() {
		if(detail != null) {
			detail.Clear();
		}
		activeFeature = null;
		detail = new Detail();
		var sk = new SketchFeature();
		detail.AddFeature(sk);
		UpdateFeatures();
		ActivateFeature(sk);
	}

	public void ReadXml(string xml) {
		activeFeature = null;
		detail.ReadXml(xml);
		UpdateFeatures();
		ActivateFeature(detail.features.Last());
	}

	public string WriteXml() {
		return detail.WriteXml();
	}

	public void AddFeature(Feature feature) {
		detail.features.Add(feature);
		meshDirty = true;
		UpdateFeatures();
	}

	public void ActivateFeature(Feature feature) {
		if(activeFeature != null) {
			var ui = featuresUI.Find(u => u.feature == activeFeature);
			var btn = ui.GetComponent<Button>();
			var cb = btn.colors;
			cb.normalColor = Color.white;
			btn.colors = cb;
			activeFeature.active = false;
		}
		activeFeature = feature;
		if(activeFeature != null) {
			var ui = featuresUI.Find(u => u.feature == activeFeature);
			var btn = ui.GetComponent<Button>();
			var cb = btn.colors;
			cb.normalColor = pressedColor;
			btn.colors = cb;
			activeFeature.active = true;
		}
		meshDirty = true;
		var visible = true;
		foreach(var f in detail.features) {
			f.visible = visible;
			if(f == activeFeature) {
				visible = false;
			}
		}

	}


	public string ExportSTL() {
		return mesh.ExportSTL();
	}
}
