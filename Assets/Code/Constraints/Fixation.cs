using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using NoteCAD;
using System.Linq;

[Serializable]
public class Fixation : Constraint {

	public Fixation(Sketch sk) : base(sk) { }
	public Fixation(Sketch sk, Id id) : base(sk, id) { }

	public Fixation(Sketch sk, IEntity c) : base(sk) {
		AddEntity(c);
	}

	public IEntity entity { get { return GetEntity(0); } }


	public override IEnumerable<Exp> equations {
		get {

			if(entity.type == IEntityType.Arc) {
				var radius = entity.Radius();
				yield return radius - radius.Eval();
				var pts = entity.points.ToArray();
				for (int i = 0; i < 2; i++) {
					var p = pts[i];
					var pos = p.Eval();
					yield return p.x - pos.x;
					yield return p.y - pos.y;
					if(sketch.is3d) {
						yield return p.z - pos.z;
					}
				}
				yield break;
			}
			foreach (var p in entity.points) {
				var pos = p.Eval();
				yield return p.x - pos.x;
				yield return p.y - pos.y;
				if(sketch.is3d) {
					yield return p.z - pos.z;
				}
			}

			if(entity.type == IEntityType.Circle) {
				var radius = entity.Radius();
				yield return radius - radius.Eval();
			}
		}
	}

	protected override void OnDraw(ICanvas canvas) {
		int ptCount = 0;
		Vector3 center = Vector3.zero;
		foreach (var p in entity.points) {
			center += p.Eval();
			ptCount++;
		}
		center /= ptCount;
		//drawCameraCircle(canvas, Camera.main, center, 10.0f * getPixelSize(), 4);
		var u = Camera.main.transform.right;
		var v = Camera.main.transform.up;
		var pix = getPixelSize();
		var w = 6.0f * pix;
		var h = 15.0f * pix;
		var sl = 8.0f * pix;
		var dl = -w * u - h * v;
		var dr = +w * u - h * v;
		var ss = 5.0f * pix;
		var sc = 5;
		canvas.DrawLine(center, center + dl);
		canvas.DrawLine(center, center + dr);
		var lc = center + dl - u * sl;
		var rc = center + dr + u * sl;
		canvas.DrawLine(lc, rc);
		var space = ((rc - lc).magnitude - ss * 1.5f) / (sc - 1);
		var ssd = -ss * u - ss * v;
		lc += u * ss;
		for(int i = 0; i < sc; i++) {
			var p = lc + u * space * i;
			canvas.DrawLine(p, p + ssd);
		}
		ref_points[0] = center;
	}

}
