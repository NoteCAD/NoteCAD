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

	Vector3 GetPointPosInPlane(int i, IPlane plane) {
		if(HasEntitiesOfType(IEntityType.Line, 1)) {
			return GetEntityOfType(IEntityType.Line, 0).GetPointPosAtInPlane(i, plane);
		} else 
		if(HasEntitiesOfType(IEntityType.Point, 2)) {
			return GetEntityOfType(IEntityType.Point, i).GetPointPosAtInPlane(0, plane);
		}
		return Vector3.zero;
	}

	protected override void OnDraw(LineCanvas canvas) {
		Vector3 p0p = GetPointPosInPlane(0, null);
		Vector3 p1p = GetPointPosInPlane(1, null);
		drawPointsDistance(p0p, p1p, canvas, Camera.main, false, true, true, 0);
	}

	protected override Matrix4x4 OnGetBasis() {
		var p0pos = GetPointPosInPlane(0, null);
		var p1pos = GetPointPosInPlane(1, null);
		return getPointsDistanceBasis(p0pos, p1pos, sketch.plane);
	}

}
