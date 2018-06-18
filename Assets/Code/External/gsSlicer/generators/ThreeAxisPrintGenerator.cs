using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using g3;
using System.Collections;

namespace gs
{

	public class Progress {
		public string stage;
		public int current;
		public int total;

		public Progress(string s, int c, int t) {
			stage = s;
			current = c;
			total = t;
		}
	}
	
	/// <summary>
	/// PrintLayerData is set of information for a single print layer
	/// </summary>
	public class PrintLayerData
	{
		public int layer_i;
		public PlanarSlice Slice;
		public SingleMaterialFFFSettings Settings;

		public PrintLayerData PreviousLayer;

		public ToolpathSetBuilder PathAccum;
		public IFillPathScheduler2d Scheduler;

		public List<IShellsFillPolygon> ShellFills;
        public List<GeneralPolygon2d> SupportAreas;

		public TemporalPathHash Spatial;

		public PrintLayerData(int layer_i, PlanarSlice slice, SingleMaterialFFFSettings settings) {
			this.layer_i = layer_i;
			Slice = slice;
			Settings = settings;
			Spatial = new TemporalPathHash();
		}
	}




    /// <summary>
    /// This is the top-level class that generates a GCodeFile for a stack of slices.
    /// Currently must subclass to provide resulting GCodeFile.
    /// </summary>
    public abstract class ThreeAxisPrintGenerator
    {
        // Data structures that must be provided by client
        protected PrintMeshAssembly PrintMeshes;
        protected PlanarSliceStack Slices;
        protected ThreeAxisPrinterCompiler Compiler;
        public SingleMaterialFFFSettings Settings;      // public because you could modify
                                                        // this during process, ie in BeginLayerF
                                                        // to implement per-layer settings

        // available after calling Generate()
        public GCodeFile Result;

        // Generally we discard the paths at each layer as we generate them. If you 
        // would like to analyze, set this to true, and then AccumulatedPaths will
        // be available after calling Generate(). The list AccumulatedPaths.Paths will 
        // be a list with a separate PathSet for each layer, in bottom-up order.
        public bool AccumulatePathSet = false;
        public ToolpathSet AccumulatedPaths = null;

		/*
		 * Customizable functions you can use to configure/modify slicer behavior
		 */

        // replace this with your own error message handler
        public Action<string, string> ErrorF = (message, stack_trace) => {
            System.Console.WriteLine("[EXCEPTION] ThreeAxisPrintGenerator: " + message + "\nSTACK TRACE: " + stack_trace);
        };


		// Replace this if you want to customize PrintLayerData type
		public Func<int, PlanarSlice, SingleMaterialFFFSettings, PrintLayerData> PrintLayerDataFactoryF;

		// Replace this to use a different path builder
		public Func<PrintLayerData, ToolpathSetBuilder> PathBuilderFactoryF;

		// Replace this to use a different scheduler
		public Func<PrintLayerData, IFillPathScheduler2d> SchedulerFactoryF;

        // Replace this to use a different shell selector
        public Func<PrintLayerData, ILayerShellsSelector> ShellSelectorFactoryF;

        // This is called at the beginning of each layer, you can replace to
        // implement progress bar, etc
        public Action<PrintLayerData> BeginLayerF;

		// This is called before we process each shell. The Tag is transferred
		// from the associated region in the PlanarSlice, if it had one, otherwise it is int.MaxValue
		public Action<IFillPolygon, int> BeginShellF;

        // called at the end of each layer, before we compile the paths
        public ILayerPathsPostProcessor LayerPostProcessor;


        // this is called on polyline paths, return *true* to filter out a path. Useful for things like very short segments, etc
        // In default Initialize(), is set to a constant multiple of tool size
        public Func<FillPolyline2d, bool> PathFilterF = null;






        protected ThreeAxisPrintGenerator()
        {
        }

        public ThreeAxisPrintGenerator(PrintMeshAssembly meshes, 
                                       PlanarSliceStack slices,
                                       SingleMaterialFFFSettings settings,
                                       ThreeAxisPrinterCompiler compiler)
        {
            Initialize(meshes, slices, settings, compiler);
        }




        public void Initialize(PrintMeshAssembly meshes, 
                               PlanarSliceStack slices,
                               SingleMaterialFFFSettings settings,
                               ThreeAxisPrinterCompiler compiler)
        {
			
            PrintMeshes = meshes;
            Slices = slices;
            Settings = settings;
            Compiler = compiler;


			// set defaults for configurable functions

			PrintLayerDataFactoryF = (layer_i, slice, settingsArg) => {
				return new PrintLayerData(layer_i, slice, settingsArg);
			};

			PathBuilderFactoryF = (layer_data) => {
				return new ToolpathSetBuilder();
			};

			SchedulerFactoryF = get_layer_scheduler;

            ShellSelectorFactoryF = (layer_data) => {
                return new NextNearestLayerShellsSelector(layer_data.ShellFills);
            };

			BeginLayerF = (layer_data) => { };

			BeginShellF = (shell_fill, tag) => { };

            LayerPostProcessor = null;

            if (PathFilterF == null)
				PathFilterF = (pline) => { return pline.ArcLength < 3 * Settings.Machine.NozzleDiamMM; };

        }



        public virtual IEnumerable<Progress> Generate()
        {
            //try {
				foreach(var i in generate_result()) {
					yield return i;
				}
                Result = extract_result();
            //} catch ( Exception e ) {
                //ErrorF(e.Message, e.StackTrace);
                //return false;
            //}
            //return true;
        }


        public virtual void GetProgress(out int curProgress, out int maxProgress)
        {
            curProgress = CurProgress;
            maxProgress = TotalProgress;
        }


        // subclasses must implement this to return GCodeFile result
        protected abstract GCodeFile extract_result();




        /*
         *  Internals
         */

 

        // tags on slice polygons get transferred to shells
        IntTagSet<IFillPolygon> ShellTags = new IntTagSet<IFillPolygon>();

        // basic progress monitoring
        int TotalProgress = 1;
        int CurProgress = 0;

        // [TODO] these should be moved to settings, or something?
        double OverhangAllowanceMM;
        protected virtual double LayerFillAngleF(int layer_i)
        {
			//return 90;
			//return (layer_i % 2 == 0) ? 0 : 90;
            return (layer_i % 2 == 0) ? -45 : 45;
        }

        // start and end layers we will solve for (intersection of layercount and LayerRangeFilter)
        protected int CurStartLayer;
        protected int CurEndLayer;






