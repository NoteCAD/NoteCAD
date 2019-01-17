using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Xml;

[Serializable]
public class AngleConstraint : ValueConstraint {

	bool supplementary_;
	public bool supplementary {
		get {
			return supplementary_;
		}
		set {
			if(value == supplementary_) return;
			supplementary_ = value;
			if(HasEntitiesOfType(IEntityType.Arc, 1)) {
				this.value.value = 2.0 * Math.PI - this.value.value;
			} else {
				this.value.value = -(Math.Sign(this.value.value) * Math.PI - this.value.value);
			}
			sketch.MarkDirtySketch(topo:true);
		}
	}

	public AngleConstraint(Sketch sk) : base(sk) { }

	public AngleConstraint(Sketch sk, IEntity[] points) : base(sk) {
		foreach(var p in points) {
			AddEntity(p);
		}
		Satisfy();
	}

	public AngleConstraint(Sketch sk, IEntity arc) : base(sk) {
		AddEntity(arc);
		value.value = Math.PI / 4;
		Satisfy();
	}

	public AngleConstraint(Sketch sk, IEntity l0, IEntity l1) : base(sk) {
		AddEntity(l0);
		AddEntity(l1);
		Satisfy();
	}

	public override IEnumerable<Exp> equations {
		get {
			var p = GetPointsExp(sketch.plane);
			ExpVector d0 = p[0] - p[1];
			ExpVector d1 = p[3] - p[2];
			bool angle360 = HasEntitiesOfType(IEntityType.Arc, 1);
			Exp angle = sketch.is3d ? ConstraintExp.angle3d(d0, d1) : ConstraintExp.angle2d(d0, d1, angle360);
			yield return angle - value;
		}
	}

	Vector3[] GetPointsInPlane(IPlane plane) {
		return GetPointsExp(plane).Select(pe => pe.Eval()).ToArray();
	}

	Vector3[] GetPoints() {
		return GetPointsInPlane(sketch.plane);
	}

	ExpVector[] GetPointsExp(IPlane plane) {
		var p = new ExpVector[4];
		if(HasEntitiesOfType(IEntityType.Point, 4)) {
			for(int i = 0; i < 4; i++) {
				p[i] = GetEntityOfType(IEntityType.Point, i).GetPointAtInPlane(0, plane);
			}
			if(supplementary) {
				SystemExt.Swap(ref p[2], ref p[3]);
			}
		} else 
		if(HasEntitiesOfType(IEntityType.Line, 2)) {
			var l0 = GetEntityOfType(IEntityType.Line, 0);
			p[0] = l0.GetPointAtInPlane(0, plane);
			p[1] = l0.GetPointAtInPlane(1, plane);
			var l1 = GetEntityOfType(IEntityType.Line, 1);
			p[2] = l1.GetPointAtInPlane(0, plane);
			p[3] = l1.GetPointAtInPlane(1, plane);
			if(supplementary) {
				SystemExt.Swap(ref p[2], ref p[3]);
			}
		} else 
		if(HasEntitiesOfType(IEntityType.Arc, 1)) {
			var arc = GetEntityOfType(IEntityType.Arc, 0);
			p[0] = arc.GetPointAtInPlane(0, plane);
			p[1] = arc.GetPointAtInPlane(2, plane);
			p[2] = arc.GetPointAtInPlane(2, plane);
			p[3] = arc.GetPointAtInPlane(1, plane);
			if(supplementary) {
				SystemExt.Swap(ref p[0], ref p[3]);
				SystemExt.Swap(ref p[1], ref p[2]);
			}
		}
		return p;
	}

