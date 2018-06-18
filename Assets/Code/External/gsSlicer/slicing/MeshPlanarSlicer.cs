using System;
using System.Collections.Generic;
using System.Threading;
using g3;
using UnityEngine;
using System.Collections;

namespace gs
{
    /// <summary>
    /// Computes a PlanarSliceStack from a set of input meshes, by horizonally
    /// slicing them at regular Z-intervals. This is where we need to sort out
    /// any complications like overlapping shells, etc. Much of that work is
    /// done in PlanarSlice.resolve().
    /// 
    /// The input meshes are not modified in this process.
    /// </summary>
	public class MeshPlanarSlicer
	{
        class SliceMesh
        {
            public DMesh3 mesh;
            public AxisAlignedBox3d bounds;

            public PrintMeshOptions options;
        }
        List<SliceMesh> Meshes = new List<SliceMesh>();


        // factory functions you can replace to customize objects/behavior
        public Func<PlanarSliceStack> SliceStackFactoryF = () => { return new PlanarSliceStack(); };
        public Func<double, int, PlanarSlice> SliceFactoryF = (ZHeight, idx) => {
            return new PlanarSlice() { Z = ZHeight, LayerIndex = idx };
        };


        /// <summary>
        /// Slice height
        /// </summary>
		public double LayerHeightMM = 0.2;

        /// <summary>
        /// Open-sheet meshes slice into open paths. For OpenPathsModes.Embedded mode, we need
        /// to subtract thickened path from the solids. This is the path thickness.
        /// </summary>
        public double OpenPathDefaultWidthMM = 0.4;


		/// <summary>
		/// Support "tips" (ie z-minima vertices) can be detected geometrically and
		/// added to PlanarSlice.InputSupportPoints. 
		/// </summary>
		public bool SupportMinZTips = false;

		/// <summary>
		/// What is the largest floating polygon we will consider a "tip"
		/// </summary>
		public double MinZTipMaxDiam = 2.0;

		/// <summary>
		/// Often desirable to support a Z-minima tip several layers "up" around it.
		/// This is how many layers.
		/// </summary>
		public int MinZTipExtraLayers = 6;


        /// <summary>
        /// Normally we slice in interval [zmin,zmax]. Set this to 0 if you
        /// want to slice [0,zmax].
        /// </summary>
        public double SetMinZValue = double.MinValue;

        /// <summary>
        /// If true, then any empty slices at bottom of stack are discarded.
        /// </summary>
        public bool DiscardEmptyBaseSlices = false;


		public enum SliceLocations {
			Base, EpsilonBase, MidLine
		}

        /// <summary>
        /// Where in layer should we compute slice
        /// </summary>
		public SliceLocations SliceLocation = SliceLocations.MidLine;

        /// <summary>
        /// How should open paths be handled. Is overriden by
        /// PrintMeshOptions.OpenPathsModes for specific meshes
        /// </summary>
        public PrintMeshOptions.OpenPathsModes DefaultOpenPathMode = PrintMeshOptions.OpenPathsModes.Clipped;


        public int MaxLayerCount = 10000;		// just for sanity-check


        // these can be used for progress tracking
        public int TotalCompute = 0;
        public int Progress = 0;


		public MeshPlanarSlicer()
		{
		}


        public int AddMesh(DMesh3 mesh, PrintMeshOptions options) {
            SliceMesh m = new SliceMesh() {
                mesh = mesh,
                bounds = mesh.CachedBounds,
                options = options
            };
            int idx = Meshes.Count;
            Meshes.Add(m);
            return idx;
		}
        public int AddMesh(DMesh3 mesh) {
            return AddMesh(mesh, PrintMeshOptions.Default());
        }


        public bool Add(PrintMeshAssembly assy)
        {
            foreach ( var pair in assy.MeshesAndOptions()) 
                AddMesh(pair.Item1, pair.Item2);
            return true;
        }



        /// <summary>
        /// Slice the meshes and return the slice stack. 
        /// </summary>
		public PlanarSliceStack Result;

