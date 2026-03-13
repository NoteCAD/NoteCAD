using System.Collections.Generic;
using UnityEngine;
using NoteCAD;

public class TrimTool : Tool {

	Entity hoveredEntity;
	double trimBegin;
	double trimEnd;
	Vector3 trimPosBegin;
	Vector3 trimPosEnd;
	bool hasBeginIntersection;
	bool hasEndIntersection;
	bool hasPreview;

	TrimTool() {
		enableHoverFilter = true;
	}

	protected override bool OnTryHover(Constraint c) {
		return false;
	}

	protected override bool OnTryHover(IEntity e) {
		return e is ISegmentaryEntity;
	}

	protected override void OnActivate() {
		editor.toolPreviewDraw = DrawPreview;
		hasPreview = false;
	}

	protected override void OnDeactivate() {
		editor.toolPreviewDraw = null;
		hasPreview = false;
	}

	protected override void OnMouseMove(Vector3 pos, ICADObject sko) {
		hasPreview = false;
		var entity = sko as Entity;
		if(entity == null) return;
		if(!(entity is ISegmentaryEntity)) return;
		if(entity.sketch == null) return;

		var intersections = CollectIntersections(entity);
		if(intersections.Count == 0) return;

		double t_mouse = entity.FindParameter(pos);
		ComputeTrimSegment(intersections, t_mouse,
			out trimBegin, out trimEnd,
			out trimPosBegin, out trimPosEnd,
			out hasBeginIntersection, out hasEndIntersection);

		hoveredEntity = entity;
		hasPreview = true;
	}

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		if(!hasPreview) return;
		if(hoveredEntity == null) return;

		editor.PushUndo();
		PerformTrim(hoveredEntity, trimPosBegin, trimPosEnd, hasBeginIntersection, hasEndIntersection);
		hasPreview = false;
		hoveredEntity = null;
	}

	void DrawPreview(ICanvas canvas) {
		if(!hasPreview || hoveredEntity == null) return;
		canvas.SetStyle("error");
		hoveredEntity.DrawParamRange(canvas, 0.0, trimBegin, trimEnd, 0.05, null);
	}

	List<(Vector3 pos, double t)> CollectIntersections(Entity entity) {
		var result = new List<(Vector3 pos, double t)>();
		foreach(var other in entity.sketch.entityList) {
			if(other == entity) continue;
			var crossings = entity.GetAllIntersections(other);
			foreach(var c in crossings) {
				double t = entity.FindParameter(c);
				if(t > 1e-4 && t < 1.0 - 1e-4) {
					result.Add((c, t));
				}
			}
		}
		result.Sort((a, b) => a.t.CompareTo(b.t));
		return result;
	}

	void ComputeTrimSegment(
		List<(Vector3 pos, double t)> intersections, double t_mouse,
		out double t_begin, out double t_end,
		out Vector3 pos_begin, out Vector3 pos_end,
		out bool has_begin, out bool has_end)
	{
		t_begin = 0.0;
		t_end = 1.0;
		pos_begin = Vector3.zero;
		pos_end = Vector3.zero;
		has_begin = false;
		has_end = false;

		foreach(var itr in intersections) {
			if(itr.t <= t_mouse) {
				t_begin = itr.t;
				pos_begin = itr.pos;
				has_begin = true;
			} else {
				t_end = itr.t;
				pos_end = itr.pos;
				has_end = true;
				break;
			}
		}
	}

	void PerformTrim(Entity entity, Vector3 posBegin, Vector3 posEnd, bool hasBegin, bool hasEnd) {
		if(!hasBegin && !hasEnd) {
			entity.Destroy();
			return;
		}

		if(!hasBegin) {
			// Trim beginning: split at posEnd, destroy the first part (original entity)
			var part = entity.Split(posEnd);
			entity.Destroy();
			return;
		}

		if(!hasEnd) {
			// Trim end: split at posBegin, destroy the second part
			var part = entity.Split(posBegin);
			if(part != null) part.Destroy();
			return;
		}

		// Trim middle: split at posBegin, then split remainder at posEnd, destroy the middle
		var part1 = entity.Split(posBegin);
		if(part1 == null) return;
		var part2 = part1.Split(posEnd);
		part1.Destroy();
	}

	protected override string OnGetDescription() {
		return "hover over entity segment to preview trim, click to remove";
	}
}
