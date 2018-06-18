using System;
using System.Collections.Generic;
using g3;

namespace gs 
{
    /// <summary>
    /// This class implements calculation of the filament extrusion distance/volume along
    /// a ToolpathSet. Currently the actual extrusion calculation is quite basic.
    /// 
    /// Note that this is (currently) also where retraction happens. Possibly this would
    /// be better handled elsewhere, since ideally it is a temporary +/- to the extrusion,
    /// such that the actual accumulated extrusion amount is not modified.
    /// 
    /// </summary>
	public class CalculateExtrusion 
	{
		public ToolpathSet Paths;
		public SingleMaterialFFFSettings Settings;

        public bool EnableRetraction = true;

		double FilamentDiam = 1.75;
		double NozzleDiam = 0.4;
		double LayerHeight = 0.2;
		double FixedRetractDistance = 1.3;
        double SupportExtrudeScale = 0.9;

        // [RMS] if we travel less than this distance, we don't retract
        double MinRetractTravelLength = 2.5;

		// output statistics
		public int NumPaths = 0;
		public double ExtrusionLength = 0;


		public CalculateExtrusion(ToolpathSet paths, SingleMaterialFFFSettings settings) 
		{
			Paths = paths;
			Settings = settings;

            EnableRetraction = settings.EnableRetraction;
            FilamentDiam = settings.Machine.FilamentDiamMM;
			NozzleDiam = settings.Machine.NozzleDiamMM;
			LayerHeight = settings.LayerHeightMM;
			FixedRetractDistance = settings.RetractDistanceMM;
            MinRetractTravelLength = settings.MinRetractTravelLength;
            SupportExtrudeScale = settings.SupportVolumeScale;
        }



		public void Calculate(Vector3d vStartPos, double fStartA, bool alreadyInRetract = false)
		{
			double curA = fStartA;
			Vector3d curPos = vStartPos;
			double curRate = 0;

			bool inRetract = alreadyInRetract;

			// filter paths
			List<IToolpath> allPaths = new List<IToolpath>();
			foreach ( IToolpath ipath in Paths ) {
				ToolpathUtil.ApplyToLeafPaths(ipath, (p) => { 
					if (p is LinearToolpath3<PrintVertex> || p is ResetExtruderPathHack) { 
						allPaths.Add(p); 
					} 
				});
			}
			int N = allPaths.Count;


            LinearToolpath3<PrintVertex> prev_path = null;
            for ( int pi = 0; pi < N; ++pi ) {
				if ( allPaths[pi] is ResetExtruderPathHack ) {
					curA = 0;
					continue;
				}
				LinearToolpath3<PrintVertex> path = allPaths[pi] as LinearToolpath3<PrintVertex>;

				if ( path == null )
					throw new Exception("Invalid path type!");
				if ( ! (path.Type == ToolpathTypes.Deposition || path.Type == ToolpathTypes.PlaneChange || path.Type == ToolpathTypes.Travel) )
					throw new Exception("Unknown path type!");

                // if we are travelling between two extrusion paths, and neither is support, 
                // and the travel distance is very short,then we will skip the retract. 
                // [TODO] should only do this on interior travels. We should determine that upstream and set a flag on travel path.
                bool skip_retract = false;
                if ( path.Type == ToolpathTypes.Travel && path.Length < MinRetractTravelLength ) {
                    bool prev_is_model_deposition =
                        (prev_path != null) && (prev_path.Type == ToolpathTypes.Deposition) && ((prev_path.TypeModifiers & FillTypeFlags.SupportMaterial) == 0);
                    LinearToolpath3<PrintVertex> next_path = (pi < N-1) ? (allPaths[pi + 1] as LinearToolpath3<PrintVertex>) : null;
                    bool next_is_model_deposition =
                        (next_path != null) && (next_path.Type == ToolpathTypes.Deposition) && ((next_path.TypeModifiers & FillTypeFlags.SupportMaterial) == 0);
                    skip_retract = prev_is_model_deposition && next_is_model_deposition;
                }
                if (EnableRetraction == false)
                    skip_retract = true;

                for ( int i = 0; i < path.VertexCount; ++i ) {
					bool last_vtx = (i == path.VertexCount-1);

					Vector3d newPos = path[i].Position;
					double newRate = path[i].FeedRate;

					if ( path.Type != ToolpathTypes.Deposition ) {

                        // [RMS] if we switched to a travel move we retract, unless we don't
                        if (skip_retract == false) {
                            if (!inRetract) {
                                curA -= FixedRetractDistance;
                                inRetract = true;
                            } 
                        }

						curPos = newPos;
						curRate = newRate;

					} else {

                        // for i == 0 this dist is always 0 !!
                        double dist = (newPos - curPos).Length;

                        if (i == 0) {
                            Util.gDevAssert(dist == 0);     // next path starts at end of previous!!
                            if (inRetract) {
                                curA += FixedRetractDistance;
                                inRetract = false;
                            }
                        } else {
                            curPos = newPos;
                            curRate = newRate;

                            double vol_scale = 1;
							if ((path.TypeModifiers & FillTypeFlags.SupportMaterial) != 0)
								vol_scale *= SupportExtrudeScale;
							else if ((path.TypeModifiers & FillTypeFlags.BridgeSupport) != 0)
								vol_scale *= Settings.BridgeVolumeScale;

                            double feed = ExtrusionMath.PathLengthToFilamentLength(
                                Settings.LayerHeightMM, Settings.Machine.NozzleDiamMM, Settings.Machine.FilamentDiamMM,
                                dist, vol_scale);
                            curA += feed;
                        }
					}

					PrintVertex v = path[i];
					v.Extrusion = GCodeUtil.Extrude(curA);
					path.UpdateVertex(i, v);

				}

                prev_path = path;
            }

			NumPaths = N;
			ExtrusionLength = curA;

		} // Calculate()


		bool is_connection(Index3i flags) {
			return (flags.a & (int)TPVertexFlags.IsConnector) != 0;
		}



	}
}
