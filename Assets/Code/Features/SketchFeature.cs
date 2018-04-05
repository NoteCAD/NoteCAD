using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

public class SketchFeature : Feature, IPlane {
	List<List<Entity>> loops = new List<List<Entity>>();
	protected LineCanvas canvas;
	Sketch sketch;
	Mesh mainMesh;
	GameObject go;
	GameObject loopObj;
	Matrix4x4 transform_;
	public Matrix4x4 transform {
		get {
			if(transformDirty) {
				transform_ = CalculateTransform();
			}
			return transform_;
		}
	}
	bool transformDirty = true;

	IdPath uId = new IdPath();
	IdPath vId = new IdPath();
	IdPath pId = new IdPath();

	public IEntity u {
		get {
			return detail.GetObjectById(uId) as IEntity;
		}
		set {
			uId = value.id;
			transformDirty = true;
		}
	}

	public IEntity v {
		get {
			return detail.GetObjectById(vId) as IEntity;
		}
		set {
			vId = value.id;
			transformDirty = true;
		}
	}
	
	public IEntity p {
		get {
			return detail.GetObjectById(pId) as IEntity;
		}
		set {
			pId = value.id;
			transformDirty = true;
		}
	}

	Vector3 IPlane.u {
		get {
			return transform.GetColumn(0);
		}
	}

	Vector3 IPlane.v {
		get {
			return transform.GetColumn(1);
		}
	}

	Vector3 IPlane.n {
		get {
			return transform.GetColumn(2);
		}
	}

	Vector3 IPlane.o {
		get {
			return transform.GetColumn(3);
		}
	}

	public Vector3 GetNormal() {
		return transform.GetColumn(2);
	}

	public Vector3 GetPosition() {
		return transform.GetColumn(3);
	}

	Matrix4x4 CalculateTransform() {
		if(u == null) return Matrix4x4.identity;
		var ud = u.GetDirectionInPlane(null).Eval().normalized;
		var vd = v.GetDirectionInPlane(null).Eval();
		var nd = Vector3.Cross(ud, vd).normalized;
		vd = Vector3.Cross(nd, ud).normalized;
		Vector4 p4 = p.points.First().Eval();
		p4.w = 1.0f;

		Matrix4x4 result = Matrix4x4.identity;
		result.SetColumn(0, ud);
		result.SetColumn(1, vd);
		result.SetColumn(2, nd);
		result.SetColumn(3, p4);
		transformDirty = false;
		return result;
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
		//TODO: save sketch id
		//if(sketch.guid == guid) return sketch;
		//return null;
		return sketch;
	}

	public override Bounds bounds { get { return sketch.calculateBounds(); } }

	public SketchFeature() {
		sketch = new Sketch();
		sketch.feature = this;
		sketch.plane = this;
		canvas = GameObject.Instantiate(EntityConfig.instance.lineCanvas);
		mainMesh = new Mesh();
		loopObj = new GameObject("loops");
		var mf = loopObj.AddComponent<MeshFilter>();
		var mr = loopObj.AddComponent<MeshRenderer>();
		loopObj.transform.SetParent(canvas.gameObject.transform, false);
		mf.mesh = mainMesh;
		mr.material = EntityConfig.instance.loopMaterial;
		go = new GameObject("SketchFeature");
		canvas.parent = go;
	}

	protected override void OnGenerateEquations(EquationSystem sys) {
		sketch.GenerateEquations(sys);
	}

	public bool IsTopologyChanged() {
		return sketch.topologyChanged || sketch.constraintsTopologyChanged;
	}

