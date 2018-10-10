using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[Serializable]
public class PointsDistance : ValueConstraint {

	public ExpVector p0exp { get { return GetPointInPlane(0, sketch.plane); } }
	public ExpVector p1exp { get { return GetPointInPlane(1, sketch.plane); } }

	public PointsDistance(Sketch sk) : base(sk) { }

	public PointsDistance(Sketch sk, IEntity p0, IEntity p1) : base(sk) {
		AddEntity(p0);
		AddEntity(p1);
		Satisfy();
	}

	public PointsDistance(Sketch sk, IEntity line) : base(sk) {
		AddEntity(line);
		Satisfy();
	}

	public override IEnumerable<Exp> equations {
		get {
			yield return (p1exp - p0exp).Magnitude() - value.exp;
		}
	}
	
	ExpVector GetPointInPlane(int i, IPlane plane) {
		if(HasEntitiesOfType(IEntityType.Line, 1)) {
			return GetEntityOfType(IEntityType.Line, 0).GetPointAtInPlane(i, plane);
		} else 
		if(HasEntitiesOfType(IEntityType.Point, 2)) {
			return GetEntityOfType(IEntityType.Point, i).GetPointAtInPlane(0, plane);
		}
		return null;
	}

	protected override void OnDraw(LineCanvas canvas) {
		Vector3 p0p = GetPointInPlane(0, null).Eval();
		Vector3 p1p = GetPointInPlane(1, null).Eval();
		drawPointsDistance(p0p, p1p, canvas, Camera.main, false, true, true, 0);
	}

	protected override Matrix4x4 OnGetBasis() {
		var p0pos = GetPointInPlane(0, null).Eval();
		var p1pos = GetPointInPlane(1, null).Eval();
		return getPointsDistanceBasis(p0pos, p1pos, sketch.plane);
	}

}
