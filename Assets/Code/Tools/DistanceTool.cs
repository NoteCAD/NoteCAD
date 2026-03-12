using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NoteCAD;

public class DistanceTool : Tool {

	IEntity e0;
	ValueConstraint constraint;
	Vector3 click;

	DistanceTool() {
		enableHoverFilter = true;
	}

	protected override bool OnTryHover(Constraint c) {
		return false;
	}

	T SpawnConstraint<T>(Func<T> create) where T: ValueConstraint {
		if(constraint != null) {
			constraint.Destroy();
			editor.PopUndo();
		}
		editor.PushUndo();
		var c = create();
		constraint = c;
		constraint.pos = WorldPlanePos;
		click = WorldPlanePos;
		constraint.isSelectable = false;
		return c;
	}

	void Finish() {
		constraint.isSelectable = true;
		MoveTool.instance.EditConstraintValue(constraint, pushUndo:false);
		constraint = null;
		e0 = null;
	}

	protected override bool OnTryHover(IEntity e) {
		return e.type == IEntityType.Point || e.type == IEntityType.Line || e.IsCircular();
	}

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		var entity = sko as IEntity;
		if(constraint != null && entity == null) {
			Finish();
			return;
		}
		if(entity == null) return;

		if(e0 == null) {
			e0 = entity;
			if(entity.type == IEntityType.Line) {
				SpawnConstraint(() => new PointsDistance(DetailEditor.instance.currentSketch.GetSketch(), entity));
			} else 
			if(entity.IsCircular()) {
				SpawnConstraint(() => new Diameter(DetailEditor.instance.currentSketch.GetSketch(), entity));
			}
		} else {
			if(entity.type == IEntityType.Point || e0.IsCircular() && entity.type == IEntityType.Line) {
				var t = e0;
				e0 = entity;
				entity = t;
			}
			if(e0.type == IEntityType.Point) {
				if(entity.type == IEntityType.Point) {
					SpawnConstraint(() => new PointsDistance(DetailEditor.instance.currentSketch.GetSketch(), e0, entity));
				} else if(entity.type == IEntityType.Line) {
					SpawnConstraint(() => new PointLineDistance(DetailEditor.instance.currentSketch.GetSketch(), e0, entity));
				} else if(entity.IsCircular()) {
					var circle = entity as CircleEntity;
					var arc = entity as ArcEntity;
					if(circle != null && circle.center.IsCoincidentWith(e0)) {
						var c = SpawnConstraint(() => new Diameter(DetailEditor.instance.currentSketch.GetSketch(), circle));
						c.showAsRadius = true;
						e0 = null;
					} else 
					if(arc != null && arc.c.IsCoincidentWith(e0)) {
						/*var c = */SpawnConstraint(() => new Diameter(DetailEditor.instance.currentSketch.GetSketch(), arc));
						e0 = null;
					} else {
						SpawnConstraint(() => new PointCircleDistance(DetailEditor.instance.currentSketch.GetSketch(), e0, entity));
					}
				}
			} else if(e0.IsCircular()) {
				if(entity.IsCircular()) {
					SpawnConstraint(() => new CirclesDistance(DetailEditor.instance.currentSketch.GetSketch(), e0, entity));
				}
			} else if(e0.type == IEntityType.Line) {
				if(entity.type == IEntityType.Line) {
					//SpawnConstraint(() => AngleTool.CreateConstraint(e0, entity));
					SpawnConstraint(() => new LineLineDistance(DetailEditor.instance.currentSketch.GetSketch(), e0, entity));
				} else
				if(entity.IsCircular()) {
					SpawnConstraint(() => new LineCircleDistance(DetailEditor.instance.currentSketch.GetSketch(), e0, entity));
				}
			}
		}
	}

	protected override void OnDeactivate() {
		e0 = null;
		if(constraint != null) {
			constraint.Destroy();
			editor.PopUndo();
		}
		constraint = null;
	}

	protected override void OnMouseMove(Vector3 pos, ICADObject entity) {
		if(constraint != null) {
			constraint.Drag(WorldPlanePos - click);
			AutoSelectHVOption();
		}
		click = WorldPlanePos;
	}

	void AutoSelectHVOption() {
		var pd = constraint as PointsDistance;
		if(pd == null) return;
		var plane = DetailEditor.instance.currentSketch.GetSketch().plane;
		if(plane == null) return;

		var labelDir = plane.DirToPlane(WorldPlanePos - pd.GetMidpoint());
		if(labelDir.magnitude < 1e-4f) return;

		// Use atan2 to compute the drag angle, folded into [0°, 90°]:
		//   0°–30°   → near-horizontal drag → Vertical span
		//   60°–90°  → near-vertical drag   → Horizontal span
		//   30°–60°  → diagonal             → Closest (linear)
		float angleDeg = Mathf.Abs(Mathf.Atan2(labelDir.y, labelDir.x) * Mathf.Rad2Deg);
		// Fold into [0°, 90°] by mirroring at 90°.
		if(angleDeg > 90f) angleDeg = 180f - angleDeg;

		PointsDistance.Option newOption;
		if(angleDeg < 30f) {
			// Near-horizontal drag → Vertical span; pick sign from current positions.
			newOption = (pd.p1exp.y.Eval() >= pd.p0exp.y.Eval())
				? PointsDistance.Option.VerticalPositive
				: PointsDistance.Option.VerticalNegative;
		} else if(angleDeg > 60f) {
			// Near-vertical drag → Horizontal span; pick sign from current positions.
			newOption = (pd.p1exp.x.Eval() >= pd.p0exp.x.Eval())
				? PointsDistance.Option.HorizontalPositive
				: PointsDistance.Option.HorizontalNegative;
		} else {
			newOption = PointsDistance.Option.Closest;
		}

		if(newOption != pd.option) {
			var worldPos = constraint.pos;
			pd.option = newOption;
			constraint.pos = worldPos;
			pd.Satisfy();
		}
	}

	protected override string OnGetDescription() {
		return "click a line to constrain length or circle(arc) to constrain daimeter(radius). Click two entities of type point/line/circle/arc to constrain distance or angle between them.";
	}

}
