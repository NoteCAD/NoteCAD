using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using g3;

namespace gs
{
    public interface ILayerPathsPostProcessor
    {
        void Process(PrintLayerData layerData, ToolpathSet layerPaths);
    }



    public class LayerPathsPostProcessorSequence
    {
        public List<ILayerPathsPostProcessor> Posts = new List<ILayerPathsPostProcessor>();

        public virtual void Process(PrintLayerData layerData, ToolpathSet layerPaths)
        {
            foreach (var post in Posts)
                post.Process(layerData, layerPaths);
        }
    }









}
