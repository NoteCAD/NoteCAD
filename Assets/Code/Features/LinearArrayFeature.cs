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
			eid.path.Add(new Id(index));
			eid.path.AddRange(entity.id.path);
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
		var shift = feature.shiftDir * index;
		return entity.PointOn(t) + shift;
	}

	public ExpVector TangentAt(Exp t) {
		return entity.TangentAt(t);
	}

	public Exp Length() {
		return entity.Length();
	}

	public Exp Radius() {
		return entity.Radius();
	}

	public ExpVector Center() {
		var shift = feature.shiftDir * index;
		return entity.Center() + shift;
	}
}

[Serializable]
public class LinearArrayFeature : SketchFeature {
	public Param dx = new Param("dx", 5.0);
	public Param dy = new Param("dy", 5.0);
	public int repeatCount = 5;

	public LinearArrayFeature() {
		shouldHoverWhenInactive = false;
	}

	public Sketch sourceSketch {
		get {
			return (source as SketchFeature).GetSketch();
		}
	}

	public override ICADObject GetObjectById(IdPath id, int index) {
		if(id.path[index].value == -1) {
			return base.GetObjectById(id, index);
		}

		var arrayIndex = id.path[index].value;
		var entity = detail.GetObjectById(id, index + 1) as IEntity;
		if(entity == null) return null;
		return new ArrayEntity(entity, this, arrayIndex);
	}

	protected override void OnUpdate() {
		if(dx.changed || dy.changed) {
			MarkDirty();
		}
		dx.changed = false;
		dy.changed = false;
	}

	protected override void OnDraw(Matrix4x4 tf) {
		
		var sk = source as SketchFeature;		
		var dir = new Vector3((float)dx.value, (float)dy.value, 0f);
		var dtf = Matrix4x4.Translate(dir);

		for(int i = 0; i < repeatCount; i++) {
			sk.Draw(tf);
			tf *= dtf;
		}
		//var bounds = sk.bounds.Transformed(tf);
		//Draw(GeometryUtility.CalculateFrustumPlanes(Camera.main), bounds, tf, 0, repeatCount - 1);
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
			//var skf = source as SketchFeature;
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

	void DrawGizmoBounds(Bounds bb, Color color) {
		if(!drawGizmos) return;
			if(drawGizmos) {
				Gizmos.color = color;
				Gizmos.DrawWireCube(bb.center, bb.size);
			}
	}
	/*
	void Draw(Plane[] planes, Bounds bounds, Matrix4x4 tf, int left, int right) {
		int lmid = (int)Mathf.Floor((left + right) / 2.0f);
		int rmid = (int)Mathf.Ceil((left + right) / 2.0f);
		float expand = (float)Sketch.hoverRadius * Constraint.getPixelSize() * 2;

		for(int i = 0; i < 2; i++) {
			var l = left;
			var r = lmid;
			if(i == 1) {
				l = rmid;
				r = right;
			}
			var bb = TwoBounds(bounds, l, r);
			bb.Expand(expand);
			if(GeometryUtility.TestPlanesAABB(planes, bb)) {
				DrawGizmoBounds(bb, Color.red);
				if(l == r) {
					var sk = source as SketchFeature;
					var dir = new Vector3((float)dx.value, (float)dy.value, 0f);
					sk.Draw(tf * Matrix4x4.Translate(dir * l));

					DrawGizmoBounds(bb, Color.green);
				} else {
					Draw(planes, bounds, tf, l, r);
				}
			}
		}
	}
	*/
	void FindHits(Ray ray, Bounds bounds, int left, int right, ref HashSet<int> hits) {
		int lmid = (int)Mathf.Floor((left + right) / 2.0f);
		int rmid = (int)Mathf.Ceil((left + right) / 2.0f);
		float expand = (float)Sketch.hoverRadius * Constraint.getPixelSize() * 2;

		for(int i = 0; i < 2; i++) {
			var l = left;
			var r = lmid;
			if(i == 1) {
				l = rmid;
				r = right;
			}
			var bb = TwoBounds(bounds, l, r);
			bb.Expand(expand);
			if(bb.IntersectRay(ray)) {
				DrawGizmoBounds(bb, Color.red);
				if(l == r) {
					hits.Add(r);
					DrawGizmoBounds(bb, Color.green);
				} else {
					FindHits(ray, bounds, l, r, ref hits);
				}
			}
		}
	}

	protected override ICADObject OnHover(Vector3 mouse, Camera camera, Matrix4x4 tf, ref double dist) {
		var sk = source as SketchFeature;
		var bounds = sk.bounds.Transformed(tf);
		var ray = camera.ScreenPointToRay(mouse);

		HashSet<int> hits = new HashSet<int>();
		FindHits(ray, bounds, 0, repeatCount - 1, ref hits);

		foreach(var hit in hits) {
			Matrix4x4 move = Matrix4x4.Translate(shiftDir.Eval() * hit);
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
		drawGizmos = true;
		//HashSet<int> hits = new HashSet<int>();
		//var ray = camera.ScreenPointToRay(mouse);
		//FindHits(ray, 0, repeatCount - 1, ref hits);
		double dist = 0;
		OnHover(mouse, camera, Matrix4x4.identity, ref dist);
		drawGizmos = false;
	}

}
	