        /// <summary>
        /// This is the main driver of the slicing process
        /// </summary>
        protected virtual IEnumerable<Progress> generate_result()
        {
            // should be parameterizable? this is 45 degrees...  (is it? 45 if nozzlediam == layerheight...)
            //double fOverhangAllowance = 0.5 * settings.NozzleDiamMM;
            OverhangAllowanceMM = Settings.LayerHeightMM / Math.Tan(45 * MathUtil.Deg2Rad);

            int NProgressStepsPerLayer = 10;
            TotalProgress = NProgressStepsPerLayer * (Slices.Count - 1);
            CurProgress = 0;

            if (AccumulatePathSet == true)
                AccumulatedPaths = new ToolpathSet();

			// build spatial caches for slice polygons
			//bool need_slice_spatial = (Settings.GenerateSupport);
			bool need_slice_spatial = true;  // need this for bridges...
			if (need_slice_spatial) {
				foreach(var i in Slices.BuildSliceSpatialCaches(false)) {
					yield return i;
				}
			}

            // initialize compiler and get start nozzle position
            Compiler.Begin();

            // We need N above/below shell paths to do roof/floors, and *all* shells to do support.
            // Also we can compute shells in parallel. So we just precompute them all here.
			foreach(var i in precompute_shells()) {
				yield return i;
			}
            int nLayers = Slices.Count;

            foreach(var i in precompute_support_areas()) yield return i;

			PrintLayerData prevLayerData = null;

            // Now generate paths for each layer.
            // This could be parallelized to some extent, but we have to pass per-layer paths
            // to Scheduler in layer-order. Probably better to parallelize within-layer computes.
            CurStartLayer = Math.Max(0, Settings.LayerRangeFilter.a);
            CurEndLayer = Math.Min(nLayers-1, Settings.LayerRangeFilter.b);
            for ( int layer_i = CurStartLayer; layer_i <= CurEndLayer; ++layer_i ) {
				if(layer_i % 2 == 0) yield return new Progress("paths", layer_i - CurStartLayer, CurEndLayer - CurStartLayer);
				// allocate new layer data structure
				PrintLayerData layerdata = PrintLayerDataFactoryF(layer_i, Slices[layer_i], this.Settings);
				layerdata.PreviousLayer = prevLayerData;

				// create path accumulator
				ToolpathSetBuilder pathAccum = PathBuilderFactoryF(layerdata);
				layerdata.PathAccum = pathAccum;

				// rest of code does not directly access path builder, instead it
				// sends paths to scheduler.
				IFillPathScheduler2d layerScheduler = SchedulerFactoryF(layerdata);
                GroupScheduler2d groupScheduler = new GroupScheduler2d(layerScheduler, Compiler.NozzlePosition.xy);
                //GroupScheduler groupScheduler = new PassThroughGroupScheduler(layerScheduler, Compiler.NozzlePosition.xy);
                layerdata.Scheduler = groupScheduler;

                BeginLayerF(layerdata);
                Compiler.AppendComment(string.Format("layer {0} - {1}mm", layer_i, Compiler.NozzlePosition.z));

				layerdata.ShellFills = get_layer_shells(layer_i);

                bool is_infill = (layer_i >= Settings.FloorLayers && layer_i < nLayers - Settings.RoofLayers - 1);

                // make path-accumulator for this layer
                pathAccum.Initialize(Compiler.NozzlePosition);
                // layer-up (ie z-change)
                pathAccum.AppendZChange(Settings.LayerHeightMM, Settings.ZTravelSpeed);

                // generate roof and floor regions. This could be done in parallel, or even pre-computed
                List<GeneralPolygon2d> roof_cover = new List<GeneralPolygon2d>();
                List<GeneralPolygon2d> floor_cover = new List<GeneralPolygon2d>();
                if (is_infill) {
                    if (Settings.RoofLayers > 0) {
                        roof_cover = find_roof_areas_for_layer(layer_i);
                    } else {
                        roof_cover = find_roof_areas_for_layer(layer_i-1);     // will return "our" layer
                    }
                    if (Settings.FloorLayers > 0) {
                        floor_cover = find_floor_areas_for_layer(layer_i);
                    } else {
                        floor_cover = find_floor_areas_for_layer(layer_i+1);   // will return "our" layer
                    }
                }
                count_progress_step();

                // do support first
                // this could be done in parallel w/ roof/floor...
                List<GeneralPolygon2d> support_areas = new List<GeneralPolygon2d>();
                support_areas = get_layer_support_area(layer_i);
                if (support_areas != null) {
                    groupScheduler.BeginGroup();
                    fill_support_regions(support_areas, groupScheduler, layerdata);
                    groupScheduler.EndGroup();
                    layerdata.SupportAreas = support_areas;
                }
                count_progress_step();

                // selector determines what order we process shells in
                ILayerShellsSelector shellSelector = ShellSelectorFactoryF(layerdata);

                // a layer can contain multiple disjoint regions. Process each separately.
                IShellsFillPolygon shells_gen = shellSelector.Next(groupScheduler.CurrentPosition);
                while ( shells_gen != null ) { 

                    // schedule shell paths that we pre-computed
                    List<FillCurveSet2d> shells_gen_paths = shells_gen.GetFillCurves();
                    FillCurveSet2d outer_shell = shells_gen_paths[shells_gen_paths.Count - 1];
                    bool do_outer_last = (shells_gen_paths.Count > 1);
                    groupScheduler.BeginGroup();
                    if (do_outer_last == false) {
                        groupScheduler.AppendCurveSets(shells_gen_paths);
                    } else {
                        groupScheduler.AppendCurveSets(shells_gen_paths.GetRange(0, shells_gen_paths.Count - 1));
                    }
                    groupScheduler.EndGroup();
                    count_progress_step();

                    // allow client to do configuration (eg change settings for example)
                    BeginShellF(shells_gen, ShellTags.Get(shells_gen));

                    // solid fill areas are inner polygons of shell fills
                    List<GeneralPolygon2d> solid_fill_regions = shells_gen.GetInnerPolygons();

                    // if this is an infill layer, compute infill regions, and remaining solid regions
                    // (ie roof/floor regions, and maybe others)
                    List<GeneralPolygon2d> infill_regions = new List<GeneralPolygon2d>();
                    if (is_infill)
						infill_regions = make_infill_regions(layer_i, solid_fill_regions, roof_cover, floor_cover, out solid_fill_regions);
                    bool has_infill = (infill_regions.Count > 0);

                    // fill solid regions
                    groupScheduler.BeginGroup();
					// [RMS] always call this for now because we may have bridge regions
                    fill_solid_regions(solid_fill_regions, groupScheduler, layerdata, has_infill);
                    groupScheduler.EndGroup();

                    // fill infill regions
                    groupScheduler.BeginGroup();
                    fill_infill_regions(infill_regions, groupScheduler, layerdata);
                    groupScheduler.EndGroup();
                    count_progress_step();

                    groupScheduler.BeginGroup();
                    if (do_outer_last) {
                        groupScheduler.AppendCurveSets( new List<FillCurveSet2d>() { outer_shell } );
                    }
                    groupScheduler.EndGroup();

					shells_gen = shellSelector.Next(groupScheduler.CurrentPosition);
                }

                // append open paths
                groupScheduler.BeginGroup();
                add_open_paths(layerdata, groupScheduler);
                groupScheduler.EndGroup();

                // discard the group scheduler
                layerdata.Scheduler = groupScheduler.TargetScheduler;

				// last chance to post-process paths for this layer before they are baked in
                if ( LayerPostProcessor != null )
                    LayerPostProcessor.Process(layerdata, pathAccum.Paths);

                // change speeds if layer is going to finish too quickly
                if (Settings.MinLayerTime > 0) {
                    CalculatePrintTime layer_time_calc = new CalculatePrintTime(pathAccum.Paths, Settings);
                    layer_time_calc.EnforceMinLayerTime();
                }

                // compile this layer 
                // [TODO] we could do this in a separate thread, in a queue of jobs?
                Compiler.AppendPaths(pathAccum.Paths);

                // add this layer to running pathset
                if (AccumulatedPaths != null)
                    AccumulatedPaths.Append(pathAccum.Paths);

                // we might want to consider this layer while we process next one
                prevLayerData = layerdata;

                count_progress_step();
            }

            Compiler.End();
            CurProgress = TotalProgress;
        }

