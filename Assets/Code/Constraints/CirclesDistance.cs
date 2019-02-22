using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[Serializable]
public class CirclesDistance : ValueConstraint {

	public enum Option {
		Outside,
		FirstInside,
		SecondInside,
	}

	Option option_;
	public Option option { get { return option_; } set { option_ = value; sketch.MarkDirtySketch(topo:true); } }

	protected override Enum optionInternal { get { return option; } set { option = (Option)value; } }

	public CirclesDistance(Sketch sk) : base(sk) { }

	public CirclesDistance(Sketch sk, IEntity c0, IEntity c1) : base(sk) {
		AddEntity(c0);
		AddEntity(c1);
		value.value = 1;
		ChooseBestOption();
		Satisfy();
	}

	PointEntity getCenterPoint(IEntity e) {
		if(e is CircleEntity) return (e as CircleEntity).center;
		if(e is ArcEntity) return (e as ArcEntity).center;
		return null;
	}

	bool isCentersCoincident(IEntity c0, IEntity c1) {
		var cp0 = getCenterPoint(c0);
		var cp1 = getCenterPoint(c1);
		return cp0 != null && cp1 != null && cp0.IsCoincidentWith(cp1);
	}

	public override IEnumerable<Exp> equations {
		get {
			var c0 = GetEntity(0);
			var c1 = GetEntity(1);
			var r0 = c0.Radius();
			var r1 = c1.Radius();
			if(isCentersCoincident(c0, c1)) {
				if(option == Option.FirstInside) {
					yield return r0 - r1 - value.exp;
				} else {
					yield return r1 - r0 - value.exp;
				}
			} else {
				var dist = (c0.CenterInPlane(sketch.plane) - c1.CenterInPlane(sketch.plane)).Magnitude();
				switch(option) {
					case Option.Outside:		yield return (dist - r0 - r1) - value.exp; break;
					case Option.FirstInside:	yield return (r1 - r0 - dist) - value.exp; break;
					case Option.SecondInside:	yield return (r0 - r1 - dist) - value.exp; break;
				}
			}
		}
	}
	
	protected override void OnDraw(LineCanvas canvas) {
		var c0 = GetEntity(0);
		var c1 = GetEntity(1);
		var c0c = c0.CenterInPlane(null).Eval();
		var c1c = c1.CenterInPlane(null).Eval();
		var c0r = (float)c0.Radius().Eval();
		var c1r = (float)c1.Radius().Eval();
		var dir = (c0c - c1c).normalized;

		if(option == Option.FirstInside) {
			dir = -dir;
		}

		if(isCentersCoincident(c0, c1)) {
			dir = c0c - getLabelOffset();
			if(length(dir) < EPSILON) dir = sketch.plane.u;
			dir = normalize(dir);
		}

		var p0 = c0c - dir * c0r;
		var dir2 = (p0 - c1c).normalized;
		var p1 = c1c + dir2 * c1r;

		drawPointsDistance(p0, p1, canvas, Camera.main, false, true, true, 0);
	}

	protected override Matrix4x4 OnGetBasis() {
		var c0 = GetEntity(0);
		var c1 = GetEntity(1);
		var c0c = c0.CenterInPlane(null).Eval();
		var c1c = c1.CenterInPlane(null).Eval();
		if(isCentersCoincident(c0, c1)) {
			return sketch.plane.GetTransform() * Matrix4x4.Translate(c0c);
		}
		return getPointsDistanceBasis(c0c, c1c, sketch.plane);
	}

}
