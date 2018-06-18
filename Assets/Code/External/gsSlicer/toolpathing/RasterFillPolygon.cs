using System;
using System.Collections.Generic;
using g3;

namespace gs
{
	public class RasterFillPolygon : ICurvesFillPolygon
    {
		// polygon to fill
		public GeneralPolygon2d Polygon { get; set; }

		// parameters
		public double ToolWidth = 0.4;
		public double PathSpacing = 0.4;
		public double AngleDeg = 45.0;
		public double PathShift = 0;

		// [RMS] improve this...
		public double OverlapFactor = 0.0f;

		// if true, we inset half of tool-width from Polygon
		public bool InsetFromInputPolygon = true;

		// fill paths
		public List<FillCurveSet2d> FillCurves { get; set; }
        public List<FillCurveSet2d> GetFillCurves() { return FillCurves; }


        //SegmentSet2d BoundaryPolygonCache;

		public RasterFillPolygon(GeneralPolygon2d poly)
		{
			Polygon = poly;
			FillCurves = new List<FillCurveSet2d>();
		}


		public bool Compute()
		{
			if ( InsetFromInputPolygon ) {
				//BoundaryPolygonCache = new SegmentSet2d(Polygon);
				List<GeneralPolygon2d> current = ClipperUtil.ComputeOffsetPolygon(Polygon, -ToolWidth / 2, true);
				foreach (GeneralPolygon2d poly in current) {
					SegmentSet2d polyCache = new SegmentSet2d(poly);
					FillCurves.Add(ComputeFillPaths(poly, polyCache));
				}

			} else {
				//List<GeneralPolygon2d> boundary = ClipperUtil.ComputeOffsetPolygon(Polygon, ToolWidth / 2, true);
				//BoundaryPolygonCache = new SegmentSet2d(boundary);

				SegmentSet2d polyCache = new SegmentSet2d(Polygon);
				FillCurves.Add(ComputeFillPaths(Polygon, polyCache));

			}


			return true;
		}




		protected FillCurveSet2d ComputeFillPaths(GeneralPolygon2d poly, SegmentSet2d polyCache) 
		{
			List<List<Segment2d>> StepSpans = ComputeSegments(poly, polyCache);
			int N = StepSpans.Count;

			//double hard_max_dist = 5 * PathSpacing;

			// [TODO] need a pathfinder here, that can chain segments efficiently

			// (for now just do dumb things?)

			FillCurveSet2d paths = new FillCurveSet2d();
			//FillPolyline2d cur = new FillPolyline2d();

			foreach ( var seglist in StepSpans ) {
				foreach (Segment2d seg in seglist ) {
					FillPolyline2d fill_seg = new FillPolyline2d() {
						TypeFlags = FillTypeFlags.SolidInfill
					};
					fill_seg.AppendVertex(seg.P0);
					fill_seg.AppendVertex(seg.P1);
					paths.Append(fill_seg);
				}
			}

			return paths;
		}



		// finds segment endpoint in spans closest to input point
		int find_nearest_span_endpoint(List<Segment2d> spans, Vector2d prev, out bool reverse)
		{
			reverse = false;
			int N = spans.Count;
			int iNearest = -1;
			double dNearest = double.MaxValue;
			for (int i = 0; i < N; ++i) {
				double d0 = prev.DistanceSquared(spans[i].P0);
				double d1 = prev.DistanceSquared(spans[i].P1);
				double min = Math.Min(d0, d1);
				if ( min < dNearest ) {
					dNearest = min;
					iNearest = i;
					if (d1 < d0)
						reverse = true;
				}
			}
			return iNearest;
		}



