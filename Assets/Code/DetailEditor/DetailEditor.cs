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
	EquationSystem sys = new EquationSystem();

	ICADObject hovered_;
	public ICADObject hovered {
		get {
			return hovered_;
		}
		set {
			if(hovered_ == value) return;
			if(hovered_ != null) {
				if(hovered_ is SketchObject) {
					(hovered_ as SketchObject).isHovered = false;
				}
			}
			hovered_ = value;
			if(hovered_ != null) {
				var id = hovered_.id;
				var hh = detail.GetObjectById(id);
				Debug.Log(hh.GetType().Name);
				if(hovered_ is SketchObject) {
					(hovered_ as SketchObject).isHovered = true;
				}
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

	Feature activeFeature_;
	public Feature activeFeature {
		get {
			return activeFeature_;
		}
		set {
			ActivateFeature(value);
		}
	}
	
	IEnumerator LoadWWWFile(string url) {
		WWW www = new WWW(url);
		yield return www;
		ReadXml(www.text);
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

	public void AddDrag(Exp drag) {
		sys.AddEquation(drag);
	}

	public void RemoveDrag(Exp drag) {
		sys.RemoveEquation(drag);
	}

	void UpdateSystem() {
		sys.Clear();
		activeFeature.GenerateEquations(sys);
	}

	private void Update() {
		if(activeFeature != null) {
			if(currentSketch != null && currentSketch.IsTopologyChanged()) {
				UpdateSystem();
			}
			string result = sys.Solve().ToString();
			result += "\n" + sys.stats;
			resultText.text = result.ToString();
		}

		detail.Update();
		meshDirty = meshDirty | detail.features.OfType<MeshFeature>().Any(f => f.dirty);
		detail.MarkDirty();
		detail.UpdateDirtyUntil(activeFeature);
		if(meshDirty) {
			meshDirty = false;
			mesh.Clear();
			var instances = new List<CombineInstance>();
			foreach(var f in detail.features) {
				if(f is MeshFeature) {
					var instance = (f as MeshFeature).GenerateMesh();
					instances.Add(instance);
				}
				if(f == activeFeature) break;
			}
			mesh.CombineMeshes(instances.ToArray(), mergeSubMeshes:true, useMatrices:true);
		}
		double dist = -1.0;
		hovered = detail.HoverUntil(Input.mousePosition, Camera.main, Matrix4x4.identity, ref dist, activeFeature);

		canvas.ClearStyle("hovered");
		if(hovered != null) {
			canvas.SetStyle("hovered");
			if(hovered is IEntity) {
				canvas.DrawSegments((hovered as IEntity).SegmentsInPlane(null));
			} else
			if(hovered is SketchObject) {
				(hovered as SketchObject).Draw(canvas);
			}
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
		detail.AddFeature(feature);
		meshDirty = true;
		UpdateFeatures();
	}

	public void ActivateFeature(Feature feature) {
		if(activeFeature_ != null) {
			var ui = featuresUI.Find(u => u.feature == activeFeature_);
			var btn = ui.GetComponent<Button>();
			var cb = btn.colors;
			cb.normalColor = Color.white;
			btn.colors = cb;
			activeFeature_.active = false;
		}
		activeFeature_ = feature;
		if(activeFeature_ != null) {
			var ui = featuresUI.Find(u => u.feature == activeFeature_);
			var btn = ui.GetComponent<Button>();
			var cb = btn.colors;
			cb.normalColor = pressedColor;
			btn.colors = cb;
			activeFeature_.active = true;
			UpdateSystem();
		}
		meshDirty = true;
		var visible = true;
		if(detail != null) {
			foreach(var f in detail.features) {
				f.visible = visible;
				if(f == activeFeature_) {
					visible = false;
				}
			}
		}
	}

	public string ExportSTL() {
		return mesh.ExportSTL();
	}
}
