using System;
using System.Collections.Generic;
using System.Linq;

namespace Csg
{
	public class Tree
	{
		PolygonTreeNode polygonTree;
		Node rootnode;

		public Node RootNode { get { return rootnode; } }

		public Tree(BoundingBox bbox, List<Polygon> polygons)
		{
			polygonTree = new PolygonTreeNode();
			rootnode = new Node(null);
			if (polygons != null) AddPolygons(polygons);
		}

		public void Invert()
		{
			polygonTree.Invert();
			rootnode.Invert();
		}

		public void ClipTo(Tree tree, bool alsoRemoveCoplanarFront = false)
		{
			rootnode.ClipTo(tree, alsoRemoveCoplanarFront);
		}

		public List<Polygon> AllPolygons()
		{
			var result = new List<Polygon>();
			polygonTree.GetPolygons(result);
			return result;
		}

		public void AddPolygons(List<Polygon> polygons)
		{
			var n = polygons.Count;
			var polygontreenodes = new PolygonTreeNodeList(n);
			for (var i = 0; i < n; i++)
			{
				var p = polygonTree.AddChild(polygons[i]);
				polygontreenodes.Add(p);
			}
			rootnode.AddPolygonTreeNodes(polygontreenodes);
		}
	}

	public class Node
	{
		public Plane Plane;
		public Node Front;
		public Node Back;
		public PolygonTreeNodeList PolygonTreeNodes;
		public readonly Node Parent;

		public Node(Node parent)
		{
			PolygonTreeNodes = new PolygonTreeNodeList();
			Parent = parent;
		}

		public void Invert()
		{
			var queue = new Queue<Node>();
			queue.Enqueue(this);
			while (queue.Count > 0)
			{
				var node = queue.Dequeue();
				if (node.Plane != null) node.Plane = node.Plane.Flipped();
				if (node.Front != null) queue.Enqueue(node.Front);
				if (node.Back != null) queue.Enqueue(node.Back);
				var temp = node.Front;
				node.Front = node.Back;
				node.Back = temp;
			}
		}

		public void ClipPolygons(PolygonTreeNodeList clippolygontreenodes, bool alsoRemoveCoplanarFront)
		{
			var args = new Args { Node = this, PolygonTreeNodes = clippolygontreenodes };
			Stack<Args> stack = null;

			while (args.Node != null)
			{
				var clippingNode = args.Node;
				var polygontreenodes = args.PolygonTreeNodes;

				if (clippingNode.Plane != null)
				{
					PolygonTreeNodeList backnodes = null;
					PolygonTreeNodeList frontnodes = null;
					var plane = clippingNode.Plane;
					var numpolygontreenodes = polygontreenodes.Count;
					for (var i = 0; i < numpolygontreenodes; i++)
					{
						var polyNode = polygontreenodes[i];
						if (!polyNode.IsRemoved)
						{
							if (alsoRemoveCoplanarFront)
							{
								polyNode.SplitByPlane(plane, ref backnodes, ref backnodes, ref frontnodes, ref backnodes);
							}
							else
							{
								polyNode.SplitByPlane(plane, ref frontnodes, ref backnodes, ref frontnodes, ref backnodes);
							}
						}
					}

					if (clippingNode.Front != null && (frontnodes != null))
					{
						if (stack == null) stack = new Stack<Args>();
						stack.Push(new Args { Node = clippingNode.Front, PolygonTreeNodes = frontnodes });
					}
					var numbacknodes = backnodes == null ? 0 : backnodes.Count;
					if (clippingNode.Back != null && (numbacknodes > 0))
					{
						if (stack == null) stack = new Stack<Args>();
						stack.Push(new Args { Node = clippingNode.Back, PolygonTreeNodes = backnodes });
					}
					else {
						// there's nothing behind this plane. Delete the nodes behind this plane:
						for (var i = 0; i < numbacknodes; i++)
						{
							backnodes[i].Remove();
						}
					}
				}
				if (stack != null && stack.Count > 0) args = stack.Pop();
				else args.Node = null;
			}
		}

		public void ClipTo(Tree clippingTree, bool alsoRemoveCoplanarFront)
		{
			var node = this;
			Stack<Node> stack = null;
			while (node != null)
			{
				if (node.PolygonTreeNodes.Count > 0)
				{
					clippingTree.RootNode.ClipPolygons(node.PolygonTreeNodes, alsoRemoveCoplanarFront);
				}
				if (node.Front != null)
				{
					if (stack == null) stack = new Stack<Node>();
					stack.Push(node.Front);
				}
				if (node.Back != null)
				{
					if (stack == null) stack = new Stack<Node>();
					stack.Push(node.Back);
				}
				node = (stack != null && stack.Count > 0) ? stack.Pop() : null;
			}
		}

