using RuntimeInspectorNamespace;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

[Serializable]
public class SketchFeatureBase : Feature {
	protected LineCanvas canvas;
	protected Sketch sketch;
	GameObject go;

	/*
	[RuntimeInspectorButton("Variables", false, ButtonVisibility.InitializedObjects)]
	public void ShowVariables() {
		Tool.Inspect(sketch.parameters);
	}

	[RuntimeInspectorButton("Equations", false, ButtonVisibility.InitializedObjects)]
	public void ShowEquations() {
		var sys = new EquationSystem();
		GenerateEquations(sys);
		Tool.Inspect(sys.equationsList.Select(e => e.ToString()).ToList());
	}
	
	[RuntimeInspectorButton("Parameters", false, ButtonVisibility.InitializedObjects)]
	public void ShowParameters() {
		var sys = new EquationSystem();
		GenerateEquations(sys);
		Tool.Inspect(sys.parametersList.Select(p => p.name).ToList());
	}
	*/
	[RuntimeInspectorButton("Source Licensing", false, ButtonVisibility.InitializedObjects)]
	public void MailToLicense() {
		Application.OpenURL("mailto:contact@notecad.pro");
	}

	[RuntimeInspectorButton("Feedback", false, ButtonVisibility.InitializedObjects)]
	public void MailToFeedback() {
		Application.OpenURL("mailto:evilspirit@evilspirit.org");
	}

	bool solveParent_ = false;
	public bool solveParent {
		get {
			return solveParent_;
		}
		set {
			solveParent_ = value;
			MarkTopologyChanged();
		}
	}

	public virtual Matrix4x4 transform {
		get {
			return Matrix4x4.identity;
		}
	}

	public Vector3 GetNormal() {
		return transform.GetColumn(2);
	}

	public Vector3 GetPosition() {
		return transform.GetColumn(3);
	}

	public Matrix4x4 GetTransform() {
		return transform;
	}

	public Vector3 WorldToLocal(Vector3 pos) {
		return transform.inverse.MultiplyPoint(pos);
	}

	public Sketch GetSketch() {
		return sketch;
	}

	public override ICADObject GetChild(Id guid) {
		if(guid == sketch.guid) return sketch;
		return null;
	}

	public override Bounds bounds { get { return sketch.calculateBounds(); } }

	public SketchFeatureBase() {
		sketch = new Sketch();
		sketch.feature = this;
		sketch.is3d = true;
		canvas = GameObject.Instantiate(EntityConfig.instance.lineCanvas);
		canvas.name = "SketchFeatureCanvas";
		go = new GameObject(GetType().Name);
		canvas.transform.SetParent(go.transform, false);
		canvas.parent = go;
	}

	public override void GenerateEquations(EquationSystem sys) {
		base.GenerateEquations(sys);
		sketch.GenerateEquations(sys);
		if(solveParent && source != null) {
			source.GenerateEquations(sys);
		}
	}

	public bool IsTopologyChanged() {
		return sketch.topologyChanged || sketch.constraintsTopologyChanged;
	}

	public void MarkTopologyChanged() {
		sketch.topologyChanged = true;
	}

	public EquationSystem.SolveResult Solve() {
		var sys = new EquationSystem();
		GenerateEquations(sys);
		var result = sys.Solve();
		if(sketch.HasNonSolvable()) {
			Debug.Log("solve second time for non-solveable");
			sys.Clear();
			GenerateEquations(sys);
			return sys.Solve();
		}
		return result;
	}
	/*
	public bool IsRedundant() {
		var sys = new EquationSystem();
		GenerateEquations(sys);
		int dof;
		if(!sys.TestRank(out dof)) {
			return true;
		}
		var result = sys.Solve();
		if(!sys.TestRank(out dof)) {
			return true;
		}
		return false;
	}
	*/

	public bool ShouldRedrawConstraints() {
		return sketch.IsEntitiesChanged() || sketch.IsConstraintsChanged();
	}

