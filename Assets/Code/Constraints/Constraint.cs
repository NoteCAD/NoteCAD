using System.Collections.Generic;
using System.Xml;
using System;
using UnityEngine;

public abstract partial class Entity {

	internal void AddConstraint(Constraint c) {
		usedInConstraints.Add(c);
	}

	internal void RemoveConstraint(Constraint c) {
		usedInConstraints.Remove(c);
	}
}

public class Constraint : SketchObject {

	public bool changed;
	protected override GameObject gameObject { get { return null; } }
	//List<Entity> entities = new List<Entity>();
	List<IdPath> ids = new List<IdPath>();
	protected Vector3[] ref_points = new Vector3[2];

	protected void AddEntity<T>(T e) where T : IEntity {
		if(e is Entity) (e as Entity).AddConstraint(this);
		//ientities.Add(e);
		ids.Add(e.id);
		//return e;
	}

	public Constraint(Sketch sk) : base(sk) {
		sk.AddConstraint(this);
	}

	public override void Destroy() {
		if(isDestroyed) return;
		base.Destroy();
		for(int i = 0; i < ids.Count; i++) {
			var ent = GetEntity(i) as Entity;
			if(ent == null) continue;
			ent.RemoveConstraint(this);
		}
	}

	protected override void OnDestroy() {

	}
	
	public override void Write(XmlTextWriter xml) {
		xml.WriteStartElement("constraint");
		xml.WriteAttributeString("type", this.GetType().Name);
		base.Write(xml);
		foreach(var id in ids) {
			xml.WriteStartElement("entity");
			xml.WriteAttributeString("path", id.ToString());
			xml.WriteEndElement();
		}
		xml.WriteEndElement();
	}

	public override void Read(XmlNode xml) {
		ids.Clear();
		foreach(XmlNode node in xml.ChildNodes) {
			if(node.Name != "entity") continue;
			var path = IdPath.From(node.Attributes["path"].Value);
			var e = sketch.feature.detail.GetObjectById(path) as IEntity;
			AddEntity(e);
		}
		base.Read(xml);
	}

	public IEntity GetEntity(int i) {
		return sketch.feature.GetObjectById(ids[i]) as IEntity;
	}

	protected void SetEntity(int i, IEntity e) {
		var ent = GetEntity(i) as Entity;
		if(ent != null) {
			ent.RemoveConstraint(this);
		}
		ids[i] = e.id;
		ent = GetEntity(i) as Entity;
		if(ent != null) {
			ent.AddConstraint(this);
		}
		changed = true;
	}

	public override bool IsChanged() {
		return base.IsChanged() || changed;
	}

	public bool ReplaceEntity(IEntity before, IEntity after) {
		bool result = false;
		var beforeId = before.id;
		for(int i = 0; i < ids.Count; i++) {
			if(ids[i] != beforeId) continue;
			SetEntity(i, after);
			result = true;
		}
		return result;
	}

	protected static float length(Vector3 v) {
		return v.magnitude;
	}

	protected static Vector3 normalize(Vector3 v) {
		return v.normalized;
	}

	public static float EPSILON   = 1e-6f;
	public static float R_ARROW_H = 2.5f;
	public static float R_ARROW_W = 12.5f;
	public static float R_CIRLE_R = 5f;
	public static float R_DASH    = 8f;

	protected IPlane getPlane() {
		return sketch.plane;
	}

	protected float getPixelSize() {
		return (Camera.main.ScreenToWorldPoint(new Vector3(1f, 0f, 0f)) - Camera.main.ScreenToWorldPoint(Vector3.zero)).magnitude;
	}

	protected Matrix4x4 makeBasis(Vector3 x, Vector3 y, Vector3 z, Vector3 p) {
		Matrix4x4 result = Matrix4x4.identity;
		result.SetColumn(0, x);
		result.SetColumn(1, y);
		result.SetColumn(2, z);
		Vector4 pp = p;
		pp.w = 1;
		result.SetColumn(3, pp);
		return result;
	}
	
