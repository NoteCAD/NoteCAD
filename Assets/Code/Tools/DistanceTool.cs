using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceTool : Tool {


	IEntity p0;
	ValueConstraint constraint;
	Vector3 click;

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		if(constraint != null) {
			MoveTool.instance.EditConstraintValue(constraint);
			constraint = null;
			return;
		}
		var entity = sko as IEntity;
		if(entity == null) return;

		if(p0 == null) {
			if(entity.type == IEntityType.Line) {
				constraint = new PointsDistance(DetailEditor.instance.currentSketch.GetSketch(), entity);
				constraint.pos = WorldPlanePos;
				click = WorldPlanePos;
				return;
			} else 
			if(entity is CircleEntity) {
				var circle = entity as CircleEntity;
				constraint = new Diameter(circle.sketch, circle);
				constraint.pos = WorldPlanePos;
				click = WorldPlanePos;
				return;
			}
		}

		if(p0 != null) {
			if(entity.type == IEntityType.Point) {
				constraint = new PointsDistance(DetailEditor.instance.currentSketch.GetSketch(), p0, entity);
				constraint.pos = WorldPlanePos;
				p0 = null;
			} else if(entity.type == IEntityType.Line) {
				constraint = new PointLineDistance(DetailEditor.instance.currentSketch.GetSketch(), p0, entity);
				constraint.pos = WorldPlanePos;
				p0 = null;
			}
		} else if(entity.type == IEntityType.Point) {
			p0 = entity;
		}
	}

	protected override void OnDeactivate() {
		p0 = null;
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