	protected override void OnDraw(LineCanvas renderer) {
		
		//drawBasis(renderer);
		var basis = GetBasis();
		//Vector3 vy = basis.GetColumn(1);
		Vector3 vz = basis.GetColumn(2);
		Vector3 p = basis.GetColumn(3);

		float pix = getPixelSize();
		
		var plane = getPlane();
		var value = GetValue();
		var offset = labelPos;

		if(Math.Abs(value) > EPSILON) {
			Vector3[] pts = GetPointsInPlane(null);
					
			Vector3 dir0 = plane.projectVectorInto(pts[0]) - plane.projectVectorInto(pts[1]);
			Vector3 dir1 = plane.projectVectorInto(pts[3]) - plane.projectVectorInto(pts[2]);
			
			Vector3 rref = pos;
			float size = (length(p - rref) - 15f * pix);
			size = Mathf.Max(15f * pix, size);
			float y_sgn = 1f;//(offset.y < 0f) ? -1f : 1f;
			
			Vector3 pt0 = p + normalize(dir0) * size * y_sgn;
			Vector3 pt1 = p + normalize(dir1) * size * y_sgn;
			Vector3 spt = pt0;
			if(offset.x * y_sgn < 0.0) spt = pt1;
			
			// arc to the label
			drawArc(renderer, spt, rref, p, vz);
			
			if(HasEntitiesOfType(IEntityType.Arc, 1)) {
				renderer.DrawLine(pts[0], pt0);
				renderer.DrawLine(pts[3], pt1);
			} else {
				drawLineExtendInPlane(plane, renderer, pts[1], pts[0], pt0, 0f, 4f * pix, false);
				drawLineExtendInPlane(plane, renderer, pts[2], pts[3], pt1, 0f, 4f * pix, false);
			}
			double angle = value;
			
			bool less180 = Math.Abs(angle) < 180.0 - EPSILON;
			
			if(length(pt0 - pt1) > (2.0 * R_ARROW_W + 4.0) * pix || !less180) {
				Vector3 dd = normalize(pt0 - pt1);
				if(length(pt0 - pt1) < EPSILON) dd = normalize(Vector3.Cross(dir1, vz));
				
				// arrow 0
				Vector3 perp0 = normalize(Vector3.Cross(Vector3.Cross(dd, dir0), dir0)) * ((less180) ? 1f : -1f);
				Vector3 pc0 = normalize(pt0 + perp0 * R_ARROW_W * pix - p) * size + p;
				
				Vector3 bx0 = normalize(pc0 - pt0);
				Vector3 by0 = normalize(Vector3.Cross(bx0, vz));
				renderer.DrawLine(pt0, pt0 - by0 * R_ARROW_H * pix + bx0 * R_ARROW_W * pix);
				renderer.DrawLine(pt0, pt0 + by0 * R_ARROW_H * pix + bx0 * R_ARROW_W * pix);
				
				// arrow 1
				Vector3 perp1 = -normalize(Vector3.Cross(Vector3.Cross(dd, dir1), dir1)) * ((less180) ? 1f : -1f);
				Vector3 pc1 = normalize(pt1 + perp1 * R_ARROW_W * pix - p) * size + p;
				
				Vector3 bx1 = normalize(pc1 - pt1);
				Vector3 by1 = normalize(Vector3.Cross(bx1, vz));
				renderer.DrawLine(pt1, pt1 - by1 * R_ARROW_H * pix + bx1 * R_ARROW_W * pix);
				renderer.DrawLine(pt1, pt1 + by1 * R_ARROW_H * pix + bx1 * R_ARROW_W * pix);
			}
			
			// angle arc
			if(less180) {
				drawArc(renderer, pt0, pt1, p, vz);
			} else {
				drawAngleArc(renderer, pt0, p, (float)angle * Mathf.PI / 180f, -vz);
			}
			
			Vector3 refp = offset;
			refp.z = 0f;
			refp = basis * refp;
			setRefPoint(p + normalize(refp - p) * (size + 15f * pix));
		}
		//drawLabel(renderer, camera);
	}
	
	protected override Matrix4x4 OnGetBasis() {
		var pos = GetPoints();
		var p = pos[1];
		double angle = Math.Abs(GetValue());
		Vector3 z = Vector3.zero;
		if(Math.Abs(Math.Abs(angle) - 180.0) < EPSILON) {
			p = pos[1];
			if(sketch.plane != null) z = -sketch.plane.n;
		} else
		if(GeomUtils.isLinesCrossed(pos[0], pos[1], pos[2], pos[3], ref p, Mathf.Epsilon)) {
			z = Vector3.Cross(pos[0] - pos[1], pos[3] - pos[2]).normalized;
		}
		if(z.magnitude < Mathf.Epsilon) z = new Vector3(0.0f, 0.0f, 1.0f);
		
		var y = Quaternion.AngleAxis((float)angle / 2f, z) * (pos[0] - pos[1]).normalized;
		var x = Vector3.Cross(y, z).normalized;
		var result = UnityExt.Basis(x, y, z, p);
		return getPlane().GetTransform() * result;
	}

	public override double LabelToValue(double label) {
		return label * Math.PI / 180.0;
	}

	public override double ValueToLabel(double value) {
		return value / Math.PI * 180.0;
	}

	protected override void OnReadValueConstraint(XmlNode xml) {
		if(xml.Attributes["supplementary"] != null) {
			supplementary_ = Convert.ToBoolean(xml.Attributes["supplementary"].Value);
		}
	}

	protected override void OnWriteValueConstraint(XmlTextWriter xml) {
		xml.WriteAttributeString("supplementary", supplementary.ToString());
	}
}