	public void DrawReferenceLink(LineCanvas renderer, Camera camera) {
		float pix = getPixelSize();
		float size = 12f * pix;
		drawCameraCircle(renderer, camera, ref_points[0], size, 16);
		if(ref_points.Length > 1) {
			drawCameraCircle(renderer, camera, ref_points[1], size, 16);
			if(length(ref_points[1] - ref_points[0]) > 2f * size) {
				Vector3 dir = normalize(ref_points[1] - ref_points[0]);
				drawDottedLine(ref_points[0] + dir * size, ref_points[1] - dir * size, renderer, R_DASH * pix);
			}
		}
	}

	Vector3 rotatedAbout(Vector3 v, Vector3 axis, float angle) {
		return Quaternion.AxisAngle(axis, angle) * v;
	}

	protected void drawDottedLine(Vector3 p0, Vector3 p1, LineCanvas renderer, float step) {
		if(step == 0f) {
			renderer.DrawLine(p0, p1);
			return;
		}
		float len = length(p1 - p0);
		Vector3 dir = normalize(p1 - p0);
		Vector3 p = p0;
		int count = (int)Math.Floor(len / step);
		if(count > 1000) {
			count = 1000;
			step = len / (count - 1f);
		}
		
		bool draw = 0 % 2 == 0;
		for(int i=0; i<count; i++) {
			if(draw) renderer.DrawLine(p, p + dir * step);
			p += dir * step;
			draw = (i + 1) % 2 == 0;
		}
		if(draw) {
			float frac = len - count * step;
			if(draw) renderer.DrawLine(p, p + dir * frac);
		}
	}
	
	protected void drawAngleArc(LineCanvas renderer, Vector3 p0, Vector3 c, float angle, Vector3 vz, bool dash = false, float step = 0f) {
		float subdiv = 32f;
		if(step > 0f) {
			float len = length(p0 - c) * angle;
			subdiv = len / step;
			if(subdiv > 1000f) subdiv = 1000f;
		}
		
		Vector3 rv = p0 - c;
		Vector3 orv = rv;
		
		float i = 0f;
		int index = 0;
		while(i < subdiv) {
			i += 1f;
			if(i > subdiv) i = subdiv;
			Vector3 nrv = rotatedAbout(rv, vz, angle / subdiv * i);
			if(!dash || index % 2 == 0) renderer.DrawLine(orv + c, nrv + c);
			orv = nrv;
			index += 1;
		}
	}
	
	protected void drawArc(LineCanvas renderer, Vector3 p0, Vector3 p1, Vector3 c, Vector3 vz, bool dash = false, float step = 0f) {
		float angle = Mathf.Acos(Vector3.Dot(normalize(p0 - c), normalize(p1 - c)));
		float subdiv = 32f;
		
		if(step > 0f) {
			float len = length(p0 - c) * angle;
			subdiv = len / step;
			if(subdiv > 1000f) subdiv = 1000f;
		}
		
		if(Vector3.Dot(Vector3.Cross(p0 - c, p1 - c), vz) < 0f) angle = -angle;
		
		Vector3 rv = p0 - c;
		Vector3 orv = rv;
		
		float i = 0f;
		int index = 0;
		while(i < subdiv) {
			i += 1f;
			if(i > subdiv) i = subdiv;
			Vector3 nrv = rotatedAbout(rv, vz, angle / subdiv * i);
			if(!dash || index % 2 == 0) renderer.DrawLine(orv + c, nrv + c);
			orv = nrv;
			index += 1;
		}
	}
	
    void drawArcExtend(LineCanvas renderer, Vector3 p0, Vector3 p1,
                                 Vector3 c, Vector3 to, Vector3 vz,
                                 bool dash, float step) {
		float dd0 = Vector3.Dot(Vector3.Cross(p0 - c, p1 - c), vz);
		bool greater180 = dd0 < 0;
		Vector3 c0 = Vector3.Cross(to - c, p0 - c);
		Vector3 c1 = Vector3.Cross(to - c, p1 - c);
		float d0 = Vector3.Dot(c0, vz);
		float d1 = Vector3.Dot(c1, vz);
		
		if(greater180) {
			if(d0 < 0f || d1 > 0f) return;
		} else {
			if(!(d1 < 0f || d0 > 0f)) return;
		}
		
		Vector3 from = p0;
		if(length(to - p1) < length(to - p0)) from = p1;
		drawArc(renderer, from, to, c, vz, dash, step);
	}
	
