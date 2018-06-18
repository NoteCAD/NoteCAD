using System;
using g3;

namespace gs
{
	/// <summary>
	/// Utility functions for gcode assemblers
	/// </summary>
	public static class AssemblerUtil
	{

		/// <summary>
		/// Calculate distance of filament to extrude to draw path of
		/// given length/width/height, for given filament diameter.
		/// </summary>
		public static double CalculateExtrudedFilament(
			double pathWidth, double pathHeight, double pathLen, 
			double filamentDiam )
		{
			// [RMS] this is just formula form gsSlicer.CalculateExtrusion
			double section_area = pathWidth * pathHeight;
			double linear_vol = pathLen * section_area;
			double fil_rad = filamentDiam / 2;
			double fil_area = Math.PI * fil_rad * fil_rad;
			double fil_len = linear_vol / fil_area;
			return fil_len;
		}

	}
}
