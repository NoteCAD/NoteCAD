using System;
using g3;

namespace gs
{
    public interface ThreeAxisLaserCompiler
    {
        void Begin();
        void AppendPaths(ToolpathSet paths);
        void End();
    }



    public class SLSCompiler : ThreeAxisLaserCompiler
    {
        SingleMaterialFFFSettings Settings;
        IPathsAssembler Assembler;

        public SLSCompiler(SingleMaterialFFFSettings settings)
        {
            Settings = settings;
        }

        public virtual void Begin()
        {
            Assembler = InitializeAssembler();
        }

        // override to customize assembler
        protected virtual IPathsAssembler InitializeAssembler()
        {
            IPathsAssembler asm = new GenericPathsAssembler();
            return asm;
        }

        public virtual void End()
        {
        }



        public virtual void AppendPaths(ToolpathSet paths)
        {
            Assembler.AppendPaths(paths);
        }


        public ToolpathSet TempGetAssembledPaths()
        {
            return Assembler.TempGetAssembledPaths();
        }


    }
}