	bool drawLineExtend(LineCanvas renderer, Vector3 p0, Vector3 p1, Vector3 to, float step, float salient, bool from_to) {
		Vector3 dir = p1 - p0;
		float k = Vector3.Dot(dir, to - p0) / Vector3.Dot(dir, dir);
		Vector3 pt_on_line = p0 + dir * k;
		drawDottedLine(to, pt_on_line, renderer, step);
		Vector3 sd = Vector3.zero;
		if(salient > 0f) sd = normalize(dir) * salient;
		if(k < 0f) {
			if(from_to) {
				drawDottedLine(pt_on_line - sd, p0, renderer, step);
			} else {
				drawDottedLine(p0, pt_on_line - sd, renderer, step);
			}
			return true;
		} else
		if(k > 1f) {
			if(from_to) {
				drawDottedLine(pt_on_line + sd, p1, renderer, step);
			} else {
				drawDottedLine(p1, pt_on_line + sd, renderer, step);
			}
			return true;
		}
		return false;
	}
	
	protected bool drawLineExtendInPlane(IPlane plane, LineCanvas renderer, Vector3 p0, Vector3 p1, Vector3 to, float step, float salient, bool from_to) {
		Vector3 dir = p1 - p0;
		if(plane != null) {
			dir = plane.projectVectorInto(p1) - plane.projectVectorInto(p0);
		}
		float k = Vector3.Dot(dir, to - p0) / Vector3.Dot(dir, dir);
		Vector3 pt_on_line = p0 + (p1 - p0) * k;
		drawDottedLine(to, pt_on_line, renderer, step);
		Vector3 sd = Vector3.zero;
		if(salient > 0f) sd = normalize(dir) * salient;
		if(k < 0f) {
			if(from_to) {
				drawDottedLine(pt_on_line - sd, p0, renderer, step);
			} else {
				drawDottedLine(p0, pt_on_line - sd, renderer, step);
			}
			return true;
		} else
		if(k > 1f) {
			if(from_to) {
				drawDottedLine(pt_on_line + sd, p1, renderer, step);
			} else {
				drawDottedLine(p1, pt_on_line + sd, renderer, step);
			}
			return true;
		}
		return false;
	}
	
	void drawTangentCross(LineCanvas renderer, Vector3 pos, Vector3 dir, Vector3 pn, float pix) {
		float size = 10f * pix;
		Vector3 perp = Vector3.Cross(dir, pn);
		renderer.DrawLine(pos - perp * size, pos + perp * size);
		renderer.DrawLine(pos - dir * size, pos + dir * size);
	}
	
	protected void drawCameraCircle(LineCanvas renderer, Camera camera, Vector3 pos, float size, int num_segments = 32) {
		float angle = 2f * Mathf.PI / (float)num_segments;
		Vector3 r0 = camera.transform.right * size;
		for(int i=0; i<num_segments; i++) {
			Vector3 r1 = rotatedAbout(r0, camera.transform.forward, angle);
			renderer.DrawLine(pos + r0, pos + r1);
			r0 = r1;
		}
	}
	
	void drawGrounding(LineCanvas renderer, Camera camera, Vector3 pos, float size) {
		Vector3 x = camera.transform.right * size;
		Vector3 y = -camera.transform.up * size;
		
		renderer.DrawLine(pos, pos + y * 5f);
		renderer.DrawLine(pos + y * 5f + x * 5f, pos + y * 5f - x * 3f);
		
		// physical ground
		for(float i = -3f; i <= 5f; i += 2f) {
			renderer.DrawLine(pos + y * 5f + x * i, pos + y * 8f + x * (i - 2f));
		}
		
		// electronic ground
		// renderer.DrawLine(pos + y * 8f + x * 3f, pos + y * 8f - x * 3f);
		// renderer.DrawLine(pos + y * 11f + x * 1f, pos + y * 11f - x * 1f);
	}
	
