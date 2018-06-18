using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gs
{
    public class GenericSLSPrintGenerator : SLSPrintGenerator
    {
        GCodeFileAccumulator file_accumulator;
        //GCodeBuilder builder;
        SLSCompiler compiler;

        public GenericSLSPrintGenerator(PrintMeshAssembly meshes,
                                      PlanarSliceStack slices,
                                      SingleMaterialFFFSettings settings)
        {
            file_accumulator = new GCodeFileAccumulator();
            //builder = new GCodeBuilder(file_accumulator);
            //compiler = new SLSCompiler(builder, settings);
            compiler = new SLSCompiler(settings);

            base.Initialize(meshes, slices, settings, compiler);
        }

        protected override ToolpathSet extract_result()
        {
            return compiler.TempGetAssembledPaths();
        }
    }
}