        /// <summary>
        /// fill all infill regions
        /// </summary>
        protected virtual void fill_infill_regions(List<GeneralPolygon2d> infill_regions,
            IFillPathScheduler2d scheduler, PrintLayerData layer_data )
        {
            foreach (GeneralPolygon2d infill_poly in infill_regions) {
                List<GeneralPolygon2d> polys = new List<GeneralPolygon2d>() { infill_poly };

                if (Settings.SparseFillBorderOverlapX > 0) {
                    double offset = Settings.Machine.NozzleDiamMM * Settings.SparseFillBorderOverlapX;
                    polys = ClipperUtil.MiterOffset(polys, offset);
                }

                foreach (var poly in polys)
                    fill_infill_region(poly, scheduler, layer_data);
            }
        }


        /// <summary>
        /// fill polygon with sparse infill strategy
        /// </summary>
		protected virtual void fill_infill_region(GeneralPolygon2d infill_poly, IFillPathScheduler2d scheduler, PrintLayerData layer_data)
        {
            ICurvesFillPolygon infill_gen = new SparseLinesFillPolygon(infill_poly) {
                InsetFromInputPolygon = false,
                PathSpacing = Settings.SparseLinearInfillStepX * Settings.SolidFillPathSpacingMM(),
                ToolWidth = Settings.Machine.NozzleDiamMM,
				AngleDeg = LayerFillAngleF(layer_data.layer_i)
            };
            infill_gen.Compute();

			scheduler.AppendCurveSets(infill_gen.GetFillCurves());
        }




        protected virtual void fill_support_regions(List<GeneralPolygon2d> support_regions,
            IFillPathScheduler2d scheduler, PrintLayerData layer_data)
        {
            foreach (GeneralPolygon2d support_poly in support_regions)
                fill_support_region(support_poly, scheduler, layer_data);
        }



        /// <summary>
        /// fill polygon with support strategy
        ///     - single outer shell if Settings.EnableSupportShells = true
        ///     - then infill w/ spacing Settings.SupportSpacingStepX
        /// </summary>
		protected virtual void fill_support_region(GeneralPolygon2d support_poly, IFillPathScheduler2d scheduler, PrintLayerData layer_data)
        {
			AxisAlignedBox2d bounds = support_poly.Bounds;

			// settings may require a shell. However if support region
			// is very small, we will also use nested shells because infill
			// poly will likely be empty. In this case we nudge up the spacing
			// so that they are more loosely bonded
            // [TODO] we should only do this if we are directly below model. Otherwise this
            // branch is hit on any thin tube supports, that we could be printing empty
			int nShells = (Settings.EnableSupportShell) ? 1 : 0;
			double support_spacing = Settings.SupportSpacingStepX * Settings.SolidFillPathSpacingMM();
			double shell_spacing = Settings.Machine.NozzleDiamMM;
			if (bounds.MaxDim < 2 * support_spacing) {
				nShells = 3;
				shell_spacing = Settings.Machine.NozzleDiamMM + 0.1;
			}

            List<GeneralPolygon2d> infill_polys = new List<GeneralPolygon2d>() { support_poly };

			if (nShells > 0) {
                ShellsFillPolygon shells_gen = new ShellsFillPolygon(support_poly);
				shells_gen.PathSpacing = shell_spacing;
                shells_gen.ToolWidth = Settings.Machine.NozzleDiamMM;
				shells_gen.Layers = nShells;
                shells_gen.FilterSelfOverlaps = false;
                //shells_gen.FilterSelfOverlaps = true;
                //shells_gen.PreserveOuterShells = false;
                //shells_gen.SelfOverlapTolerance = Settings.SelfOverlapToleranceX * Settings.Machine.NozzleDiamMM;
				shells_gen.DiscardTinyPolygonAreaMM2 = 0.1;
				shells_gen.DiscardTinyPerimterLengthMM = 0.0;
                shells_gen.Compute();
                List<FillCurveSet2d> shell_fill_curves = shells_gen.GetFillCurves();
                foreach (var fillpath in shell_fill_curves)
                    fillpath.AddFlags(FillTypeFlags.SupportMaterial);
                scheduler.AppendCurveSets(shell_fill_curves);

                // expand inner polygon so that infill overlaps shell
                List<GeneralPolygon2d> inner_shells = shells_gen.GetInnerPolygons();
                if (Settings.SparseFillBorderOverlapX > 0) {
                    double offset = Settings.Machine.NozzleDiamMM * Settings.SparseFillBorderOverlapX;
                    infill_polys = ClipperUtil.MiterOffset(inner_shells, offset);
                }

            } 

            foreach ( var poly in infill_polys ) {
                SupportLinesFillPolygon infill_gen = new SupportLinesFillPolygon(poly) {
                    InsetFromInputPolygon = (Settings.EnableSupportShell == false),
					PathSpacing = support_spacing,
                    ToolWidth = Settings.Machine.NozzleDiamMM,
                    AngleDeg = 0,
                };
                infill_gen.Compute();
                scheduler.AppendCurveSets(infill_gen.GetFillCurves());
            }
        }