	protected Vector3 drawPointProjection(LineCanvas renderer, Vector3 point, float step) {
		IPlane plane = getPlane();
		if(plane == null) return point;
		Vector3 proj = plane.projectVectorInto(point);
		if(proj != point) drawDottedLine(point, proj, renderer, step);
		return proj;
	}
	
	protected Matrix4x4 getPointLineDistanceBasis(Vector3 lip0_, Vector3 lip1_, Vector3 ap_, IPlane plane) {
	
		Vector3 lip0 = lip0_;
		Vector3 lip1 = lip1_;
		Vector3 ap = ap_;
		
		if(plane != null) {
			lip0 = plane.projectVectorInto(lip0);
			lip1 = plane.projectVectorInto(lip1);
			ap = plane.projectVectorInto(ap);
		}
		
		Vector3 lid = normalize(lip1 - lip0);
		Vector3 bp = lip0 + lid * Vector3.Dot(ap - lip0, lid);
		Vector3 x = normalize(bp - ap);
		Vector3 y = lid;
		Vector3 z = normalize(Vector3.Cross(x, y));
		Vector3 p = (ap + bp) * 0.5f;
		return makeBasis(x, y, z, p);
	}
	
	protected Matrix4x4 getPointsDistanceBasis(Vector3 app, Vector3 bpp, IPlane plane) {
		if(plane == null) return Matrix4x4.Translate((app + bpp) / 2f);
		Vector3 z = plane.n;
		Vector3 ap = plane.projectVectorInto(app);
		Vector3 bp = plane.projectVectorInto(bpp);
		Vector3 x = normalize(bp - ap);
		Vector3 y = normalize(Vector3.Cross(x, z));
		Vector3 p = (ap + bp) * 0.5f;
		return makeBasis(x, y, z, p);
	}
	
	protected Vector3 projectPointLine(Vector3 p, Vector3 p0, Vector3 p1) {
		Vector3 dir = p1 - p0;
		float k = Vector3.Dot(p - p0, dir) / Vector3.Dot(dir, dir);
		return p0 + dir * k;
	}
	
	float projectPointLineCoeff(Vector3 p, Vector3 p0, Vector3 p1) {
		Vector3 dir = p1 - p0;
		return Vector3.Dot(p - p0, dir) / Vector3.Dot(dir, dir);
	}
	
	Vector3 getBasisPlaneDir(Vector3 def) {
		if(getPlane() != null) return getPlane().n;
		return def;
	}
	
	Vector3 getVisualPlaneDir(Vector3 def) {
		if(getPlane() != null) return getPlane().n;
		return def;
	}

	protected override double OnSelect(Vector3 mouse, Camera camera, Matrix4x4 tf) {
		double result = -1.0;
		for(int i = 0; i < ref_points.Length; i++) {
			var pp = camera.WorldToScreenPoint(tf.MultiplyPoint(ref_points[i]));
			pp.z = 0f;
			mouse.z = 0f;
			var dist = (pp - mouse).magnitude - 5;
			if(dist < 0f) dist = 0f;
			if(result > 0.0 && dist > result) continue;
			result = dist;
		}
		return result;
	}

}

public class ValueConstraint : Constraint {

	protected Param value = new Param("value");
	public bool reference;
	Vector3 position_;

	public Vector3 localPos {
		get {
			return position_;
		}
	}

	public Vector3 pos {
		get {
			return GetBasis().MultiplyPoint(position_);
		}
		set {
			var newPos = GetBasis().inverse.MultiplyPoint(value);
			if(position_ == newPos) return;
			position_ = newPos;
			changed = true;
			behaviour.Update();
		}
	}
	//public bool changed;
	ConstraintBehaviour behaviour;
	protected override GameObject gameObject { get { return behaviour.text.gameObject; } }