		public IEnumerable<Progress> Compute()
		{
			Result = new PlanarSliceStack();
            if (Meshes.Count == 0) yield break;
                
			Interval1d zrange = Interval1d.Empty;
			foreach ( var meshinfo in Meshes ) {
				zrange.Contain(meshinfo.bounds.Min.z);
				zrange.Contain(meshinfo.bounds.Max.z);
			}
            if (SetMinZValue != double.MinValue)
                zrange.a = SetMinZValue;

			int nLayers = (int)(zrange.Length / LayerHeightMM);
			if (nLayers > MaxLayerCount)
				throw new Exception("MeshPlanarSlicer.Compute: exceeded layer limit. Increase .MaxLayerCount.");

            // make list of slice heights (could be irregular)
            List<double> heights = new List<double>();
			for (int i = 0; i < nLayers + 1; ++i) {
				double t = zrange.a + (double)i * LayerHeightMM;
				if (SliceLocation == SliceLocations.EpsilonBase)
					t += 0.01 * LayerHeightMM;
				else if (SliceLocation == SliceLocations.MidLine)
					t += 0.5 * LayerHeightMM;
				heights.Add(t);
			}
			int NH = heights.Count;

			// process each *slice* in parallel
			PlanarSlice[] slices = new PlanarSlice[NH];
            for (int i = 0; i < NH; ++i) {
                slices[i] = SliceFactoryF(heights[i], i);
                slices[i].EmbeddedPathWidth = OpenPathDefaultWidthMM;
            }

            // assume Resolve() takes 2x as long as meshes...
            TotalCompute = (Meshes.Count * NH) +  (2*NH);
            Progress = 0;

            // compute slices separately for each mesh
            for (int mi = 0; mi < Meshes.Count; ++mi ) {
				DMesh3 mesh = Meshes[mi].mesh;
                PrintMeshOptions mesh_options = Meshes[mi].options;

                // [TODO] should we hang on to this spatial? or should it be part of assembly?
                DMeshAABBTree3 spatial = new DMeshAABBTree3(mesh, true);
				AxisAlignedBox3d bounds = Meshes[mi].bounds;

                bool is_cavity = mesh_options.IsCavity;
                bool is_support = mesh_options.IsSupport;
                bool is_closed = (mesh_options.IsOpen) ? false : mesh.IsClosed();
                var useOpenMode = (mesh_options.OpenPathMode == PrintMeshOptions.OpenPathsModes.Default) ?
                    DefaultOpenPathMode : mesh_options.OpenPathMode;

                // each layer is independent so we can do in parallel
				foreach(var i in Interval1i.Range(NH)) {
					if(i % 10 == 0) yield return new Progress("compute_slices", i, NH);
					double z = heights[i];
					if (z < bounds.Min.z || z > bounds.Max.z)
						continue;

                    // compute cut
                    Polygon2d[] polys; PolyLine2d[] paths;
                    compute_plane_curves(mesh, spatial, z, out polys, out paths);

                    // if we didn't hit anything, try again with jittered plane
                    // [TODO] this could be better...
                    if ( (is_closed && polys.Length == 0) || (is_closed == false &&  polys.Length == 0 && paths.Length == 0)) {
                        compute_plane_curves(mesh, spatial, z+LayerHeightMM*0.25, out polys, out paths);
                    }

                    if (is_closed) {
						// construct planar complex and "solids"
						// (ie outer polys and nested holes)
						PlanarComplex complex = new PlanarComplex();
						foreach (Polygon2d poly in polys)
							complex.Add(poly);

						PlanarComplex.FindSolidsOptions options
									 = PlanarComplex.FindSolidsOptions.Default;
						options.WantCurveSolids = false;
						options.SimplifyDeviationTolerance = 0.001;
						options.TrustOrientations = true;
						options.AllowOverlappingHoles = true;

						PlanarComplex.SolidRegionInfo solids =
							complex.FindSolidRegions(options);

                        if (is_support)
                            add_support_polygons(slices[i], solids.Polygons, mesh_options);
                        else if (is_cavity)
                            add_cavity_polygons(slices[i], solids.Polygons, mesh_options);
                        else
                            add_solid_polygons(slices[i], solids.Polygons, mesh_options);

                    } else if (useOpenMode != PrintMeshOptions.OpenPathsModes.Ignored) {

                        foreach (PolyLine2d pline in paths) {
                            if (useOpenMode == PrintMeshOptions.OpenPathsModes.Embedded )
                                slices[i].AddEmbeddedPath(pline);   
                            else
                                slices[i].AddClippedPath(pline);
                        }

                        // [TODO] 
                        //   - does not really handle clipped polygons properly, there will be an extra break somewhere...
                        foreach (Polygon2d poly in polys) {
                            PolyLine2d pline = new PolyLine2d(poly, true);
                            if (useOpenMode == PrintMeshOptions.OpenPathsModes.Embedded)
                                slices[i].AddEmbeddedPath(pline);
                            else
                                slices[i].AddClippedPath(pline);
                        }
                    }

					Interlocked.Increment(ref Progress);
				}
				              
			} // end mesh iter

            // resolve planar intersections, etc
            foreach(var i in Interval1i.Range(NH)) {
				//yield return new Progress("resolve", i, NH);
                slices[i].Resolve();
                Interlocked.Add(ref Progress, 2);
            }

            // discard spurious empty slices
            int last = slices.Length-1;
            while (slices[last].IsEmpty && last > 0)
                last--;
            int first = 0;
            if (DiscardEmptyBaseSlices) {
                while (slices[first].IsEmpty && first < slices.Length)
                    first++;
            }

            PlanarSliceStack stack = SliceStackFactoryF();
            for (int k = first; k <= last; ++k)
                stack.Add(slices[k]);

			if ( SupportMinZTips )
				stack.AddMinZTipSupportPoints(MinZTipMaxDiam, MinZTipExtraLayers);

			Result = stack;
		}



