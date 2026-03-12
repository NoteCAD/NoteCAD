using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using NoteCAD;

[Serializable]
public class PointsDistance : ValueConstraint {

	public enum Option {
		Closest,
		Horizontal,
		Vertical,
	}

	Option option_;
	public Option option { get { return option_; } set { option_ = value; sketch.MarkDirtySketch(topo:true); } }
	protected override Enum optionInternal { get { return option; } set { option = (Option)value; } }

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
				case Option.Horizontal:
					yield return Exp.Sqrt(Exp.Sqr(p1exp.x - p0exp.x)) - value;
					break;
				case Option.Vertical:
					yield return Exp.Sqrt(Exp.Sqr(p1exp.y - p0exp.y)) - value;
					break;
				default:
					yield return (p1exp - p0exp).Magnitude() - value;
					break;
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
		float midU = (p0l.x + p1l.x) * 0.5f;
		float midV = (p0l.y + p1l.y) * 0.5f;

		Vector3 dim0l, dim1l;
		if(horizontal) {
			float labelV = Vector3.Dot(labelW - plane.o, plane.v);
			float sy = (labelV >= midV) ? 1f : -1f;
			float dimY = midV + sy * Mathf.Max(15f * pix, Mathf.Abs(labelV - midV));
			dim0l = new Vector3(p0l.x, dimY, 0);
			dim1l = new Vector3(p1l.x, dimY, 0);
		} else {
			float labelU = Vector3.Dot(labelW - plane.o, plane.u);
			float sx = (labelU >= midU) ? 1f : -1f;
			float dimX = midU + sx * Mathf.Max(15f * pix, Mathf.Abs(labelU - midU));
			dim0l = new Vector3(dimX, p0l.y, 0);
			dim1l = new Vector3(dimX, p1l.y, 0);
		}

		Vector3 dim0 = plane.FromPlane(dim0l);
		Vector3 dim1 = plane.FromPlane(dim1l);

		float salient = 8f * pix;
		if(length(dim0 - p0p) > EPSILON)
			canvas.DrawLine(p0p, dim0 + normalize(dim0 - p0p) * salient);
		if(length(dim1 - p1p) > EPSILON)
			canvas.DrawLine(p1p, dim1 + normalize(dim1 - p1p) * salient);

		canvas.DrawLine(dim0, dim1);

		if(length(dim0 - dim1) > EPSILON) {
			Vector3 dir = normalize(dim1 - dim0);
			float half_dist = length(dim0 - dim1) * 0.5f;
			bool stroke = half_dist <= (R_ARROW_W * 2f + 1f) * pix;
			drawArrow(canvas, dim0, dir, stroke);
			drawArrow(canvas, dim1, -dir, stroke);
		}

		SetLabelPos(plane.FromPlane((dim0l + dim1l) * 0.5f));
	}

	protected override void OnDraw(ICanvas canvas) {
		Vector3 p0p = GetPointPosInPlane(0, null);
		Vector3 p1p = GetPointPosInPlane(1, null);
		if(option == Option.Horizontal || option == Option.Vertical) {
			DrawHVDistance(p0p, p1p, canvas, option == Option.Horizontal);
		} else {
			drawPointsDistance(p0p, p1p, canvas, Camera.main, false, true, true, 0);
		}
	}

	protected override Matrix4x4 OnGetBasis() {
		var p0pos = GetPointPosInPlane(0, null);
		var p1pos = GetPointPosInPlane(1, null);
		var plane = sketch.plane;
		if(plane != null && (option == Option.Horizontal || option == Option.Vertical)) {
			var p0l = plane.ToPlane(p0pos);
			var p1l = plane.ToPlane(p1pos);
			var origin = plane.FromPlane(new Vector3((p0l.x + p1l.x) * 0.5f, (p0l.y + p1l.y) * 0.5f, 0));
			if(option == Option.Horizontal) {
				return UnityExt.Basis(plane.u, plane.v, plane.n, origin);
			} else {
				return UnityExt.Basis(plane.v, plane.u, plane.n, origin);
			}
		}
		return getPointsDistanceBasis(p0pos, p1pos, sketch.plane);
	}

}