	public EquationSystem.SolveResult Solve() {
		var sys = new EquationSystem();
		GenerateEquations(sys);
		return sys.Solve();
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

	public void DrawConstraints(LineCanvas canvas) {
		canvas.ClearStyle("constraints");
		canvas.SetStyle("constraints");
		foreach(var c in sketch.constraintList) {
			c.Draw(canvas);
		}
	}

	protected override void OnUpdateDirty() {
		transformDirty = true;
		go.transform.position = transform.MultiplyPoint(Vector3.zero);
		go.transform.rotation = Quaternion.LookRotation(transform.GetColumn(2), transform.GetColumn(1));
		canvas.gameObject.transform.position = go.transform.position;
		canvas.gameObject.transform.rotation = go.transform.rotation;

		if(sourceChanged) {
			Debug.Log("Solve!!!!!!");
			Solve();
		}
		/*
		if(p != null) {
			canvas.ClearStyle("error");
			canvas.SetStyle("error");
			var pos = Vector3.zero;
			var udir = Vector3.right;
			var vdir = Vector3.up;
			var ndir = Vector3.forward;
			canvas.DrawLine(pos, pos + udir * 10.0f);
			canvas.DrawLine(pos, pos + vdir * 10.0f);
			canvas.DrawLine(pos, pos + ndir * 10.0f);
			canvas.ClearStyle("constraints");
			canvas.SetStyle("constraints");
			pos = go.transform.InverseTransformPoint(GetPosition().Eval());
			udir = go.transform.InverseTransformDirection(u.GetDirection().Eval());
			vdir = go.transform.InverseTransformDirection(v.GetDirection().Eval());
			canvas.DrawLine(pos, pos + udir * 10.0f);
			canvas.DrawLine(pos, pos + vdir * 10.0f);
		}*/
		if(sketch.topologyChanged) {
			loops = sketch.GenerateLoops();
		}

		var loopsChanged = loops.Any(l => l.Any(e => e.IsChanged()));
		if(loopsChanged || sketch.topologyChanged) {
			CreateLoops();
		}

		if(sketch.IsEntitiesChanged()) {
			canvas.ClearStyle("entities");
			canvas.SetStyle("entities");
			foreach(var e in sketch.entityList) {
				e.Draw(canvas);
			}

			canvas.ClearStyle("error");
			canvas.SetStyle("error");
			foreach(var e in sketch.entityList) {
				if(!e.isError) continue;
				e.Draw(canvas);
			}
		}

		sketch.MarkUnchanged();
		canvas.UpdateDirty();
	}

	protected override ICADObject OnHover(Vector3 mouse, Camera camera, Matrix4x4 tf, ref double objDist) {
		var resTf = GetTransform() * tf;
		return sketch.Hover(mouse, camera, resTf, ref objDist);
	}

	protected override void OnUpdate() {

		if(sketch.IsConstraintsChanged() || sketch.IsEntitiesChanged() || sketch.IsDirty()) {
			MarkDirty();
		}
	}

	public override GameObject gameObject {
		get {
			return go;
		}
	}

	void CreateLoops() {
		var itr = new Vector3();
		foreach(var loop in loops) {
			loop.ForEach(e => e.isError = false);
			foreach(var e0 in loop) {
				foreach(var e1 in loop) {
					if(e0 == e1) continue;
					var cross = e0.IsCrossed(e1, ref itr);
					e0.isError = e0.isError || cross;
					e1.isError = e1.isError || cross;
				}
			}
		}
		var polygons = Sketch.GetPolygons(loops.Where(l => l.All(e => !e.isError)).ToList());
		mainMesh.Clear();
		MeshUtils.CreateMeshRegion(polygons, ref mainMesh);
	}

	protected override void OnWrite(XmlTextWriter xml) {
		xml.WriteStartElement("references");
		uId.Write(xml, "u");
		vId.Write(xml, "v");
		pId.Write(xml, "o");
		xml.WriteEndElement();
		sketch.Write(xml);
	}

	protected override void OnRead(XmlNode xml) {
		foreach(XmlNode nodeKind in xml.ChildNodes) {
			if(nodeKind.Name != "references") continue;
			foreach(XmlNode idNode in nodeKind.ChildNodes) {
				var name = idNode.Attributes["name"].Value;
				switch(name) {
					case "u": uId.Read(idNode); break;
					case "v": vId.Read(idNode); break;
					case "o": pId.Read(idNode); break;
				}
			}
		}
		sketch.Read(xml);
	}


	protected override void OnClear() {
		sketch.Clear();
		GameObject.Destroy(go);
		GameObject.Destroy(canvas.gameObject);
	}

	public List<List<Entity>> GetLoops() {
		return loops;
	}

	public bool shouldHoverWhenInactive = false;

	public override bool ShouldHoverWhenInactive() {
		return shouldHoverWhenInactive;
	}

	protected override void OnActivate(bool state) {
		go.SetActive(state && visible);
		loopObj.SetActive(state && visible);
	}

	protected override void OnShow(bool state) {
		go.SetActive(state && active);
		loopObj.SetActive(state && active);
	}
}
