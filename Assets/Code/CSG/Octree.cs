using System;
using System.Collections.Generic;
using System.Linq;

namespace Csg
{
	public class Octree
	{
		public readonly OctreeNode RootNode;
		public static Octree Unit()
		{
			return new Octree(new BoundingBox(1, 1, 1), 2);
		}
		public Octree(BoundingBox bbox, int maxDepth)
		{
			RootNode = new OctreeNode(bbox, 0, maxDepth);
		}
		public List<OctreeNode> AllNodes
		{
			get
			{
				var s = new Stack<OctreeNode>();
				s.Push(RootNode);
				var r = new List<OctreeNode>();
				while (s.Count > 0)
				{
					var n = s.Pop();
					r.Add(n);
					if (n.Children != null)
					{
						foreach (var c in n.Children) s.Push(c);
					}
				}
				return r;
			}
		}
	}

	public class OctreeNode
	{
		public readonly BoundingBox BoundingBox;
		public readonly OctreeNode[] Children;
		public readonly List<PolygonTreeNode> Polygons = new List<PolygonTreeNode>();
		static readonly Vector3D[] coffsets = new[] {
			new Vector3D(0.25, 0.25, 0.25),
			new Vector3D(-0.25, 0.25, 0.25),
			new Vector3D(-0.25, -0.25, 0.25),
			new Vector3D(0.25, -0.25, 0.25),
			new Vector3D(0.25, 0.25, -0.25),
			new Vector3D(-0.25, 0.25, -0.25),
			new Vector3D(-0.25, -0.25, -0.25),
			new Vector3D(0.25, -0.25, -0.25),
		};
		public OctreeNode(BoundingBox bbox, int depth, int maxDepth)
		{
			BoundingBox = bbox;
			if (depth + 1 <= maxDepth)
			{
				Children = new OctreeNode[8];
				var cbbox = new BoundingBox(bbox.Size.X / 2, bbox.Size.X / 2, bbox.Size.X / 2) +
					bbox.Center;
				for (var i = 0; i < 8; i++)
				{
					Children[i] = new OctreeNode(cbbox + coffsets[i] * bbox.Size, depth + 1, maxDepth);
				}
			}
			else {
				Children = null;
			}
		}
		public void AddPolygon(PolygonTreeNode polygon)
		{
			this.Polygons.Add(polygon);
			if (Children == null) return;
			var pbox = polygon.BoundingBox;
			for (var i = 0; i < 8; i++)
			{
				if (Children[i].BoundingBox.Intersects(pbox))
				{
					Children[i].AddPolygon(polygon);
				}
			}
		}
		public void AddPolygons(List<PolygonTreeNode> polygons)
		{
			foreach (var p in polygons)
			{
				AddPolygon(p);
			}
		}
		//public override string ToString() => $"{Polygons.Count} @ [{BoundingBox}]";
	}
}
