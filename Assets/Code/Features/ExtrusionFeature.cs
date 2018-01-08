using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;


class ExtrudedPoint : IPoint {
	public ExpVector expression;

	public ExpVector exp {
		get {
			return expression;
		}
	}
}

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
			eid.path.Insert(0, new Guid(index, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
			return eid;
		}
	}

	public IEnumerable<IPoint> points {
		get {
			var shift = new ExpVector(0, 0, extrusion.extrude.exp * index);
			foreach(var p in entity.points) {
				yield return new ExtrudedPoint { expression = p.exp + shift };
			}
		}
	}

	public IEnumerable<Vector3> segments {
		get {
			var shift = new Vector3(0, 0, (float)extrusion.extrude.value * index);
			foreach(var p in (entity as IEntity).segments) {
				yield return p + shift;
			}
		}
	}

	public ExpVector PointOn(Exp t) {
		throw new NotImplementedException();
	}
}

public class ExtrusionFeature : MeshFeature {
	public Param extrude = new Param("e", 5.0);
	Mesh mesh = new Mesh();
	GameObject go;

	public override GameObject gameObject {
		get {
			return null;
		}
	}

	public override CADObject GetChild(Guid guid) {
		throw new NotImplementedException();
	}

	protected override void OnUpdate() {
		if(extrude.changed) {
			MarkDirty();
		}
		extrude.changed = false;
	}

	protected override Mesh OnGenerateMesh() {
		MeshUtils.CreateMeshExtrusion(Sketch.GetPolygons((source as SketchFeature).GetLoops()), (float)extrude.value, ref mesh);
		return mesh;
	}

	protected override void OnUpdateDirty() {
		GameObject.Destroy(go);
		go = new GameObject("ExtrusionFeature");
		var bottom = GameObject.Instantiate(source.gameObject, go.transform);
		bottom.SetActive(true);
		var top = GameObject.Instantiate(source.gameObject, go.transform);
		top.SetActive(true);
		top.transform.localPosition = new Vector3(0, 0, (float)extrude.value);
		go.SetActive(visible);
	}

	protected override void OnShow(bool state) {
		if(go != null) {
			go.SetActive(state);
		}
	}

	protected override void OnWrite(XmlTextWriter xml) {

	}

	protected override ISketchObject OnHover(Vector3 mouse, Camera camera, Matrix4x4 tf, ref double dist) {
		var sk = source as SketchFeature;
		double d0 = -1;
		var r0 = sk.Hover(mouse, camera, tf, ref d0);
		if(!(r0 is Entity)) r0 = null;

		Matrix4x4 move = Matrix4x4.Translate(Vector3.forward * (float)extrude.value);
		double d1 = -1;
		var r1 = sk.Hover(mouse, camera, tf * move, ref d1);
		if(!(r1 is Entity)) r1 = null;

		if(r0 == null || r1 != null && d1 < d0) {
			r0 = new ExtrudedEntity(r1 as Entity, this, 1);
			d0 = d1;
		} else if(r0 != null) {
			r0 = new ExtrudedEntity(r0 as Entity, this, 0);
		}

		if(r0 != null && (dist < 0.0 || d0 < dist)) {
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
	