using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using g3;

namespace gs
{

    public class TemporalPathHash
    {
        public double HashBucketSize = 5.0;

        DVector<Segment2d> Segments;
        DVector<int> Times;


        SegmentHashGrid2d<int> Hash;


        public TemporalPathHash()
        {
            Segments = new DVector<Segment2d>();
            Times = new DVector<int>();

            Hash = new SegmentHashGrid2d<int>(HashBucketSize, -1);
        }


        public void AppendSegment(Vector2d p0, Vector2d p1)
        {
            // todo
        }



    }
}
