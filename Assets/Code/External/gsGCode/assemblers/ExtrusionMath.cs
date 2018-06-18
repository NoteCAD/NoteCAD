using System;
using g3;

namespace gs
{
    /// <summary>
    /// Utility class for filament extrusion calculations
    /// </summary>
    public static class ExtrusionMath
    {

        /// <summary>
        /// This function computes the amount of filament to extrude (ie how
        /// much to turn extruder stepper) along pathLen distance.
        /// volumeScale allows for local tuning of this.
        /// </summary>
        public static double PathLengthToFilamentLength(
            double layerHeight, double nozzleDiam, double filamentDiam,
            double pathLen, 
            double volumeScale = 1.0)
        {
            double section_area = nozzleDiam * layerHeight;
            double linear_volume = pathLen * section_area;
            linear_volume *= volumeScale;

            double filamentRadius = filamentDiam * 0.5;
            double filament_section_area = Math.PI * filamentRadius * filamentRadius;
            double filament_len = linear_volume / filament_section_area;

            return filament_len;
        }

    }
}