	public ValueConstraint(Sketch sk) : base(sk) {
		behaviour = GameObject.Instantiate(EntityConfig.instance.constraint);
		behaviour.constraint = this;
	}

	protected override void OnDestroy() {
		GameObject.Destroy(behaviour.gameObject);
	}

	public override IEnumerable<Param> parameters {
		get {
			if(!reference) yield break;
			yield return value;
		}
	}

	protected override void OnDrag(Vector3 delta) {
		if(delta == Vector3.zero) return;
		pos += delta;
	}

	public Matrix4x4 GetBasis() {
		return OnGetBasis()/* * sketch.plane.GetTransform()*/;
	}

	protected virtual Matrix4x4 OnGetBasis() {
		return Matrix4x4.identity;
	}

	public double GetValue() {
		return ValueToLabel(value.value);
	}

	public void SetValue(double v) {
		value.value = LabelToValue(v);
	}

	public virtual double ValueToLabel(double value) {
		return value;
	}

	public virtual double LabelToValue(double label) {
		return label;
	}

	protected virtual bool OnSatisfy() {
		EquationSystem sys = new EquationSystem();
		sys.AddParameter(value);
		sys.AddEquations(equations);
		return sys.Solve() == EquationSystem.SolveResult.OKAY;
	}

	public bool Satisfy() {
		return OnSatisfy();
	}

	protected void setRefPoint(Vector3 pos) {
		ref_points[0] = pos;
	}

	protected override void OnWrite(XmlTextWriter xml) {
		xml.WriteAttributeString("x", pos.x.ToStr());
		xml.WriteAttributeString("y", pos.y.ToStr());
		xml.WriteAttributeString("z", pos.z.ToStr());
		xml.WriteAttributeString("value", GetValue().ToStr());
	}

	public override void Read(XmlNode xml) {
		base.Read(xml);
		Vector3 pos;
		pos.x = xml.Attributes["x"].Value.ToFloat();
		pos.y = xml.Attributes["y"].Value.ToFloat();
		pos.z = xml.Attributes["z"].Value.ToFloat();
		this.pos = pos;
		SetValue(xml.Attributes["value"].Value.ToDouble());
	}

	protected override double OnSelect(Vector3 mouse, Camera camera, Matrix4x4 tf) {
		var pp = camera.WorldToScreenPoint(pos);
		pp.z = 0f;
		mouse.z = 0f;
		var dist = (pp - mouse).magnitude - 5;
		if(dist < 0f) return 0f;
		return dist;
	}

