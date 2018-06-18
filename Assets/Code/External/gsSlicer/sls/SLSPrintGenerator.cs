using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace gs
{


    /// <summary>

    /// </summary>
    public abstract class SLSPrintGenerator
    {
        // Data structures that must be provided by client
        protected PrintMeshAssembly PrintMeshes;
        protected PlanarSliceStack Slices;
        protected ThreeAxisLaserCompiler Compiler;
        public SingleMaterialFFFSettings Settings;      // public because you could modify
                                                        // this during process, ie in BeginLayerF
                                                        // to implement per-layer settings

        // available after calling Generate()
        public ToolpathSet Result;

        // replace this with your own error message handler
        public Action<string, string> ErrorF = (message, stack_trace) => {
            System.Console.WriteLine("[EXCEPTION] SLSPrintGenerator: " + message + "\nSTACK TRACE: " + stack_trace);
        };

        // This is called at the beginning of each layer, you can replace to
        // implement progress bar, etc
        public Action<int> BeginLayerF = (layeri) => { };

        // This is called before we process each shell. The Tag is transferred
        // from the associated region in the PlanarSlice, if it had one, otherwise it is int.MaxValue
        public Action<IFillPolygon, int> BeginShellF = (shell_fill, tag) => { };


        protected SLSPrintGenerator()
        {
        }

        public SLSPrintGenerator(PrintMeshAssembly meshes, 
                                       PlanarSliceStack slices,
                                       SingleMaterialFFFSettings settings,
                                       ThreeAxisLaserCompiler compiler)
        {
            Initialize(meshes, slices, settings, compiler);
        }



        public void Initialize(PrintMeshAssembly meshes, 
                               PlanarSliceStack slices,
                               SingleMaterialFFFSettings settings,
                               ThreeAxisLaserCompiler compiler)
        {
            PrintMeshes = meshes;
            Slices = slices;
            Settings = settings;
            Compiler = compiler;
        }



        public virtual bool Generate()
        {
            try {
                generate_result();
                Result = extract_result();
            } catch ( Exception e ) {
                ErrorF(e.Message, e.StackTrace);
                return false;
            }
            return true;
        }



        // subclasses must implement this to return GCodeFile result
        protected abstract ToolpathSet extract_result();




        /*
         *  Internals
         */


        List<ShellsFillPolygon>[] LayerShells;

        // tags on slice polygons get transferred to shells
        IntTagSet<IFillPolygon> ShellTags = new IntTagSet<IFillPolygon>();

        // [TODO] these should be moved to settings, or something?
        double OverhangAllowanceMM;
        protected virtual double LayerFillAngleF(int layer_i)
        {
            return (layer_i % 2 == 0) ? 0 : 90;
        }



        /// <summary>
        /// This is the main driver of the slicing process
        /// </summary>
        protected virtual void generate_result()
        {

            // should be parameterizable? this is 45 degrees...  (is it? 45 if nozzlediam == layerheight...)
            //double fOverhangAllowance = 0.5 * settings.NozzleDiamMM;
            OverhangAllowanceMM = Settings.LayerHeightMM / Math.Tan(45 * MathUtil.Deg2Rad);


            // initialize compiler and get start nozzle position
            Compiler.Begin();

            // We need N above/below shell paths to do roof/floors, and *all* shells to do support.
            // Also we can compute shells in parallel. So we just precompute them all here.
            precompute_shells();
            int nLayers = Slices.Count;

            // Now generate paths for each layer.
            // This could be parallelized to some extent, but we have to pass per-layer paths
            // to Scheduler in layer-order. Probably better to parallelize within-layer computes.
            for ( int layer_i = 0; layer_i < nLayers; ++layer_i ) {
                BeginLayerF(layer_i);

                // make path-accumulator for this layer
                ToolpathSetBuilder paths = new ToolpathSetBuilder();

                // TODO FIX
                //paths.Initialize(Compiler.NozzlePosition);
                paths.Initialize( (double)(layer_i) * Settings.LayerHeightMM * Vector3d.AxisZ ); 

                // layer-up (ie z-change)
                paths.AppendZChange(Settings.LayerHeightMM, Settings.ZTravelSpeed);

                // rest of code does not directly access path builder, instead if
                // sends paths to scheduler.
                SequentialScheduler2d scheduler = new SequentialScheduler2d(paths, Settings);

                // a layer can contain multiple disjoint regions. Process each separately.
                List<ShellsFillPolygon> layer_shells = LayerShells[layer_i];
                for (int si = 0; si < layer_shells.Count; si++) {

                    // schedule shell paths that we pre-computed
                    ShellsFillPolygon shells_gen = layer_shells[si];
                    scheduler.AppendCurveSets(shells_gen.Shells);

                    // all client to do configuration (eg change settings for example)
                    BeginShellF(shells_gen, ShellTags.Get(shells_gen));

                    // solid fill areas are inner polygons of shell fills
                    List<GeneralPolygon2d> solid_fill_regions = shells_gen.InnerPolygons;

                    // fill solid regions
                    foreach (GeneralPolygon2d solid_poly in solid_fill_regions)
                        fill_solid_region(layer_i, solid_poly, scheduler, false);
                }

                // resulting paths for this layer (Currently we are just discarding this after compiling)
                ToolpathSet layerPaths = paths.Paths;

                // compile this layer
                Compiler.AppendPaths(layerPaths);
            }

            Compiler.End();
        }




        /// <summary>
        /// Fill polygon with solid fill strategy. 
        /// If bIsInfillAdjacent, then we optionally add one or more shells around the solid
        /// fill, to give the solid fill something to stick to (imagine dense linear fill adjacent
        /// to sparse infill area - when the extruder zigs, most of the time there is nothing
        /// for the filament to attach to, so it pulls back. ugly!)
        /// </summary>
        protected virtual void fill_solid_region(int layer_i, GeneralPolygon2d solid_poly, 
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
                interior_shells.Compute();
                scheduler.AppendCurveSets(interior_shells.Shells);
                fillPolys = interior_shells.InnerPolygons;
            }

            // now actually fill solid regions
            foreach (GeneralPolygon2d fillPoly in fillPolys) {
                TiledFillPolygon tiled_fill = new TiledFillPolygon(fillPoly) {
                    TileSize = 13.1*Settings.SolidFillPathSpacingMM(),
                    TileOverlap = 0.3* Settings.SolidFillPathSpacingMM()
                };
                tiled_fill.TileFillGeneratorF = (tilePoly, index) => {
                    int odd = ((index.x+index.y) % 2 == 0) ? 1 : 0;
                    RasterFillPolygon solid_gen = new RasterFillPolygon(tilePoly) {
                        InsetFromInputPolygon = false,
                        PathSpacing = Settings.SolidFillPathSpacingMM(),
                        ToolWidth = Settings.Machine.NozzleDiamMM,
                        AngleDeg = LayerFillAngleF(layer_i + odd)
                    };
                    return solid_gen;
                };

                tiled_fill.Compute();
                scheduler.AppendCurveSets(tiled_fill.FillCurves);
            }
        }



  

        protected virtual void precompute_shells()
        {
            int nLayers = Slices.Count;

            LayerShells = new List<ShellsFillPolygon>[nLayers];
            gParallel.ForEach(Interval1i.Range(nLayers), (layeri) => {
                PlanarSlice slice = Slices[layeri];
                LayerShells[layeri] = new List<ShellsFillPolygon>();

                List<GeneralPolygon2d> solids = slice.Solids;

                foreach (GeneralPolygon2d shape in solids) {
                    ShellsFillPolygon shells_gen = new ShellsFillPolygon(shape);
                    shells_gen.PathSpacing = Settings.SolidFillPathSpacingMM();
                    shells_gen.ToolWidth = Settings.Machine.NozzleDiamMM;
                    shells_gen.Layers = Settings.Shells;
                    shells_gen.InsetInnerPolygons = false;
                    shells_gen.Compute();
                    LayerShells[layeri].Add(shells_gen);

                    if (slice.Tags.Has(shape))
                        ShellTags.Add(shells_gen, slice.Tags.Get(shape));
                }
            });
        }




    }




}