        /// <summary>
        /// fill set of solid regions
        /// </summary>
        protected virtual void fill_solid_regions(List<GeneralPolygon2d> solid_regions,
            IFillPathScheduler2d scheduler, PrintLayerData layer_data, bool bIsInfillAdjacent)
        {
			// if we have bridge regions on this layer, we subtract them from solid regions
			// and fill them using bridge strategy
			if (layer_data.layer_i > 0 && Settings.EnableBridging) {
				// bridge regions for layer i were computed at layer i-1...
				List<GeneralPolygon2d> bridge_regions = get_layer_bridge_area(layer_data.layer_i - 1);

				if (bridge_regions.Count > 0) {
					// bridge_regions are the physical bridge polygon.
					// solid_regions are the regions we have not yet filled this layer.
					// bridge works better if there is a 'landing pad' on either side, so we
					// expand and then clip with the solid regions, to get actual bridge fill region.

					double path_width = Settings.Machine.NozzleDiamMM;
					double shells_width = Settings.Shells * path_width;
					bridge_regions = ClipperUtil.MiterOffset(bridge_regions, shells_width);
					bridge_regions = ClipperUtil.Intersection(bridge_regions, solid_regions);

					if (bridge_regions.Count > 0) {
						// now have to subtract bridge region from solid region, in case there is leftover.
						// We are not going to inset bridge region or solid fill,  
						// so we need to add *two* half-width tolerances
						var offset_regions = ClipperUtil.MiterOffset(bridge_regions, Settings.Machine.NozzleDiamMM);
						solid_regions = ClipperUtil.Difference(solid_regions, offset_regions);

						foreach (var bridge_poly in bridge_regions)
							fill_bridge_region(bridge_poly, scheduler, layer_data);
					}
				}
			}

            foreach (GeneralPolygon2d solid_poly in solid_regions)
                fill_solid_region(layer_data, solid_poly, scheduler, bIsInfillAdjacent);
        }



        /// <summary>
        /// Fill polygon with solid fill strategy. 
        /// If bIsInfillAdjacent, then we optionally add one or more shells around the solid
        /// fill, to give the solid fill something to stick to (imagine dense linear fill adjacent
        /// to sparse infill area - when the extruder zigs, most of the time there is nothing
        /// for the filament to attach to, so it pulls back. ugly!)
        /// </summary>
        protected virtual void fill_solid_region(PrintLayerData layer_data, 
		                                         GeneralPolygon2d solid_poly, 
                                                 IFillPathScheduler2d scheduler,
                                                 bool bIsInfillAdjacent = false )
        {
            List<GeneralPolygon2d> fillPolys = new List<GeneralPolygon2d>() { solid_poly };

            // if we are on an infill layer, and this shell has some infill region,
            // then we are going to draw contours around solid fill so it has
            // something to stick to
            // [TODO] should only be doing this if solid-fill is adjecent to infill region.
            //   But how to determine this? not easly because we don't know which polys
            //   came from where. Would need to do loop above per-polygon
            if (bIsInfillAdjacent && Settings.InteriorSolidRegionShells > 0) {
                ShellsFillPolygon interior_shells = new ShellsFillPolygon(solid_poly);
                interior_shells.PathSpacing = Settings.SolidFillPathSpacingMM();
                interior_shells.ToolWidth = Settings.Machine.NozzleDiamMM;
                interior_shells.Layers = Settings.InteriorSolidRegionShells;
                interior_shells.InsetFromInputPolygonX = 0;
                interior_shells.ShellType = ShellsFillPolygon.ShellTypes.InternalShell;
                interior_shells.FilterSelfOverlaps = Settings.ClipSelfOverlaps;
                interior_shells.SelfOverlapTolerance = Settings.SelfOverlapToleranceX * Settings.Machine.NozzleDiamMM;
                interior_shells.Compute();
                scheduler.AppendCurveSets(interior_shells.GetFillCurves());
                fillPolys = interior_shells.InnerPolygons;
            }

            if (Settings.SolidFillBorderOverlapX > 0) {
                double offset = Settings.Machine.NozzleDiamMM * Settings.SolidFillBorderOverlapX;
                fillPolys = ClipperUtil.MiterOffset(fillPolys, offset);
            }

            // now actually fill solid regions
            foreach (GeneralPolygon2d fillPoly in fillPolys) {
				ICurvesFillPolygon solid_gen = new ParallelLinesFillPolygon(fillPoly) {
                    InsetFromInputPolygon = false,
                    PathSpacing = Settings.SolidFillPathSpacingMM(),
                    ToolWidth = Settings.Machine.NozzleDiamMM,
                    AngleDeg = LayerFillAngleF(layer_data.layer_i)
                };

                solid_gen.Compute();

				scheduler.AppendCurveSets(solid_gen.GetFillCurves());
            }
        }




		/// <summary>
		/// Fill a bridge region. Goal is to use shortest paths possible.
		/// So, instead of just using fixed angle, we fit bounding box and
		/// use the shorter axis. 
		/// </summary>
		protected virtual void fill_bridge_region(GeneralPolygon2d poly, IFillPathScheduler2d scheduler, PrintLayerData layer_data)
		{
			double spacing = Settings.BridgeFillPathSpacingMM();

			// fit bbox to try to find fill angle that has shortest spans
			Box2d box = poly.Outer.MinimalBoundingBox(0.00001);
			Vector2d axis = (box.Extent.x > box.Extent.y) ? box.AxisY : box.AxisX;
			double angle = Math.Atan2(axis.y, axis.x) * MathUtil.Rad2Deg;

			// [RMS] should we do something like this?
			//if (Settings.SolidFillBorderOverlapX > 0) {
			//	double offset = Settings.Machine.NozzleDiamMM * Settings.SolidFillBorderOverlapX;
			//	fillPolys = ClipperUtil.MiterOffset(fillPolys, offset);
			//}

			BridgeLinesFillPolygon fill_gen = new BridgeLinesFillPolygon(poly) {
				InsetFromInputPolygon = false,
				PathSpacing = spacing,
				ToolWidth = Settings.Machine.NozzleDiamMM,
				AngleDeg = angle,
			};
			fill_gen.Compute();
			scheduler.AppendCurveSets(fill_gen.GetFillCurves());
		}







        /// <summary>
        /// Determine the sparse infill and solid fill regions for a layer, given the input regions that
        /// need to be filled, and the roof/floor areas above/below this layer. 
        /// </summary>
        protected virtual List<GeneralPolygon2d> make_infill_regions(int layer_i, 
		                                                     List<GeneralPolygon2d> fillRegions, 
                                                             List<GeneralPolygon2d> roof_cover, 
                                                             List<GeneralPolygon2d> floor_cover, 
                                                             out List<GeneralPolygon2d> solid_regions)
                                                            
