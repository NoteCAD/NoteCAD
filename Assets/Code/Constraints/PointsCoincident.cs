using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class PointsCoincident : Constraint {

	public IEntity p0 { get { return GetEntity(0); } set { SetEntity(0, value); } }
	public IEntity p1 { get { return GetEntity(1); } set { SetEntity(1, value); } }

	public PointsCoincident(Sketch sk) : base(sk) { }

	public PointsCoincident(Sketch sk, IEntity p0, IEntity p1) : base(sk) {
		AddEntity(p0);
		AddEntity(p1);
	}

	public override IEnumerable<Exp> equations {
		get {
			var pe0 = p0.GetPointAtInPlane(0, sketch.plane);
			var pe1 = p1.GetPointAtInPlane(0, sketch.plane);
			yield return pe0.x - pe1.x;
			yield return pe0.y - pe1.y;
			if(sketch.is3d) yield return pe0.z - pe1.z;
		}
	}

	public IEntity GetOtherPoint(IEntity p) {
		if(p0 == p) return p1;
		return p0;
	}

	protected override double OnSelect(Vector3 mouse, Camera camera, Matrix4x4 tf) {
		return -1;
	}

	protected override bool OnMarqueeSelect(Rect rect, bool wholeObject, Camera camera, Matrix4x4 tf) {
		var pos = p0.GetPointAtInPlane(0, null).Eval();
		Vector2 pp = camera.WorldToScreenPoint(tf.MultiplyPoint(pos));
		return rect.Contains(pp);
	}
}
