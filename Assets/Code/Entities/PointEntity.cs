using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Xml;
using System;

public class PointEntity : Entity {

	public Param x = new Param("x");
	public Param y = new Param("y");
	public Param z = new Param("z");

	public bool is3d {
		get {
			return sketch.is3d;
		}
	}

	public PointEntity(Sketch sk) : base(sk) {
		
	}

	public override IEntityType type { get { return IEntityType.Point; } }

	public Vector3 GetPosition() {
		if(transform != null) {
			return exp.Eval();
		}
		return new Vector3((float)x.value, (float)y.value, (float)z.value);
	}

	public void SetPosition(Vector3 pos) {
		if(transform != null) return;
		x.value = pos.x;
		y.value = pos.y;
		if(is3d) z.value = pos.z;
	}

	public override IEnumerable<Vector3> segments {
		get {
			yield return pos;
		}
	}

	public Vector3 pos {
		get {
			return GetPosition();
		}
		set {
			SetPosition(value);
		}
	}

	public ExpVector exp {
		get {
			if(transform != null) {
				return transform(new ExpVector(x, y, z));
			}
			return new ExpVector(x, y, z);
		}
	}

	public override IEnumerable<Param> parameters {
		get {
			yield return x;
			yield return y;
			if(is3d) yield return z;
		}
	}

	public override IEnumerable<PointEntity> points {
		get {
			yield return this;
		}
	}

	public override bool IsChanged() {
		return x.changed || y.changed || z.changed;
	}

	protected override void OnDrag(Vector3 delta) {
		x.value += delta.x;
		y.value += delta.y;
		if(is3d) z.value += delta.z;
	}
	
	private bool IsCoincidentWith(PointEntity point, PointEntity exclude) {
		for(int i = 0; i < usedInConstraints.Count; i++) {
			var c = usedInConstraints[i] as PointsCoincident;
			if(c == null) continue;
			var p = c.GetOtherPoint(this) as PointEntity;
			if(p == point || p != exclude && p != null && p.IsCoincidentWith(point, this)) {
				return true;
			}
		}
		return false;
		/*
		return constraints.
			OfType<PointsCoincident>().
			Select(c => c.GetOtherPoint(this)).
			Any(p => p == point || p != exclude && p.IsCoincidentWith(point, this));
		*/
	}

	public bool IsCoincidentWith(PointEntity point) {
		return IsCoincidentWith(point, null);
	}

	protected override void OnWrite(XmlTextWriter xml) {
		xml.WriteAttributeString("x", x.value.ToStr());
		xml.WriteAttributeString("y", y.value.ToStr());
		if(is3d) xml.WriteAttributeString("z", z.value.ToStr());
	}

	protected override void OnRead(XmlNode xml) {
		x.value = xml.Attributes["x"].Value.ToDouble();
		y.value = xml.Attributes["y"].Value.ToDouble();
		if(is3d) z.value = xml.Attributes["z"].Value.ToDouble();
	}

	public static double IsSelected(Vector3 pos, Vector3 mouse, Camera camera, Matrix4x4 tf) {
		var pp = camera.WorldToScreenPoint(tf.MultiplyPoint(pos));
		pp.z = 0f;
		mouse.z = 0f;
		var dist = (pp - mouse).magnitude - 5;
		if(dist < 0.0) return 0.0;
		return dist;
	}

	protected override double OnSelect(Vector3 mouse, Camera camera, Matrix4x4 tf) {
		var pp = camera.WorldToScreenPoint(tf.MultiplyPoint(pos));
		pp.z = 0f;
		mouse.z = 0f;
		var dist = (pp - mouse).magnitude - 5;
		if(dist < 0.0) return 0.0;
		return dist;
	}

	protected override void OnDraw(LineCanvas canvas) {
		canvas.SetStyle("points");
		canvas.DrawPoint(pos);
	}

	public override ExpVector PointOn(Exp t) {
		return exp;
	}

	public override ExpVector TangentAt(Exp t) {
		return null;
	}
}