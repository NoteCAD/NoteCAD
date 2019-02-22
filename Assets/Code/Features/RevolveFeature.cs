using Csg;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

class RevolvedEntity : IEntity {
	Entity entity;
	RevolveFeature feature;
	long index;

	IEntityType IEntity.type { get { return entity.type; } }

	public RevolvedEntity(Entity e, RevolveFeature f, long i) {
		entity = e;
		feature = f;
		index = i;
	}

	public IdPath id {
		get {
			var eid = feature.id;
			eid.path.Add(entity.guid.WithSecond(index));
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

	public IEnumerable<Vector3> segments {
		get {
			foreach(var p in (entity as IEntity).SegmentsInPlane(null)) {
				yield return feature.Transform(p, index);
			}
		}
	}

	public ExpVector PointOn(Exp t) {
		return feature.Transform(entity.plane.FromPlane(entity.PointOn(t)), index);
	}

	public ExpVector TangentAt(Exp t) {
		return null;
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

class RevolvedPointEntity : IEntity {
	PointEntity entity;
	RevolveFeature feature;

	IEntityType IEntity.type { get { return IEntityType.Helix; } }

	public RevolvedPointEntity(PointEntity e, RevolveFeature f) {
		entity = e;
		feature = f;
	}

	public IdPath id {
		get {
			var eid = feature.id;
			eid.path.Add(entity.guid.WithSecond(2));
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
			var exp = entity.plane.FromPlane(entity.exp);
			yield return feature.Transform(exp, 0);
			yield return feature.Transform(exp, 1);
		}
	}

	public IEnumerable<Vector3> segments {
		get {
			var point = entity.plane.FromPlane(entity.pos);
			int subdiv = (int)Mathf.Ceil(Mathf.Abs((float)feature.angle.value / Mathf.PI * 180f) / (float)feature.meshAngleStep);
			var da = (float)feature.angle.value / subdiv;


			var ax = feature.GetAxis().Eval();
			var axn = ax.normalized;
			var o = feature.GetOrigin().Eval();
			var prj = ExpVector.ProjectPointToLine(point, o, o + ax);
			var ra = Mathf.Atan2((float)feature.step.value / 4.0f, (point - prj).magnitude);
			var rot = ExpVector.RotateAround(point, point - prj, o, ra);

			for(int i = 0; i <= subdiv; i++) {
				var a = i * da;
				var t = a / (2.0f * Mathf.PI);
				var res = ExpVector.RotateAround(rot, ax, o, a);
				yield return res + axn * t * (float)feature.step.value;
			}
		}
	}

	public ExpVector PointOn(Exp t) {
		var exp = entity.plane.FromPlane(entity.exp);
		return feature.PointOn(t, exp);
	}

	public ExpVector TangentAt(Exp t) {
		Param p = new Param("pOn");
		var pt = PointOn(p);
		var result = new ExpVector(pt.x.Deriv(p), pt.y.Deriv(p), pt.z.Deriv(p));
		result.x.Substitute(p, t);
		result.y.Substitute(p, t);
		result.z.Substitute(p, t);
		return result;
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
/*
class ExtrudedPlane : IEntity, IPlane {
	Entity entity;
	ExtrusionFeature extrusion;

	IEntityType IEntity.type { get { return entity.type; } }

	public ExtrudedPlane(Entity e, ExtrusionFeature ex) {
		entity = e;
		extrusion = ex;
	}

	public IdPath id {
		get {
			var eid = extrusion.id;
			eid.path.Insert(0, entity.guid.WithSecond(3));
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
			yield break;
		}
	}

	public IEnumerable<Vector3> segments {
		get {
			yield break;
		}
	}

	public ExpVector PointOn(Exp t) {
		throw new NotImplementedException();
	}
}
*/
[Serializable]
public class RevolveFeature : MeshFeature {
	public Param angle = new Param("a", 2f * Math.PI);
	public Param step = new Param("s", 0f);

	double _meshAngleStep = 20f;	
	[SerializeField] public double meshAngleStep { get { return _meshAngleStep; } set { _meshAngleStep = value; MarkDirty(); } }

	[SerializeField]
	public double Angle {
		get {
			return angle.value / Math.PI * 180.0;
		}
		set {
			angle.value = value * Math.PI / 180.0;
		}
	}

	[SerializeField]
	public double Step {
		get {
			return step.value;
		}
		set {
			step.value = value;
		}
	}

	bool _stepFixed = true;
	[SerializeField] public bool stepFixed { get { return _stepFixed; } set { _stepFixed = value; MarkTopologyChanged(); } }

	bool _angleFixed = true;
	[SerializeField] public bool angleFixed { get { return _angleFixed; } set { _angleFixed = value; MarkTopologyChanged(); } }

	IdPath axisId = new IdPath();
	IdPath originId = new IdPath();
	bool shouldInvertAxis = false;

	public IEntity axis {
		get {
			return detail.GetObjectById(axisId) as IEntity;
		}
		set {
			axisId = value.id;
		}
	}
	
	public IEntity origin {
		get {
			return detail.GetObjectById(originId) as IEntity;
		}
		set {
			originId = value.id;
		}
	}

	public Sketch sourceSketch {
		get {
			return (source as SketchFeature).GetSketch();
		}
	}

	public override ICADObject GetChild(Id guid) {
		var result = base.GetChild(guid);
		if(result != null) return result;

		var entity = sourceSketch.GetEntity(guid.WithoutSecond());
		if(guid.second == 2) return new RevolvedPointEntity(entity as PointEntity, this);
		return new RevolvedEntity(entity, this, guid.second);
	}

	protected override void OnUpdate() {
		if(angle.changed || step.changed) {
			MarkDirty();
			angle.changed = false;
			step.changed = false;
		}
	}

	public ExpVector GetAxis(IPlane plane = null) {
		var ax = axis.GetDirectionInPlane(plane);
		if(shouldInvertAxis) ax = -ax;
		return ax;
	}

	public ExpVector GetOrigin(IPlane plane = null) {
		return origin.GetPointAtInPlane(0, plane);
	}

	protected override Solid OnGenerateMesh() {
		var loops = (source as SketchFeature).GetLoops();
		var axis = GetAxis().Eval().normalized;
		var point = GetOrigin().Eval();
		return MeshUtils.CreateSolidRevolve(loops, (float)(angle.value / Math.PI * 180.0), (float)step.value, axis, point, (float)meshAngleStep, (source as SketchFeature).GetTransform(), id);
	}

	public ExpVector Transform(ExpVector point, long i) {
		return PointOn(new Exp(angle) * i, point);
	}

	public Vector3 Transform(Vector3 point, long i) {
		return PointOn((float)angle.value * i, point);
	}
	
	public ExpVector PointOn(Exp a, ExpVector point) {
		/*
		var ax = GetAxis();
		var axn = ax.Normalized();
		var o = GetOrigin();
		var prj = ExpVector.ProjectPointToLine(point, o, o + ax);
		var ra = Exp.Atan2(new Exp(step) / 4.0, (point - prj).Magnitude());
		*/
		var ax = GetAxis().Eval();
		var axn = ax.normalized;
		var o = GetOrigin().Eval();
		var prj = ExpVector.ProjectPointToLine(point.Eval(), o, o + ax);

		var t = a / (2.0 * Mathf.PI);
		var ra = Exp.Atan2(new Exp(step) / 4.0, (point.Eval() - prj).magnitude);
		var res = ExpVector.RotateAround(point, point - prj, o, ra);
		res = ExpVector.RotateAround(res, ax, o, a);
		return res + (ExpVector)axn * t * step;
	}

	public Vector3 PointOn(float a, Vector3 point) {
		var ax = GetAxis().Eval();
		var axn = ax.normalized;
		var t = a / (2.0f * Mathf.PI);
		var o = GetOrigin().Eval();
		var prj = ExpVector.ProjectPointToLine(point, o, o + ax);
		var ra = Mathf.Atan2((float)step.value / 4.0f, (point - prj).magnitude);
		var res = ExpVector.RotateAround(point, point - prj, o, ra);
		res = ExpVector.RotateAround(res, ax, o, a);
		return res + axn * t * (float)step.value;
	}

	protected override void OnUpdateDirty() {
		canvas.SetStyle("entities");
		var sk = (source as SketchFeature).GetSketch();

		bool axisDirectionFound = false;
		var ax = axis.GetDirectionInPlane(null).Eval();
		var o = GetOrigin(null).Eval();
		foreach(var e in sk.entityList) {
			if(e.type != IEntityType.Point) continue;
			var pos = e.PointExpInPlane(null).Eval();

			var prj = ExpVector.ProjectPointToLine(pos, o, o + ax);
			if((prj - pos).magnitude < 1e-6) continue;

			var ax1 = Vector3.Cross(sourceSketch.plane.n, pos - prj);
			shouldInvertAxis = (Vector3.Dot(ax, ax1) > 0f);
			axisDirectionFound = true;
			break;
		}

		// probably, some curved enity, so try actual curve points
		if(!axisDirectionFound) {
			foreach(var e in sk.entityList) {
				if(e.type == IEntityType.Point) continue;
				foreach(var pos in e.SegmentsInPlane(null)) {
					var prj = ExpVector.ProjectPointToLine(pos, o, o + ax);
					if((prj - pos).magnitude < 1e-6) continue;

					var ax1 = Vector3.Cross(sourceSketch.plane.n, pos - prj);
					shouldInvertAxis = (Vector3.Dot(ax, ax1) > 0f);
					axisDirectionFound = true;
					break;
				}
			}
		}

		foreach(var e in sk.entityList.OfType<PointEntity>()) {
			var ext = new RevolvedPointEntity(e, this);
			canvas.DrawSegments(ext.segments);
		}

		foreach(var e in sk.entityList) {
			var ext = new RevolvedEntity(e, this, 0);
			canvas.DrawSegments(ext.segments);
			ext = new RevolvedEntity(e, this, 1);
			canvas.DrawSegments(ext.segments);
		}
		
	}

	protected override void OnWriteMeshFeature(XmlTextWriter xml) {
		xml.WriteAttributeString("angle", angle.value.ToStr());
		xml.WriteAttributeString("step", step.value.ToStr());
		xml.WriteAttributeString("meshAngleStep", meshAngleStep.ToStr());
		xml.WriteAttributeString("axis", axisId.ToString());
		xml.WriteAttributeString("origin", originId.ToString());
		xml.WriteAttributeString("angleFixed", angleFixed.ToString());
		xml.WriteAttributeString("stepFixed", stepFixed.ToString());
	}

	protected override void OnReadMeshFeature(XmlNode xml) {
		angle.value = xml.Attributes["angle"].Value.ToDouble();
		step.value = xml.Attributes["step"].Value.ToDouble();
		meshAngleStep = xml.Attributes["meshAngleStep"].Value.ToDouble();
		axisId.Parse(xml.Attributes["axis"].Value);
		originId.Parse(xml.Attributes["origin"].Value);
		angleFixed = Convert.ToBoolean(xml.Attributes["angleFixed"].Value);
		stepFixed = Convert.ToBoolean(xml.Attributes["stepFixed"].Value);
	}

	protected override ICADObject OnHover(Vector3 mouse, Camera camera, UnityEngine.Matrix4x4 tf, ref double dist) {
		var sk = source as SketchFeature;

		var points = sk.GetSketch().entityList.OfType<PointEntity>();
		double min = -1.0;
		IEntity hover = null;
		//var sktf = tf * sk.GetTransform();
		foreach(var p in points) {
			var e = new RevolvedPointEntity(p, this);
			double d = e.Hover(mouse, camera, tf);
			if(d < 0) continue;
			if(d > Sketch.hoverRadius) continue;
			if(min >= 0.0 && d > min) continue;
			min = d;
			hover = e;
		}

		foreach(var p in sourceSketch.entityList) {
			var e = new RevolvedEntity(p, this, 0);
			double d = e.Hover(mouse, camera, tf);
			if(d < 0) continue;
			if(d > Sketch.hoverRadius) continue;
			if(min >= 0.0 && d > min) continue;
			min = d;
			hover = e;
		}

		foreach(var p in sourceSketch.entityList) {
			var e = new RevolvedEntity(p, this, 1);
			double d = e.Hover(mouse, camera, tf);
			if(d < 0) continue;
			if(d > Sketch.hoverRadius) continue;
			if(min >= 0.0 && d > min) continue;
			min = d;
			hover = e;
		}
		if(hover != null) {
			dist = min;
		}
		return hover;
	}

	protected override void OnGenerateEquations(EquationSystem sys) {
		if(!angleFixed) sys.AddParameter(angle);
		if(!stepFixed) sys.AddParameter(step);
	}
}
	