        {
            List<GeneralPolygon2d> infillPolys = fillRegions;

            List<GeneralPolygon2d> roofPolys = ClipperUtil.Difference(fillRegions, roof_cover);
            List<GeneralPolygon2d> floorPolys = ClipperUtil.Difference(fillRegions, floor_cover);
            solid_regions = ClipperUtil.Union(roofPolys, floorPolys);
            if (solid_regions == null)
                solid_regions = new List<GeneralPolygon2d>();

            // [TODO] I think maybe we should actually do another set of contours for the
            // solid region. At least one. This gives the solid & infill something to
            // connect to, and gives the contours above a continuous bonding thread

            // subtract solid fill from infill regions. However because we *don't*
            // inset fill regions, we need to subtract (solid+offset), so that
            // infill won't overlap solid region
            if (solid_regions.Count > 0) {
                List<GeneralPolygon2d> solidWithBorder =
                    ClipperUtil.MiterOffset(solid_regions, Settings.Machine.NozzleDiamMM);
                infillPolys = ClipperUtil.Difference(infillPolys, solidWithBorder);
            }

            return infillPolys;
        }




        /// <summary>
        /// construct region that needs to be solid for "roofs".
        /// This is the intersection of infill polygons for the next N layers.
        /// </summary>
        protected virtual List<GeneralPolygon2d> find_roof_areas_for_layer(int layer_i)
        {
            List<GeneralPolygon2d> roof_cover = new List<GeneralPolygon2d>();

            foreach (IShellsFillPolygon shells in get_layer_shells(layer_i+1))
                roof_cover.AddRange(shells.GetInnerPolygons());

            // If we want > 1 roof layer, we need to look further ahead.
            // The full area we need to print as "roof" is the infill minus
            // the intersection of the infill areas above
            for (int k = 2; k <= Settings.RoofLayers; ++k) {
                int ri = layer_i + k;
                if (ri < LayerShells.Length) {
                    List<GeneralPolygon2d> infillN = new List<GeneralPolygon2d>();
                    foreach (IShellsFillPolygon shells in get_layer_shells(ri))
                        infillN.AddRange(shells.GetInnerPolygons());

                    roof_cover = ClipperUtil.Intersection(roof_cover, infillN);
                }
            }

            // add overhang allowance. Technically any non-vertical surface will result in
            // non-empty roof regions. However we do not need to explicitly support roofs
            // until they are "too horizontal". 
            var result = ClipperUtil.MiterOffset(roof_cover, OverhangAllowanceMM);
            return result;
        }




        /// <summary>
        /// construct region that needs to be solid for "floors"
        /// </summary>
        protected virtual List<GeneralPolygon2d> find_floor_areas_for_layer(int layer_i)
        {
            List<GeneralPolygon2d> floor_cover = new List<GeneralPolygon2d>();

            foreach (IShellsFillPolygon shells in get_layer_shells(layer_i - 1))
                floor_cover.AddRange(shells.GetInnerPolygons());

            // If we want > 1 floor layer, we need to look further back.
            for (int k = 2; k <= Settings.FloorLayers; ++k) {
                int ri = layer_i - k;
                if (ri > 0) {
                    List<GeneralPolygon2d> infillN = new List<GeneralPolygon2d>();
                    foreach (IShellsFillPolygon shells in get_layer_shells(ri))
                        infillN.AddRange(shells.GetInnerPolygons());

                    floor_cover = ClipperUtil.Intersection(floor_cover, infillN);
                }
            }

            // add overhang allowance. 
            var result = ClipperUtil.MiterOffset(floor_cover, OverhangAllowanceMM);
            return result;
        }




        /// <summary>
        /// schedule any non-polygonal paths for the given layer (eg paths
        /// that resulted from open meshes, for example)
        /// </summary>
		protected virtual void add_open_paths(PrintLayerData layerdata, IFillPathScheduler2d scheduler)
        {
			PlanarSlice slice = layerdata.Slice;
            if (slice.Paths.Count == 0)
                return;

            FillCurveSet2d paths = new FillCurveSet2d();
            for ( int pi = 0; pi < slice.Paths.Count; ++pi ) {
				FillPolyline2d pline = new FillPolyline2d(slice.Paths[pi]) {
					TypeFlags = FillTypeFlags.OpenShellCurve
				};

                // leave space for end-blobs (input paths are extent we want to hit)
                pline.Trim(Settings.Machine.NozzleDiamMM / 2);

                // ignore tiny paths
                if (PathFilterF != null && PathFilterF(pline) == true)
                    continue;

                paths.Append(pline);
            }

            scheduler.AppendCurveSets(new List<FillCurveSet2d>() { paths });
        }








        // The set of perimeter fills for each layer. 
        // If we have sparse infill, we need to have multiple shells available to do roof/floors.
        // To do support, we ideally would have them all.
        // Currently we precompute all shell-fills up-front, in precompute_shells().
        // However you could override this behavior, eg do on-demand compute, in GetLayerShells()
        protected List<IShellsFillPolygon>[] LayerShells;

        /// <summary>
        /// return the set of shell-fills for a layer. This includes both the shell-fill paths
        /// and the remaining regions that need to be filled.
        /// </summary>
		protected virtual List<IShellsFillPolygon> get_layer_shells(int layer_i) {
            // evaluate shell on-demand
            //if ( LayerShells[layeri] == null ) {
            //    PlanarSlice slice = Slices[layeri];
            //    LayerShells[layeri] = compute_shells_for_slice(slice);
            //}
			return LayerShells[layer_i];
        }

        /// <summary>
        /// compute all the shells for the entire slice-stack
        /// </summary>
        protected virtual IEnumerable<Progress> precompute_shells()
        {
            int nLayers = Slices.Count;
            LayerShells = new List<IShellsFillPolygon>[nLayers];

            int max_roof_floor = Math.Max(Settings.RoofLayers, Settings.FloorLayers);
            int start_layer = Math.Max(0, Settings.LayerRangeFilter.a-max_roof_floor);
            int end_layer = Math.Min(nLayers - 1, Settings.LayerRangeFilter.b+max_roof_floor);

            Interval1i solve_shells = new Interval1i(start_layer, end_layer);
            foreach(var layeri in solve_shells) {
                PlanarSlice slice = Slices[layeri];
                LayerShells[layeri] = compute_shells_for_slice(slice);
                count_progress_step();
				if(layeri % 20 == 0) yield return new Progress("shells", layeri - solve_shells.a, solve_shells.Length);
            }
        }

        /// <summary>
        /// compute all the shell-fills for a given slice
        /// </summary>
        protected virtual List<IShellsFillPolygon> compute_shells_for_slice(PlanarSlice slice)
        {
            List<IShellsFillPolygon> layer_shells = new List<IShellsFillPolygon>();
            foreach (GeneralPolygon2d shape in slice.Solids) {
                IShellsFillPolygon shells_gen = compute_shells_for_shape(shape);
                layer_shells.Add(shells_gen);

                if (slice.Tags.Has(shape)) {
                    lock (ShellTags) {
                        ShellTags.Add(shells_gen, slice.Tags.Get(shape));
                    }
                }
            }
            return layer_shells;
        }

