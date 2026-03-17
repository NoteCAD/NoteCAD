using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Csg;
using RuntimeInspectorNamespace;
using System.IO;
using System.Xml;
using UnityEngine.Networking;
using NoteCAD;

public delegate bool HoverFilter(ICADObject co);

public class DetailEditor : MonoBehaviour {

	static DetailEditor instance_;

	public TextAsset newFile;
	public Vector3 defaultPlaneU = Vector3.right;
	public Vector3 defaultPlaneV = Vector3.up;
	public Vector3 defaultPlanePos = Vector3.zero;

	public static DetailEditor instance {
		get {
			if(instance_ == null) {
				instance_ = FindObjectOfType<DetailEditor>();
			}
			return instance_;
		}
	}

	Detail detail;

	public Detail GetDetail() { return detail; }

	public UndoRedo undoRedo;

	public GameObject labelParent;
	public Text resultText;
	public Text detailName;
	public GameObject featuresContent;
	public FeatureUI featureUIPrefab;
	public List<FeatureUI> featuresUI;
	public Color pressedColor;
	public Mesh mesh;
	public Mesh selectedMesh;
	public Solid solid;
	public RuntimeInspector inspector;
	public bool toolInspector = false;

	bool meshDirty = true;

	LineCanvas canvas;
	public Tool activeTool;
	EquationSystem sys = new EquationSystem();
	public HashSet<IdPath> selection = new HashSet<IdPath>();

