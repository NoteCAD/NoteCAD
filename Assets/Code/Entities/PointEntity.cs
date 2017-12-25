using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Xml;

public class PointEntity : Entity {

	public Param x = new Param("x");
	public Param y = new Param("y");
	public Param z = new Param("z");

	bool is3d;

	PointBehaviour behaviour;

	public PointEntity(Sketch sk) : base(sk) {
		behaviour = GameObject.Instantiate(EntityConfig.instance.pointPrefab);
		behaviour.entity = this;
		is3d = false;
	}

	public Vector3 GetPosition() {
		return new Vector3((float)x.value, (float)y.value, (float)z.value);
	}

	public void SetPosition(Vector3 pos) {
		x.value = pos.x;
		y.value = pos.y;
		if(is3d) z.value = pos.z;
		behaviour.LateUpdate();
	}

	public Vector3 pos {
		get {
			return GetPosition();
		}
		set {
			SetPosition(value);
		}
	}

	public ExpVector GetPositionExp() {
		return new ExpVector(x, y, z);
	}

	public ExpVector exp {
		get {
			return new ExpVector(x, y, z);
		}
	}

	protected override GameObject gameObject { get { return behaviour.gameObject; } }

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
			var p = c.GetOtherPoint(this);
			if(p == point || p != exclude && p.IsCoincidentWith(point, this)) {
				return true;
			}
		}
		return constraints.
			OfType<PointsCoincident>().
			Select(c => c.GetOtherPoint(this)).
			Any(p => p == point || p != exclude && p.IsCoincidentWith(point, this));
	}

	public bool IsCoincidentWith(PointEntity point) {
		return IsCoincidentWith(point, null);
	}

	protected override void OnWrite(XmlTextWriter xml) {
		xml.WriteAttributeString("x", x.value.ToString());
		xml.WriteAttributeString("y", y.value.ToString());
		if(is3d) xml.WriteAttributeString("z", z.value.ToString());
	}

	protected override void OnRead(XmlNode xml) {
		x.value = double.Parse(xml.Attributes["x"].Value);
		y.value = double.Parse(xml.Attributes["y"].Value);
		if(is3d) z.value = double.Parse(xml.Attributes["z"].Value);
	}
}