        /// <summary>
        /// compute a shell-fill for the given shape (assumption is that shape.Outer 
        /// is anoutermost perimeter)
        /// </summary>
        protected virtual IShellsFillPolygon compute_shells_for_shape(GeneralPolygon2d shape)
        {
            ShellsFillPolygon shells_gen = new ShellsFillPolygon(shape);
            shells_gen.PathSpacing = Settings.SolidFillPathSpacingMM();
            shells_gen.ToolWidth = Settings.Machine.NozzleDiamMM;
            shells_gen.Layers = Settings.Shells;
            shells_gen.FilterSelfOverlaps = Settings.ClipSelfOverlaps;
            shells_gen.SelfOverlapTolerance = Settings.SelfOverlapToleranceX * Settings.Machine.NozzleDiamMM;

            shells_gen.Compute();
            return shells_gen;
        }







        // The set of support areas for each layer
        protected List<GeneralPolygon2d>[] LayerSupportAreas;

        /// <summary>
        /// return the set of support-region polygons for a layer. 
        /// </summary>
		protected virtual List<GeneralPolygon2d> get_layer_support_area(int layer_i)
        {
            return LayerSupportAreas[layer_i];
        }

		// The set of bridge areas for each layer. These are basically the support
		// areas that we can bridge. So, they are one layer below the model area.
		protected List<GeneralPolygon2d>[] LayerBridgeAreas;

		/// <summary>
		/// return the set of bridgeable support-region polygons for a layer.
		/// Note that the bridge regions for layer i are at layer i-1 (because
		/// these are the support areas)
		/// </summary>
		protected virtual List<GeneralPolygon2d> get_layer_bridge_area(int layer_i)
		{
			return LayerBridgeAreas[layer_i];
		}




        /// <summary>
        /// compute support volumes for entire slice-stack
        /// </summary>
        protected virtual IEnumerable<Progress> precompute_support_areas()
        {
			foreach(var i in generate_bridge_areas()) yield return i;
			
            if (Settings.GenerateSupport)
                foreach(var i in generate_support_areas()) yield return i;
            else
                foreach(var i in add_existing_support_areas()) yield return i;
			
        }


		/// <summary>
        /// Find the unsupported regions in each layer that can be bridged
        /// </summary>
		protected virtual IEnumerable<Progress> generate_bridge_areas()
		{
			int nLayers = Slices.Count;

			LayerBridgeAreas = new List<GeneralPolygon2d>[nLayers];
			if (nLayers <= 1)
				yield break;

			// [RMS] does this make sense? maybe should be using 0 here?
			double bridge_tol = Settings.Machine.NozzleDiamMM * 0.5;
			double min_area = Settings.Machine.NozzleDiamMM;
			min_area *= min_area;

			foreach(var layeri in Interval1i.Range(nLayers - 1)) {
				PlanarSlice slice = Slices[layeri];
				PlanarSlice next_slice = Slices[layeri + 1];

				// To find bridgeable regions, we compute all floating regions in next layer. 
				// Then we look for polys that are bridgeable, ie thing enough and fully anchored.
				List<GeneralPolygon2d> bridgePolys = null;
				if (Settings.EnableBridging) {
					bridgePolys = ClipperUtil.Difference(next_slice.Solids, slice.Solids);
					bridgePolys = CurveUtils2.Filter(bridgePolys, (p) => {
						return layeri > 0 && is_bridgeable(p, layeri, bridge_tol);
					});
					bridgePolys = CurveUtils2.FilterDegenerate(bridgePolys, min_area);
				}

				LayerBridgeAreas[layeri] = (bridgePolys != null)
					? bridgePolys : new List<GeneralPolygon2d>();
				if(layeri % 20 == 0) yield return new Progress("bridge_areas", layeri, nLayers);
			}
			LayerBridgeAreas[nLayers - 1] = new List<GeneralPolygon2d>();

		}



