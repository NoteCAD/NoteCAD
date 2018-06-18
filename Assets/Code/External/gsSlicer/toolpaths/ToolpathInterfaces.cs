using System;
using System.Collections.Generic;
using System.Linq;

using g3;

namespace gs 
{
	public enum ToolpathTypes {
		Deposition,
		Travel,
		PlaneChange,

        CustomAssemblerCommands,

		Composite,
		Custom
	};

    /// <summary>
    /// PathVertex.Flags field is 3 ints that can be used for whatever purpose.
    /// First int we assume is one of these values, or a client-defined value.
    /// </summary>
    [Flags]
    public enum TPVertexFlags
    {
        None            = 0,
        IsConnector     = 1,            // connects spans of a linear fill. also currently not used (!)
        IsSupport       = 1 << 1        // unused currently?
    }


    public class TPVertexData
    {
        // information about the move *to* this vertex (ie the segment before it)
        public TPVertexFlags Flags = TPVertexFlags.None;

        // (optional) modifier functions that will be applied to this vertex
        // during gcode emission
        public Func<Vector3d, Vector3d> PositionF = null;
        public Func<double, double> FeedRateModifierF = null;
        public Func<Vector3d, Vector3d> ExtrusionModifierF = null;
    }


	public interface IToolpathVertex
    {
		Vector3d Position { get; set; }
        double FeedRate { get; set; }
        TPVertexData ExtendedData { get; set; }
    }


	public struct PrintVertex : IToolpathVertex 
	{
		public Vector3d Position { get; set; }
        public double FeedRate { get; set; }
        public TPVertexData ExtendedData { get; set; }

        public Vector3d Extrusion { get; set; }

        public object Source { get; set; }

		public PrintVertex(Vector3d pos, double rate) {
			Position = pos;
			FeedRate = rate;
			Extrusion = Vector3d.Zero;
            ExtendedData = null;
            Source = null;
		}

		public PrintVertex(Vector3d pos, double rate, double ExtruderA) {
			Position = pos;
			FeedRate = rate;
			Extrusion = new Vector3d(ExtruderA, 0, 0);
            ExtendedData = null;
            Source = null;
		}

		public static implicit operator Vector3d(PrintVertex v)
		{
			return v.Position;
		}
	};


	public interface IToolpath
	{
		ToolpathTypes Type { get; }
		bool IsPlanar { get; }
		bool IsLinear { get; }

		Vector3d StartPosition { get; }
		Vector3d EndPosition { get; }
		AxisAlignedBox3d Bounds { get; }

		bool HasFinitePositions { get; }
		IEnumerable<Vector3d> AllPositionsItr();
	}

	public interface ILinearToolpath<T> : IToolpath, IEnumerable<T>
	{
		T this[int key] { get; }
	}

	public interface IBuildLinearToolpath<T> : ILinearToolpath<T>
	{
		void ChangeType(ToolpathTypes type);
		void AppendVertex(T v);	
		void UpdateVertex(int i, T v);
		
		int VertexCount { get; }
		T Start { get; }
		T End { get; }
	}


	public interface IToolpathSet : IToolpath, IEnumerable<IToolpath>
	{
	}



	// Just a utility class we can subclass to create custom "marker" paths
	// in the path stream.
	public class SentinelToolpath : IToolpath
	{
		public virtual ToolpathTypes Type { 
			get {
				return ToolpathTypes.Custom;
			}
		}
		public virtual bool IsPlanar { 
			get {
				return false;
			}
		}
		public virtual bool IsLinear { 
			get {
				return false;
			}
		}

		public virtual Vector3d StartPosition {
			get {
				return Vector3d.Zero;
			}
		}

		public virtual Vector3d EndPosition {
			get {
				return Vector3d.Zero;
			}
		}

		public virtual AxisAlignedBox3d Bounds { 
			get {
				return AxisAlignedBox3d.Zero;
			}
		}

		public bool HasFinitePositions { 
			get { return false; }
		}
		public IEnumerable<Vector3d> AllPositionsItr() {
			return Enumerable.Empty<Vector3d>();
		}

	}



    public class AssemblerCommandsToolpath : SentinelToolpath
    {
        public override ToolpathTypes Type {
            get { return ToolpathTypes.CustomAssemblerCommands; }
        }

        public Action<IDepositionAssembler, ThreeAxisPrinterCompiler> AssemblerF;
    }

}
