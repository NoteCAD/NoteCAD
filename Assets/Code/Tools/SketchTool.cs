using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NoteCAD;

public class SketchTool : Tool {
	IEntity p;
	IEntity u;
	IEntity v;

	SketchTool() {
		enableHoverFilter = true;
	}

	protected override bool OnTryHover(Constraint c) {
		return false;
	}
	
	protected override bool OnTryHover(IEntity e) {
		if(p == null) return e.type == IEntityType.Point;
		if(u == null) return e.type == IEntityType.Line;
		if(e.type != IEntityType.Line) return false;
		var dir0 = e.GetDirectionInPlane(null).Eval().normalized;
		var dir1 = u.GetDirectionInPlane(null).Eval().normalized;
		return !GeomUtils.IsVectorsParallel(dir0, dir1);
	}

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		if(sko == null) return;
		var entity = sko as IEntity;
		if(p == null) {
			if(entity.type == IEntityType.Point) {
				p = entity;
			}
		} else if(u == null) {
			if(entity.type == IEntityType.Line) {
				u = entity;
			}
		} else if(v == null) {
			if(entity.type == IEntityType.Line && entity != u) {
				v = entity;
				StopTool();
				editor.PushUndo();
				var feature = new SketchFeature();
				DetailEditor.instance.AddFeature(feature); 
				feature.u = u;
				feature.v = v;
				feature.p = p;
				feature.source = DetailEditor.instance.activeFeature;

				IPlane plane = feature as IPlane;

				if(Vector3.Dot(plane.n, Camera.main.transform.forward) < 0f) {
					feature.u = v;
					feature.v = u;
				}

				DetailEditor.instance.ActivateFeature(feature);
				CameraController.instance.AnimateToPlane(feature);
			}
		}
	}

	protected override void OnDeactivate() {
		u = null;
		v = null;
		p = null;
	}

	protected override void OnActivate() {
	}

	protected override string OnGetDescription() {
		return "click some point first and then click two non-parallel lines to define a new workplane. The point will be origin of the plane, and plane normal will be perpendicular to lines.";
	}

}
