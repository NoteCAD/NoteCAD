using System;
using System.Collections.Generic;
using g3;
using System.Collections.ObjectModel;

namespace gs
{
	[Flags]
	public enum FillTypeFlags
	{
		Unknown = 0,

		PerimeterShell = 1,
		OutermostShell = 1<<1,
		OuterPerimeter = PerimeterShell | OutermostShell,

		InteriorShell = 1<<2,

		OpenShellCurve = 1<<3,    // ie for single-line-wide features

		SolidInfill = 1<<8,
		SparseInfill = 1<<9,

        SupportMaterial = 1<<10,
		BridgeSupport = 1<<11
	}



    /// <summary>
    /// things that are common to FillPolyline2d and FillPolygon2d
    /// </summary>
    public interface FillCurve2d
    {
        bool HasTypeFlag(FillTypeFlags f);
    }



	/// <summary>
	/// Additive polygon fill curve
	/// </summary>
	public class FillPolygon2d : Polygon2d, FillCurve2d
    {
		public FillTypeFlags TypeFlags = FillTypeFlags.Unknown;

		public bool HasTypeFlag(FillTypeFlags f) {
			return (TypeFlags & f) != 0;
		}

		public FillPolygon2d() : base()
		{
		}

		public FillPolygon2d(Vector2d[] v) : base(v)
		{
		}

		public FillPolygon2d(Polygon2d p) : base(p)
		{
		}	
	}





	/// <summary>
	/// Additive polyline fill curve
	/// </summary>
	public class FillPolyline2d : PolyLine2d, FillCurve2d
    {
		public FillTypeFlags TypeFlags = FillTypeFlags.Unknown;

		public bool HasTypeFlag(FillTypeFlags f) {
			return (TypeFlags & f) == f;
		}

		// [TODO] maybe remove? see below.
		List<TPVertexFlags> flags;
		bool has_flags = false;

		public FillPolyline2d() : base()
		{
		}

		public FillPolyline2d(Vector2d[] v) : base(v)
		{
		}

		public FillPolyline2d(PolyLine2d p) : base(p)
		{
		}

		void alloc_flags()
		{
			if (flags == null) {
				flags = new List<TPVertexFlags>();
				for (int i = 0; i < vertices.Count; ++i)
					flags.Add(TPVertexFlags.None);
			}
		}

		public override void AppendVertex(Vector2d v)
		{
			base.AppendVertex(v);
			if (flags != null)
				flags.Add(TPVertexFlags.None);
		}
		public override void AppendVertices(IEnumerable<Vector2d> v)
		{
			base.AppendVertices(v);
			if (flags != null) {
				foreach (var x in v)
					flags.Add(TPVertexFlags.None);
			}
		}

		public override void Reverse()
		{
			base.Reverse();
			if (flags != null)
				flags.Reverse();
		}
		public override void Simplify(double clusterTol = 0.0001,
										double lineDeviationTol = 0.01,
									  bool bSimplifyStraightLines = true)
		{
            int n = vertices.Count;

            int i, k, pv;            // misc counters
            Vector2d[] vt = new Vector2d[n];  // vertex buffer
            bool has_flags = HasFlags;
            TPVertexFlags[] vf = (has_flags) ? new TPVertexFlags[n] : null;
            bool[] mk = new bool[n];
            for (i = 0; i < n; ++i)     // marker buffer
                mk[i] = false;

            // STAGE 1.  Vertex Reduction within tolerance of prior vertex cluster
            double clusterTol2 = clusterTol * clusterTol;
            vt[0] = vertices[0];              // start at the beginning
            for (i = k = 1, pv = 0; i < n; i++) {
                if ((vertices[i] - vertices[pv]).LengthSquared < clusterTol2)
                    continue;
                if (has_flags)
                    vf[k] = flags[i];
                vt[k++] = vertices[i];
                pv = i;
            }
            if (pv < n - 1)
                vt[k++] = vertices[n - 1];      // finish at the end

            // STAGE 2.  Douglas-Peucker polyline simplification
            if (lineDeviationTol > 0) {
                mk[0] = mk[k - 1] = true;       // mark the first and last vertices
                simplifyDP(lineDeviationTol, vt, 0, k - 1, mk);
            } else {
                for (i = 0; i < k; ++i)
                    mk[i] = true;
            }

            // copy marked vertices back to this polygon
            vertices = new List<Vector2d>();
            flags = (has_flags) ? new List<TPVertexFlags>() : null;
            for (i = 0; i < k; ++i) {
                if (mk[i]) {
                    vertices.Add(vt[i]);
                    if (has_flags)
                        flags.Add(vf[i]);
                }
            }
            Timestamp++;

            return;
        }


		public void AppendVertex(Vector2d v, TPVertexFlags flag)
		{
			alloc_flags();
			base.AppendVertex(v);
			flags.Add(flag);
			has_flags = true;
		}
		public void AppendVertices(IEnumerable<Vector2d> v, IEnumerable<TPVertexFlags> f)
		{
			alloc_flags();
			base.AppendVertices(v);
			flags.AddRange(f);
			has_flags = true;
		}


		// [RMS] this is *only* used for PathUtil.ConnectorVFlags. Maybe remove this capability?
		public TPVertexFlags GetFlag(int i) { return (flags == null) ? TPVertexFlags.None : flags[i]; }
		public void SetFlag(int i, TPVertexFlags flag) { alloc_flags(); flags[i] = flag; }

		public bool HasFlags {
			get { return flags != null && has_flags; }
		}
		public ReadOnlyCollection<TPVertexFlags> Flags() { return flags.AsReadOnly(); }
	}
}
