using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using g3;

namespace gs
{
    /// <summary>
    /// Generate models for printer calibration
    /// </summary>
    public static class CalibrationModelGenerator
    {

        /// <summary>
        /// Generates a row of cylinders tessellated w/ different chord lengths
        ///   eg 10x1cm : CalibrationModelGenerator.MakePrintStepSizeTest(10.0f, 10.0f, 0.1, 1.0, 10);
        /// </summary>
        public static DMesh3 MakePrintStepSizeTest(double cylDiam, double cylHeight, double lowStep, double highStep, int nSteps)
        {
            double spacing = 2.0f;
            float r = (float)cylDiam * 0.5f;
            double cx = 0.5 * (nSteps * cylDiam + (nSteps - 1) * spacing);

            DMesh3 accumMesh = new DMesh3();

            double cur_x = -cx + cylDiam / 2;
            for ( int k = 0; k < nSteps; ++k ) {
                double t = (double)k / (double)(nSteps - 1);
                double chord_len = (1.0 - t) * lowStep + (t) * highStep;
                int slices = (int)((MathUtil.TwoPI * r) / chord_len);

                CappedCylinderGenerator cylgen = new CappedCylinderGenerator() {
                    BaseRadius = r, TopRadius = r, Height = (float)cylHeight,
                    Slices = slices,
                    NoSharedVertices = false
                };
                DMesh3 cylMesh = cylgen.Generate().MakeDMesh();
                MeshTransforms.Translate(cylMesh, -cylMesh.CachedBounds.Min.y * Vector3d.AxisY);
                MeshTransforms.Translate(cylMesh, cur_x * Vector3d.AxisX);
                cur_x += cylDiam + spacing;
                MeshEditor.Append(accumMesh, cylMesh);
            }

            MeshTransforms.ConvertYUpToZUp(accumMesh);

            return accumMesh;

        }


    }
}
