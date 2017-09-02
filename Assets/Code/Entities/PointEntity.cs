using System.Collections.Generic;
using UnityEngine;

public class PointEntity : Entity {

	public Param x = new Param("x");
	public Param y = new Param("y");
	public Param z = new Param("z");

	PointBehaviour behaviour;

	public PointEntity(Sketch sk) : base(sk) {
		behaviour = GameObject.Instantiate(EntityConfig.instance.pointPrefab);
		behaviour.entity = this;
	}

	public Vector3 GetPosition() {
		return new Vector3((float)x.value, (float)y.value, (float)z.value);
	}

	public void SetPosition(Vector3 pos) {
		x.value = pos.x;
		y.value = pos.y;
		z.value = pos.z;
	}

	public ExpVector GetPositionExp() {
		return new ExpVector(x, y, z);
	}

	public ExpVector exp {
		get {
			return new ExpVector(x, y, z);
		}
	}

	public override GameObject gameObject { get { return behaviour.gameObject; } }

	public override IEnumerable<Param> parameters {
		get {
			yield return x;
			yield return y;
			yield return z;
		}
	}

	public override IEnumerable<PointEntity> points {
		get {
			yield return this;
		}
	}

	public bool IsChanged() {
		return x.changed || y.changed || z.changed;
	}

}