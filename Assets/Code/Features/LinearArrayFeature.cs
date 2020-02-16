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
			foreach(var pe in entity.PointsInPlane(null)) {
				yield return feature.Transform(pe, index);
			}
		}
	}

	public IEnumerable<IEnumerable<Vector3>> segments {
		get {
			//var tf = feature.GetTransform(index);
			foreach(var lp in (entity as IEntity).SegmentsInPlane(null)) {
				//yield return lp.Select(p => tf.MultiplyPoint3x4(p));
				yield return lp.Select(p => feature.Transform(p, index).Eval());
			}
		}
	}

	public ExpVector PointOn(Exp t) {
		return feature.Transform(entity.PointOn(t), index);
	}

	public ExpVector TangentAt(Exp t) {
		return feature.TransformDir(entity.TangentAt(t), index);
	}

	public Exp Length() {
		return null;
	}

	public Exp Radius() {
		return null;
	}

	public ExpVector Center() {
		return null;
	}
}

[Serializable]
public class LinearArrayFeature : SketchFeature {
	public Param dx = new Param("dx", 5.0);
	public Param dy = new Param("dy", 5.0);
	public Param da = new Param("da", 5.0 * Math.PI / 180.0);
	public Param dsx = new Param("dsx", 1.0);
	public Param dsy = new Param("dsy", 1.0);
	public int repeatCount = 5;
	bool translate_ = true;
	bool rotate_ = false;
	bool scale_ = false;

	public bool translate { 
		get {
			return translate_;
		}
		set {
			translate_ = value;
			MarkTopologyChanged();
		}
	}

	public bool rotate { 
		get {
			return rotate_;
		}
		set {
			rotate_ = value;
			MarkTopologyChanged();
		}
	}