        protected virtual void add_support_polygons(PlanarSlice slice, List<GeneralPolygon2d> polygons, PrintMeshOptions options)
        {
            slice.AddSupportPolygons(polygons);
        }

        protected virtual void add_cavity_polygons(PlanarSlice slice, List<GeneralPolygon2d> polygons, PrintMeshOptions options)
        {
            slice.AddCavityPolygons(polygons);
        }

        protected virtual void add_solid_polygons(PlanarSlice slice, List<GeneralPolygon2d> polygons, PrintMeshOptions options)
        {
            slice.AddPolygons(polygons);
        }



        static bool compute_plane_curves(DMesh3 mesh, DMeshAABBTree3 spatial, double z, out Polygon2d[] loops, out PolyLine2d[] curves )
        {
            Func<Vector3d, double> planeF = (v) => {
                return v.z - z;
            };

            // find list of triangles that intersect this z-value
            PlaneIntersectionTraversal planeIntr = new PlaneIntersectionTraversal(mesh, z);
            spatial.DoTraversal(planeIntr);
            List<int> triangles = planeIntr.triangles;

            // compute intersection iso-curves, which produces a 3D graph of undirected edges
            MeshIsoCurves iso = new MeshIsoCurves(mesh, planeF) { WantGraphEdgeInfo = true };
            iso.Compute(triangles);
            DGraph3 graph = iso.Graph;
            if ( graph.EdgeCount == 0 ) {
                loops = new Polygon2d[0];
                curves = new PolyLine2d[0];
                return false;
            }

            // extract loops and open curves from graph
            DGraph3Util.Curves c = DGraph3Util.ExtractCurves(graph, false, iso.ShouldReverseGraphEdge);
            loops = new Polygon2d[c.Loops.Count];
            for (int li = 0; li < loops.Length; ++li) {
                DCurve3 loop = c.Loops[li];
                loops[li] = new Polygon2d();
                foreach (Vector3d v in loop.Vertices) 
                    loops[li].AppendVertex(v.xy);
            }

            curves = new PolyLine2d[c.Paths.Count];
            for (int pi = 0; pi < curves.Length; ++pi) {
                DCurve3 span = c.Paths[pi];
                curves[pi] = new PolyLine2d();
                foreach (Vector3d v in span.Vertices) 
                    curves[pi].AppendVertex(v.xy);
            }

            return true;
        }



        class PlaneIntersectionTraversal : DMeshAABBTree3.TreeTraversal
        {
            public DMesh3 Mesh;
            public double Z;
            public List<int> triangles = new List<int>();
            public PlaneIntersectionTraversal(DMesh3 mesh, double z)
            {
                this.Mesh = mesh;
                this.Z = z;
                this.NextBoxF = (box, depth) => {
                    return (Z >= box.Min.z && Z <= box.Max.z);
                };
                this.NextTriangleF = (tID) => {
                    AxisAlignedBox3d box = Mesh.GetTriBounds(tID);
                    if (Z >= box.Min.z && z <= box.Max.z)
                        triangles.Add(tID);
                };
            }
        }


	}
}
