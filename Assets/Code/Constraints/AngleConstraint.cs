using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AngleConstraint : ValueConstraint {

	public IEntity l0p0 { get { return GetEntity(0); } set { SetEntity(0, value); } }
	public IEntity l0p1 { get { return GetEntity(1); } set { SetEntity(1, value); } }
	public IEntity l1p0 { get { return GetEntity(2); } set { SetEntity(2, value); } }
	public IEntity l1p1 { get { return GetEntity(3); } set { SetEntity(3, value); } }

	public AngleConstraint(Sketch sk) : base(sk) { }

	public AngleConstraint(Sketch sk, LineEntity l0, LineEntity l1) : base(sk) {
		var points = GetPoints(l0, l1);
		foreach(var p in points) {
			AddEntity(p);
		}
		Satisfy();
	}

	public AngleConstraint(Sketch sk, IEntity l0, IEntity l1) : base(sk) {
		Satisfy();
	}

	public override IEnumerable<Exp> equations {
		get {
			var points = new IEntity[] { l0p0, l0p1, l1p0, l1p1 };
			var p = points.Select(pt => pt.PointExpInPlane(sketch.plane)).ToArray();
			ExpVector d0 = p[0] - p[1];
			ExpVector d1 = p[3] - p[2];
			Exp du = d1.x * d0.x + d1.y * d0.y;
			Exp dv = d0.x * d1.y - d0.y * d1.x;
			yield return Exp.Atan2(dv, du) - value;
		}
	}

	Vector3[] GetPointsInPlane(IPlane plane) {
		var points = new IEntity[] { l0p0, l0p1, l1p0, l1p1 };
		return points.Select(pt => pt.PointExpInPlane(plane).Eval()).ToArray();
	}

	Vector3[] GetPoints() {
		return GetPointsInPlane(sketch.plane);
	}

	PointEntity[] GetPoints(LineEntity l0, LineEntity l1) {
		if(l0.p0.IsCoincidentWith(l1.p0)) return new PointEntity[4] { l0.p1, l0.p0, l1.p0, l1.p1 };
		if(l0.p0.IsCoincidentWith(l1.p1)) return new PointEntity[4] { l0.p1, l0.p0, l1.p1, l1.p0 };
		if(l0.p1.IsCoincidentWith(l1.p0)) return new PointEntity[4] { l0.p0, l0.p1, l1.p0, l1.p1 };
		if(l0.p1.IsCoincidentWith(l1.p1)) return new PointEntity[4] { l0.p0, l0.p1, l1.p1, l1.p0 };
		return new PointEntity[4] { l0.p0, l0.p1, l1.p0, l1.p1 };
	}

	protected override void OnDraw(LineCanvas renderer) {
		
		//drawBasis(renderer, camera);
		var basis = GetBasis();
		//Vector3 vy = basis.GetColumn(1);
		Vector3 vz = basis.GetColumn(2);
		Vector3 p = basis.GetColumn(3);

		float pix = getPixelSize();
		
		var plane = getPlane();
		var value = GetValue();
		var offset = localPos;

		if(Math.Abs(value) > EPSILON) {
			Vector3[] pts = GetPointsInPlane(null);
					
			Vector3 dir0 = plane.projectVectorInto(pts[0]) - plane.projectVectorInto(pts[1]);
			Vector3 dir1 = plane.projectVectorInto(pts[3]) - plane.projectVectorInto(pts[2]);
			
			Vector3 rref = pos;
			float size = (length(p - rref) - 15f * pix);
			size = Mathf.Max(15f * pix, size);
			float y_sgn = (offset.y < 0f) ? -1f : 1f;
			
			Vector3 pt0 = p + normalize(dir0) * size * y_sgn;
			Vector3 pt1 = p + normalize(dir1) * size * y_sgn;
			Vector3 spt = pt0;
			if(offset.x * y_sgn < 0.0) spt = pt1;
			
			// arc to the label
			drawArc(renderer, spt, rref, p, vz);
			
			//if(hasEntitiesTyped <EntityPoint> (3)) {
			//	drawDottedLine(pts[0], pt0, renderer, R_DASH * pix);
			//	drawDottedLine(pts[3], pt1, renderer, R_DASH * pix);
			//} else {
				// line extends to the arc
				drawLineExtendInPlane(plane, renderer, pts[1], pts[0], pt0, 0f, 4f * pix, false);
				drawLineExtendInPlane(plane, renderer, pts[2], pts[3], pt1, 0f, 4f * pix, false);
			//}
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
				drawAngleArc(renderer, pt0, p, (float)angle * Mathf.PI / 180f, vz);
			}
			
			Vector3 refp = offset;
			refp.z = 0f;
			refp = basis * refp;
			setRefPoint(p + normalize(refp - p) * (size + 15f * pix));
		}
		//drawLabel(renderer, camera);
	}
	
/*
		EntityPoint *points[4];
  		if(!getSortedPoints(points)) return dMat4();
		EntityPlane *plane = getPlane();
		dVec3 pos[4];
		for(int i=0; i<4; i++) {
			pos[i] = points[i]->getPos();
			if(plane != nullptr) pos[i] = plane->projectVectorInto(pos[i]);
		}
		dVec3 p = pos[1];
		
		if(abs(getValueParamValue()) > EPSILON) {
			p = atIntersectionOfLines(pos[0], pos[1], pos[2], pos[3], nullptr);
		}
		
		double angle = getValueParamValue();
		
		if(is_signed && variant != 1 && option == 1) {
			angle = -angle;
		}
		
		dVec3 z = getBasisPlaneDir(normalize(cross(pos[0] - pos[1], pos[3] - pos[2])));
		if(length(z) < EPSILON) z = dVec3(0.0, 0.0, 1.0);
		
		dVec3 y = rotatedAbout(normalize(pos[0] - pos[1]), z, angle / 2.0);
		dVec3 x = normalize(cross(y, z));
		
		return makeBasis(x, y, z, p);
*/
	protected override Matrix4x4 OnGetBasis() {
		var pos = GetPoints();
		var p = pos[1];
		double angle = Math.Abs(GetValue());
		if(GeomUtils.isLinesCrossed(pos[0], pos[1], pos[2], pos[3], ref p, Mathf.Epsilon)) {
		}
		var z = Vector3.Cross(pos[0] - pos[1], pos[3] - pos[2]).normalized;
		if(z.magnitude < Mathf.Epsilon) z = new Vector3(0.0f, 0.0f, 1.0f);
		
		var y = Quaternion.AngleAxis((float)angle / 2f, z) * (pos[0] - pos[1]).normalized;
		var x = Vector3.Cross(y, z).normalized;
		var result = new Matrix4x4();
		Vector4 p4 = p;
		p4.w = 1.0f;
		result.SetColumn(0, x);	
		result.SetColumn(1, y);
		result.SetColumn(2, z);
		result.SetColumn(3, p4);
		return getPlane().GetTransform() * result;
	}

	public override double LabelToValue(double label) {
		return label * Math.PI / 180.0;
	}

	public override double ValueToLabel(double value) {
		return value / Math.PI * 180.0;
	}
}
