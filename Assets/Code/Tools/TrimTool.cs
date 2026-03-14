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
	IntersectionInfo trimInfoBegin;
	IntersectionInfo trimInfoEnd;
	bool hasPreview;

	const double ENDPOINT_TOLERANCE  = 1e-4;
	const double DUPLICATE_TOLERANCE = 1e-3;

	// Tracks the source of each detected intersection so the trim operation
	// can create the right incidence constraint at the new endpoint.
	struct IntersectionInfo {
		public Vector3 pos;
		public double t;
		// Entity that crosses the trimmed entity at this point.
		// Null when the intersection comes from an existing PointOn constraint (see onPoint).
		public Entity other;
		// Point already constrained on the trimmed entity via an existing PointOn constraint.
		// When non-null, other is null and a PointsCoincident is used instead of a new PointOn.
		public PointEntity onPoint;
	}

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
		hasPreview = false;
	}

	protected override void OnDeactivate() {
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
				out trimInfoBegin, out trimInfoEnd);
		} else {
			ComputeTrimSegment(intersections, t_mouse,
				out trimBegin, out trimEnd, out trimPosBegin, out trimPosEnd,
				out trimInfoBegin, out trimInfoEnd);
		}

		hoveredEntity = entity;
		hasPreview = true;
	}

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {
		if(!hasPreview) return;
		if(hoveredEntity == null) return;

		editor.PushUndo();
		if(hoveredEntity is ILoopEntity) {
			PerformLoopTrim(hoveredEntity, trimBegin, trimEnd, trimPosBegin, trimPosEnd, trimInfoBegin, trimInfoEnd);
		} else {
			PerformTrim(hoveredEntity, trimBegin, trimEnd, trimPosBegin, trimPosEnd, trimInfoBegin, trimInfoEnd);
		}
		hasPreview = false;
		hoveredEntity = null;
	}

	protected override void OnDrawPreview(ICanvas canvas) {
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

	// Collect all intersections with other entities; uses Newton refinement for precision.
	// Also includes PointOn constraint positions so adjacent endpoints act as boundaries.
	// includeTouches=true in GetAllIntersections also captures endpoint-on-entity touches
	// (PointsCoincident-based T-junctions) that plain INTERSECTION checks miss.
	List<IntersectionInfo> CollectIntersections(Entity entity) {
		var result = new List<IntersectionInfo>();
		foreach(var other in entity.sketch.entityList) {
			if(other == entity) continue;
			foreach(var pt in entity.GetAllIntersections(other, refine: true, includeTouches: true)) {
				AddIntersectionIfNew(entity, new IntersectionInfo { pos = pt, other = other }, result);
			}
		}
		// PointOn constraints where this entity is the "on" entity act as trim boundaries.
		// The constrained point is stored so that a PointsCoincident can be added at trim time.
		foreach(var c in entity.constraints) {
			var pon = c as PointOn;
			if(pon == null || pon.on != entity) continue;
			var ptEntity = pon.point as PointEntity;
			AddIntersectionIfNew(entity, new IntersectionInfo { pos = pon.pointPos, onPoint = ptEntity }, result);
		}
		result.Sort((a, b) => a.t.CompareTo(b.t));
		return result;
	}

	void AddIntersectionIfNew(Entity entity, IntersectionInfo info, List<IntersectionInfo> result) {
		info.t = entity.FindParameter(info.pos);
		foreach(var existing in result) {
			if(Math.Abs(existing.t - info.t) < DUPLICATE_TOLERANCE) return;
		}
		result.Add(info);
	}

	// For segmentary entities: find the trim segment containing the mouse parameter
	void ComputeTrimSegment(
		List<IntersectionInfo> intersections, double t_mouse,
		out double t_begin, out double t_end,
		out Vector3 pos_begin, out Vector3 pos_end,
		out IntersectionInfo info_begin, out IntersectionInfo info_end)
	{
		t_begin = 0.0;
		t_end = 1.0;
		pos_begin = Vector3.zero;
		pos_end = Vector3.zero;
		info_begin = new IntersectionInfo();
		info_end = new IntersectionInfo();
		foreach(var itr in intersections) {
			if(itr.t <= t_mouse) {
				t_begin = itr.t;
				pos_begin = itr.pos;
				info_begin = itr;
			} else {
				t_end = itr.t;
				pos_end = itr.pos;
				info_end = itr;
				break;
			}
		}
	}

	// For loop entities: find the trim segment (with wraparound support)
	void ComputeLoopTrimSegment(
		List<IntersectionInfo> intersections, double t_mouse,
		out double t_begin, out double t_end,
		out Vector3 pos_begin, out Vector3 pos_end,
		out IntersectionInfo info_begin, out IntersectionInfo info_end)
	{
		t_begin = 0.0;
		t_end = 1.0;
		pos_begin = Vector3.zero;
		pos_end = Vector3.zero;
		info_begin = new IntersectionInfo();
		info_end = new IntersectionInfo();
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
				info_begin = intersections[i];
				t_end = tB;
				pos_end = intersections[(i + 1) % n].pos;
				info_end = intersections[(i + 1) % n];
				return;
			}
		}
	}

	void PerformTrim(Entity entity, double t_begin, double t_end, Vector3 posBegin, Vector3 posEnd,
	                 IntersectionInfo infoBegin, IntersectionInfo infoEnd) {
		bool fromStart = t_begin < ENDPOINT_TOLERANCE;
		bool toEnd    = t_end   > 1.0 - ENDPOINT_TOLERANCE;

		if(fromStart && toEnd) { entity.Destroy(); return; }

		var sketch = entity.sketch;
		if(fromStart) {
			// Split at posEnd, keep second part, destroy first
			var part = entity.Split(posEnd);
			entity.Destroy();
			if(part is ISegmentaryEntity seg) AddTrimConstraint(sketch, seg.begin, infoEnd);
			return;
		}
		if(toEnd) {
			// Split at posBegin, keep first part, destroy second
			var part = entity.Split(posBegin);
			if(part != null) part.Destroy();
			if(entity is ISegmentaryEntity seg) AddTrimConstraint(sketch, seg.end, infoBegin);
			return;
		}
		// Middle trim
		var part1 = entity.Split(posBegin);
		if(part1 == null) return;
		var part2 = part1.Split(posEnd);
		part1.Destroy();
		if(entity is ISegmentaryEntity segA) AddTrimConstraint(sketch, segA.end, infoBegin);
		if(part2 is ISegmentaryEntity segB) AddTrimConstraint(sketch, segB.begin, infoEnd);
	}

	void PerformLoopTrim(Entity entity, double t_begin, double t_end, Vector3 posBegin, Vector3 posEnd,
	                     IntersectionInfo infoBegin, IntersectionInfo infoEnd) {
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
			AddTrimConstraint(sketch, arc.p0, infoEnd);
			AddTrimConstraint(sketch, arc.p1, infoBegin);
		} else if(entity is EllipseEntity ellipse) {
			var arc = new EllipticArcEntity(sketch);
			arc.p0.pos = arcP0;
			arc.p1.pos = arcP1;
			arc.center.pos = ellipse.center.pos;
			if(style != null) arc.style = style;
			AddTrimConstraint(sketch, arc.p0, infoEnd);
			AddTrimConstraint(sketch, arc.p1, infoBegin);
		}
		entity.Destroy();
	}

	// Add the appropriate incidence constraint between a newly-created trim endpoint
	// and the entity/point that defined the trim boundary at that location.
	//   - If the boundary came from an existing PointOn constraint, the split point
	//     is made coincident with that already-constrained point.
	//   - If the boundary is an endpoint of the other entity, PointsCoincident is used.
	//   - Otherwise a PointOn constraint places the split point on the other entity.
	void AddTrimConstraint(Sketch sketch, PointEntity splitPoint, IntersectionInfo info) {
		if(info.onPoint != null) {
			new PointsCoincident(sketch, splitPoint, info.onPoint);
		} else if(info.other != null) {
			if(info.other is ISegmentaryEntity segOther) {
				double tOther = info.other.FindParameter(info.pos);
				if(tOther < ENDPOINT_TOLERANCE) {
					new PointsCoincident(sketch, splitPoint, segOther.begin);
					return;
				}
				if(tOther > 1.0 - ENDPOINT_TOLERANCE) {
					new PointsCoincident(sketch, splitPoint, segOther.end);
					return;
				}
			}
			new PointOn(sketch, splitPoint, info.other);
		}
	}

	protected override string OnGetDescription() {
		return "hover over entity segment to preview trim, click to remove";
	}
}
