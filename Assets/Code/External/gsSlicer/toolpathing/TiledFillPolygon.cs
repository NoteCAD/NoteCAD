using System;
using System.Collections.Generic;
using g3;

namespace gs
{
	public class TiledFillPolygon : ICurvesFillPolygon
    {
        public Func<GeneralPolygon2d, Vector2i, ICurvesFillPolygon> TileFillGeneratorF;

		// polygon to fill
		public GeneralPolygon2d Polygon { get; set; }

        public double TileSize = 10.0;    // mm
        public double TileOverlap = 0.0;  // mm


		// fill paths
		public List<FillCurveSet2d> FillCurves { get; set; }
        public List<FillCurveSet2d> GetFillCurves() { return FillCurves; }

        public TiledFillPolygon(GeneralPolygon2d poly)
		{
			Polygon = poly;
			FillCurves = new List<FillCurveSet2d>();
		}


        class Tile
        {
            public Vector2i index;
            public Polygon2d poly;
            public List<GeneralPolygon2d> regions;
            public ICurvesFillPolygon[] fills;
        }


		public bool Compute()
		{
            AxisAlignedBox2d bounds = Polygon.Bounds;

            ScaleGridIndexer2 index = new ScaleGridIndexer2() { CellSize = TileSize };

            Vector2i min = index.ToGrid(bounds.Min) - Vector2i.One;
            Vector2i max = index.ToGrid(bounds.Max);

            List<Tile> Tiles = new List<Tile>();

            for (int y = min.y; y <= max.y; ++y) {
                for (int x = min.x; x <= max.x; ++x) {
                    Tile t = new Tile();
                    t.index = new Vector2i(x, y);
                    Vector2d tile_min = index.FromGrid(t.index);
                    Vector2d tile_max = index.FromGrid(t.index + Vector2i.One);
                    AxisAlignedBox2d tile_box = new AxisAlignedBox2d(tile_min, tile_max);
                    tile_box.Expand(TileOverlap);
                    t.poly = new Polygon2d(new Vector2d[] { tile_box.GetCorner(0), tile_box.GetCorner(1), tile_box.GetCorner(2), tile_box.GetCorner(3) });
                    Tiles.Add(t);
                }
            }


            gParallel.ForEach(Tiles, (tile) => {
                tile.regions =
                    ClipperUtil.PolygonBoolean(Polygon, new GeneralPolygon2d(tile.poly), ClipperUtil.BooleanOp.Intersection);
            });


            List<ICurvesFillPolygon> all_fills = new List<ICurvesFillPolygon>();

            foreach ( Tile t in Tiles ) {
                if (t.regions.Count == 0)
                    continue;
                t.fills = new ICurvesFillPolygon[t.regions.Count];
                for ( int k = 0; k < t.regions.Count; ++k) {
                    t.fills[k] = TileFillGeneratorF(t.regions[k], t.index);
                    if ( t.fills[k] != null )
                        all_fills.Add(t.fills[k]);
                }
            }


            gParallel.ForEach(all_fills, (fill) => {
                fill.Compute();
            });


            FillCurves = new List<FillCurveSet2d>();
            foreach (ICurvesFillPolygon fill in all_fills) {
                List<FillCurveSet2d> result = fill.GetFillCurves();
                if (result != null && result.Count > 0)
                    FillCurves.AddRange(result);
            }


            return true;
		}

        
        

	}
}
