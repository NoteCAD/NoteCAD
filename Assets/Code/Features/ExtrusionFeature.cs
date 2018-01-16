using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

class ExtrudedEntity : IEntity {
	Entity entity;
	ExtrusionFeature extrusion;
	int index;

	public ExtrudedEntity(Entity e, ExtrusionFeature ex, int i) {
		entity = e;
		extrusion = ex;
		index = i;
	}

	public Id id {
		get {
			var eid = extrusion.id;
			eid.path.Insert(0, entity.guid);
			eid.path.Insert(0, Id.IndexGuid(index));
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

class ExtrudedPointEntity : IEntity {
	PointEntity entity;
	ExtrusionFeature extrusion;

	public ExtrudedPointEntity(PointEntity e, ExtrusionFeature ex) {
		entity = e;
		extrusion = ex;
	}

	public Id id {
		get {
			var eid = extrusion.id;
			eid.path.Insert(0, entity.guid);
			eid.path.Insert(0, Id.IndexGuid(2));
			return eid;
		}
	}

	public IEnumerable<ExpVector> points {
		get {
			yield return entity.exp;
			yield return entity.exp + extrusion.extrusionDir;
		}
	}

	public IEnumerable<Vector3> segments {
		get {
			var pos = entity.pos;
			yield return pos;
			yield return pos + extrusion.extrusionDir.Eval();
		}
	}

	public ExpVector PointOn(Exp t) {
		throw new NotImplementedException();
	}
}


class CADContainter : CADObject {
	public Entity entity;
	public ExtrusionFeature feature;

	public override Guid guid {
		get {
			return entity.guid;
		}
	}

	public override CADObject parentObject {
		get {
			return feature;
		}
	}

	public override ICADObject GetChild(Guid guid) {
		int index = Id.GetIndex(guid);
		if(index == 2) return new ExtrudedPointEntity(entity as PointEntity, feature);
		return new ExtrudedEntity(entity, feature, index);
	}
}

public class ExtrusionFeature : MeshFeature {
	public Param extrude = new Param("e", 5.0);
	Mesh mesh = new Mesh();
	GameObject go;

	public Sketch sketch {
		get {
			return (source as SketchFeature).GetSketch();
		}
	}

	public override GameObject gameObject {
		get {
			return go;
		}
	}

	public override ICADObject GetChild(Guid guid) {
		return new CADContainter { entity = sketch.GetEntity(guid), feature = this };
	}

	protected override void OnUpdate() {
		if(extrude.changed) {
			MarkDirty();
		}
		extrude.changed = false;
	}

	protected override CombineInstance OnGenerateMesh() {
		var instance = new CombineInstance();
		MeshUtils.CreateMeshExtrusion(Sketch.GetPolygons((source as SketchFeature).GetLoops()), (float)extrude.value, ref mesh);
		instance.mesh = mesh;
		instance.transform = (source as SketchFeature).GetTransform();
		return instance;
	}

	protected override void OnUpdateDirty() {
		GameObject.Destroy(go);
		go = new GameObject("ExtrusionFeature");
		var bottom = GameObject.Instantiate(source.gameObject, go.transform);
		bottom.SetActive(true);
		var top = GameObject.Instantiate(source.gameObject, go.transform);
		top.SetActive(true);
		var dir = extrusionDir.Eval();
		//top.transform.Translate(dir);
		top.transform.position += dir;
		go.SetActive(visible);
		//go.transform.position = skf.gameObject.transform.position;
		//go.transform.rotation = skf.gameObject.transform.rotation;
	}

	protected override void OnShow(bool state) {
		if(go != null) {
			go.SetActive(state);
		}
	}

	protected override void OnWrite(XmlTextWriter xml) {

	}

	public ExpVector extrusionDir {
		get {
			var skf = source as SketchFeature;
			return skf.GetNormal().Normalized() * extrude.exp;
		}
	}

	protected override ICADObject OnHover(Vector3 mouse, Camera camera, Matrix4x4 tf, ref double dist) {
		var sk = source as SketchFeature;

		double d0 = -1;
		var r0 = sk.Hover(mouse, camera, tf, ref d0);
		if(!(r0 is Entity)) r0 = null;

		Matrix4x4 move = Matrix4x4.Translate(Vector3.forward * (float)extrude.value);
		double d1 = -1;
		var r1 = sk.Hover(mouse, camera, tf * move, ref d1);
		if(!(r1 is Entity)) r1 = null;

		if(r1 != null && (r0 == null || d1 < d0)) {
			r0 = new ExtrudedEntity(r1 as Entity, this, 1);
			d0 = d1;
		} else if(r0 != null) {
			r0 = new ExtrudedEntity(r0 as Entity, this, 0);
		}

		var points = sk.GetSketch().entityList.OfType<PointEntity>();
		var dir = extrusionDir.Eval();
		double min = -1.0;
		PointEntity hover = null;
		foreach(var p in points) {
			var pp = p.pos;
			var p0 = camera.WorldToScreenPoint(pp);
			var p1 = camera.WorldToScreenPoint(pp + dir);
			double d = GeomUtils.DistancePointSegment2D(mouse, p0, p1);
			if(d > 5.0) continue;
			if(min >= 0.0 && d > min) continue;
			min = d;
			hover = p;
		}

		if(hover != null && (r0 == null || d0 > min)) {
			dist = min;
			return new ExtrudedPointEntity(hover, this);
		}

		if(r0 != null) {
			dist = d0;
			return r0;
		}

		return null;
	}

	//public override ISketchObject GetObjectById

	protected override void OnGenerateEquations(EquationSystem sys) {
		sys.AddParameter(extrude);
	}
}
	