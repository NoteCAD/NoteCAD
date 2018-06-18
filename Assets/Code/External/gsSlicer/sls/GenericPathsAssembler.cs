using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace gs
{
    public interface IPathsAssembler
    {
        void AppendPaths(IToolpathSet paths);

        // [TODO] we should replace this w/ a separte assembler/builder, even if the assembler is trivial!!
        ToolpathSet TempGetAssembledPaths();
    }


    public class GenericPathsAssembler : IPathsAssembler
    {
        public ToolpathSet AccumulatedPaths;


        public GenericPathsAssembler()
        {
            AccumulatedPaths = new ToolpathSet();
        }


        public void AppendPaths(IToolpathSet paths)
        {
            AccumulatedPaths.Append(paths);
        }


        public ToolpathSet TempGetAssembledPaths()
        {
            return AccumulatedPaths;
        }
    }
}
