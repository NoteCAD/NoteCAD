using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using g3;

namespace gs
{
    public interface ILayerShellsSelector
    {
        IShellsFillPolygon Next(Vector2d currentPosition);
    }


    public class InOrderShellSelector : ILayerShellsSelector
    {
        public List<IShellsFillPolygon> LayerShells;
        int iCurrent;

        public InOrderShellSelector(List<IShellsFillPolygon> shells)
        {
            LayerShells = shells;
            iCurrent = 0;
        }

        public IShellsFillPolygon Next(Vector2d currentPosition)
        {
            if (iCurrent < LayerShells.Count)
                return LayerShells[iCurrent++];
            else
                return null;
        }
    }




    public class NextNearestLayerShellsSelector : ILayerShellsSelector
    {
        public List<IShellsFillPolygon> LayerShells;
        HashSet<IShellsFillPolygon> remaining;

        public NextNearestLayerShellsSelector(List<IShellsFillPolygon> shells)
        {
            LayerShells = shells;
            remaining = new HashSet<IShellsFillPolygon>(shells);
        }

        public IShellsFillPolygon Next(Vector2d currentPosition)
        {
            if (remaining.Count == 0)
                return null;

            IShellsFillPolygon nearest = null;
            double nearest_dist = double.MaxValue;
            foreach (IShellsFillPolygon shell in remaining) {
                double dist = shell.Polygon.Outer.DistanceSquared(currentPosition);
                if ( dist < nearest_dist) {
                    nearest_dist = dist;
                    nearest = shell;
                }
            }
            remaining.Remove(nearest);
            return nearest;
        }
    }

}
