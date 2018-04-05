using Csg;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

class ArrayEntity : IEntity {
	IEntity entity;
	LinearArrayFeature feature;
	long index;

	IEntityType IEntity.type { get { return entity.type; } }

	public ArrayEntity(IEntity e, LinearArrayFeature f, long i) {
		entity = e;
		feature = f;
		index = i;
	}

	public IdPath id {
		get {
			var eid = feature.id;
			if(entity is Entity) {
				eid.path.Insert(0, (entity as Entity).guid.WithSecond(index));
			}
			return eid;
		}
	}

	public IPlane plane {
		get {
			return null;
		}
	}

	public IEnumerable<ExpVector> points {
		get {
			var shift = feature.shiftDir * index;
			foreach(var pe in entity.PointsInPlane(null)) {
				yield return pe + shift;
			}
		}
	}

	public IEnumerable<Vector3> segments {
		get {
			var shift = feature.shiftDir.Eval() * index;
			foreach(var p in (entity as IEntity).SegmentsInPlane(null)) {
				yield return p + shift;
			}
		}
	}

	public ExpVector PointOn(Exp t) {
		throw new NotImplementedException();
	}
}

public class LinearArrayFeature : SketchFeature {
	public Param dx = new Param("dx", 5.0);
	public Param dy = new Param("dy", 5.0);
	public int repeatCount = 5;
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

	public override ICADObject GetChild(Id guid) {
		var entity = sketch.GetEntity(guid.WithoutSecond());
		return new ArrayEntity(entity, this, guid.second);
	}

	protected override void OnUpdate() {
		if(dx.changed || dy.changed) {
			MarkDirty();
		}
		dx.changed = false;
		dy.changed = false;
	}

	protected override void OnUpdateDirty() {
		base.OnUpdateDirty();
		GameObject.Destroy(go);
		go = new GameObject("LinearArrayFeature");
		var sk = (source as SketchFeature).GetSketch();
		
		for(int i = 0; i < repeatCount; i++) {
			var mesh = GameObject.Instantiate(source.gameObject, go.transform);
			mesh.SetActive(true);
			var dir = shiftDir.Eval() * i;
			mesh.transform.position += dir;
			go.SetActive(visible);
		}
	}

	protected override void OnShow(bool state) {
		if(go != null) {
			go.SetActive(state);
		}
	}

	protected override void OnClear() {
		GameObject.Destroy(go);
	}
	/*
	protected override void OnWriteMeshFeature(XmlTextWriter xml) {
		xml.WriteAttributeString("length", extrude.value.ToStr());
	}

	protected override void OnReadMeshFeature(XmlNode xml) {
		extrude.value = xml.Attributes["length"].Value.ToDouble();
	}
	*/
	public ExpVector shiftDir {
		get {
			var skf = source as SketchFeature;
			return new ExpVector(dx, dy, 0f);
		}
	}
	
	Bounds TwoBounds(Bounds source, int min, int max) {
		var shift = shiftDir.Eval();

		var one = source;
		one.center += shift * min;

		var two = source;
		two.center += shift * max;

		one.Encapsulate(two);
		return one;
	}

	public static bool drawGizmos = false;

	void FindHits(Ray ray, Bounds bounds, int left, int right, ref HashSet<int> hits) {
		int lmid = (int)Mathf.Floor((left + right) / 2.0f);
		int rmid = (int)Mathf.Ceil((left + right) / 2.0f);
		float expand = (float)Sketch.hoverRadius * Constraint.getPixelSize();
		var bb = TwoBounds(bounds, left, lmid);
		bb.Expand(expand);
		if(bb.IntersectRay(ray)) {
			if(drawGizmos) {
				Gizmos.color = Color.red;
				Gizmos.DrawWireCube(bb.center, bb.size);
			}
			if(left == lmid) {
				hits.Add(lmid);
				if(drawGizmos) {
					Gizmos.color = Color.green;
					Gizmos.DrawWireCube(bb.center, bb.size);
				}
			} else {
				FindHits(ray, bounds, left, lmid, ref hits);
			}
		}
		bb = TwoBounds(bounds, rmid, right);
		bb.Expand(expand);
		if(bb.IntersectRay(ray)) {
			if(drawGizmos) {
				Gizmos.color = Color.red;
				Gizmos.DrawWireCube(bb.center, bb.size);
			}
			if(rmid == right) {
				hits.Add(rmid);
				if(drawGizmos) {
					Gizmos.color = Color.green;
					Gizmos.DrawWireCube(bb.center, bb.size);
				}
			} else {
				FindHits(ray, bounds, rmid, right, ref hits);
			}
		}
	}

	protected override ICADObject OnHover(Vector3 mouse, Camera camera, UnityEngine.Matrix4x4 tf, ref double dist) {
		var sk = source as SketchFeature;
		var bounds = sk.bounds.Transformed(tf);
		var ray = camera.ScreenPointToRay(mouse);

		HashSet<int> hits = new HashSet<int>();
		FindHits(ray, bounds, 0, repeatCount - 1, ref hits);

		foreach(var hit in hits) {
			UnityEngine.Matrix4x4 move = UnityEngine.Matrix4x4.Translate(shiftDir.Eval() * hit);
			double d1 = -1;
			var r1 = sk.Hover(mouse, camera, tf * move, ref d1);

			if(r1 is IEntity) {
				dist = d1;
				return new ArrayEntity(r1 as IEntity, this, hit);
			}
		}
		return base.OnHover(mouse, camera, tf, ref dist);
	}

	protected override void OnGenerateEquations(EquationSystem sys) {
		sys.AddParameter(dx);
		sys.AddParameter(dy);
		//sketch.GenerateEquations(sys);
		base.OnGenerateEquations(sys);
	}

	public override Bounds bounds {
		get {
			var sk = source as SketchFeature;
			return TwoBounds(sk.bounds, 0, repeatCount - 1);
		}
	}

	public void DrawGizmos(Vector3 mouse, Camera camera) {
		var ray = camera.ScreenPointToRay(mouse);
		HashSet<int> hits = new HashSet<int>();
		drawGizmos = true;
		//FindHits(ray, 0, repeatCount - 1, ref hits);
		double dist = 0;
		OnHover(mouse, camera, UnityEngine.Matrix4x4.identity, ref dist);
		drawGizmos = false;
	}

}
	