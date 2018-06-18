using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using g3;

namespace gs
{
	public class ParallelLinesFillPolygon : ICurvesFillPolygon
    {
		// polygon to fill
		public GeneralPolygon2d Polygon { get; set; }

		// parameters
		public double ToolWidth = 0.4;
		public double PathSpacing = 0.4;
		public double AngleDeg = 45.0;
		public double PathShift = 0;

		// if true, we will nudge PathSpacing up/down to ensure that 
		// we don't leave pathwidth-gap at end of span
		public bool AdjustSpacingToMaximizeFill = true;

        // paths shorter than this are discarded
        public double MinPathLengthMM = 2.0;

		// if true, we inset half of tool-width from Polygon
		public bool InsetFromInputPolygon = true;

        public enum SimplificationLevel {
            None = 0,
            Minor = 1,              // eigth-tool-width
            Moderate = 2,           // quarter-tool-width
            Aggressive = 3          // half-tool-width
        }
        public SimplificationLevel SimplifyAmount = SimplificationLevel.Minor;

        // this flag is set on all Paths
        public FillTypeFlags TypeFlags = FillTypeFlags.SolidInfill;


		// fill paths
		public List<FillCurveSet2d> FillCurves { get; set; }
        public List<FillCurveSet2d> GetFillCurves() { return FillCurves; }

        // [RMS] only using this for hit-testing to make sure no connectors cross polygon border...
        // [TODO] replace with GeneralPolygon2dBoxTree (currently does not have intersection test!)
        //SegmentSet2d BoundaryPolygonCache;

