using System;
using System.Collections.Generic;
using g3;

namespace gs 
{
    public static class ToolpathUtil
    {
        public static void ApplyToLeafPaths(IToolpath root, Action<IToolpath> LeafF) {
            if (root is IToolpathSet) {
                ApplyToLeafPaths(root as IToolpathSet, LeafF);
            } else {
                LeafF(root);
            }
        }
        public static void ApplyToLeafPaths(IToolpathSet root, Action<IToolpath> LeafF) {
            foreach (var ipath in (root as IToolpathSet))
                ApplyToLeafPaths(ipath, LeafF);
        }



        public static List<IToolpath> FlattenPaths(IToolpath root) {
            List<IToolpath> result = new List<IToolpath>();
            ApplyToLeafPaths(root, (p) => {
                result.Add(p);
            });
            return result;
        }



        public static void AddPerVertexFlags<T>(LinearToolpath3<T> toolpath, IList<TPVertexFlags> flags) where T : IToolpathVertex
        {
            int N = toolpath.VertexCount;
            for ( int i = 0; i < N; ++i ) {
                T vtx = toolpath[i];
                if (vtx.ExtendedData == null)
                    vtx.ExtendedData = new TPVertexData();
                vtx.ExtendedData.Flags |= flags[i];
                toolpath.UpdateVertex(i, vtx);
            }
        }



        public static void SetConstantPerVertexData<T>(LinearToolpath3<T> toolpath, TPVertexData data) where T : IToolpathVertex
        {
            int N = toolpath.VertexCount;
            for (int i = 0; i < N; ++i) {
                T vtx = toolpath[i];
                vtx.ExtendedData = data;
                toolpath.UpdateVertex(i, vtx);
            }
        }



    }
}