	protected void drawPointLineDistance(Vector3 lip0_, Vector3 lip1_, Vector3 p0_, LineCanvas renderer, Camera camera) {
		
		float pix = getPixelSize();
		
		Vector3 lip0 = drawPointProjection(renderer, lip0_, R_DASH * pix);
		Vector3 lip1 = drawPointProjection(renderer, lip1_, R_DASH * pix);
		Vector3 p0 = drawPointProjection(renderer, p0_, R_DASH * pix);
		
		if(lip0 != lip0_ || lip1 != lip1_) {
			drawDottedLine(lip0, lip1, renderer, R_DASH * pix);
		}
		
		Matrix4x4 basis = getPointLineDistanceBasis(lip0, lip1, p0, getPlane());
		
		Vector3 lid = normalize(lip1 - lip0);
		
		Vector3 p1 = lip0 + lid * Vector3.Dot(p0 - lip0, lid);
		
		Vector3 vx = basis.GetColumn(0);
		Vector3 vy = basis.GetColumn(1);
		Vector3 vp = basis.GetColumn(3);
		
		Vector3 label_offset = getLabelOffset();
		Vector3 offset = Vector3.zero;
		offset.x = Vector3.Dot(label_offset - vp, vx);
		offset.y = Vector3.Dot(label_offset - vp, vy);
		
		// sgn label y
		float sy = ((offset.y  > EPSILON) ? 1f : 0f) - ((offset.y < -EPSILON) ? 1f : 0f);
		
		Vector3 lp0 = p0 + vy * offset.y;
		Vector3 lp1 = p1 + vy * offset.y;
		
		// vertical lines
		renderer.DrawLine(p0, lp0 + vy * 8f * pix * sy);
		
		float lk = Vector3.Dot(lp1 - lip0, lid);
		if(lk < 0f) {
			renderer.DrawLine(lip0, lp1 + normalize(lp1 - lip0) * 8f * pix);
		} else if(lk > length(lip1 - lip0)) {
			renderer.DrawLine(lip1, lp1 + normalize(lp1 - lip1) * 8f * pix);
		}
		
		// distance line
		renderer.DrawLine(lp0, lp1);
		
		// sgn arrow x
		float sx = 1f;
		
		// half distance
		float half_dist = length(p0 - p1) * 0.5f;
		
		if(Mathf.Abs(offset.x) > half_dist) sx = -1f;
		
		if(sx < 0f || length(lp0 - lp1) > (R_ARROW_W * 2f + 1f) * pix) {
			// arrow lp0
			renderer.DrawLine(lp0, lp0 - vy * R_ARROW_H * pix + vx * R_ARROW_W * pix * sx);
			renderer.DrawLine(lp0, lp0 + vy * R_ARROW_H * pix + vx * R_ARROW_W * pix * sx);
			
			// arrow lp1
			renderer.DrawLine(lp1, lp1 - vy * R_ARROW_H * pix - vx * R_ARROW_W * pix * sx);
			renderer.DrawLine(lp1, lp1 + vy * R_ARROW_H * pix - vx * R_ARROW_W * pix * sx);
		} else {
			// stroke lp0
			renderer.DrawLine(lp0 - vy * R_ARROW_H * pix + vx * R_ARROW_H * pix, lp0 + vy * R_ARROW_H * pix - vx * R_ARROW_H * pix);
			
			// stroke lp1
			renderer.DrawLine(lp1 - vy * R_ARROW_H * pix + vx * R_ARROW_H * pix, lp1 + vy * R_ARROW_H * pix - vx * R_ARROW_H * pix);
		}
		
		Vector3 lv0 = lp0;
		Vector3 lv1 = lp1;
		//bool da1 = arrow1;
		
		// if label lays from other side
		if(offset.x > half_dist) {
			lv0 = lp1;
			lv1 = lp0;
			//da1 = arrow0;
		}
		
		// if label is ouside
		if(Mathf.Abs(offset.x) > half_dist) {
			
			Vector3 dir = vp + vy * offset.y + vx * offset.x - lv0;
			float len = Mathf.Max(length(dir), 21f * pix);
			
			// line to the label
			renderer.DrawLine(lv0, lv0 + normalize(dir) * len);
			
			// opposite arrow line
			/*if(da1)*/ renderer.DrawLine(lv1, lv1 - normalize(dir) * 21f * pix);
			setRefPoint(lv0 + normalize(dir) * (len + 16f * pix));
		} else {
			setRefPoint(basis.MultiplyPoint(offset) + vy * sy * 13f * pix);
		}
		
		//drawLabel(renderer, camera);
	}
	
	Vector3 getLabelOffset() {
		return pos;
	}