		public ParallelLinesFillPolygon(GeneralPolygon2d poly)
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
                    FillCurveSet2d fillPaths = ComputeFillPaths(poly);
                    if (fillPaths != null )
					    FillCurves.Add(fillPaths);
				}

			} else {
				List<GeneralPolygon2d> boundary = ClipperUtil.ComputeOffsetPolygon(Polygon, ToolWidth / 2, true);
				//BoundaryPolygonCache = new SegmentSet2d(boundary);
                FillCurveSet2d fillPaths = ComputeFillPaths(Polygon);
                if (fillPaths != null)
                    FillCurves.Add(fillPaths);
			}


			return true;
		}





        /// <summary>
        /// fill poly w/ adjacent straight line segments, connected by connectors
        /// </summary>
        protected FillCurveSet2d ComputeFillPaths(GeneralPolygon2d poly)
        {
            FillCurveSet2d paths = new FillCurveSet2d();

            // smooth the input poly a little bit, this simplifies the filling
            // (simplify after?)
            //GeneralPolygon2d smoothed = poly.Duplicate();
            //CurveUtils2.LaplacianSmoothConstrained(smoothed, 0.5, 5, ToolWidth / 2, true, false);
            //poly = smoothed;

            // compute 2D non-manifold graph consisting of original polygon and
            // inserted line segments
            DGraph2 spanGraph = ComputeSpanGraph(poly);
            if (spanGraph == null || spanGraph.VertexCount == poly.VertexCount)
                return paths;


            DGraph2 pathGraph = BuildPathGraph(spanGraph);


            HashSet<int> boundaries = new HashSet<int>();
            foreach (int vid in pathGraph.VertexIndices()) {
                if (pathGraph.IsBoundaryVertex(vid))
                    boundaries.Add(vid);
                if (pathGraph.IsJunctionVertex(vid))
                    throw new Exception("DenseLinesFillPolygon: PathGraph has a junction???");
            }

            // walk paths from boundary vertices
            while (boundaries.Count > 0) {
                int start_vid = boundaries.First();
                boundaries.Remove(start_vid);
                int vid = start_vid;
                int eid = pathGraph.GetVtxEdges(vid)[0];

                FillPolyline2d path = new FillPolyline2d() { TypeFlags = this.TypeFlags };

                path.AppendVertex(pathGraph.GetVertex(vid));
                while (true) {
                    Index2i next = DGraph2Util.NextEdgeAndVtx(eid, vid, pathGraph);
                    eid = next.a;
                    vid = next.b;
                    int gid = pathGraph.GetEdgeGroup(eid);
                    if (gid < 0) {
                        path.AppendVertex(pathGraph.GetVertex(vid), TPVertexFlags.IsConnector);
                    } else {
                        path.AppendVertex(pathGraph.GetVertex(vid));
                    }

                    if (boundaries.Contains(vid)) {
                        boundaries.Remove(vid);
                        break;
                    }
                }

                // discard paths that are too short
                if (path.ArcLength < MinPathLengthMM)
                    continue;


                // run polyline simplification to get rid of unneccesary detail in connectors
                // [TODO] we could do this at graph level...)
                // [TODO] maybe should be checkign for collisions? we could end up creating
                //  non-trivial overlaps here...
                if ( SimplifyAmount != SimplificationLevel.None && path.VertexCount > 2 ) {
                    PolySimplification2 simp = new PolySimplification2(path);
                    switch (SimplifyAmount) {
                        default:
                        case SimplificationLevel.Minor:
                            simp.SimplifyDeviationThreshold = ToolWidth / 4; break;
                        case SimplificationLevel.Aggressive:
                            simp.SimplifyDeviationThreshold = ToolWidth; break;
                        case SimplificationLevel.Moderate:
                            simp.SimplifyDeviationThreshold = ToolWidth / 2; break;
                    }
                    simp.Simplify();
                    path = new FillPolyline2d(simp.Result.ToArray()) { TypeFlags = this.TypeFlags };
                }

                paths.Append(path);
            }

            return paths;
        }










        /// <summary>
        /// Assumption is that input graph is a polygon with inserted ray-spans. We want to
        /// find a set of paths (ie no junctions) that cover all the spans, and travel between
        /// adjacent spans along edges of the input polygon. 
        /// </summary>
        protected DGraph2 BuildPathGraph(DGraph2 input)
        {
            int NV = input.MaxVertexID;

            /*
             * OK, as input we have a graph of our original polygon and a bunch of inserted
             * segments ("spans"). Orig polygon segments have gid < 0, and span segments >= 0.
             * However between polygon/span junctions, we have an arbitrary # of polygon edges.
             * So first step is to simplify these to single-edge "connectors", in new graph MinGraph.
             * the [connector-edge, path] mappings (if pathlen > 1) are stored in MinEdgePaths
             * We also store a weight for each connector edge in EdgeWeights (just distance for now)
             */

            DGraph2 MinGraph = new DGraph2();
            Dictionary<int, List<int>> MinEdgePaths = new Dictionary<int, List<int>>();
            DVector<double> EdgeWeights = new DVector<double>(); EdgeWeights.resize(NV);
            BitArray done_edge = new BitArray(input.MaxEdgeID);  // we should see each edge twice, this avoids repetition

            // vertex map from input graph to MinGraph
            int[] MapV = new int[NV];
            for (int i = 0; i < NV; ++i)
                MapV[i] = -1;

            for ( int a = 0; a < NV; ++a ) {
                if (input.IsVertex(a) == false || input.IsJunctionVertex(a) == false)
                    continue;

                if ( MapV[a] == -1 ) 
                    MapV[a] = MinGraph.AppendVertex(input.GetVertex(a));

                foreach ( int eid in input.VtxEdgesItr(a) ) {
                    if (done_edge[eid])
                        continue;

                    Index2i ev = input.GetEdgeV(eid);
                    int b = (ev.a == a) ? ev.b : ev.a;

                    if (input.IsJunctionVertex(b)) {
                        // if we have junction/juntion connection, we can just copy this edge to MinGraph

                        if (MapV[b] == -1)
                            MapV[b] = MinGraph.AppendVertex(input.GetVertex(b));

                        int gid = input.GetEdgeGroup(eid);
                        int existing = MinGraph.FindEdge(MapV[a], MapV[b]);
                        if ( existing == DMesh3.InvalidID ) {
                            int new_eid = MinGraph.AppendEdge(MapV[a], MapV[b], gid);
                            double path_len = input.GetEdgeSegment(eid).Length;
                            EdgeWeights.insertAt(path_len, new_eid);
                        } else {
                            // we may have inserted this edge already in the simplify branch, this happens eg at the
                            // edge of a circle where the minimal path is between the same vertices as the segment.
                            // But if this is also a fill edge, we want to treat it that way (determind via positive gid)
                            if (gid >= 0)
                                MinGraph.SetEdgeGroup(existing, gid);
                        }

                    } else {
                        // not a junction - walk until we find other vtx, and add single edge to MinGraph
                        List<int> path = DGraph2Util.WalkToNextNonRegularVtx(input, a, eid);
                        if (path == null || path.Count < 2)
                            throw new Exception("build_min_graph: invalid walk!");

                        int c = path[path.Count - 1];
                        if (MapV[c] == -1)
                            MapV[c] = MinGraph.AppendVertex(input.GetVertex(c));

                        if (MinGraph.FindEdge(MapV[a], MapV[c]) == DMesh3.InvalidID) {
                            int new_eid = MinGraph.AppendEdge(MapV[a], MapV[c], -2);
                            path.Add(MapV[a]); path.Add(MapV[c]);
                            MinEdgePaths[new_eid] = path;
                            double path_len = DGraph2Util.PathLength(input, path);
                            EdgeWeights.insertAt(path_len, new_eid);
                        }
                    }

                    done_edge[eid] = true;
                }
            }


            // [TODO] filter MinGraph to remove invalid connectors
            //    - can a connector between two connectors happen? that would be bad.
            ///   - connector that is too close to paths should be ignored (ie avoid collisions)


            /*
             * Now that we have MinGraph, we can easily walk between the spans because
             * they are connected by at most one edge. To find a sequence of spans, we
             * pick one to start, then walk along connectors, discarding as we go,
             * so that we don't pass through these vertices again. Repeat until
             * there are no remaining spans.
             */

            // [TODO]
            //  do we actually have to delete from MinGraph? this prevents us from doing
            //  certain things, like trying different options. Maybe could use a hash for
            //  remaining vertices and edges instead?

            DGraph2 PathGraph = new DGraph2();
            Vector2d sortAxis = Vector2d.FromAngleDeg(AngleDeg).Perp;

            while (true) {

                // find most extreme edge to start at
                // [TODO] could use segment gid here as we set them based on insertion span!
                // [TODO] could use a smarter metric? like, closest to previous last endpoint? Using
                //   extrema like this tends to produce longest spans, though...
                double min_dot = double.MaxValue;
                int start_eid = -1;
                foreach (int eid in MinGraph.EdgeIndices()) {
                    Index3i evg = MinGraph.GetEdge(eid);
                    if (evg.c >= 0) {
                        double dot = MinGraph.GetVertex(evg.a).Dot(sortAxis);
                        if (dot < min_dot) {
                            min_dot = dot;
                            start_eid = eid;
                        }
                    }
                }
                if (start_eid == -1)
                    break;   // if we could not find a start edge, we must be done!

                // ok now walk forward through connectors and spans. We do this in 
                // connector/span pairs - we are always at an end-of-span point, and
                // we pick a next-connector and then a next-span. 
                // We need to keep track of vertices in both the pathgraph and mingraph,
                // these are the "new" and "old" vertices
                Index3i start_evg = MinGraph.GetEdge(start_eid);
                int new_start = PathGraph.AppendVertex(MinGraph.GetVertex(start_evg.a));
                int new_prev = PathGraph.AppendVertex(MinGraph.GetVertex(start_evg.b));
                int old_prev = start_evg.b;
                PathGraph.AppendEdge(new_start, new_prev, start_evg.c);
                MinGraph.RemoveVertex(start_evg.a, true);
                while (true) {

                    // choose next connector edge, outgoing from current vtx
                    int connector_e = -1;
                    foreach (int eid in MinGraph.VtxEdgesItr(old_prev)) {
                        Index3i evg = MinGraph.GetEdge(eid);
                        if (evg.c >= 0)
                            continue;  // what?? 
                        if (connector_e == -1 || EdgeWeights[connector_e] > EdgeWeights[eid])
                            connector_e = eid;
                    }
                    if (connector_e == -1)
                        break;

                    // find the vertex at end of connector
                    Index3i conn_evg = MinGraph.GetEdge(connector_e);
                    int old_conn_v = (conn_evg.a == old_prev) ? conn_evg.b : conn_evg.a;

                    // can never look at prev vertex again, or any edges connected to it
                    // [TODO] are we sure none of these edges are unused spans?!?
                    MinGraph.RemoveVertex(old_prev, true);

                    // now find outgoing span edge
                    int span_e = -1;
                    foreach (int eid in MinGraph.VtxEdgesItr(old_conn_v)) {
                        Index3i evg = MinGraph.GetEdge(eid);
                        if (evg.c >= 0) {
                            span_e = eid;
                            break;
                        }
                    }
                    if (span_e == -1)
                        break;   // disaster!

                    // find vertex at far end of span
                    Index3i span_evg = MinGraph.GetEdge(span_e);
                    int old_span_v = (span_evg.a == old_conn_v) ? span_evg.b : span_evg.a;

                    // ok we want to insert the connectr to the path graph, however the
                    // connector might actually have come from a more complex path in the input graph.
                    int new_conn_next = -1;
                    if (MinEdgePaths.ContainsKey(connector_e)) {
                        // complex path case. Note that the order [old_prev, old_conn_v] may be the opposite
                        // of the order in the pathv. But above, we appended the [a,b] edge order to the pathv.
                        // So we can check if we need to flip, but this means we need to be a bit clever w/ indices...
                        List<int> pathv = MinEdgePaths[connector_e];
                        int N = pathv.Count;
                        int path_prev = new_prev;
                        int k = 1;
                        if (pathv[N - 2] != old_prev) {   // case where order flipped
                            pathv.Reverse();
                            k = 3;
                        } else {
                            N = N - 2;
                        }
                        while ( k < N ) { 
                            int path_next = PathGraph.AppendVertex(input.GetVertex(pathv[k]));
                            PathGraph.AppendEdge(path_prev, path_next);
                            path_prev = path_next;
                            k++;
                        }
                        new_conn_next = path_prev;

                    } else {
                        new_conn_next = PathGraph.AppendVertex(MinGraph.GetVertex(old_conn_v));
                        PathGraph.AppendEdge(new_prev, new_conn_next, conn_evg.c);
                    }

                    // add span to path
                    int new_fill_next = PathGraph.AppendVertex(MinGraph.GetVertex(old_span_v));
                    PathGraph.AppendEdge(new_conn_next, new_fill_next, span_evg.c);

                    // remove the connector vertex
                    MinGraph.RemoveVertex(old_conn_v, true);

                    // next iter starts at far end of span
                    new_prev = new_fill_next;
                    old_prev = old_span_v;
                }

                sortAxis = -sortAxis;
            }


            // for testing/debugging
            //SVGWriter writer = new SVGWriter();
            ////writer.AddGraph(input, SVGWriter.Style.Outline("blue", 0.1f));
            //writer.AddGraph(MinGraph, SVGWriter.Style.Outline("red", 0.1f));
            ////foreach ( int eid in MinGraph.EdgeIndices() ) {
            ////    if ( MinGraph.GetEdgeGroup(eid) >= 0 )  writer.AddLine(MinGraph.GetEdgeSegment(eid), SVGWriter.Style.Outline("green", 0.07f));
            ////}
            ////writer.AddGraph(MinGraph, SVGWriter.Style.Outline("black", 0.03f));
            //writer.AddGraph(PathGraph, SVGWriter.Style.Outline("black", 0.03f));
            //foreach (int vid in PathGraph.VertexIndices()) {
            //    if (PathGraph.IsBoundaryVertex(vid))
            //        writer.AddCircle(new Circle2d(PathGraph.GetVertex(vid), 0.5f), SVGWriter.Style.Outline("blue", 0.03f));
            //}
            ////writer.AddGraph(IntervalGraph, SVGWriter.Style.Outline("black", 0.03f));
            //writer.Write("c:\\scratch\\MIN_GRAPH.svg");


            return PathGraph;
        }




        /// <summary>
        /// shoot parallel set of 2D rays at input polygon, and find portions 
        /// of rays that are inside the polygon (we call these "spans"). These
        /// are inserted into the polygon, resulting in a non-manifold 2D graph.
        /// </summary>
        protected DGraph2 ComputeSpanGraph(GeneralPolygon2d poly)
        {
            double angleRad = AngleDeg * MathUtil.Deg2Rad;
            Vector2d dir = new Vector2d(Math.Cos(angleRad), Math.Sin(angleRad));

            // compute projection span along axis
            Vector2d axis = dir.Perp;
            Interval1d axisInterval = Interval1d.Empty;
            Interval1d dirInterval = Interval1d.Empty;
            foreach (Vector2d v in poly.Outer.Vertices) {
                dirInterval.Contain(v.Dot(dir));
                axisInterval.Contain(v.Dot(axis));
            }
            // [TODO] also check holes? or assume they are contained? should be
            //  classified as outside by winding check anyway...

            // construct interval we will step along to shoot parallel rays
            dirInterval.a -= 10 * ToolWidth;
            dirInterval.b += 10 * ToolWidth;
            double extent = dirInterval.Length;

			// nudge in a very tiny amount so that if poly is a rectangle, first
			// line is not directly on boundary
            axisInterval.a += ToolWidth * 0.01;
            axisInterval.b -= ToolWidth * 0.01;
			axisInterval.a -= PathShift;
            if (axisInterval.b < axisInterval.a)
                return null;     // [RMS] is this right? I guess so. interval is too small to fill?

            // If we are doing a dense fill, we want to pack as tightly as possible. 
            // But if we are doing a sparse fill, then we want layers to stack.
            // So in that case, snap the interval to increments of the spacing
            //  (does this work?)
            bool bIsSparse = (PathSpacing > ToolWidth * 2);
            if ( bIsSparse ) {
                // snap axisInterval.a to grid so that layers are aligned
                double snapped_a = Snapping.SnapToIncrement(axisInterval.a, PathSpacing);
                if (snapped_a > axisInterval.a)
                    snapped_a -= PathSpacing;
                axisInterval.a = snapped_a;
            }

            Vector2d startCorner = axisInterval.a * axis + dirInterval.a * dir;
            double range = axisInterval.Length;
            int N = (int)(range / PathSpacing) + 1;

			// nudge spacing so that we exactly fill the available space
			double use_spacing = PathSpacing;
			if ( bIsSparse == false && AdjustSpacingToMaximizeFill ) {
				int nn = (int)(range / use_spacing);
				use_spacing = range / (double)nn;
				N = (int)(range / use_spacing) + 1;
			}

            DGraph2 graph = new DGraph2();
            graph.AppendPolygon(poly);
            GraphSplitter2d splitter = new GraphSplitter2d(graph);
            splitter.InsideTestF = poly.Contains;

            // insert sequential rays
            for (int ti = 0; ti <= N; ++ti) {
				Vector2d o = startCorner + (double)ti * use_spacing * axis;
                Line2d ray = new Line2d(o, dir);
                splitter.InsertLine(ray, ti);
            }

            return graph;
        }




    }
}