        /// <summary>
        /// Auto-generate the planar solids required to support each area,
        /// and then sweep them downwards.
        /// </summary>
        protected virtual IEnumerable<Progress> generate_support_areas()
        {
            /*
             *  Here is the strategy for computing support areas:
             *    For layer i, support region is union of:
             *         1) Difference[ layer i+1 solids, offset(layer i solids, fOverhangAngleDist) ]
             *              (ie the bit of the next layer we need to support)
             *         2) support region at layer i+1
             *         3) any pre-defined support solids  (ie from user-defined mesh) 
             *         
             *    Once we have support region at layer i, we subtract expanded layer i solid, to leave a gap
             *         (this is fSupportGapInLayer)
             *    
             *    Note that for (2) above, it is frequently the case that support regions in successive
             *    layers are disjoint. To merge these regions, we dilate/union/contract.
             *    The dilation amount is the fMergeDownDilate parameter.
             */

            double fPrintWidth = Settings.Machine.NozzleDiamMM;


            // "insert" distance that is related to overhang angle
            //  distance = 0 means, full support
            //  distance = nozzle_diam means, 45 degrees overhang
            //  ***can't use angle w/ nozzle diam. Angle is related to layer height.
            //double fOverhangAngleDist = 0;
            double fOverhangAngleDist = Settings.LayerHeightMM / Math.Tan(Settings.SupportOverhangAngleDeg * MathUtil.Deg2Rad);
            //double fOverhangAngleDist = Settings.LayerHeightMM / Math.Tan(45 * MathUtil.Deg2Rad);
            //double fOverhangAngleDist = Settings.Machine.NozzleDiamMM;          // support inset half a nozzle
            //double fOverhangAngleDist = Settings.Machine.NozzleDiamMM * 0.9;  // support directly below outer perimeter
            //double fOverhangAngleDist = Settings.Machine.NozzleDiamMM;          // support inset half a nozzle

            // amount we dilate/contract support regions when merging them,
            // to ensure overlap. Using a larger value here has the effect of
            // smoothing out the support polygons. However it can also end up
            // merging disjoint regions...
            double fMergeDownDilate = Settings.Machine.NozzleDiamMM * 2.0;

            // space we leave between support polygons and solids
            // [TODO] do we need to include SupportAreaOffsetX here?
            double fSupportGapInLayer = Settings.SupportSolidSpace;

			// extra offset we add to support polygons, eg to nudge them
			// in/out depending on shell layers, etc
			double fSupportOffset = Settings.SupportAreaOffsetX * Settings.Machine.NozzleDiamMM;

			// we will throw away holes in support regions smaller than these thresholds
			double DiscardHoleSizeMM = Settings.Machine.NozzleDiamMM;
			double DiscardHoleArea = DiscardHoleSizeMM * DiscardHoleSizeMM;

			// if support poly is further than this from model, we consider
			// it a min-z-tip and it gets special handling
			double fSupportMinDist = Settings.Machine.NozzleDiamMM;


			int nLayers = Slices.Count;
			LayerSupportAreas = new List<GeneralPolygon2d>[nLayers];
			if (nLayers <= 1)
				yield break;


			/*
			 * Step 1: compute absolute support polygon for each layer
			 */

			// For layer i, compute support region needed to support layer (i+1)
			// This is the *absolute* support area - no inset for filament width or spacing from model
			foreach(var layeri in Interval1i.Range(nLayers - 1)) {
				if(layeri % 10 == 0) yield return new Progress("support_areas_step1", layeri, nLayers);
				PlanarSlice slice = Slices[layeri];
				PlanarSlice next_slice = Slices[layeri + 1];

				// expand this layer and subtract from next layer. leftovers are
				// what needs to be supported on next layer.
				List<GeneralPolygon2d> expandPolys = ClipperUtil.MiterOffset(slice.Solids, fOverhangAngleDist);
				List<GeneralPolygon2d> supportPolys = ClipperUtil.Difference(next_slice.Solids, expandPolys);

				// subtract regions we are going to bridge
				List<GeneralPolygon2d> bridgePolys = get_layer_bridge_area(layeri);
				if (bridgePolys.Count > 0) {
					supportPolys = ClipperUtil.Difference(supportPolys, bridgePolys);
				}

				// if we have an support inset/outset, apply it here.
				// for insets the poly may disappear, in that case we
				// keep the original poly.
				// [TODO] handle partial-disappears
				if (fSupportOffset != 0) {
					List<GeneralPolygon2d> offsetPolys = new List<GeneralPolygon2d>();
					foreach (var poly in supportPolys) {
						List<GeneralPolygon2d> offset = ClipperUtil.MiterOffset(poly, fSupportOffset);
						// if offset is empty, use original poly
						if (offset.Count == 0) {
							offsetPolys.Add(poly);
						} else {
							offsetPolys.AddRange(offset);
						}
					}
					supportPolys = offsetPolys;
				}

				// now we need to deal with tiny polys. If they are min-z-tips,
				// we want to add larger support regions underneath them. 
				// We determine this by measuring distance to this layer.
				// NOTE: we **cannot** discard tiny polys here, because a bunch of
				// tiny per-layer polygons may merge into larger support regions
				// after dilate/contract, eg on angled thin strips. 
				// NOTE2: we could discard tiny polys if we are sure they are
				// sufficiently supported, but the test is kind of expensive, 
				// need to spatial query in a ring and make sure it is 
				// connected on multiple sides...
				List<GeneralPolygon2d> filteredPolys = new List<GeneralPolygon2d>();
				foreach ( var poly in supportPolys ) {
					var bounds = poly.Bounds;
					// big enough to keep
					if (bounds.MaxDim > 4*fPrintWidth) {
						filteredPolys.Add(poly);
						continue;
					}
					// check distance. if we are not close to any solids, this 
					// is a min-z-tip, and gets a larger support poly.
					double dist_sqr = slice.DistanceSquared(bounds.Center, 3*fSupportMinDist);
					if (dist_sqr < fSupportMinDist * fSupportMinDist) {
						filteredPolys.Add(poly);
					} else {
						filteredPolys.Add(make_support_point_poly(bounds.Center));
					}
				}
				supportPolys.Clear();
				supportPolys.AddRange(filteredPolys);

				// add any explicit support points in this layer as circles
				foreach (Vector2d v in slice.InputSupportPoints)
					supportPolys.Add(make_support_point_poly(v));

                LayerSupportAreas[layeri] = supportPolys;
                count_progress_step();
            };
            LayerSupportAreas[nLayers-1] = new List<GeneralPolygon2d>();


			/*
			 * Step 2: sweep support polygons downwards
			 */

			// now merge support layers. Process is to track "current" support area,
			// at layer below we union with that layers support, and then subtract
			// that layers solids. 
			List<GeneralPolygon2d> prevSupport = LayerSupportAreas[nLayers - 1];
            for (int i = nLayers - 2; i >= 0; --i) {
				yield return new Progress("support_areas_step2", nLayers - 2 - i, nLayers - 2);
                PlanarSlice slice = Slices[i];

                // union down
                List<GeneralPolygon2d> combineSupport = null;

                // [RMS] smooth the support polygon from the previous layer. if we allow
                // shrinking then they will shrink to nothing, though...need to bound this somehow
                List<GeneralPolygon2d> support_above = new List<GeneralPolygon2d>();
                bool grow = true, shrink = false;
                foreach ( GeneralPolygon2d solid in prevSupport ) {
                    GeneralPolygon2d copy = new GeneralPolygon2d();
                    copy.Outer = new Polygon2d(solid.Outer);
                    if ( grow || shrink )
                        CurveUtils2.LaplacianSmoothConstrained(copy.Outer, 0.5, 5, fMergeDownDilate, shrink, grow);

                    List<GeneralPolygon2d> outer_clip = (solid.Holes.Count == 0) ? null : ClipperUtil.ComputeOffsetPolygon(copy, -fPrintWidth, true);
                    foreach (Polygon2d hole in solid.Holes) {
                        if (hole.Bounds.MaxDim < DiscardHoleSizeMM || Math.Abs(hole.SignedArea) < DiscardHoleArea)
                            continue;
                        Polygon2d new_hole = new Polygon2d(hole);
                        if (grow || shrink)
                            CurveUtils2.LaplacianSmoothConstrained(new_hole, 0.5, 5, fMergeDownDilate, shrink, grow);

                        List<GeneralPolygon2d> clipped_holes = 
                            ClipperUtil.Difference(new GeneralPolygon2d(new_hole), outer_clip);
                        foreach (GeneralPolygon2d cliphole in clipped_holes) {
                            new_hole = cliphole.Outer;
                            if (new_hole.Bounds.MaxDim > DiscardHoleSizeMM && Math.Abs(new_hole.SignedArea) > DiscardHoleArea) {
                                if (new_hole.IsClockwise == false )
                                    new_hole.Reverse();
                                copy.AddHole(new_hole, false);
                            }
                        }
                    }

                    support_above.Add(copy);
                }


                // [TODO] should discard small interior holes here if they don't intersect layer...


                // [RMS] support polygons on successive layers they will not necessarily intersect, because
                // they are offset inwards on each layer. But as we merge down, we want them to be combined.
                // So, we do a dilate / boolean / contract. 
                bool dilate = true;
                if (dilate) {
                    List<GeneralPolygon2d> a = ClipperUtil.MiterOffset(support_above, fMergeDownDilate);
                    List<GeneralPolygon2d> b = ClipperUtil.MiterOffset(LayerSupportAreas[i], fMergeDownDilate);
                    combineSupport = ClipperUtil.Union(a, b);
                    combineSupport = ClipperUtil.MiterOffset(combineSupport, -fMergeDownDilate);
                } else {
                    combineSupport = ClipperUtil.Union(support_above, LayerSupportAreas[i]);
                }

                // support area we propagate down is combined area minus solid
                prevSupport = ClipperUtil.Difference(combineSupport, slice.Solids);

                // [TODO] everything after here can be done in parallel in a second pass, right?

                // if we have explicit support, we can union it in now
                if ( slice.SupportSolids.Count > 0 ) {
                    combineSupport = ClipperUtil.Union(combineSupport, slice.SupportSolids);
                }

                // make sure there is space between solid and support
                List<GeneralPolygon2d> dilatedSolid = ClipperUtil.MiterOffset(slice.Solids, fSupportGapInLayer);
                combineSupport = ClipperUtil.Difference(combineSupport, dilatedSolid);

                LayerSupportAreas[i] = new List<GeneralPolygon2d>();
                foreach (GeneralPolygon2d poly in combineSupport) {
                    PolySimplification2.Simplify(poly, 0.5*Settings.Machine.NozzleDiamMM);
                    LayerSupportAreas[i].Add(poly);
                }
            }
        }


