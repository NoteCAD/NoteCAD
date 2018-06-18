using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using g3;

namespace gs
{
	public class LayersDetector
	{
		public ToolpathSet Paths;

		// layers with fewer points than this are filtered out
		public int MinLayerCount = 4;
		public int RoundLayerToPrecision = 3;

		public Dictionary<double, int> LayersCounts;
		public List<double> LayerZ;

		public double EstimatedLayerHeight;

		public LayersDetector(ToolpathSet paths, double knownLayerHeight = 0)
		{
			Paths = paths;
			Compute();
            if (knownLayerHeight > 0)
                EstimatedLayerHeight = knownLayerHeight;
        }



		public int Layers {
			get { return LayerZ.Count; }
		}
        public int Count {
            get { return LayerZ.Count; }
        }

		public double GetLayerZ(int iLayer) {
            iLayer = MathUtil.Clamp(iLayer, 0, Layers - 1);
            return LayerZ[iLayer];
		}

		public Interval1d GetLayerZInterval(int iLayer) {
			if (Layers == 0)
				return Interval1d.Zero;

			iLayer = MathUtil.Clamp(iLayer, 0, Layers - 1);

			double low = LayerZ[iLayer] - EstimatedLayerHeight * 0.45f;
			double high = LayerZ[iLayer] + EstimatedLayerHeight * 0.45f;

			//double low = (iLayer <= 0) ? LayerZ[iLayer] :
			//	(LayerZ[iLayer] + LayerZ[iLayer - 1]) * 0.5;
			//double high = (iLayer == Layers-1) ? LayerZ[iLayer] :
				//(LayerZ[iLayer] + LayerZ[iLayer + 1]) * 0.5;
			
			return new Interval1d(low, high);
		}

        /// <summary>
        /// Find layer closest to fZ
        /// </summary>
        public int GetLayerIndex(double fZ)
        {
            int i = 0;
            double minDist = double.MaxValue;
            while (i < LayerZ.Count) {
                double d = Math.Abs(LayerZ[i] - fZ);
                if (d < minDist) {
                    minDist = d;
                    i++;
                } else {
                    return i-1;
                }
            }
            return LayerZ.Count - 1;
        }


        public void Compute() 
		{
			LayersCounts = new Dictionary<double, int>();

			Action<IToolpath> processPathF = (path) => {
				if ( path.HasFinitePositions ) {
					foreach (Vector3d v in path.AllPositionsItr())
						accumulate(v);
				}
			};
			Action<IToolpathSet> processPathsF = null;
			processPathsF = (paths) => {
				foreach (IToolpath path in paths) {
					if (path is IToolpathSet)
						processPathsF(path as IToolpathSet);
					else
						processPathF(path);
				}
			};

			processPathsF(Paths);

			List<double> erase = new List<double>();
			foreach ( var v in LayersCounts ) {
                // [RMS] nothing should be at Z=0
                if ( v.Key == 0 ) {
                    erase.Add(v.Key);
                    continue;
                }
				if (v.Value < MinLayerCount)
					erase.Add(v.Key);
			}
			foreach (var e in erase)
				LayersCounts.Remove(e);

			LayerZ = new List<double>(LayersCounts.Keys);
			LayerZ.Sort();

			// estimate layer height
			Dictionary<double, int> LayerHeights = new Dictionary<double, int>();
			for (int i = 0; i < LayerZ.Count - 1; ++i ) {
				double dz = Math.Round(LayerZ[i + 1] - LayerZ[i], 3);
				if (LayerHeights.ContainsKey(dz) == false)
					LayerHeights[dz] = 0;
				LayerHeights[dz] = LayerHeights[dz] + 1;
			}
			double best_height = 0; int max_count = 0;
			foreach ( var pair in LayerHeights ) {
				if ( pair.Value > max_count ) {
					max_count = pair.Value;
					best_height = pair.Key;
				}
			}
			EstimatedLayerHeight = best_height;

		}


		void accumulate(Vector3d v) {
			if (v.z == GCodeUtil.UnspecifiedValue)
				return;
			double z = Math.Round(v.z, RoundLayerToPrecision);
			int count = 0;
			if ( LayersCounts.TryGetValue(z, out count) ) {
				count++;
				LayersCounts[z] = count;
			} else {
				LayersCounts.Add(z, 1);
			}
		}
	}
}
