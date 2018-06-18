using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using g3;

namespace gs
{
    /// <summary>
    /// Collection of loop and curve centerlines of fill curves
    /// 
    /// [TODO] support thickness variation along curves?
    /// </summary>
	public class FillCurveSet2d
	{
		public List<FillPolygon2d> Loops = new List<FillPolygon2d>();
		public List<FillPolyline2d> Curves = new List<FillPolyline2d>();

		public FillCurveSet2d()
		{
		}


		public void Append(GeneralPolygon2d poly, FillTypeFlags typeFlags) {
			Loops.Add(new FillPolygon2d(poly.Outer) { TypeFlags = typeFlags });
			foreach (var h in poly.Holes)
				Loops.Add(new FillPolygon2d(h) { TypeFlags = typeFlags });
		}

		public void Append(List<GeneralPolygon2d> polys, FillTypeFlags typeFlags) {
			foreach (var p in polys)
				Append(p, typeFlags);
		}

		public void Append(Polygon2d poly, FillTypeFlags typeFlags) {
			Loops.Add(new FillPolygon2d(poly) { TypeFlags = typeFlags } );
        }

        public void Append(List<Polygon2d> polys, FillTypeFlags typeFlags) {
            foreach (var p in polys)
				Append(p, typeFlags);
        }

        public void Append(FillPolygon2d loop) {
			Loops.Add(loop);
		}

        public void Append(List<FillPolygon2d> loops) {
            foreach ( var l in loops )
			    Loops.Add(l);
		}


        public void Append(FillPolyline2d curve) {
			Curves.Add(curve);
		}

		public void Append(List<FillPolyline2d> curves) {
			foreach (var p in curves)
				Append(p);
		}


        public void SetFlags(FillTypeFlags flags)
        {
            foreach (var loop in Loops)
                loop.TypeFlags = flags;
            foreach (var curve in Curves)
                curve.TypeFlags = flags;
        }

        public void AddFlags(FillTypeFlags flags)
        {
            foreach (var loop in Loops)
                loop.TypeFlags |= flags;
            foreach (var curve in Curves)
                curve.TypeFlags |= flags;
        }



        // DEPRECATED - remove?
        // this connects up the paths with small connectors? used in DenseLinesFillPolygon
        public void OptimizeCurves(double max_dist, Func<Segment2d, bool> ValidateF) {
			int[] which = new int[4];
			double[] dists = new double[4];
			for (int ci = 0; ci < Curves.Count; ++ci ) {
				FillPolyline2d l0 = Curves[ci];

				// find closest viable connection
				int iClosest = -1;
				int iClosestCase = -1;
				for (int cj = ci + 1; cj < Curves.Count; ++cj) {
					FillPolyline2d l1 = Curves[cj];
					dists[0] = l0.Start.Distance(l1.Start);  which[0] = 0;
					dists[1] = l0.Start.Distance(l1.End);  which[1] = 1;
					dists[2] = l0.End.Distance(l1.Start);  which[2] = 2;
					dists[3] = l0.End.Distance(l1.End);  which[3] = 3;
					Array.Sort(dists, which);

					for (int k = 0; k < 4 && iClosest != cj; ++k) {
						if (dists[k] > max_dist)
							continue;
						Segment2d connector = get_case(l0, l1, which[k]);
						if (ValidateF(connector) == false)
							continue;
						iClosest = cj;
						iClosestCase = which[k];
					}
				}

				if (iClosest == -1)
					continue;

				// [TODO] it would be better to preserve start/direction of
				//   longest path, if possible. Maybe make that an option?

				// ok we will join ci w/ iClosest. May need reverse one
				FillPolyline2d ljoin = Curves[iClosest];
				if (iClosestCase == 0) {
					l0.Reverse();
				} else if (iClosestCase == 1) {
					l0.Reverse();
					ljoin.Reverse();
				} else if (iClosestCase == 3) {
					ljoin.Reverse();
				}

				// now we are in straight-append order
				l0.AppendVertices(ljoin);
				Curves.RemoveAt(iClosest);

				// force check again w/ this curve
				ci--;
			}

		}


		static Segment2d get_case(FillPolyline2d l0, FillPolyline2d l1, int which) {
			if (which == 0)
				return new Segment2d(l0.Start, l1.Start);
			else if (which == 1)
				return new Segment2d(l0.Start, l1.End);
			else if (which == 2)
				return new Segment2d(l0.End, l1.Start);
			else
				return new Segment2d(l0.End, l1.End);
		}


	}
}