		public void AddPolygonTreeNodes(PolygonTreeNodeList addpolygontreenodes)
		{
			var args = new Args { Node = this, PolygonTreeNodes = addpolygontreenodes };
			var stack = new Stack<Args>();
			while (args.Node != null)
			{
				var node = args.Node;
				var polygontreenodes = args.PolygonTreeNodes;

				if (polygontreenodes.Count == 0)
				{
					// Nothing to do
				}
				else {
					var _this = node;
					if (node.Plane == null)
					{
						var bestplane = polygontreenodes[0].GetPolygon().Plane;
						node.Plane = bestplane;
					}

					var frontnodes = new PolygonTreeNodeList();
					var backnodes = new PolygonTreeNodeList();

					for (int i = 0, n = polygontreenodes.Count; i < n; i++)
					{
						polygontreenodes[i].SplitByPlane(_this.Plane, ref _this.PolygonTreeNodes, ref backnodes, ref frontnodes, ref backnodes);
					}

					if (frontnodes.Count > 0)
					{
						if (node.Front == null) node.Front = new Node(node);
						stack.Push(new Args { Node = node.Front, PolygonTreeNodes = frontnodes });
					}
					if (backnodes.Count > 0)
					{
						if (node.Back == null) node.Back = new Node(node);
						stack.Push(new Args { Node = node.Back, PolygonTreeNodes = backnodes });
					}
				}

				if (stack.Count > 0) args = stack.Pop();
				else args.Node = null;
			}
		}
		struct Args
		{
			public Node Node;
			public PolygonTreeNodeList PolygonTreeNodes;
		}
	}

	public class PolygonTreeNode
	{
		PolygonTreeNode parent;
		PolygonTreeNodeList children;
		Polygon polygon;
		bool removed;

		public PolygonTreeNode()
		{
			parent = null;
			children = new PolygonTreeNodeList();
			polygon = null;
			removed = false;
		}

		public BoundingBox BoundingBox { get { if(polygon == null) return null; return polygon.BoundingBox; } }

		public void AddPolygons(List<Polygon> polygons)
		{
			if (!IsRootNode)
			{
				throw new InvalidOperationException("New polygons can only be added to  root nodes.");
			}
			for (var i = 0; i < polygons.Count; i++)
			{
				AddChild(polygons[i]);
			}
		}

		public void Remove()
		{
			if (!this.removed)
			{
				this.removed = true;

#if DEBUG
				if (this.IsRootNode) throw new InvalidOperationException("Can't remove root node");
				if (this.children.Count > 0) throw new InvalidOperationException("Can't remove nodes with children");
#endif

				// remove ourselves from the parent's children list:
				var parentschildren = this.parent.children;
				parentschildren.Remove(this);

				// invalidate the parent's polygon, and of all parents above it:
				this.parent.RecursivelyInvalidatePolygon();
			}
		}

		public bool IsRemoved { get { return removed; } }

		public bool IsRootNode { get { return parent == null; } }

		public void Invert()
		{
			if (!IsRootNode) throw new InvalidOperationException("Only the root nodes are invertable.");
			InvertSub();
		}

		public Polygon GetPolygon()
		{
			if (polygon == null) throw new InvalidOperationException("Node is not associated with a polygon.");
			return this.polygon;
		}

		public void GetPolygons(List<Polygon> result)
		{
			var queue = new Queue<PolygonTreeNodeList>();
			queue.Enqueue(new PolygonTreeNodeList(this));
			while (queue.Count > 0)
			{
				var children = queue.Dequeue();
				var l = children.Count;
				for (int j = 0; j < l; j++)
				{
					var node = children[j];
					if (node.polygon != null)
					{
						result.Add(node.polygon);
					}
					else {
						queue.Enqueue(node.children);
					}
				}
			}
		}

		public void SplitByPlane(Plane plane, ref PolygonTreeNodeList coplanarfrontnodes, ref PolygonTreeNodeList coplanarbacknodes, ref PolygonTreeNodeList frontnodes, ref PolygonTreeNodeList backnodes)
		{
			if (children.Count > 0)
			{
				var queue = new Queue<PolygonTreeNodeList>();
				queue.Enqueue(children);
				while (queue.Count > 0)
				{
					var nodes = queue.Dequeue();
					var l = nodes.Count;
					for (int j = 0; j < l; j++)
					{
						var node = nodes[j];
						if (node.children.Count > 0)
						{
							queue.Enqueue(node.children);
						}
						else {
							node.SplitPolygonByPlane(plane, ref coplanarfrontnodes, ref coplanarbacknodes, ref frontnodes, ref backnodes);
						}
					}
				}
			}
			else {
				SplitPolygonByPlane(plane, ref coplanarfrontnodes, ref coplanarbacknodes, ref frontnodes, ref backnodes);
			}
		}

