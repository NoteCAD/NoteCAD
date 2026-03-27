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
	Entity trimOtherBegin;
	Entity trimOtherEnd;
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
				out trimBegin, out trimEnd, out trimPosBegin, out trimPosEnd,
				out trimOtherBegin, out trimOtherEnd);
		} else {
			ComputeTrimSegment(intersections, t_mouse,
				out trimBegin, out trimEnd, out trimPosBegin, out trimPosEnd,
				out trimOtherBegin, out trimOtherEnd);
		}

		hoveredEntity = entity;
		hasPreview = true;
	}

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		if(!hasPreview) return;
		if(hoveredEntity == null) return;

		editor.PushUndo();
		if(hoveredEntity is ILoopEntity) {
			PerformLoopTrim(hoveredEntity, trimBegin, trimEnd, trimPosBegin, trimPosEnd, trimOtherBegin, trimOtherEnd);
		} else {
			PerformTrim(hoveredEntity, trimBegin, trimEnd, trimPosBegin, trimPosEnd, trimOtherBegin, trimOtherEnd);
		}
		hasPreview = false;
		hoveredEntity = null;
	}

	void DrawPreview(ICanvas canvas) {
		if(!hasPreview || hoveredEntity == null) return;
		canvas.SetStyle("trimPreview");
		Func<double, double, double> getStep = (p0, p1) => {
			if(hoveredEntity is FunctionEntity fe) {
				var subdiv = Math.Ceiling(Math.Abs(p1 - p0) * fe.GetTotalSubdivision());
				return Math.Abs(p1 - p0) / (subdiv > 1e-6 ? subdiv : 1.0);
			}
			return 1.0 / 64.0;
		};

		// For loop entities the trim segment may wrap around past t=1
		if(hoveredEntity is ILoopEntity && trimBegin > trimEnd + 1e-6) {
			hoveredEntity.DrawParamRange(canvas, 0.0, trimBegin, 1.0, getStep(0.0 , trimBegin), null);
			hoveredEntity.DrawParamRange(canvas, 0.0, 0.0, trimEnd, getStep(0.0, trimEnd), null);
		} else {
			hoveredEntity.DrawParamRange(canvas, 0.0, trimBegin, trimEnd, getStep(trimBegin, trimEnd), null);
		}
	}

	// Collect all intersections with other entities; uses Newton refinement for precision.
	// Also includes PointOn constraint positions so adjacent endpoints act as boundaries.
	// includeTouches=true in GetAllIntersections also captures endpoint-on-entity touches
	// (PointsCoincident-based T-junctions) that plain INTERSECTION checks miss.
	List<(Vector3 pos, double t, Entity other)> CollectIntersections(Entity entity) {
		var result = new List<(Vector3 pos, double t, Entity other)>();
		foreach(var other in entity.sketch.entityList) {
			if(other == entity) continue;
			foreach(var pt in entity.GetIntersections(other, refine: true, includeTouches: true)) {
				AddIntersectionIfNew(entity, pt, result, other);
			}
		}
		// PointOn constraints where this entity is the "on" entity act as trim boundaries;
		// mark with other=null because the incidence constraint already exists.
		foreach(var c in entity.constraints) {
			var pon = c as PointOn;
			if(pon == null || pon.on != entity) continue;
			AddIntersectionIfNew(entity, pon.pointPos, result, null);
		}
		result.Sort((a, b) => a.t.CompareTo(b.t));
		return result;
	}

	void AddIntersectionIfNew(Entity entity, Vector3 pt, List<(Vector3 pos, double t, Entity other)> result, Entity other) {
		double t = entity.FindParameter(pt);
		foreach(var existing in result) {
			if(Math.Abs(existing.t - t) < DUPLICATE_TOLERANCE) return;
		}
		result.Add((pt, t, other));
	}

	// For segmentary entities: find the trim segment containing the mouse parameter
	void ComputeTrimSegment(
		List<(Vector3 pos, double t, Entity other)> intersections, double t_mouse,
		out double t_begin, out double t_end,
		out Vector3 pos_begin, out Vector3 pos_end,
		out Entity other_begin, out Entity other_end)
	{
		t_begin = 0.0;
		t_end = 1.0;
		pos_begin = Vector3.zero;
		pos_end = Vector3.zero;
		other_begin = null;
		other_end = null;
		foreach(var itr in intersections) {
			if(itr.t <= t_mouse) {
				t_begin = itr.t;
				pos_begin = itr.pos;
				other_begin = itr.other;
			} else {
				t_end = itr.t;
				pos_end = itr.pos;
				other_end = itr.other;
				break;
			}
		}
	}

	// For loop entities: find the trim segment (with wraparound support)
	void ComputeLoopTrimSegment(
		List<(Vector3 pos, double t, Entity other)> intersections, double t_mouse,
		out double t_begin, out double t_end,
		out Vector3 pos_begin, out Vector3 pos_end,
		out Entity other_begin, out Entity other_end)
	{
		t_begin = 0.0;
		t_end = 1.0;
		pos_begin = Vector3.zero;
		pos_end = Vector3.zero;
		other_begin = null;
		other_end = null;
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
				other_begin = intersections[i].other;
				t_end = tB;
				pos_end = intersections[(i + 1) % n].pos;
				other_end = intersections[(i + 1) % n].other;
				return;
			}
		}
	}

	void PerformTrim(Entity entity, double t_begin, double t_end, Vector3 posBegin, Vector3 posEnd, Entity otherBegin, Entity otherEnd) {
		bool fromStart = t_begin < ENDPOINT_TOLERANCE;
		bool toEnd    = t_end   > 1.0 - ENDPOINT_TOLERANCE;

		if(fromStart && toEnd) { entity.Destroy(); return; }

		if(fromStart) {
			// Split at posEnd, keep second part, destroy first
			var part = entity.Split(posEnd);
			entity.Destroy();
			if(part is ISegmentaryEntity seg)
				AddIncidenceConstraint(seg.begin, otherEnd);
			return;
		}
		if(toEnd) {
			// Split at posBegin, keep first part, destroy second
			var part = entity.Split(posBegin);
			if(part != null) part.Destroy();
			if(entity is ISegmentaryEntity seg)
				AddIncidenceConstraint(seg.end, otherBegin);
			return;
		}
		// Middle trim
		var part1 = entity.Split(posBegin);
		if(part1 == null) return;
		var part2 = part1.Split(posEnd);
		part1.Destroy();
		if(entity is ISegmentaryEntity entSeg)
			AddIncidenceConstraint(entSeg.end, otherBegin);
		if(part2 is ISegmentaryEntity part2Seg)
			AddIncidenceConstraint(part2Seg.begin, otherEnd);
	}

	void PerformLoopTrim(Entity entity, double t_begin, double t_end, Vector3 posBegin, Vector3 posEnd, Entity otherBegin, Entity otherEnd) {
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
			AddIncidenceConstraint(arc.p0, otherEnd);
			AddIncidenceConstraint(arc.p1, otherBegin);
		} else if(entity is EllipseEntity ellipse) {
			var arc = new EllipticArcEntity(sketch);
			arc.r0.value = ellipse.radius0;
			arc.r1.value = ellipse.radius1;
			arc.basis.CopyFrom(ellipse.basis);
			// EllipseEntity maps t -> angle = t * 2*PI;
			// arc starts at t_end, goes CCW to t_begin (wrapping if needed)
			arc.beginAngle.value = t_end * 2.0 * Math.PI;
			double endT = t_begin < t_end ? t_begin + 1.0 : t_begin;
			arc.deltaAngle.value = (endT - t_end) * 2.0 * Math.PI;
			arc.p0.pos = arcP0;
			arc.p1.pos = arcP1;
			if(style != null) arc.style = style;
			AddIncidenceConstraint(arc.p0, otherEnd);
			AddIncidenceConstraint(arc.p1, otherBegin);
		}
		entity.Destroy();
	}

	// Create a PointsCoincident or PointOn incidence constraint between a trimmed
	// endpoint and the entity it was cut against. Skips if already constrained.
	void AddIncidenceConstraint(PointEntity pt, Entity other) {
		if(other == null) return;
		PointOn existingPOn = null;
		if(pt.IsCoincidentWithCurve(other, ref existingPOn)) return;
		var sketch = pt.sketch;
		double t = other.FindParameter(pt.pos);
		if(other is ISegmentaryEntity seg) {
			if(t < ENDPOINT_TOLERANCE) {
				if(!pt.IsCoincidentWith(seg.begin))
					new PointsCoincident(sketch, pt, seg.begin);
				return;
			}
			if(t > 1.0 - ENDPOINT_TOLERANCE) {
				if(!pt.IsCoincidentWith(seg.end))
					new PointsCoincident(sketch, pt, seg.end);
				return;
			}
		}
		new PointOn(sketch, pt, other);
	}

	protected override string OnGetDescription() {
		return "hover over entity segment to preview trim, click to remove";
	}
}