        /// <summary>
        /// Add explicit support solids defined in PlanarSlices. This is called when
        /// Settings.GenerateSupport = false, otherwise these solids are included in
        /// precompute_support_areas().  (todo: have that call this?)
        /// </summary>
        protected virtual IEnumerable<Progress> add_existing_support_areas()
        {
            // space we leave between support polygons and solids
            double fSupportGapInLayer = Settings.SupportSolidSpace;

            int nLayers = Slices.Count;
            LayerSupportAreas = new List<GeneralPolygon2d>[nLayers];
            if (nLayers <= 1)
                yield break;

            foreach(var i in Interval1i.Range(Slices.Count)) {
                PlanarSlice slice = Slices[i];
				yield return new Progress("existing_support_areas", i, Slices.Count);
                if (slice.SupportSolids.Count == 0)
                    continue;

                // if we have explicit support, we can union it in now
                List<GeneralPolygon2d> combineSupport = slice.SupportSolids;

                // simplify each poly
                // [RMS] for existing support we do only a very tiny amount of simplification...
                foreach (GeneralPolygon2d poly in combineSupport) {
                    PolySimplification2.Simplify(poly, 0.05 * Settings.Machine.NozzleDiamMM);
                }

                // make sure there is space between solid and support
                List<GeneralPolygon2d> dilatedSolid = ClipperUtil.MiterOffset(slice.Solids, fSupportGapInLayer);
                combineSupport = ClipperUtil.Difference(combineSupport, dilatedSolid);

                LayerSupportAreas[i] = combineSupport;

                count_progress_step();
            }
        }


		/*
		 * Bridging and Support utility functions
		 */ 

		/// <summary>
		/// generate support point polygon (eg circle)
		/// </summary>
		protected virtual GeneralPolygon2d make_support_point_poly(Vector2d v, double diameter = -1)
		{
			if ( diameter <= 0 )
				diameter = Settings.SupportPointDiam;
			Polygon2d circ = Polygon2d.MakeCircle(
				diameter * 0.5, Settings.SupportPointSides);
			circ.Translate(v);
			return new GeneralPolygon2d(circ);			
		}

		/// <summary>
		/// Check if polygon can be bridged. Currently we allow this if all hold:
		/// 1) contracting by max bridge width produces empty polygon
		/// 2) all "turning" vertices of polygon are connected to previous layer
		/// [TODO] not sure this actually guarantees that unsupported distances
		/// *between* turns are within bridge threshold...
		/// </summary>
		protected virtual bool is_bridgeable(GeneralPolygon2d support_poly, int iLayer, double fTolDelta)
		{
			double max_bridge_dist = Settings.MaxBridgeWidthMM;

			// if we inset by half bridge dist, and this doesn't completely wipe out 
			// polygon, then it is too wide to bridge, somewhere
			// [TODO] this is a reasonable way to decompose into bridgeable chunks...
			double inset_delta = max_bridge_dist * 0.55;
			List<GeneralPolygon2d> offset = ClipperUtil.MiterOffset(support_poly, -inset_delta);
			if (offset != null && offset.Count > 0)
				return false;

			if (is_fully_connected(support_poly.Outer, iLayer, fTolDelta) == false)
				return false;
			foreach (var h in support_poly.Holes) {
				if (is_fully_connected(h, iLayer, fTolDelta) == false)
					return false;
			}

			return true;
		}

		/// <summary> 
		/// check if all turn vertices of poly are connected ( see is_connected(vector2d) )
		/// </summary>
		protected virtual bool is_fully_connected(Polygon2d poly, int iLayer, double fTolDelta)
		{
			int NV = poly.VertexCount;
			for (int k = 0; k < NV; ++k) {
				Vector2d v = poly[k];
				if ( k > 0 && poly.OpeningAngleDeg(k) > 179 )
					continue;
				if (is_connected(poly[k], iLayer, fTolDelta) == false)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check if position is "connected" to a solid in the slice
		/// at layer i, where connected means distance is within tolerance
		/// [TODO] I don't think this will return true if pos is inside one of the solids...
		/// </summary>
		protected virtual bool is_connected(Vector2d pos, int iLayer, double fTolDelta)
		{
			double maxdist = fTolDelta;
			double maxdist_sqr = maxdist * maxdist;

			PlanarSlice slice = Slices[iLayer];
			double dist_sqr = slice.DistanceSquared(pos, maxdist_sqr, true, true);
			if (dist_sqr < maxdist_sqr)
				return true;
			
			return false;
		}



        /// <summary>
        /// Factory function to return a new PathScheduler to use for this layer.
        /// </summary>
		protected virtual IFillPathScheduler2d get_layer_scheduler(PrintLayerData layer_data)
        {
			SequentialScheduler2d scheduler = new SequentialScheduler2d(layer_data.PathAccum, layer_data.Settings);

            // be careful on first layer
			scheduler.SpeedHint = (layer_data.layer_i == CurStartLayer) ?
                SchedulerSpeedHint.Careful : SchedulerSpeedHint.Rapid;

            return scheduler;
        }



        protected virtual void count_progress_step()
        {
            Interlocked.Increment(ref CurProgress);
        }



    }




}
