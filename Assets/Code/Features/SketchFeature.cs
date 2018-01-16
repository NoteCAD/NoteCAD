using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
/*
class SketchEntity : IEntity {
	Entity entity;
	SketchFeature sketch;

	public SketchEntity(Entity e, SketchFeature sk) {
		entity = e;
		sketch = sk;
	}

	public Id id {
		get {
			var eid = sketch.id;
			eid.path.Insert(0, entity.guid);
			return eid;
		}
	}

	public IEnumerable<ExpVector> points {
		get {
			var shift = extrusion.extrusionDir * index;
			foreach(var p in entity.points) {
				yield return p.exp + shift;
			}
		}
	}

	public IEnumerable<Vector3> segments {
		get {
			var shift = extrusion.extrusionDir.Eval() * index;
			var ie = entity as IEntity;
			foreach(var p in (entity as IEntity).segments) {
				yield return p + shift;
			}
		}
	}

	public ExpVector PointOn(Exp t) {
		throw new NotImplementedException();
	}
}
*/
public class SketchFeature : Feature {
	List<List<Entity>> loops = new List<List<Entity>>();
	protected LineCanvas canvas;
	Sketch sketch;
	Mesh mainMesh;
	GameObject go;
	Matrix4x4 transform;

	Id uId = new Id();
	Id vId = new Id();
	Id pId = new Id();

	public IEntity u {
		get {
			return detail.GetObjectById(uId) as IEntity;
		}
		set {
			uId = value.id;
		}
	}

	public IEntity v {
		get {
			return detail.GetObjectById(vId) as IEntity;
		}
		set {
			vId = value.id;
		}
	}
	
	public IEntity p {
		get {
			return detail.GetObjectById(pId) as IEntity;
		}
		set {
			pId = value.id;
		}
	}
	
	public ExpVector GetNormal() {
		if(u == null) return new ExpVector(0, 0, 1);
		var ud = u.GetDirection();
		var vd = v.GetDirection();
		return ExpVector.Cross(ud, vd);
	}

	public ExpVector GetPosition() {
		if(p == null) return new ExpVector(0, 0, 0);
		return p.points.First();
	}

	public Matrix4x4 GetTransform() {
		if(u == null) return Matrix4x4.identity;
		var ud = u.GetDirection().Eval().normalized;
		var vd = v.GetDirection().Eval();
		var nd = Vector3.Cross(ud, vd).normalized;
		vd = Vector3.Cross(nd, ud).normalized;
		Vector4 p4 = p.points.First().Eval();
		p4.w = 1.0f;

		Matrix4x4 result = Matrix4x4.identity;
		result.SetColumn(0, ud);
		result.SetColumn(1, vd);
		result.SetColumn(2, nd);
		result.SetColumn(3, p4);
		return result;
	}

	public Vector3 WorldToLocal(Vector3 pos) {
		return transform.inverse.MultiplyPoint(pos);
	}

	public Sketch GetSketch() {
		return sketch;
	}

	public override ICADObject GetChild(Guid guid) {
		if(sketch.guid == guid) return sketch;
		return null;
	}

	public SketchFeature() {
		sketch = new Sketch();
		sketch.feature = this;
		canvas = GameObject.Instantiate(EntityConfig.instance.lineCanvas);
		mainMesh = new Mesh();
		go = new GameObject("SketchFeature");
		var mf = go.AddComponent<MeshFilter>();
		var mr = go.AddComponent<MeshRenderer>();
		mf.mesh = mainMesh;
		mr.material = EntityConfig.instance.meshMaterial;
		canvas.parent = go;
	}

	protected override void OnGenerateEquations(EquationSystem sys) {
		sketch.GenerateEquations(sys);
	}

	public bool IsTopologyChanged() {
		return sketch.topologyChanged || sketch.constraintsTopologyChanged;
	}

	protected override void OnUpdateDirty() {
		transform = GetTransform();
		go.transform.position = transform.MultiplyPoint(Vector3.zero);
		go.transform.rotation = Quaternion.LookRotation(transform.GetColumn(2), transform.GetColumn(1));
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

		if(sketch.IsConstraintsChanged()) {
			canvas.ClearStyle("constraints");
			canvas.SetStyle("constraints");
			foreach(var c in sketch.constraintList) {
				c.Draw(canvas);
			}
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

		var loopsChanged = loops.Any(l => l.Any(e => e.IsChanged()));
		if(loopsChanged || sketch.topologyChanged) {
			CreateLoops();
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
		if(mainMesh == null) {
		}
		mainMesh.Clear();
		MeshUtils.CreateMeshRegion(polygons, ref mainMesh);
	}

	protected override void OnWrite(XmlTextWriter xml) {
		sketch.Write(xml);
	}

	protected override void OnRead(XmlNode xml) {
		sketch.Read(xml);
	}


	protected override void OnClear() {
		sketch.Clear();
		Update();
	}

	public List<List<Entity>> GetLoops() {
		return loops;
	}

	public override bool ShouldHoverWhenInactive() {
		return false;
	}

	protected override void OnActivate(bool state) {
		go.SetActive(state);
	}
}