	/*public */bool scale { 
		get {
			return scale_;
		}
		set {
			scale_ = value;
			MarkTopologyChanged();
		}
	}

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
		if(dx.changed || dy.changed || da.changed || dsx.changed || dsy.changed) {
			MarkDirty();
		}
		dx.changed = false;
		dy.changed = false;
		da.changed = false;
		dsx.changed = false;
		dsy.changed = false;
	}

	protected override List<List<IEntity>> OnGenerateLoops() {
		var result = new List<List<IEntity>>();
		var loops = sourceSketch.GenerateLoops();
		for(int i = 0; i < repeatCount; i++) {
			result.AddRange(loops.Select(el => el.Select(e => new ArrayEntity(e, this, i) as IEntity).ToList()));
		}
		result.AddRange(base.OnGenerateLoops());
		return result;
	}

	protected override void OnDraw(Matrix4x4 tf) {
		
		var sk = source as SketchFeature;		

		for(int i = 0; i < repeatCount; i++) {
			var dtf = GetTransform(i);
			sk.Draw(tf * dtf);
		}
		//var bounds = sk.bounds.Transformed(tf);
		//Draw(GeometryUtility.CalculateFrustumPlanes(Camera.main), bounds, tf, 0, repeatCount - 1);
	}

	public override void DrawEntities(ICanvas canvas) {

		base.DrawEntities(canvas);
	}

	/*
	protected override void OnWriteMeshFeature(XmlTextWriter xml) {
		xml.WriteAttributeString("length", extrude.value.ToStr());
	}

	protected override void OnReadMeshFeature(XmlNode xml) {
		extrude.value = xml.Attributes["length"].Value.ToDouble();
	}
	*/

	public ExpVector Rotate(ExpVector v, Exp angle) {
		var c = Exp.Cos(angle);
		var s = Exp.Sin(angle);
		return new ExpVector(
			v.x * c - v.y * s,
			v.x * s + v.y * c,
			0.0
		);
	}

	public Matrix4x4 Rotate(float angle) {
		return Matrix4x4.Rotate(Quaternion.AxisAngle(Vector3.forward, angle));
	}

	public Matrix4x4 GetTransform(long index) {
		Matrix4x4 result = Matrix4x4.identity;
		if(translate) {
			result *= Matrix4x4.Translate(new Vector3((float)dx.value, (float)dy.value, 0f) * index);
		}
		if(rotate) {
			result *= Rotate((float)(da.value * index));
		}
		if(scale) {
			result *= Matrix4x4.Scale(new Vector3((float)dsx.value, (float)dsy.value, 1f) * index);
		}

		return result;
	}

	public ExpVector Transform(ExpVector v, long index) {
		ExpVector result = v;
		if(scale) {
			result.x *= dsx.exp * index;
			result.y *= dsy.exp * index;
		}
		if(rotate) {
			result = Rotate(result, da.exp * index);
		}
		if(translate) {
			result.x += dx.exp * index;
			result.y += dy.exp * index;
		}
		return result;
	}

	public ExpVector TransformDir(ExpVector v, long index) {
		ExpVector result = v;
		if(rotate) {
			result = Rotate(result, da.exp * index);
		}
		return result;
	}
	
	Bounds TwoBounds(Bounds source, int min, int max) {
		var one = source.Transformed(GetTransform(min));
		for(int i = min + 1; i <= max; i++) {
			var two = source.Transformed(GetTransform(i));
			one.Encapsulate(two);
		}
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
	void FindHits(Ray ray, Bounds bounds, Matrix4x4 tf, int left, int right, ref HashSet<int> hits) {
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
			bb = bb.Transformed(tf);
			if(bb.IntersectRay(ray)) {
				DrawGizmoBounds(bb, Color.red);
				if(l == r) {
					hits.Add(r);
					DrawGizmoBounds(bb, Color.green);
				} else {
					FindHits(ray, bounds, tf, l, r, ref hits);
				}
			}
		}
	}

	protected override ICADObject OnHover(Vector3 mouse, Camera camera, Matrix4x4 tf, HoverFilter filter, ref double dist) {
		var sk = source as SketchFeature;
		var bounds = sk.bounds;
		var ray = camera.ScreenPointToRay(mouse);

		HashSet<int> hits = new HashSet<int>();
		FindHits(ray, bounds, tf, 0, repeatCount - 1, ref hits);

		foreach(var hit in hits) {
			Matrix4x4 move = GetTransform(hit);
			double d1 = -1;
			var r1 = sk.Hover(mouse, camera, tf * move, filter, ref d1);

			if(r1 is IEntity) {
				dist = d1;
				return new ArrayEntity(r1 as IEntity, this, hit);
			}
		}
		return base.OnHover(mouse, camera, tf, filter, ref dist);
	}

	protected override void OnGenerateEquations(EquationSystem sys) {
		if(translate) {
			sys.AddParameter(dx);
			sys.AddParameter(dy);
		}
		if(rotate) {
			sys.AddParameter(da);
		}
		if(scale	) {
			sys.AddParameter(dsx);
			sys.AddParameter(dsy);
		}
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
		OnHover(mouse, camera, Matrix4x4.identity, null, ref dist);
		drawGizmos = false;
	}

	protected override void OnReadSketchFeature(XmlNode xml) {
		repeatCount = Convert.ToInt32(xml.Attributes["repeatCount"].Value);
		translate = Convert.ToBoolean(xml.Attributes["translate"].Value);
		rotate = Convert.ToBoolean(xml.Attributes["rotate"].Value);
		scale = Convert.ToBoolean(xml.Attributes["scale"].Value);

		dx.value = xml.Attributes["dx"].Value.ToDouble();
		dy.value = xml.Attributes["dy"].Value.ToDouble();
		da.value = xml.Attributes["da"].Value.ToDouble();
		dsx.value = xml.Attributes["dsx"].Value.ToDouble();
		dsy.value = xml.Attributes["dsy"].Value.ToDouble();
	}

	protected override void OnWriteSketchFeature(XmlWriter xml) {
		xml.WriteAttributeString("repeatCount", repeatCount.ToString());
		xml.WriteAttributeString("translate", translate.ToString());
		xml.WriteAttributeString("rotate", rotate.ToString());
		xml.WriteAttributeString("scale", scale.ToString());

		xml.WriteAttributeString("dx", dx.value.ToStr());
		xml.WriteAttributeString("dy", dy.value.ToStr());
		xml.WriteAttributeString("da", da.value.ToStr());
		xml.WriteAttributeString("dsx", dsx.value.ToStr());
		xml.WriteAttributeString("dsy", dsy.value.ToStr());
	}

}
	