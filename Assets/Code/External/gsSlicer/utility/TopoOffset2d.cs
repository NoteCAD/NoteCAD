using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace gs
{
    /// <summary>
    /// Compute offset curves+skeleton from an input polygon-with-holes.
    /// 
    /// This uses an el Topo-style incremental front-tracking strategy.
    /// Possibly not the most efficient but it makes it easy to hack (eg constraints/etc)
    /// </summary>
    public class TopoOffset2d
    {
        // inputs
        public GeneralPolygon2d Polygon;
        public double Offset = 0.1f;
        public double PointSpacing = 1.0f;

        // options
        public bool DisconnectGraphJunctions = false;   // if true, output graph has no junctions

        // outputs
        public DGraph2 Graph;



        bool _enable_profiling = false;


        public TopoOffset2d(GeneralPolygon2d poly, double fOffset, double fPointSpacing, bool bAutoCompute = true )
        {
            Polygon = poly;
            Offset = fOffset;
            PointSpacing = fPointSpacing;
            if (bAutoCompute)
                Compute();
        }

        public TopoOffset2d(GeneralPolygon2d poly)
        {
            Polygon = poly;
        }

        public DGraph2 Compute()
        {
            reset_caches();
            Graph = compute_result(Polygon, Offset, PointSpacing);
            return Graph;
        }


        public static DGraph2 QuickCompute(GeneralPolygon2d poly, double fOffset, double fPointSpacing)
        {
            TopoOffset2d o = new TopoOffset2d(poly, fOffset, fPointSpacing);
            return o.Graph;
        }



        GeneralPolygon2dBoxTree poly_tree;      // this is constant between runs...

        DVector<Vector2d> offset_cache;
        DVector<Vector2d> position_cache;
        DVector<Vector2d> collapse_cache;
        DVector<double> last_step_size;

        PointHashGrid2d<int> graph_cache;

        protected void reset_caches()
        {
            //offset_cache = null;
            //position_cache = null;
            //collapse_cache = null;
            //graph_cache = null;
            //last_step_size = null;
        }


        protected DGraph2 compute_result(GeneralPolygon2d poly, double fOffset, double fTargetSpacing)
        {
            double dt = fTargetSpacing / 2;
            int nSteps = (int)(Math.Abs(fOffset) / dt);
            if (nSteps < 10)
                nSteps = 10;

            // [TODO] we could cache this over multiple runs...
            DGraph2 graph = new DGraph2();
            graph.AppendPolygon(poly.Outer);
            foreach (var h in poly.Holes)
                graph.AppendPolygon(h);

            // resample to nbrhood of target spacing
            SplitToMaxEdgeLength(graph, fTargetSpacing * 1.33);

            // build bvtree for polygon
            if (poly_tree == null || poly_tree.Polygon != poly)
                poly_tree = new GeneralPolygon2dBoxTree(poly);

            // allocate and resize caches as necessary
            if (offset_cache == null)                       offset_cache = new DVector<Vector2d>();
            if ( offset_cache.size < graph.VertexCount )    offset_cache.resize(graph.VertexCount * 2);
            if (position_cache == null)                     position_cache = new DVector<Vector2d>();
            if (position_cache.size < graph.VertexCount)    position_cache.resize(graph.VertexCount * 2);
            if ( collapse_cache == null )                   collapse_cache = new DVector<Vector2d>();
            if (collapse_cache.size < graph.VertexCount)    collapse_cache.resize(graph.VertexCount * 2);
            if (last_step_size == null)                     last_step_size = new DVector<double>();
            if (last_step_size.size < graph.VertexCount)    last_step_size.resize(graph.VertexCount * 2);


            // insert all points into a hashgrid. We will dynamically update this grid as we proceed
            // [TODO] is this a good bounds-size?
            graph_cache = new PointHashGrid2d<int>(poly.Bounds.MaxDim / 64, -1);
            foreach (int vid in graph.VertexIndices())
                graph_cache.InsertPoint(vid, graph.GetVertex(vid));

            LocalProfiler p = (_enable_profiling) ? new LocalProfiler() : null;
            if (_enable_profiling) { p.Start("All"); }


            // run a bunch of steps. The last few are tuning steps where we use half-steps, 
            // which seems to help?
            int TUNE_STEPS = nSteps / 2;
            nSteps *= 2;
            for (int i = 0; i < nSteps; ++i) {

                if (_enable_profiling) { p.Start("offset"); }

                double step_dt = dt;
                if (i > nSteps - TUNE_STEPS)
                    step_dt = dt / 2;
                if (last_step_size.size < graph.VertexCount)
                    last_step_size.resize(graph.VertexCount+256);

                // Each vertex steps forward. In fact we compute two steps and average them, 
                // this helps w/ convergence. To produce more accurate convergence, we track
                // the size of the actual step we took at the last round, and use that the next
                // time. (The assumption is that the steps will get smaller at the target distance).
                gParallel.ForEach(graph.VertexIndices(), (vid) => {

                    // use tracked step size if we have it
                    double use_dt = step_dt;
                    if (last_step_size[vid] > 0)
                        use_dt = Math.Min(last_step_size[vid], dt);

                    Vector2d cur_pos = graph.GetVertex(vid);
                    double err, err_2;
                    // take two sequential steps and average them. this vastly improves convergence.
                    Vector2d new_pos = compute_offset_step(cur_pos, poly, fOffset, use_dt, out err);
                    Vector2d new_pos_2 = compute_offset_step(new_pos, poly, fOffset, use_dt, out err_2);

                    // weighted blend of points - prefer one w/ smaller error
                    //double w = 1.0 / Math.Max(err, MathUtil.ZeroTolerancef);
                    //double w_2 = 1.0 / Math.Max(err_2, MathUtil.ZeroTolerancef);
                    //new_pos = w * new_pos + w_2 * new_pos_2;
                    //new_pos /= (w + w_2);
                    // [RMS] weighted blend doesn't seem to matter if we are tracking per-vertex step size.
                    new_pos = Vector2d.Lerp(new_pos, new_pos_2, 0.5);

                    // keep track of actual step we are taking and use that next iteration
                    double actual_step_dist = cur_pos.Distance(new_pos);
                    if (last_step_size[vid] == 0)
                        last_step_size[vid] = actual_step_dist;
                    else
                        last_step_size[vid] = (0.75) * last_step_size[vid] + (0.25) * actual_step_dist;

                    // update point in hashtable and graph
                    graph_cache.UpdatePoint(vid, cur_pos, new_pos);
                    graph.SetVertex(vid, new_pos);
                });


                if (_enable_profiling) { p.StopAndAccumulate("offset"); p.Start("smooth"); }

                // Do a smoothing pass, but for the last few steps, reduce smoothing
                // (otherwise it pulls away from target solution)
                int smooth_steps = 5; double smooth_alpha = 0.75;
                if (i > nSteps - TUNE_STEPS) {
                    smooth_steps = 2; smooth_alpha = 0.25;
                }
                smooth_pass(graph, smooth_steps, smooth_alpha, fTargetSpacing / 2);

                if (_enable_profiling) { p.StopAndAccumulate("smooth"); p.Start("join"); }

                // if a vertex is within targetSpacing from another vertex, and they are
                // not geodesically connected in the graph, them we merge/weld them together.
                int joined = 0;
                do {
                    //joined = JoinInTolerance(graph, fMergeThresh);
                    //joined = JoinInTolerance_Parallel(graph, fMergeThresh);
                    joined = JoinInTolerance_Parallel_Cache(graph, fTargetSpacing);
                } while (joined > 0);

                if (_enable_profiling) { p.StopAndAccumulate("join"); p.Start("refine"); }

                // now do a pass of graph refinement, to collapse short edges and split long ones
                CollapseToMinEdgeLength(graph, fTargetSpacing * 0.66f);
                SplitToMaxEdgeLength(graph, fTargetSpacing * 1.33);

                if (_enable_profiling) { p.StopAndAccumulate("refine"); }
            }

            if (_enable_profiling) { p.Stop("All"); 
                System.Console.WriteLine("All: " + p.Elapsed("All"));
                System.Console.WriteLine(p.AllAccumulatedTimes()); }


            // get rid of junction vertices, if requested
            if (DisconnectGraphJunctions) {
                DGraph2Util.DisconnectJunctions(graph);
            }

            return graph;
        }


        
        // Compute step-forward at cur_pos. We find the closest point on the poly,
        // and step away from that, unless we go to far, then we step back.
        protected Vector2d compute_offset_step(Vector2d cur_pos, GeneralPolygon2d poly, double fTargetOffset, double stepSize, out double err)
        {
            int iHole, iSeg; double segT;
            double distSqr =
                poly_tree.DistanceSquared(cur_pos, out iHole, out iSeg, out segT);

            double dist = Math.Sqrt(distSqr);
            Vector2d normal = poly.GetNormal(iSeg, segT, iHole);

            // flip for negative offsets
            if (fTargetOffset < 0) {
                fTargetOffset = -fTargetOffset;
                normal = -normal;
            }

            double step = stepSize;
            if (dist > fTargetOffset) {
                step = Math.Max(fTargetOffset - dist, -step);
            } else {
                step = Math.Min(fTargetOffset - dist, step);
            }
            err = Math.Abs(fTargetOffset - dist);

            Vector2d new_pos = cur_pos - step * normal;
            return new_pos;
        }



        // smooth vertices, but don't move further than max_move
        protected void smooth_pass(DGraph2 graph, int passes, double smooth_alpha, double max_move)
        {
            double max_move_sqr = max_move * max_move;
            int NV = graph.MaxVertexID;
            DVector<Vector2d> smoothedV = offset_cache;
            if (smoothedV.size < NV)
                smoothedV.resize(NV);

            if (position_cache.size < NV)
                position_cache.resize(NV);

            for (int pi = 0; pi < passes; ++pi) {

                gParallel.ForEach(Interval1i.Range(NV), (vid) => {
                    if (!graph.IsVertex(vid))
                        return;
                    Vector2d v = graph.GetVertex(vid);
                    Vector2d c = Vector2d.Zero;
                    int n = 0;
                    foreach (int vnbr in graph.VtxVerticesItr(vid)) {
                        c += graph.GetVertex(vnbr);
                        n++;
                    }
                    if (n >= 2) {
                        c /= n;
                        Vector2d dv = (smooth_alpha) * (c - v);
                        if (dv.LengthSquared > max_move_sqr) {
                            /*double d = */dv.Normalize();
                            dv *= max_move;
                        }
                        v += dv;
                    }
                    smoothedV[vid] = v;
                });


                if (pi == 0) {
                    for (int vid = 0; vid < NV; ++vid) {
                        if (graph.IsVertex(vid)) {
                            position_cache[vid] = graph.GetVertex(vid);
                            graph.SetVertex(vid, smoothedV[vid]);
                        }
                    }
                } else {
                    for (int vid = 0; vid < NV; ++vid) {
                        if (graph.IsVertex(vid))
                            graph.SetVertex(vid, smoothedV[vid]);
                    }
                }
            }

            for (int vid = 0; vid < NV; ++vid) {
                if (graph.IsVertex(vid))
                    graph_cache.UpdatePointUnsafe(vid, position_cache[vid], smoothedV[vid]);
            }


        }



        // join disconnected vertices within distance threshold
        protected int JoinInTolerance_Parallel(DGraph2 graph, double fMergeDist)
        {
            double mergeSqr = fMergeDist * fMergeDist;

            int NV = graph.MaxVertexID;
            if (collapse_cache.size < NV)
                collapse_cache.resize(NV);

            gParallel.ForEach(Interval1i.Range(NV), (a) => {
                collapse_cache[a] = new Vector2d(-1, double.MaxValue);
                if (!graph.IsVertex(a))
                    return;

                Vector2d va = graph.GetVertex(a);

                int bNearest = -1;
                double nearDistSqr = double.MaxValue;
                for (int b = a + 1; b < NV; ++b) {
                    if (b == a || graph.IsVertex(b) == false)
                        continue;
                    double distsqr = va.DistanceSquared(graph.GetVertex(b));
                    if (distsqr < mergeSqr && distsqr < nearDistSqr) {
                        if (graph.FindEdge(a, b) == DGraph2.InvalidID) {
                            nearDistSqr = distsqr;
                            bNearest = b;
                        }
                    }
                }
                if (bNearest != -1)
                    collapse_cache[a] = new Vector2d(bNearest, nearDistSqr);
            });

            // [TODO] sort

            int merged = 0;
            for (int a = 0; a < NV; ++a) {
                if (collapse_cache[a].x == -1)
                    continue;

                int bNearest = (int)collapse_cache[a].x;

                Vector2d pos_a = graph.GetVertex(a);
                Vector2d pos_bNearest = graph.GetVertex(bNearest);

                /*int eid = */graph.AppendEdge(a, bNearest);
                DGraph2.EdgeCollapseInfo collapseInfo;
                graph.CollapseEdge(bNearest, a, out collapseInfo);
                graph_cache.RemovePointUnsafe(a, pos_a);
                last_step_size[a] = 0;
                graph_cache.UpdatePointUnsafe(bNearest, pos_bNearest, graph.GetVertex(bNearest));
                merged++;
            }
            return merged;
        }




        // join disconnected vertices within distance threshold. Use point-hashtable to make this faster.
        protected int JoinInTolerance_Parallel_Cache(DGraph2 graph, double fMergeDist)
        {
            double mergeSqr = fMergeDist * fMergeDist;

            int NV = graph.MaxVertexID;
            if (collapse_cache.size < NV)
                collapse_cache.resize(NV);

            gParallel.ForEach(Interval1i.Range(NV), (a) => {
                collapse_cache[a] = new Vector2d(-1, double.MaxValue);
                if (!graph.IsVertex(a))
                    return;

                Vector2d va = graph.GetVertex(a);

                KeyValuePair<int, double> found =
                    graph_cache.FindNearestInRadius(va, mergeSqr,
                        (b) => { return va.DistanceSquared(graph.GetVertex(b)); },
                        (b) => { return b <= a || (graph.FindEdge(a, b) != DGraph2.InvalidID); });

                if (found.Key != -1) {
                    collapse_cache[a] = new Vector2d(found.Key, found.Value);
                }
            });

            // [TODO] sort

            int merged = 0;
            for (int a = 0; a < NV; ++a) {
                if (collapse_cache[a].x == -1)
                    continue;

                int bNearest = (int)collapse_cache[a].x;
                if (!graph.IsVertex(bNearest))
                    continue;

                Vector2d pos_a = graph.GetVertex(a);
                Vector2d pos_bNearest = graph.GetVertex(bNearest);

                /*int eid = */graph.AppendEdge(a, bNearest);
                DGraph2.EdgeCollapseInfo collapseInfo;
                graph.CollapseEdge(bNearest, a, out collapseInfo);

                graph_cache.RemovePointUnsafe(a, pos_a);
                last_step_size[a] = 0;
                graph_cache.UpdatePointUnsafe(bNearest, pos_bNearest, graph.GetVertex(bNearest));
                collapse_cache[bNearest] = new Vector2d(-1, double.MaxValue);

                merged++;
            }
            return merged;
        }



        // collapse edges shorter than fMinLen
        // NOTE: basically the same as DGraph2Resampler.CollapseToMinEdgeLength, but updates
        // our internal caches. Could we merge somehow?
        protected void CollapseToMinEdgeLength(DGraph2 graph, double fMinLen)
        {
            double sharp_threshold_deg = 140.0f;

            double minLenSqr = fMinLen * fMinLen;
            bool done = false;
            int max_passes = 100;
            int pass_count = 0;
            while (done == false && pass_count++ < max_passes) {
                done = true;

                // [RMS] do modulo-indexing here to avoid pathological cases where we do things like
                // continually collapse a short edge adjacent to a long edge (which will result in crazy over-collapse)
                int N = graph.MaxEdgeID;
                const int nPrime = 31337;     // any prime will do...
                int cur_eid = 0;
                do {
                    int eid = cur_eid;
                    cur_eid = (cur_eid + nPrime) % N;

                    if (!graph.IsEdge(eid))
                        continue;
                    Index2i ev = graph.GetEdgeV(eid);

                    Vector2d va = graph.GetVertex(ev.a);
                    Vector2d vb = graph.GetVertex(ev.b);
                    double distSqr = va.DistanceSquared(vb);
                    if (distSqr < minLenSqr) {

                        int vtx_idx = -1;    // collapse to this vertex

                        // check valences. want to preserve positions of non-valence-2
                        int na = graph.GetVtxEdgeCount(ev.a);
                        int nb = graph.GetVtxEdgeCount(ev.b);
                        if (na != 2 && nb != 2)
                            continue;
                        if (na != 2)
                            vtx_idx = 0;
                        else if (nb != 2)
                            vtx_idx = 1;

                        // check opening angles. want to preserve sharp(er) angles
                        if (vtx_idx == -1) {
                            double opena = Math.Abs(graph.OpeningAngle(ev.a));
                            double openb = Math.Abs(graph.OpeningAngle(ev.b));
                            if (opena < sharp_threshold_deg && openb < sharp_threshold_deg)
                                continue;
                            else if (opena < sharp_threshold_deg)
                                vtx_idx = 0;
                            else if (openb < sharp_threshold_deg)
                                vtx_idx = 1;
                        }

                        Vector2d newPos = (vtx_idx == -1) ? 0.5 * (va + vb) : ((vtx_idx == 0) ? va : vb);

                        int keep = ev.a, remove = ev.b;
                        if (vtx_idx == 1) {
                            remove = ev.a; keep = ev.b;
                        }

                        Vector2d remove_pos = graph.GetVertex(remove);
                        Vector2d keep_pos = graph.GetVertex(keep);

                        DGraph2.EdgeCollapseInfo collapseInfo;
                        if (graph.CollapseEdge(keep, remove, out collapseInfo) == MeshResult.Ok) {
                            graph_cache.RemovePointUnsafe(collapseInfo.vRemoved, remove_pos);
                            last_step_size[collapseInfo.vRemoved] = 0;
                            graph_cache.UpdatePointUnsafe(collapseInfo.vKept, keep_pos, newPos);
                            graph.SetVertex(collapseInfo.vKept, newPos);
                            done = false;
                        }
                    }

                } while (cur_eid != 0);
            }
        }






        // split edges longer than fMinLen
        // NOTE: basically the same as DGraph2Resampler.SplitToMaxEdgeLength, but updates
        // our internal caches. Could we merge somehow?
        protected void SplitToMaxEdgeLength(DGraph2 graph, double fMaxLen)
        {
            List<int> queue = new List<int>();
            int NE = graph.MaxEdgeID;
            for (int eid = 0; eid < NE; ++eid) {
                if (!graph.IsEdge(eid))
                    continue;
                Index2i ev = graph.GetEdgeV(eid);
                double dist = graph.GetVertex(ev.a).Distance(graph.GetVertex(ev.b));
                if (dist > fMaxLen) {
                    DGraph2.EdgeSplitInfo splitInfo;
                    if (graph.SplitEdge(eid, out splitInfo) == MeshResult.Ok) {
                        if (graph_cache != null)
                            graph_cache.InsertPointUnsafe(splitInfo.vNew, graph.GetVertex(splitInfo.vNew));
                        if (dist > 2 * fMaxLen) {
                            queue.Add(eid);
                            queue.Add(splitInfo.eNewBN);
                        }
                    }
                }
            }
            while (queue.Count > 0) {
                int eid = queue[queue.Count - 1];
                queue.RemoveAt(queue.Count - 1);
                if (!graph.IsEdge(eid))
                    continue;
                Index2i ev = graph.GetEdgeV(eid);
                double dist = graph.GetVertex(ev.a).Distance(graph.GetVertex(ev.b));
                if (dist > fMaxLen) {
                    DGraph2.EdgeSplitInfo splitInfo;
                    if (graph.SplitEdge(eid, out splitInfo) == MeshResult.Ok) {
                        if (graph_cache != null)
                            graph_cache.InsertPointUnsafe(splitInfo.vNew, graph.GetVertex(splitInfo.vNew));
                        if (dist > 2 * fMaxLen) {
                            queue.Add(eid);
                            queue.Add(splitInfo.eNewBN);
                        }
                    }
                }
            }
        }






    }
}
