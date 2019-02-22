using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceTool : Tool {

	IEntity e0;
	ValueConstraint constraint;
	Vector3 click;

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
					SpawnConstraint(() => AngleTool.CreateConstraint(e0, entity));
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
		}
		click = WorldPlanePos;
	}

	protected override string OnGetDescription() {
		return "click a two points or a line for constraining distance/length and then click where you want to create dimension value. You can change dimension value by double clicking it when MoveTool is active.";
	}

}
