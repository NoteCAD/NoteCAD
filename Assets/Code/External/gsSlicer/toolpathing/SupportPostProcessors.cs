using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using g3;

namespace gs
{
    // [TODO] find a way to not hardcode this??
    using LinearToolpath = LinearToolpath3<PrintVertex>;


    /// <summary>
    /// This post-processor finds model vertices that are above the support polygons of
    /// the previous layer, and offsets them vertically. This encourages delamination
    /// of model from support.
    /// 
    /// [TODO] accelleration data structure for polygon containment?
    /// [TODO] a long path that crosses support, but with neither endpoint contained,
    ///    will not be affected. Maybe sample at subdivisions, and be able to insert
    ///    additional vertices into path?
    /// [TODO] vary printing speed? esp at turns, which this can round off a little bit...
    /// </summary>
    public class SupportConnectionPostProcessor : ILayerPathsPostProcessor
    {

        public double ZOffsetMM = 0.2f;

        public virtual void Process(PrintLayerData layerData, ToolpathSet layerPaths)
        {
            if (layerData.PreviousLayer == null)
                return;
            if (layerData.PreviousLayer.SupportAreas == null || layerData.PreviousLayer.SupportAreas.Count == 0)
                return;

            LayerCache cache = build_cache(layerData.PreviousLayer);

            Func<Vector3d, Vector3d> ZOffsetF = (v) => {
                return new Vector3d(v.x, v.y, v.z + ZOffsetMM);
            };

            foreach ( var toolpath in layerPaths ) {
                LinearToolpath tp = toolpath as LinearToolpath;
                if (tp == null)
                    continue;
                if ((tp.TypeModifiers & FillTypeFlags.SupportMaterial) != 0)
                    continue;

                int N = tp.VertexCount;
                //for ( int i = 0; i < N; ++i ) {
                for ( int i = 1; i < N-1; ++i ) {       // start and end cannot be modified!
                    PrintVertex v = tp[i];
                    if ( is_over_support(v.Position.xy, ref cache) ) {
                        v.Position = ZOffsetF(v.Position);
                        tp.UpdateVertex(i, v);
                    }
                }
            }

        }



        struct LayerCache
        {
            public List<GeneralPolygon2d> SupportAreas;
            public AxisAlignedBox2d[] SupportAreaBounds;
            public AxisAlignedBox2d AllSupportBounds;
        }


        LayerCache build_cache(PrintLayerData layerData)
        {
            LayerCache cache = new LayerCache();

            cache.SupportAreas = ClipperUtil.MiterOffset(layerData.SupportAreas, layerData.Settings.Machine.NozzleDiamMM);
            cache.SupportAreaBounds = new AxisAlignedBox2d[cache.SupportAreas.Count];
            cache.AllSupportBounds = AxisAlignedBox2d.Empty;
            for (int i = 0; i < cache.SupportAreas.Count; ++i) {
                cache.SupportAreaBounds[i] = cache.SupportAreas[i].Bounds;
                cache.AllSupportBounds.Contain(cache.SupportAreaBounds[i]);
            }

            return cache;
        }


        bool is_over_support(Vector2d v, ref LayerCache cache)
        {
            if (cache.AllSupportBounds.Contains(v) == false)
                return false;

            int N = cache.SupportAreaBounds.Length;
            for (int i = 0; i < N; ++i) {
                if (cache.SupportAreaBounds[i].Contains(v))
                    if (cache.SupportAreas[i].Contains(v))
                        return true;
            }
            return false;
        }


    }
}