	public HoverFilter hoverFilter = null;

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
				//Debug.Log(id.ToString());
				//var hh = detail.GetObjectById(id);
				//Debug.Log(id.ToString());
				//Debug.Log(id.ToString() + " " + hh.GetType().Name);
				if(hovered_ is SketchObject) {
					(hovered_ as SketchObject).isHovered = true;
				}
			}
		}
	}

	public SketchFeatureBase currentSketch {
		get {
			return activeFeature as SketchFeatureBase;
		}
		set {
			ActivateFeature(value);
		}
	}

	public SketchFeature currentWorkplane {
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

	public bool IsSelected(ICADObject obj) {
		if(obj == null) return false;
		return selection.Contains(obj.id);
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

	GameObject CreateMeshObject(string name, Mesh mesh, Material material) {
		var go = new GameObject(name);
		var mf = go.AddComponent<MeshFilter>();
		var mr = go.AddComponent<MeshRenderer>();
		mesh.name = name;
		mf.mesh = mesh;
		mr.material = material;
		return go;
	}

	DetailEditor() {
		undoRedo = new UndoRedo(this);
	}

	public void PushUndo() {
		undoRedo.Push();
	}

	public void PopUndo() {
		undoRedo.Pop();
	}

	private void Start() {
		Debug.Log($"Screen.DPI {Screen.dpi}");
		instance_ = this;
		mesh = new Mesh();
		selectedMesh = new Mesh();
		var mgo = CreateMeshObject("DetailMesh", mesh, EntityConfig.instance.meshMaterial);
		mgo.transform.parent = transform;
		mgo = CreateMeshObject("DetailMeshSelection", selectedMesh, EntityConfig.instance.loopMaterial);
		mgo.transform.parent = transform;
		New();
		canvas = GameObject.Instantiate(EntityConfig.instance.lineCanvas, transform);
		if(NoteCADJS.GetParam("filename") != "") {
			var uri = new Uri(Application.absoluteURL);
			var url = "https://" + uri.Host + ":" + uri.Port + "/Files/" + NoteCADJS.GetParam("filename");
			StartCoroutine(LoadWWWFile(url));
		}
		if(NoteCADJS.GetParam("lang") != "") {
			Trans.currentLang = NoteCADJS.GetParam("lang");
		}
	}

	void UpdateFeatures() {
		for(int i = featuresUI.Count - 1; i >= 0; i--) {
			Destroy(featuresUI[i].gameObject);
		}
		featuresUI.Clear();
		if (featuresContent != null) {
			foreach(var f in detail.features) {
				var ui = Instantiate(featureUIPrefab, featuresContent.transform);
				ui.feature = f;
				featuresUI.Add(ui);
			}
		}
		ActivateFeature(activeFeature);
	}
	List<Exp> draggedEquations = new List<Exp>();
	public void AddDrag(Exp drag) {
		draggedEquations.Add(drag);
		sys.AddEquation(drag);
	}

	public void RemoveDrag(Exp drag) {
		draggedEquations.Remove(drag);
		sys.RemoveEquation(drag);
	}

	void UpdateSystem() {
		sys.Clear();
		activeFeature.GenerateEquations(sys);
		foreach(var drag in draggedEquations) {
			sys.AddEquation(drag);
		}
	}
	string dofText;

	public bool suppressCombine = false;
	public bool suppressHovering = false;
	public bool suppressSolve = false;

	private void UpdateMesh() {
		canvas.ClearStyle("edges");
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
							case CombineOp.Assembly: mf.combined = Solids.Assembly(result, mf.solid); break;
						}
						combinedCount++;
					}
					result = mf.combined;
				}
			}
			if(f == activeFeature) break;
		}
		//UnityEngine.Debug.Log("combined " + combinedCount + " meshes");
		solid = result;
		if(result != null) {
			mesh.FromSolid(result);
		}
	}

	IEnumerator UpdateMeshFromServer() {
		var url = detail.settings.meshProvider;
		var format = detail.settings.providerFormat == DetailSettings.MeshProviderFormat.JSON ? "application/json" : "application/xml";
		var data = detail.settings.providerFormat == DetailSettings.MeshProviderFormat.JSON ? detail.WriteJsonAsString() : detail.WriteXmlAsString(false);
		using(var req = UnityWebRequest.Post(url, data, format)) {
			yield return req.SendWebRequest();

			switch (req.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError("Request mesh error");
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError("HTTP Error: " + req.error);
                    break;
                case UnityWebRequest.Result.Success:
					CadBin cadBin = new();
					cadBin.read(req.downloadHandler.data);
					canvas.ClearStyle("edges");
					canvas.SetStyle("edges");
					cadBin.toMesh(mesh, canvas);
					/* STL
					MemoryStream ms = new MemoryStream(req.downloadHandler.data);
					var meshes = Parabox.STL.pb_Stl_Importer.Import(ms);
					mesh.Clear();
					if (meshes != null) {
						mesh.indexFormat = meshes[0].indexFormat;
						mesh.vertices = meshes[0].vertices;
						mesh.normals = meshes[0].normals;
						mesh.triangles = meshes[0].triangles;
						mesh.uv = meshes[0].uv;
						mesh.tangents = meshes[0].tangents;
						mesh.RecalculateBounds();
						mesh.RecalculateNormals();
						mesh.RecalculateTangents();
					}
					*/
                    break;
            }
		}
	}

	public void Update() {
		if(activeFeature != null && ! detail.settings.suppressSolver) {
			if(currentSketch != null && (currentSketch.GetSketch().IsConstraintsChanged() || currentSketch.GetSketch().IsEntitiesChanged()) || sys.IsDirty) {
				suppressSolve = false;
			}
			if(currentSketch != null && currentSketch.IsTopologyChanged()) {
				UpdateSystem();
			}
			var res = (!suppressSolve || sys.HasDragged()) ? sys.Solve() : EquationSystem.SolveResult.DIDNT_CONVEGE;
			if(res == EquationSystem.SolveResult.DIDNT_CONVEGE) {
				suppressSolve = true;
			}
			if(currentSketch != null && currentSketch.GetSketch().HasNonSolvable()) {
				Debug.Log("Resolve because of non solvable");
				UpdateSystem();

				res = (!suppressSolve || sys.HasDragged()) ? sys.Solve() : EquationSystem.SolveResult.DIDNT_CONVEGE;
			}
			string result = "";
			result += (GC.GetTotalMemory(false) / 1024 / 1024.0).ToString("0.##") + " mb\n";
			result += res.ToString() + "\n";
			if(sys.dofChanged) {
				if(res == EquationSystem.SolveResult.OKAY && !sys.HasDragged() && sys.CurrentEquationsCount() <= 1024) {
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
			result += "Undo: " + undoRedo.Count() + "\n";
			result += "UndoSize: " + undoRedo.Size() + "\n";
			//result += sys.stats;
			if (resultText != null) {
				resultText.text = result.ToString();
			}
		} else {
			if (resultText != null) {
				resultText.text = "Solver suppressed";
			}
		}
		if (detailName != null) {
			detailName.text = detail.name;
		}

		detail.UpdateUntil(activeFeature);
		//detail.Update();

		meshDirty = meshDirty | detail.features.Take(detail.features.IndexOf(activeFeature) + 1).OfType<MeshFeature>().Any(f => f.dirty);
		detail.MarkDirty();
		detail.UpdateDirtyUntil(activeFeature);
		if(meshDirty && !suppressCombine) {
			meshDirty = false;
			if (detail.settings.useMeshProvider) {
				StartCoroutine(UpdateMeshFromServer());
			} else {
				UpdateMesh();
			}
		}

		if(!CameraController.instance.IsMoving && !suppressHovering) {
			double dist = -1.0;

			bool hf(ICADObject co) {
				return (hoverFilter == null || hoverFilter(co)) && ShouldShowConstraint(co);
			}

			hovered = detail.HoverUntil(Input.mousePosition, Camera.main, UnityEngine.Matrix4x4.identity, ref dist, activeFeature, hf);
			/*
			if(hovered == null && solid != null) {
				var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				var id = solid.Raytrace(ray);
				selectedMesh.FromSolid(solid, id);
			} else {
				selectedMesh.Clear();
			}*/
		}

		canvas.ClearStyle("hovered");
		canvas.ClearStyle("hoveredPoints");
		if(hovered != null) {
			DrawCadObject(hovered, "hovered", true);
		}

		canvas.ClearStyle("selected");
		canvas.ClearStyle("selectedPoints");
		foreach(var idp in selection) {
			var obj = detail.GetObjectById(idp);
			if(obj == null) continue;
			DrawCadObject(obj, "selected", selection.Count == 1);
		}

		if(!toolInspector) {
			if(selection.Count == 1) {
				var obj = detail.GetObjectById(selection.First());
				Inspect(obj);
			} else {
				Inspect(activeFeature);
			}
		}

		if(activeFeature is SketchFeatureBase) {
			var sk = activeFeature as SketchFeatureBase;
			sk.DrawConstraints(canvas, c => ShouldShowConstraint(c));

			//var skk = activeFeature as SketchFeature;
			//if(skk != null) skk.DrawTriangulation(canvas);
		} else {
			canvas.ClearStyle("constraints");
		}

		canvas.ClearStyle("trimPreview");
		activeTool?.DrawPreview(canvas);
	}

	void DrawCadObject(ICADObject obj, string style, bool drawPoints) {
		var sko = obj as SketchObject;
		if(sko != null && !sko.isVisible) return;
		var he = obj as IEntity;
		canvas.SetStyle((he != null && he.type == IEntityType.Point) ? style + "Points" : style);
		if(he != null) {
			canvas.DrawSegments((obj as IEntity).SegmentsInPlane(null));
			if (drawPoints && detail.settings.displayPoints != DetailSettings.DisplayPoints.All) {
				canvas.SetStyle(style + "Points");
				foreach(var p in he.points) {
					canvas.DrawPoint(p.Eval());
				}
			}
		} else
		if(sko != null) {
			sko.Draw(canvas);
		}
	}

	public bool RemoveById(IdPath idp) {
		var obj = detail.GetObjectById(idp);
		if(obj is SketchObject) {
			var sko = obj as SketchObject;
			sko.Destroy();
			return true;
		}
		return false;
	}

	private void LateUpdate() {
		detail.Draw(UnityEngine.Matrix4x4.identity);
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
	}


	bool ShouldShowConstraint(ICADObject co) {
		if(co is Constraint c) {
			return detail.settings.showConstraints && !c.IsDimension || detail.settings.showDimensions && c.IsDimension;
		}
		return true;
	}

	private void OnGUI() {
		GUIStyle style = new GUIStyle();
		style.richText = false;
		style.alignment = TextAnchor.MiddleCenter;
		style.font = EntityConfig.instance.systemFont;
		style.fontSize = (int)(16f * (Screen.dpi / 100f));
		if(activeFeature is SketchFeatureBase) {
			var sk = activeFeature as SketchFeatureBase;
			foreach(var c in sk.GetSketch().constraintList) {
				if(!c.isVisible) continue;
				if(!(c is ValueConstraint)) continue;
				if(!ShouldShowConstraint(c)) continue;

				var constraint = c as ValueConstraint;
				if(!constraint.valueVisible) continue;
				if(MoveTool.instance.IsConstraintEditing(constraint)) continue;
				var pos = constraint.GetLabelPos();
				pos = Camera.main.WorldToScreenPoint(pos);
				var txt = constraint.GetLabel();
				var rect = new Rect(pos.x, Camera.main.pixelHeight - pos.y, 0, 0);
				var isSelected = IsSelected(c);
				style.normal.textColor = Color.black;
				for(int i = -1; i <= 1; i++) {
					for(int j = -1; j <= 1; j++) {
						if(i == 0 || j == 0) continue;
						GUI.Label(new Rect(rect.x + i, rect.y + j, 0, 0), txt, style);
					}
				}

				if(hovered == c) {
					style.normal.textColor = canvas.GetStyle("hovered").color;
				} else
				if(isSelected) {
					style.normal.textColor = canvas.GetStyle("selected").color;
				} else {
					style.normal.textColor = Color.white;
				}
				GUI.Label(rect, txt, style);

			}
		}
	}


	UnityEngine.Matrix4x4 GetDefaultTransform() {
		var ud = defaultPlaneU;
		var vd = defaultPlaneV;
		var nd = Vector3.Cross(ud, vd).normalized;
		vd = Vector3.Cross(nd, ud).normalized;
		return UnityExt.Basis(ud, vd, nd, defaultPlanePos);
	}

	public void New() {
		if(detail != null) {
			detail.Clear();
		}
		Clear();
		undoRedo.Clear();
		detail = new Detail();
		detail.gameObject.transform.parent = gameObject.transform;

		if(newFile != null) {
			ReadXml(newFile.text);
		} else {
			var style = detail.styles.AddStyle();
			style.stroke.Set(EntityConfig.instance.styles.styles.First(s => s.name == "entities"));
			style.stroke.name = "Default";
			var defTf = GetDefaultTransform();
			var sk = new SketchFeature();
			sk.shouldHoverWhenInactive = true;
			sk.defaultTransfrom = defTf;
			new PointEntity(sk.GetSketch());
			detail.AddFeature(sk);
			sk = new SketchFeature();
			sk.defaultTransfrom = defTf;
			detail.AddFeature(sk);
			UpdateFeatures();
			StylesUI.instance?.UpdateStyles();
			sys.Clear();
			ActivateFeature(sk);
		}

	}

	void Clear() {
		activeFeature = null;
		selection.Clear();
	}

	public void ReadXml(string xml, bool readView = true, bool activateLast = true) {
		Clear();
		IdPath active = null;
		detail.ReadXml(xml, readView, out active);
		if(active.IsNull()) active = detail.features.Last().id;
		UpdateFeatures();
		ActivateFeature(active);
		StylesUI.instance?.UpdateStyles();
	}

	public string CopySelection() {
		if(currentSketch == null || currentSketch.GetSketch() == null) return "";

		var text = new StringWriter();
		var xmlW = new XmlTextWriter(text);
		xmlW.Formatting = Formatting.Indented;
		xmlW.IndentChar = '\t';
		xmlW.Indentation = 1;
		xmlW.WriteStartDocument();
		var xml = new WriterXml(xmlW);
		xml.WriteBeginElement("copy");
		xml.WriteAttribute("program", "NoteCAD");
		xml.WriteAttribute("version", "0");
		xml.WriteAttribute("pos", Tool.MousePos.ToStr());

		var sk = currentSketch.GetSketch();
		var objects = new HashSet<SketchObject>(
			selection
				.Select(s => detail.GetObjectById(s))
				.OfType<SketchObject>()
				.Where(o => !(o is Constraint) || (o as Constraint).objects.All(co => co is SketchObject && (co as SketchObject).sketch == sk)));

		sk.Write(xml, o =>
			objects.Contains(o) &&
			(!(o is Entity) || !objects.Contains((o as Entity).parent)) &&
			(!(o is Constraint) || (o as Constraint).objects.All(co => co is SketchObject && objects.Contains(co as SketchObject)))
		);

		xml.WriteEndElement();
		return text.ToString();
	}

	public List<IdPath> Paste(string str) {
		if(currentSketch == null || currentSketch.GetSketch() == null) return null;

		var xml = new XmlDocument();
		xml.LoadXml(str);

		if(xml.DocumentElement.Attributes["program"] == null || xml.DocumentElement.Attributes["program"].Value != "NoteCAD") {
			return null;
		}

		var pos = xml.DocumentElement.Attributes["pos"].Value.ToVector3();
		var delta = Tool.MousePos - pos;

		var sk = currentSketch.GetSketch();
		sk.Read(xml.DocumentElement, true);
		var result = sk.idMapping;
		sk.idMapping = null;

		var objs = result.Select(o => sk.GetChild(o.Value)).ToList();
		MoveTool.ShiftObjects(objs, delta);

		return objs.Select(o => o.id).ToList();
	}

	public void MarqueeSelect(Rect rect, bool wholeObject) {
		var result = new List<ICADObject>();
		detail.MarqueeSelectUntil(rect, wholeObject, Camera.main, UnityEngine.Matrix4x4.identity, ref result, activeFeature);
		foreach(var co in result) {
			selection.Add(co.id);
		}
	}

	public string WriteXml(bool encrypt = false) {
		return detail.WriteXmlAsString(encrypt);
	}

	public void AddFeature(Feature feature) {
		detail.AddFeature(feature);
		meshDirty = true;
		UpdateFeatures();
	}

	public void ActivateFeature(IdPath path) {
		var feature = (Feature)detail.GetObjectById(path);
		ActivateFeature(feature);
	}

	public void ActivateFeature(Feature feature) {
		bool skipActive = (activeFeature_ == feature);
		if(activeFeature_ != null) {
			var ui = featuresUI.Find(u => u.feature == activeFeature_);
			if (ui != null) {
				var btn = ui.GetComponent<Button>();
				var cb = btn.colors;
				cb.normalColor = Color.white;
				btn.colors = cb;
			}
			if(!skipActive) activeFeature_.active = false;
		}
		activeFeature_ = feature;
		if(activeFeature_ != null) {
			var ui = featuresUI.Find(u => u.feature == activeFeature_);
			if (ui != null) {
				var btn = ui.GetComponent<Button>();
				var cb = btn.colors;
				cb.normalColor = pressedColor;
				btn.colors = cb;
			}
			if(!skipActive) activeFeature_.active = true;
			Inspect(activeFeature_);
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

	public string ExportOBJ() {
		return mesh.ExportOBJ();
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
			if(currentSketch is LinearArrayFeature laf) {
				laf.DrawGizmos(Input.mousePosition, Camera.main);
			} else {
				Gizmos.DrawWireCube(bounds.center, bounds.size);
			}
		}

	}

	public void Inspect(object obj) {
		if(inspector == null) {
			return;
		}
		inspector.Inspect(obj);
		Trans.late(inspector, obj);
	}

}