	protected void drawPointsDistance(Vector3 pp0, Vector3 pp1, LineCanvas renderer, Camera camera, bool label, bool arrow0, bool arrow1, int style) {
		float pix = getPixelSize();
		
		Vector3 p0 = drawPointProjection(renderer, pp0, R_DASH * pix);
		Vector3 p1 = drawPointProjection(renderer, pp1, R_DASH * pix);
		
		Matrix4x4 basis;
		
		if(getPlane() == null) {
			Vector3 p = getLabelOffset();
			Vector3 x = normalize(p1 - p0);
			Vector3 y;
			y = p - projectPointLine(p, p0, p1);
			if(length(y) < EPSILON) y = Vector3.Cross(camera.transform.forward, x);
			y = normalize(y);
			Vector3 z = Vector3.Cross(x, y);
			basis = makeBasis(x, y, z, (p0 + p1) * 0.5f);
		} else {
			basis = getPointsDistanceBasis(p0, p1, getPlane());
		}
		
		Vector3 vx = basis.GetColumn(0);
		Vector3 vy = basis.GetColumn(1);
		Vector3 vp = basis.GetColumn(3);
		
		Vector3 label_offset = getLabelOffset();
		Vector3 offset = Vector3.zero;
		offset.x = Vector3.Dot(label_offset - vp, vx);
		offset.y = Vector3.Dot(label_offset - vp, vy);
		
		// sgn label y
		float sy = ((offset.y  > EPSILON) ? 1f : 0f) - ((offset.y < -EPSILON) ? 1f : 0f);
		offset.y = sy * Mathf.Max(15f * pix, Mathf.Abs(offset.y));
		
		// distance line points
		Vector3 lp0 = p0 + vy * offset.y;
		Vector3 lp1 = p1 + vy * offset.y;
		
		// vertical lines
		if(Mathf.Abs(sy) > EPSILON) {
			Vector3 salient = vy * 8f * pix * sy;
			if(style == 0) {
				renderer.DrawLine(p0, lp0 + salient);
				renderer.DrawLine(p1, lp1 + salient);
			} else {
				renderer.DrawLine(lp0 - salient, lp0 + salient);
				renderer.DrawLine(lp1 - salient, lp1 + salient);
			}
		}
		
		// distance line
		renderer.DrawLine(lp0, lp1);
		
		// sgn arrow x
		float sx = 1f;
		
		// half distance
		float half_dist = length(p0 - p1) * 0.5f;
		
		// if label ouside
		if(Mathf.Abs(offset.x) > half_dist) sx = -1f;
		
		// if label ourside distance area or sceren distance not too small, draw arrows
		if((sx < 0f || length(lp0 - lp1) > (R_ARROW_W * 2f + 1f) * pix) && style != 1) {
			// arrow lp0
			if(arrow0) {
				renderer.DrawLine(lp0, lp0 - vy * R_ARROW_H * pix + vx * R_ARROW_W * pix * sx);
				renderer.DrawLine(lp0, lp0 + vy * R_ARROW_H * pix + vx * R_ARROW_W * pix * sx);
			}
			
			// arrow lp1
			if(arrow1) {
				renderer.DrawLine(lp1, lp1 - vy * R_ARROW_H * pix - vx * R_ARROW_W * pix * sx);
				renderer.DrawLine(lp1, lp1 + vy * R_ARROW_H * pix - vx * R_ARROW_W * pix * sx);
			}
		} else {
			// stroke lp0
			renderer.DrawLine(lp0 - vy * R_ARROW_H * pix + vx * R_ARROW_H * pix, lp0 + vy * R_ARROW_H * pix - vx * R_ARROW_H * pix);
			
			// stroke lp1
			renderer.DrawLine(lp1 - vy * R_ARROW_H * pix + vx * R_ARROW_H * pix, lp1 + vy * R_ARROW_H * pix - vx * R_ARROW_H * pix);
		}
		
		Vector3 lv0 = lp0;
		Vector3 lv1 = lp1;
		bool da1 = arrow1;
		
		// if label lays from other side
		if(offset.x > half_dist) {
			lv0 = lp1;
			lv1 = lp0;
			da1 = arrow0;
		}
		
		// if label is ouside
		if(Mathf.Abs(offset.x) > half_dist) {
			
			Vector3 dir = vp + vy * offset.y + vx * offset.x - lv0;
			float len = Mathf.Max(length(dir), 21f * pix);
			
			// line to the label
			renderer.DrawLine(lv0, lv0 + normalize(dir) * len);
			
			// opposite arrow line
			if(da1) renderer.DrawLine(lv1, lv1 - normalize(dir) * 21f * pix);
			setRefPoint(lv0 + normalize(dir) * (len + 16f * pix));
		} else {
			setRefPoint(basis.MultiplyPoint(offset) + vy * sy * 13f * pix);
		}
		
		//drawCameraCircle(renderer, camera, getLabelOffset(), 3f * pix);
		//if(label) drawLabel(renderer, camera);
	}
}