		void SplitPolygonByPlane(Plane plane, ref PolygonTreeNodeList coplanarfrontnodes, ref PolygonTreeNodeList coplanarbacknodes, ref PolygonTreeNodeList frontnodes, ref PolygonTreeNodeList backnodes)
		{
			var polygon = this.polygon;
			if (polygon != null)
			{
				var bound = polygon.BoundingSphere;
				var sphereradius = bound.Radius + 1.0e-4;
				var planenormal = plane.Normal;
				var spherecenter = bound.Center;
				var d = planenormal.Dot(spherecenter) - plane.W;
				if (d > sphereradius)
				{
					if (frontnodes == null) frontnodes = new PolygonTreeNodeList();
					frontnodes.Add(this);
				}
				else if (d < -sphereradius)
				{
					if (backnodes == null) backnodes = new PolygonTreeNodeList();
					backnodes.Add(this);
				}
				else {
					SplitPolygonResult splitresult;
					plane.SplitPolygon(polygon, out splitresult);
					switch (splitresult.Type)
					{
						case 0:
							if (coplanarfrontnodes == null) coplanarfrontnodes = new PolygonTreeNodeList();
							coplanarfrontnodes.Add(this);
							break;
						case 1:
							if (coplanarbacknodes == null) coplanarbacknodes = new PolygonTreeNodeList();
							coplanarbacknodes.Add(this);
							break;
						case 2:
							if (frontnodes == null) frontnodes = new PolygonTreeNodeList();
							frontnodes.Add(this);
							break;
						case 3:
							if (backnodes == null) backnodes = new PolygonTreeNodeList();
							backnodes.Add(this);
							break;
						default:
							if (splitresult.Front != null)
							{
								var frontnode = AddChild(splitresult.Front);
								if (frontnodes == null) frontnodes = new PolygonTreeNodeList();
								frontnodes.Add(frontnode);
							}
							if (splitresult.Back != null)
							{
								var backnode = AddChild(splitresult.Back);
								if (backnodes == null) backnodes = new PolygonTreeNodeList();
								backnodes.Add(backnode);
							}
							break;
					}
				}
			}
		}

		public PolygonTreeNode AddChild(Polygon polygon)
		{
			var newchild = new PolygonTreeNode();
			newchild.parent = this;
			newchild.polygon = polygon;
			children.Add(newchild);
			return newchild;
		}

		void InvertSub()
		{
			var queue = new Queue<PolygonTreeNodeList>();
			queue.Enqueue(new PolygonTreeNodeList(this));
			while (queue.Count > 0)
			{
				var children = queue.Dequeue();
				var l = children.Count;
				for (int j = 0; j < l; j++)
				{
					var node = children[j];
					if (node.polygon != null)
					{
						node.polygon = node.polygon.Flipped();
					}
					queue.Enqueue(node.children);
				}
			}
		}

		void RecursivelyInvalidatePolygon()
		{
			var node = this;
			while (node.polygon != null)
			{
				node.polygon = null;
				if (node.parent != null)
				{
					node = node.parent;
				}
			}
		}
	}

	public class PolygonTreeNodeList
	{
		PolygonTreeNode singleton = null;
		List<PolygonTreeNode> store = null;

		public PolygonTreeNodeList()
		{
		}
		public PolygonTreeNodeList(PolygonTreeNode value)
		{
			singleton = value;
		}
		public PolygonTreeNodeList(int capacity)
		{
			if (capacity > 1)
			{
				store = new List<PolygonTreeNode>(capacity);
			}
		}

		public int Count
		{
			get
			{
				if (store != null) return store.Count;
				return singleton != null ? 1 : 0;
			}
		}
		public PolygonTreeNode this[int index]
		{
			get
			{
				if (store != null) return store[index];
				return singleton;
			}
		}

		public void Add(PolygonTreeNode value)
		{
			if (store != null)
			{
				store.Add(value);
			}
			else {
				if (singleton == null)
				{
					singleton = value;
				}
				else
				{
					store = new List<PolygonTreeNode>();
					store.Add(singleton);
					store.Add(value);
					singleton = null;
				}
			}
		}

		public void Remove(PolygonTreeNode value)
		{
			if (store != null)
			{
				store.Remove(value);
			}
			else {
				if (Object.ReferenceEquals(singleton, value))
				{
					singleton = null;
				}
			}
		}
	}
}

