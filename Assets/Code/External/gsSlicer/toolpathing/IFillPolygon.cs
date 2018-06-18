using System;
using System.Collections.Generic;
using g3;

namespace gs
{
	public interface IFillPolygon
	{
		GeneralPolygon2d Polygon { get; }
		bool Compute();
	}


    public interface ICurvesFillPolygon : IFillPolygon
    {
        List<FillCurveSet2d> GetFillCurves();
    }

    public interface IShellsFillPolygon : ICurvesFillPolygon
    {
        List<GeneralPolygon2d> GetInnerPolygons();
    }

}
