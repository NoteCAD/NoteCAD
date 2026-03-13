using System;
using System.Collections.Generic;
using UnityEngine;
using NoteCAD;

public class TrimTool : Tool {

	Entity hoveredEntity;
	double trimBegin;
	double trimEnd;
	Vector3 trimPosBegin;
	Vector3 trimPosEnd;
	bool hasPreview;

	const double ENDPOINT_TOLERANCE  = 1e-4;
	const double DUPLICATE_TOLERANCE = 1e-3;

	TrimTool() {
		enableHoverFilter = true;
	}

	protected override bool OnTryHover(Constraint c) {
		return false;
	}

	protected override bool OnTryHover(IEntity e) {
		return e is ISegmentaryEntity || e is ILoopEntity;
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
		if(entity.sketch == null) return;
		bool isLoop = entity is ILoopEntity;
		bool isSegment = entity is ISegmentaryEntity;
		if(!isSegment && !isLoop) return;

		var intersections = CollectIntersections(entity);
		double t_mouse = entity.FindParameter(pos);

		if(isLoop) {
			ComputeLoopTrimSegment(intersections, t_mouse,
				out trimBegin, out trimEnd, out trimPosBegin, out trimPosEnd);
		} else {
			ComputeTrimSegment(intersections, t_mouse,
				out trimBegin, out trimEnd, out trimPosBegin, out trimPosEnd);
		}

		hoveredEntity = entity;
		hasPreview = true;
	}

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		if(!hasPreview) return;
		if(hoveredEntity == null) return;

		editor.PushUndo();
		if(hoveredEntity is ILoopEntity) {
			PerformLoopTrim(hoveredEntity, trimBegin, trimEnd, trimPosBegin, trimPosEnd);
		} else {
			PerformTrim(hoveredEntity, trimBegin, trimEnd, trimPosBegin, trimPosEnd);
		}
		hasPreview = false;
		hoveredEntity = null;
	}

	void DrawPreview(ICanvas canvas) {
		if(!hasPreview || hoveredEntity == null) return;
		canvas.SetStyle("trimPreview");
		double step = (hoveredEntity is FunctionEntity fe) ? fe.GetTrimPreviewStep() : 1.0 / 64.0;
		// For loop entities the trim segment may wrap around past t=1
		if(hoveredEntity is ILoopEntity && trimBegin > trimEnd + 1e-6) {
			hoveredEntity.DrawParamRange(canvas, 0.0, trimBegin, 1.0, step, null);
			hoveredEntity.DrawParamRange(canvas, 0.0, 0.0, trimEnd, step, null);
		} else {
			hoveredEntity.DrawParamRange(canvas, 0.0, trimBegin, trimEnd, step, null);
		}
	}

	// Collect all intersections with other entities; uses Newton refinement for precision
	List<(Vector3 pos, double t)> CollectIntersections(Entity entity) {
		var result = new List<(Vector3 pos, double t)>();
		foreach(var other in entity.sketch.entityList) {
			if(other == entity) continue;
			foreach(var pt in entity.GetAllIntersections(other, refine: true)) {
				double t = entity.FindParameter(pt);
				bool dupe = false;
				foreach(var existing in result) {
					if(Math.Abs(existing.t - t) < DUPLICATE_TOLERANCE) { dupe = true; break; }
				}
				if(!dupe) result.Add((pt, t));
			}
		}
		result.Sort((a, b) => a.t.CompareTo(b.t));
		return result;
	}

	// For segmentary entities: find the trim segment containing the mouse parameter
	void ComputeTrimSegment(
		List<(Vector3 pos, double t)> intersections, double t_mouse,
		out double t_begin, out double t_end,
		out Vector3 pos_begin, out Vector3 pos_end)
	{
		t_begin = 0.0;
		t_end = 1.0;
		pos_begin = Vector3.zero;
		pos_end = Vector3.zero;
		foreach(var itr in intersections) {
			if(itr.t <= t_mouse) {
				t_begin = itr.t;
				pos_begin = itr.pos;
			} else {
				t_end = itr.t;
				pos_end = itr.pos;
				break;
			}
		}
	}

	// For loop entities: find the trim segment (with wraparound support)
	void ComputeLoopTrimSegment(
		List<(Vector3 pos, double t)> intersections, double t_mouse,
		out double t_begin, out double t_end,
		out Vector3 pos_begin, out Vector3 pos_end)
	{
		t_begin = 0.0;
		t_end = 1.0;
		pos_begin = Vector3.zero;
		pos_end = Vector3.zero;
		int n = intersections.Count;
		if(n < 2) return;
		for(int i = 0; i < n; i++) {
			double tA = intersections[i].t;
			double tB = intersections[(i + 1) % n].t;
			bool inSeg = (i + 1 < n)
				? (t_mouse >= tA && t_mouse <= tB)
				: (t_mouse >= tA || t_mouse <= tB);
			if(inSeg) {
				t_begin = tA;
				pos_begin = intersections[i].pos;
				t_end = tB;
				pos_end = intersections[(i + 1) % n].pos;
				return;
			}
		}
	}

	void PerformTrim(Entity entity, double t_begin, double t_end, Vector3 posBegin, Vector3 posEnd) {
		bool fromStart = t_begin < ENDPOINT_TOLERANCE;
		bool toEnd    = t_end   > 1.0 - ENDPOINT_TOLERANCE;

		if(fromStart && toEnd) { entity.Destroy(); return; }

		if(fromStart) {
			// Split at posEnd, keep second part, destroy first
			var part = entity.Split(posEnd);
			entity.Destroy();
			return;
		}
		if(toEnd) {
			// Split at posBegin, keep first part, destroy second
			var part = entity.Split(posBegin);
			if(part != null) part.Destroy();
			return;
		}
		// Middle trim
		var part1 = entity.Split(posBegin);
		if(part1 == null) return;
		var part2 = part1.Split(posEnd);
		part1.Destroy();
	}

	void PerformLoopTrim(Entity entity, double t_begin, double t_end, Vector3 posBegin, Vector3 posEnd) {
		// If no valid pair of intersections, remove the whole loop
		if(t_begin < ENDPOINT_TOLERANCE && t_end > 1.0 - ENDPOINT_TOLERANCE) {
			entity.Destroy();
			return;
		}
		var sketch = entity.sketch;
		var style = entity.style;
		Param pOn = new Param("pOn");
		var ptOn = entity.PointOn(pOn);
		// Keep arc from t_end to t_begin (counterclockwise, wrapping past t=0)
		pOn.value = t_end;
		var arcP0 = ptOn.Eval();
		pOn.value = t_begin;
		var arcP1 = ptOn.Eval();

		if(entity is CircleEntity circle) {
			var arc = new ArcEntity(sketch);
			arc.p0.pos = arcP0;
			arc.p1.pos = arcP1;
			arc.center.pos = circle.center.pos;
			if(style != null) arc.style = style;
		} else if(entity is EllipseEntity ellipse) {
			var arc = new EllipticArcEntity(sketch);
			arc.p0.pos = arcP0;
			arc.p1.pos = arcP1;
			arc.center.pos = ellipse.center.pos;
			if(style != null) arc.style = style;
		}
		entity.Destroy();
	}

	protected override string OnGetDescription() {
		return "hover over entity segment to preview trim, click to remove";
	}
}