	public virtual void DrawEntities(ICanvas canvas) {
		foreach(var e in sketch.entityList) {
			if(!e.isVisible) continue;
			if(e.style == null) {
				canvas.SetStyle("entities");
			} else {
				canvas.SetStyle(e.style);
			}
			e.Draw(canvas);
		}
	}

	public void DrawConstraints(ICanvas canvas, Func<Constraint, bool> filter = null) {
		if(canvas is LineCanvas) (canvas as LineCanvas).ClearStyle("constraints");
		canvas.SetStyle("constraints");
		foreach(var c in sketch.constraintList) {
			if(!c.isVisible) continue;
			if(filter != null && !filter(c)) continue;
			c.Draw(canvas);
		}
	}

	public override void UpdateDirty() {
		if(!dirty) return;

		if(sourceChanged) {
			if(Solve() != EquationSystem.SolveResult.OKAY) {
				Debug.LogError("Solve Failed!!!!!!");
			}
		}
		canvas.Clear();
		base.UpdateDirty();
		go.transform.SetMatrix(transform);
	
		DrawEntities(canvas);

		sketch.MarkUnchanged();
		canvas.UpdateDirty();
	}


	public override void MarqueeSelect(Rect rect, bool wholeObject, Camera camera, Matrix4x4 tf, ref List<ICADObject> result) {
		var resTf = GetTransform() * tf;
		sketch.MarqueeSelect(rect, wholeObject, camera, resTf, ref result);
	}

	public override ICADObject Hover(Vector3 mouse, Camera camera, Matrix4x4 tf, HoverFilter filter, ref double objDist) {
		double dist = -1;
		var resTf = GetTransform() * tf;
		var result1 = sketch.Hover(mouse, camera, resTf, filter, ref objDist);
		if(result1 is IEntity && (result1 as IEntity).type == IEntityType.Point) return result1;
		var result = base.Hover(mouse, camera, resTf, filter, ref dist);
		if(result != null && result1 != null) {
			if(dist < objDist) {
				objDist = dist;
				return result;
			}
		}
		if(result != null) {
			objDist = dist;
			return result;
		}
		return result1;
	}
	
	public override void Draw(Matrix4x4 tf) {
		base.Draw(tf);
		canvas.DrawToGraphics(GetTransform() * tf);
	}

	public override void Update() {
		base.Update();
		if(sketch.IsConstraintsChanged() || sketch.IsEntitiesChanged() || sketch.IsDirty()) {
			MarkDirty();
			detail.MarkDirtyAfter(this);
		}
	}

	public override GameObject gameObject {
		get {
			return go;
		}
	}


	protected sealed override void OnWrite(Writer xml) {
		xml.WriteAttribute("solveParent", solveParent);
		if(shouldHoverWhenInactive) {
			xml.WriteAttribute("alwaysHover", shouldHoverWhenInactive);
		}
		OnWriteSketchFeatureBase(xml);
		sketch.Write(xml);
	}

	protected virtual void OnWriteSketchFeatureBase(Writer xml) {
		
	}

	protected sealed override void OnRead(XmlNode xml) {
		if(xml.Attributes["solveParent"] != null) {
			solveParent = Convert.ToBoolean(xml.Attributes["solveParent"].Value);
		}
		if(xml.Attributes["alwaysHover"] != null) {
			shouldHoverWhenInactive = Convert.ToBoolean(xml.Attributes["alwaysHover"].Value);
		}
		OnReadSketchFeatureBase(xml);
		sketch.Read(xml);
	}

	protected virtual void OnReadSketchFeatureBase(XmlNode xml) {
		
	}

	public override void Clear() {
		base.Clear();
		sketch.Clear();
		GameObject.Destroy(go);
		GameObject.Destroy(canvas.gameObject);
	}

	[NonSerialized]
	public bool shouldHoverWhenInactive = true;

	public override bool ShouldHoverWhenInactive() {
		return shouldHoverWhenInactive;
	}

	public override void Activate(bool state) {
		base.Activate(state);
		go.SetActive(state && visible);
	}

	public override void Show(bool state) {
		base.Show(state);
		go.SetActive(state && active);
	}
}
