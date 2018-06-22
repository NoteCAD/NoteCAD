using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Csg;

public class DetailEditor : MonoBehaviour {

	static DetailEditor instance_;
	public static DetailEditor instance {
		get {
			if(instance_ == null) {
				instance_ = FindObjectOfType<DetailEditor>();
			}
			return instance_;
		}
	}

	Detail detail;

	public GameObject labelParent;
	public Text resultText;
	public GameObject featuresContent;
	public FeatureUI featureUIPrefab;
	public List<FeatureUI> featuresUI;
	public Color pressedColor;
	public Mesh mesh;

	bool meshDirty = true;
	bool justSwitchedToSketch = true;

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
				//var hh = detail.GetObjectById(id);
				//Debug.Log(id.ToString() + " " + hh.GetType().Name);
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

	public bool IsFirstMeshFeature(MeshFeature mf) {
		var fi = detail.features.FindIndex(f => f is MeshFeature);
		var mi = detail.features.IndexOf(mf);
		return fi == mi;
	}

	private void Start() {
		instance_ = this;
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
	string dofText;

	public bool suppressCombine = false;

	private void Update() {
		if(activeFeature != null) {
			if(currentSketch != null && currentSketch.IsTopologyChanged()) {
				UpdateSystem();
			}
			var res = sys.Solve();
			string result = "";
			result += (GC.GetTotalMemory(false) / 1024 / 1024.0).ToString("0.##") + " mb\n";
			result += res.ToString() + "\n";
			if(sys.dofChanged) {
				if(res == EquationSystem.SolveResult.OKAY && !sys.HasDragged()) {
					int dof;
					bool ok = sys.TestRank(out dof);
					if(!ok) {
						dofText = "<color=\"#FF3030\">DOF: " + dof + "</color>\n";
					} else if(dof == 0) {
						dofText = "<color=\"#30FF30\">DOF: " + dof + "</color>\n";
					} else {
						dofText = "<color=\"#FFFFFF\">DOF: " + dof + "</color>\n";
					}
				} else {
					dofText = "<color=\"#303030\">DOF: ?</color>\n";
				}
			}
			result += dofText;
			//result += sys.stats;
			resultText.text = result.ToString();
		}

		detail.Update();
		meshDirty = meshDirty | detail.features.OfType<MeshFeature>().Any(f => f.dirty);
		detail.MarkDirty();
		detail.UpdateDirtyUntil(activeFeature);
		if(meshDirty && !suppressCombine) {
			meshDirty = false;
			mesh.Clear();
			Solid result = null;
			int combinedCount = 0;
			foreach(var f in detail.features) {
				var mf = f as MeshFeature;
				if(mf != null) {
					if(result == null) {
						result = mf.solid;
					} else {
						if(mf.combined == null) {
							//#if UNITY_WEBGL
								//if(combinedCount > 0) {
								//	break;
								//}
							//#endif
							switch(mf.operation) {
								case CombineOp.Union: mf.combined = Solids.Union(result, mf.solid); break;
								case CombineOp.Difference: mf.combined = Solids.Difference(result, mf.solid); break;
								case CombineOp.Intersection: mf.combined = Solids.Intersection(result, mf.solid); break;
							}
							combinedCount++;
						}
						result = mf.combined;
					}
				}
				if(f == activeFeature) break;
			}
			Debug.Log("combined " + combinedCount + " meshes");
			if(result != null) {
				mesh.FromSolid(result);
			}
		}

		double dist = -1.0;
		hovered = detail.HoverUntil(Input.mousePosition, Camera.main, UnityEngine.Matrix4x4.identity, ref dist, activeFeature);

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

		if(activeFeature is SketchFeature) {
			var sk = activeFeature as SketchFeature;
			if(sk.ShouldRedrawConstraints() || justSwitchedToSketch) {
				sk.DrawConstraints(canvas);
			}
		} else {
			canvas.ClearStyle("constraints");
		}
	}

	private void LateUpdate() {
		detail.Draw(UnityEngine.Matrix4x4.identity);
		GC.Collect();
	}

	private void OnGUI() {
		GUIStyle style = new GUIStyle();
		style.alignment = TextAnchor.MiddleCenter;
		if(activeFeature is SketchFeature) {
			var sk = activeFeature as SketchFeature;
			foreach(var c in sk.GetSketch().constraintList) {
				if(!(c is ValueConstraint)) continue;
				if(hovered == c) {
					style.normal.textColor = canvas.GetStyle("hovered").color;
				} else {
					style.normal.textColor = Color.white;
				}
				var constraint = c as ValueConstraint;
				var pos = constraint.pos;
				pos = Camera.main.WorldToScreenPoint(pos);
				var txt = Math.Abs(constraint.GetValue()).ToString("0.##");
				GUI.Label(new Rect(pos.x, Camera.main.pixelHeight - pos.y, 0, 0), txt, style);
			}
		}
	}
		

	public void New() {
		if(detail != null) {
			detail.Clear();
		}
		activeFeature = null;
		detail = new Detail();
		var sk = new SketchFeature();
		sk.shouldHoverWhenInactive = true;
		new PointEntity(sk.GetSketch());
		detail.AddFeature(sk);
		sk = new SketchFeature();
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
		bool skipActive = (activeFeature_ == feature);
		if(activeFeature_ != null) {
			var ui = featuresUI.Find(u => u.feature == activeFeature_);
			var btn = ui.GetComponent<Button>();
			var cb = btn.colors;
			cb.normalColor = Color.white;
			btn.colors = cb;
			if(!skipActive) activeFeature_.active = false;
		}
		activeFeature_ = feature;
		if(activeFeature_ != null) {
			var ui = featuresUI.Find(u => u.feature == activeFeature_);
			var btn = ui.GetComponent<Button>();
			var cb = btn.colors;
			cb.normalColor = pressedColor;
			btn.colors = cb;
			if(!skipActive) activeFeature_.active = true;
			justSwitchedToSketch = activeFeature_ is SketchFeature;
			UpdateSystem();
		}
		meshDirty = true;
		if(detail != null) {
			var visible = true;
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

	public string ExportCurrentSTL() {
		if(activeFeature is MeshFeature) {
			return (activeFeature as MeshFeature).solid.ToStlString(activeFeature.GetType().Name);
		}
		return "";
	}

	private void OnDrawGizmos() {
		if(currentSketch != null) {
			var bounds = currentSketch.bounds;
			if(currentSketch is LinearArrayFeature) {
				var laf = currentSketch as LinearArrayFeature;
				laf.DrawGizmos(Input.mousePosition, Camera.main);
			} else {
				Gizmos.DrawWireCube(bounds.center, bounds.size);
			}
		}
	}
}