		protected List<List<Segment2d>> ComputeSegments(GeneralPolygon2d poly, SegmentSet2d polyCache) {

			List<List<Segment2d>> PerRaySpans = new List<List<Segment2d>>();

			double angleRad = AngleDeg * MathUtil.Deg2Rad;
			Vector2d dir = new Vector2d(Math.Cos(angleRad), Math.Sin(angleRad));

			// compute projection span along axis
			Vector2d axis = dir.Perp;
			Interval1d axisInterval = Interval1d.Empty;
			Interval1d dirInterval = Interval1d.Empty;
			foreach ( Vector2d v in poly.Outer.Vertices ) {
				dirInterval.Contain(v.Dot(dir));
				axisInterval.Contain(v.Dot(axis));
			}
			// [TODO] also check holes? or assume they are contained?

			dirInterval.a -= 10 * ToolWidth;
			dirInterval.b += 10 * ToolWidth;
			double extent = dirInterval.Length;

			axisInterval.a += ToolWidth * 0.1 + PathShift;
			axisInterval.b -= ToolWidth * 0.1;
			if (axisInterval.b < axisInterval.a)
				return PerRaySpans;		// [RMS] is this right? I guess so. interval is too small to fill?

			Vector2d startCorner = axisInterval.a * axis + dirInterval.a * dir;
			double range = axisInterval.Length;
			int N = (int)(range / PathSpacing);

			for (int ti = 0; ti <= N; ++ti ) {
				double t = (double)ti / (double)N;
				Vector2d o = startCorner + (t * range) * axis;
				Segment2d ray = new Segment2d(o, o + extent * dir);

				List<Segment2d> spans = compute_polygon_ray_spans(poly, ray, startCorner, axis, t, polyCache);
				PerRaySpans.Add(spans);
			}

			return PerRaySpans;
		}



		// yikes not robust at all!!
		protected List<Segment2d> compute_polygon_ray_spans(GeneralPolygon2d poly, Segment2d ray, Vector2d axis_origin, Vector2d axis, double axisT, SegmentSet2d segments) 
		{

			List<double> hits = new List<double>();     // todo reusable buffer
			segments.FindAllIntersections(ray, hits, null, null, false);
			hits.Sort();

			bool clean = true;
			for (int i = 0; i < hits.Count - 1 && clean; ++i ) {
				if ( hits[i+1]-hits[i] < MathUtil.Epsilonf ) 
					clean = false;
			}
			if (!clean)
				hits = extract_valid_segments(poly, ray, hits);

			if (hits.Count % 2 != 0)
				throw new Exception("DenseLineFill.ComputeAllSpans: have not handled hard cases...");

			List<Segment2d> spans = new List<Segment2d>();
			for (int i = 0; i < hits.Count / 2; ++i ) {
				Vector2d p0 = ray.PointAt(hits[2 * i]);
				Vector2d p1 = ray.PointAt(hits[2 * i + 1]);
				spans.Add(new Segment2d(p0, p1));
			}

			return spans;
		}




		/// <summary>
		/// hits is a sorted list of t-values along ray. This function
		/// tries to pull out the valid pairs, ie where the segment between the
		/// pair is inside poly.
		/// 
		/// numerical problems:
		///    - no guarantee that all intersection t's are in hits list 
		///       (although we are being conservative in SegmentSet2d, testing extent+eps)
		///    - poly.Contains() could return false for points very near to border
		///       (in unfortunate case this means we discard valid segments. in 
		///        pathological case it means we produce invalid ones)
		/// </summary>
		List<double> extract_valid_segments(GeneralPolygon2d poly, Segment2d ray, List<double> hits) {
			double eps = MathUtil.Epsilonf;

			List<double> result = new List<double>();
			int i = 0;
			int j = i + 1;

			while (j < hits.Count) {

				// find next non-dupe
				while (j < hits.Count && hits[j] - hits[i] < eps) {
					j++;
				}
                if (j == hits.Count)
                    continue;

				// ok check if midpoint is inside or outside
				double mid_t = (hits[i] + hits[j]) * 0.5;
				Vector2d mid = ray.PointAt(mid_t);

				// not robust...eek
				bool isInside = poly.Contains(mid);
				if ( isInside ) {
					// ok we add this segment, and then we start looking at next point (?)
					result.Add(hits[i]);
					result.Add(hits[j]);
					i = j + 1;
					j = i + 1;
				} else {
					// ok we were not inside, so start search at j
					i = j;
					j++;
				}


			}

			return result;
		}

	}
}
