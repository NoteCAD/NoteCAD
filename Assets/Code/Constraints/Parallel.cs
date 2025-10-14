using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NoteCAD;

[Serializable]
public class Parallel : Constraint {

	public enum Option {
		Codirected,
		Antidirected,
		Any
	}

	Option option_;

	public Option option { get { return option_; } set { option_ = value; sketch.MarkDirtySketch(topo:true); } }
	protected override Enum optionInternal { get { return option; } set { option = (Option)value; } }

	public Parallel(Sketch sk) : base(sk) { }
	public Parallel(Sketch sk, Id id) : base(sk, id) { }

	public Parallel(Sketch sk, IEntity l0, IEntity l1) : base(sk) {
		AddEntity(l0);
		AddEntity(l1);
		ChooseBestOption();
	}

	Param k = new Param("k", 1.0);
	public override IEnumerable<Param> parameters {
		get {
			if (sketch.is3d) {
				yield return k;
			}
		}
	}

	public override IEnumerable<Exp> equations {
		get {
			var l0 = GetEntityOfType(IEntityType.Line, 0);
			var l1 = GetEntityOfType(IEntityType.Line, 1);

			ExpVector d0 = l0.GetPointAtInPlane(0, sketch.plane) - l0.GetPointAtInPlane(1, sketch.plane);
			ExpVector d1 = l1.GetPointAtInPlane(0, sketch.plane) - l1.GetPointAtInPlane(1, sketch.plane);
			
			if (sketch.is3d) {
				ExpVector l0p0 = l0.GetPointAtInPlane(0, sketch.plane);
				ExpVector l0p1 = l0.GetPointAtInPlane(1, sketch.plane);

				ExpVector eq = new ExpVector(0.0, 0.0, 0.0);
				switch(option) {
					case Option.Codirected: eq = l0p0 - (l0p1 + d1 * Exp.Abs(k)); break;
					case Option.Antidirected: eq = l0p1 - (l0p0 + d1 * Exp.Abs(k)); break;
					case Option.Any: eq = l0p1 - (l0p0 + d1 * k); break;
				}
				yield return eq.x;
				yield return eq.y;
				yield return eq.z;
				yield break;
			}
			
			/*
			if (sketch.is3d) {
				ExpVector l0p0 = l0.GetPointAtInPlane(0, sketch.plane);
				ExpVector l0p1 = l0.GetPointAtInPlane(1, sketch.plane);
				ExpVector l1p0 = l1.GetPointAtInPlane(0, sketch.plane);
				ExpVector l1p1 = l1.GetPointAtInPlane(1, sketch.plane);

				// this is dof-correct parallelity
				yield return ConstraintExp.pointLineDistance(l0p0, l1p0, l1p1, sketch.is3d) - ConstraintExp.pointLineDistance(l0p1, l1p0, l1p1, sketch.is3d);
				yield return ExpVector.Dot(ExpVector.Cross(d0, l1p0 - l0p1), l1p1 - l0p0);
				//yield return ExpVector.Dot(ExpVector.Cross(d0, l1p0 - l0p1), l1p1 - l0p0) / (l1p1 - l0p0).Magnitude() / (l1p0 - l0p1).Magnitude() / d0.Magnitude();
				//yield return ExpVector.Dot(ExpVector.Cross(d0, l1p0 - l0p1), l1p1 - l0p0) / (l1p1 - l0p0).Magnitude() / ExpVector.Cross(d0, l1p0 - l0p1).Magnitude();
				yield break;
			}
			*/
			/*
			if (sketch.is3d) {
				ExpVector eq = new ExpVector(0.0, 0.0, 0.0);
				switch(option) {
					case Option.Codirected: eq = d0.Normalized() - d1.Normalized(); break;
					case Option.Antidirected: eq = d0.Normalized() + d1.Normalized(); break;
				}
				yield return eq.x;
				yield return eq.y;
				yield return eq.z;
				yield break;
			}
			*/
			/*
			if (sketch.is3d) {
				yield return ExpVector.Cross(d0, d1).Magnitude() / d0.Magnitude() / d1.Magnitude();
				yield break;
			}
			*/			
			/*
			if (sketch.is3d) {
				switch(option) {
					case Option.Codirected: yield return ExpVector.Dot(d0, d1) - d0.Magnitude() * d1.Magnitude(); break;
					case Option.Antidirected: yield return ExpVector.Dot(d0, d1) + d0.Magnitude() * d1.Magnitude(); break;
				}
				yield break;
			}
			*/
			/*
			if (sketch.is3d) {
				switch(option) {
					case Option.Codirected: yield return ExpVector.Dot(d0, d1) / d0.Magnitude() / d1.Magnitude() - 1.0; break;
					case Option.Antidirected: yield return ExpVector.Dot(d0, d1) / d0.Magnitude() / d1.Magnitude() + 1.0; break;
				}
				yield break;
			}
			*/
			
			switch(option) {
				case Option.Codirected: yield return ConstraintExp.angle2d(d0, d1); break;
				case Option.Antidirected: yield return Exp.Abs(ConstraintExp.angle2d(d0, d1)) - Math.PI; break;
				case Option.Any: yield return ExpVector.Cross(d0, d1).z / d0.Magnitude() / d1.Magnitude(); break;
			}
			
		}
	}

	void DrawStroke(ICanvas canvas, IEntity line, int rpt) {
		var p0 = line.GetPointAtInPlane(0, null).Eval();
		var p1 = line.GetPointAtInPlane(1, null).Eval();
		float len = (p1 - p0).magnitude;
		float size = Mathf.Min(len, 10f * getPixelSize());
		Vector3 dir = (p1 - p0).normalized * size / 2f;
		Vector3 perp = Vector3.Cross(p1 - p0, Camera.main.transform.forward).normalized * 3f * getPixelSize();
		Vector3 pos = (p1 + p0) / 2f;
		ref_points[rpt] = sketch.plane.ToPlane(pos);
		canvas.DrawLine(pos + dir + perp, pos - dir + perp);
		canvas.DrawLine(pos + dir - perp, pos - dir - perp);
	}

	protected override void OnDraw(ICanvas canvas) {
		var l0 = GetEntityOfType(IEntityType.Line, 0);
		var l1 = GetEntityOfType(IEntityType.Line, 1);
		DrawStroke(canvas, l0, 0);
		DrawStroke(canvas, l1, 1);
		if(shouldDrawLink) {
			DrawReferenceLink(canvas, Camera.main);
		}
	}

}
