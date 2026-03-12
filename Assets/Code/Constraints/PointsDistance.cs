using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using NoteCAD;

[Serializable]
public class PointsDistance : ValueConstraint {

	public enum Option {
		Closest,
		HorizontalPositive,
		HorizontalNegative,
		VerticalPositive,
		VerticalNegative,
	}

	Option option_;
	public Option option { get { return option_; } set { option_ = value; sketch.MarkDirtySketch(topo:true); } }
	protected override Enum optionInternal { get { return option; } set { option = (Option)value; } }

	bool isHorizontal => option == Option.HorizontalPositive || option == Option.HorizontalNegative;
	bool isVertical   => option == Option.VerticalPositive   || option == Option.VerticalNegative;

	public ExpVector p0exp { get { return GetPointInPlane(0, sketch.plane); } }
	public ExpVector p1exp { get { return GetPointInPlane(1, sketch.plane); } }

	public PointsDistance(Sketch sk) : base(sk) { }
	public PointsDistance(Sketch sk, Id id) : base(sk, id) { }

	public PointsDistance(Sketch sk, IEntity p0, IEntity p1) : base(sk) {
		AddEntity(p0);
		AddEntity(p1);
		Satisfy();
	}

	public PointsDistance(Sketch sk, IEntity line) : base(sk) {
		AddEntity(line);
		Satisfy();
	}

	protected override IEnumerable<Exp> constraintEquations {
		get {
			switch(option) {
				case Option.HorizontalPositive: yield return p1exp.x - p0exp.x - value; break;
				case Option.HorizontalNegative: yield return p0exp.x - p1exp.x - value; break;
				case Option.VerticalPositive:   yield return p1exp.y - p0exp.y - value; break;
				case Option.VerticalNegative:   yield return p0exp.y - p1exp.y - value; break;
				default: yield return (p1exp - p0exp).Magnitude() - value; break;
			}
		}
	}

	public override ValueUnits units => ValueUnits.LENGTH;

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

	public Vector3 GetMidpoint() {
		return (GetPointPosInPlane(0, null) + GetPointPosInPlane(1, null)) * 0.5f;
	}

	void DrawHVDistance(Vector3 p0p, Vector3 p1p, ICanvas canvas, bool horizontal) {
		float pix = getPixelSize();
		var plane = sketch.plane;
		if(plane == null) {
			drawPointsDistance(p0p, p1p, canvas, Camera.main);
			return;
		}

		Vector3 p0l = plane.ToPlane(p0p);
		Vector3 p1l = plane.ToPlane(p1p);
		Vector3 labelW = getLabelOffset();

		Vector3 dim0l, dim1l;
		if(horizontal) {
			float labelV = Vector3.Dot(labelW - plane.o, plane.v);
			dim0l = new Vector3(p0l.x, labelV, 0);
			dim1l = new Vector3(p1l.x, labelV, 0);
		} else {
			float labelU = Vector3.Dot(labelW - plane.o, plane.u);
			dim0l = new Vector3(labelU, p0l.y, 0);
			dim1l = new Vector3(labelU, p1l.y, 0);
		}

		Vector3 dim0 = plane.FromPlane(dim0l);
		Vector3 dim1 = plane.FromPlane(dim1l);

		// Draw witness lines with a small salient beyond the dimension line.
		float salient = 8f * pix;
		if(length(dim0 - p0p) > EPSILON)
			canvas.DrawLine(p0p, dim0 + normalize(dim0 - p0p) * salient);
		if(length(dim1 - p1p) > EPSILON)
			canvas.DrawLine(p1p, dim1 + normalize(dim1 - p1p) * salient);

		// Delegate the dimension line, arrows and label positioning to the shared helper.
		// Since dim0/dim1 are already on the sketch plane, drawPointProjection is a no-op
		// and all arrow/outside-label logic is handled correctly.
		drawPointsDistance(dim0, dim1, canvas, Camera.main, label: false, arrow0: true, arrow1: true, style: 0);
	}

	protected override void OnDraw(ICanvas canvas) {
		Vector3 p0p = GetPointPosInPlane(0, null);
		Vector3 p1p = GetPointPosInPlane(1, null);
		if(isHorizontal || isVertical) {
			DrawHVDistance(p0p, p1p, canvas, isHorizontal);
		} else {
			drawPointsDistance(p0p, p1p, canvas, Camera.main, false, true, true, 0);
		}
	}

	protected override Matrix4x4 OnGetBasis() {
		var p0pos = GetPointPosInPlane(0, null);
		var p1pos = GetPointPosInPlane(1, null);
		var plane = sketch.plane;
		if(plane != null && (isHorizontal || isVertical)) {
			var midpointWorld = (p0pos + p1pos) * 0.5f;
			// For horizontal: x-axis = plane.u (along dim line), y-axis = plane.v (label offset direction).
			// For vertical:   x-axis = plane.v (along dim line), y-axis = plane.u (label offset direction).
			if(isHorizontal) {
				return UnityExt.Basis(plane.u, plane.v, plane.n, midpointWorld);
			} else {
				return UnityExt.Basis(plane.v, plane.u, plane.n, midpointWorld);
			}
		}
		return getPointsDistanceBasis(p0pos, p1pos, sketch.plane);
	}

}

