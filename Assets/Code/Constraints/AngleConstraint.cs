using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AngleConstraint : ValueConstraint {

	public LineEntity l0 { get { return GetEntity(0) as LineEntity; } set { SetEntity(0, value); } }
	public LineEntity l1 { get { return GetEntity(1) as LineEntity; } set { SetEntity(1, value); } }

	public AngleConstraint(Sketch sk) : base(sk) { }

	public AngleConstraint(Sketch sk, LineEntity l0, LineEntity l1) : base(sk) {
		AddEntity(l0);
		AddEntity(l1);
		Satisfy();
	}

	public override IEnumerable<Exp> equations {
		get {
			var p = GetPoints();
			ExpVector d0 = p[0].exp - p[1].exp;
			ExpVector d1 = p[3].exp - p[2].exp;
			Exp du = d1.x * d0.x + d1.y * d0.y;
			Exp dv = d0.x * d1.y - d0.y * d1.x;
			yield return Exp.Atan2(dv, du) - value;
		}
	}

	PointEntity[] GetPoints() {
		if(l0.p0.IsCoincidentWith(l1.p0)) return new PointEntity[4] { l0.p1, l0.p0, l1.p0, l1.p1 };
		if(l0.p0.IsCoincidentWith(l1.p1)) return new PointEntity[4] { l0.p1, l0.p0, l1.p1, l1.p0 };
		if(l0.p1.IsCoincidentWith(l1.p0)) return new PointEntity[4] { l0.p0, l0.p1, l1.p0, l1.p1 };
		if(l0.p1.IsCoincidentWith(l1.p1)) return new PointEntity[4] { l0.p0, l0.p1, l1.p1, l1.p0 };
		return new PointEntity[4] { l0.p0, l0.p1, l1.p0, l1.p1 };
	}

	bool DrawLineExtend(LineCanvas canvas, Vector3 p0, Vector3 p1, Vector3 to, float salient) {
		var dir = p1 - p0;
		float k = Vector3.Dot(dir, to - p0) / Vector3.Dot(dir, dir);
		var pt_on_line = p0 + dir * k;
		canvas.DrawLine(to, pt_on_line);
		Vector3 sd = Vector3.zero;
		if(salient > 0.0) sd = dir.normalized * salient;
		if(k < 0.0) {
			canvas.DrawLine(p0, pt_on_line - sd);
			return true;
		} else
		if(k > 1.0) {
			canvas.DrawLine(p1, pt_on_line + sd);
			return true;
		}
		return false;
	}

	protected override void OnDraw(LineCanvas canvas) {
		var basis = GetBasis();
		var vx = (Vector3)basis.GetColumn(0);
		var vy = (Vector3)basis.GetColumn(1);
		var vz = (Vector3)basis.GetColumn(2);
		var p = (Vector3)basis.GetColumn(3);

		float pix = 0.1f;
		double value = GetValue();
		
		if(Math.Abs(value) > Mathf.Epsilon) {
		
			Vector3[] pts = GetPoints().Select(pt => pt.pos).ToArray();
			var dir0 = pts[0] - pts[1];
			var dir1 = pts[3] - pts[2];
			
			var refer = pos;
			var offset = GetBasis().inverse.MultiplyPoint(pos);
			Debug.Log(offset.ToString());
			float size = ((p - refer).magnitude - 15.0f * pix);
			size = Math.Max(15.0f * pix, size);
			float y_sgn = (offset.y < 0.0f) ? -1.0f : 1.0f;
			
			var pt0 = p + dir0.normalized * size * y_sgn;
			var pt1 = p + dir1.normalized * size * y_sgn;
			var spt = pt0;
			if(offset.x * y_sgn < 0.0) spt = pt1;
			
			// arc to the label
			canvas.DrawArc(spt, refer, p, vz);
			
			// line extends to the arc
			DrawLineExtend(canvas, pts[1], pts[0], pt0, 4.0f * pix);
			DrawLineExtend(canvas, pts[2], pts[3], pt1, 4.0f * pix);

			
			bool less180 = Math.Abs(value) < 180.0 - Mathf.Epsilon;
			/*
			if(length(pt0 - pt1) > (2.0 * R_ARROW_W + 4.0) * pix || !less180) {
				dVec3 dd = normalize(pt0 - pt1);
				if(length(pt0 - pt1) < EPSILON) dd = normalize(cross(dir1, vz));
				
				// arrow 0
				dVec3 perp0 = normalize(cross(cross(dd, dir0), dir0)) * ((less180) ? 1.0 : -1.0);
				dVec3 pc0 = normalize(pt0 + perp0 * R_ARROW_W * pix - p) * size + p;
				
				dVec3 bx0 = normalize(pc0 - pt0);
				dVec3 by0 = normalize(cross(bx0, vz));
				renderer->line(pt0, pt0 - by0 * R_ARROW_H * pix + bx0 * R_ARROW_W * pix);
				renderer->line(pt0, pt0 + by0 * R_ARROW_H * pix + bx0 * R_ARROW_W * pix);
				
				// arrow 1
				dVec3 perp1 = -normalize(cross(cross(dd, dir1), dir1)) * ((less180) ? 1.0 : -1.0);
				dVec3 pc1 = normalize(pt1 + perp1 * R_ARROW_W * pix - p) * size + p;
				
				dVec3 bx1 = normalize(pc1 - pt1);
				dVec3 by1 = normalize(cross(bx1, vz));
				renderer->line(pt1, pt1 - by1 * R_ARROW_H * pix + bx1 * R_ARROW_W * pix);
				renderer->line(pt1, pt1 + by1 * R_ARROW_H * pix + bx1 * R_ARROW_W * pix);
			}
			*/
			// angle arc
			//if(less180) {
				canvas.DrawArc(pt0, pt1, p, vz);
			//} else {
			//	drawAngleArc(renderer, pt0, p, angle * PI / 180.0, vz);
			//}
			/*
			dVec3 refp = offset;
			refp.z = 0.0;
			refp = basis * refp;
			setRefPoint(p + normalize(refp - p) * (size + 15.0 * pix));
			*/
		}
	}

	protected override bool OnIsChanged() {
		return l0.IsChanged() || l1.IsChanged();
	}
	
	protected override Matrix4x4 OnGetBasis() {
		/*
		var points = GetPoints();
		var pos = points[1].GetPosition();
		var dir = l0.p0.GetPosition() - l0.p1.GetPosition();
		var ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
		var rot = Quaternion.AngleAxis(ang, Vector3.forward);
		return Matrix4x4.TRS(pos, rot, Vector3.one); 
		*/
		var pos = GetPoints().Select(pt => pt.pos).ToArray();
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
		return result;
	}

	public override double LabelToValue(double label) {
		return label * Math.PI / 180.0;
	}

	public override double ValueToLabel(double value) {
		return value / Math.PI * 180.0;
	}
}
