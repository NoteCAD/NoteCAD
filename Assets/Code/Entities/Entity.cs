using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Xml;
using System;

namespace NoteCAD {

	public enum IEntityType {
		Point,
		Line,
		Arc,
		Circle,
		Helix,
		Plane,
		Function,
		Spline,
		Ellipse,
		EllipticArc,
		Sketch,
		Offset
	}

	public interface IEntity : ICADObject {
		IEnumerable<ExpVector> points { get; }			// enough for dragging
		IEnumerable<IEnumerable<Vector3>> segments { get; }			// enough for drawing
		ExpVector PointOn(Exp t);						// enough for constraining
		ExpVector TangentAt(Exp t);
		Exp Length();
		Exp Radius();
		ExpVector Center();
		IPlane plane { get; }
		IEntityType type { get; }
		//Style style { get; }
	}

	public static class IEntityUtils {

		public static ExpVector NormalAt(this IEntity self, Exp t) {
			return self.NormalAtInPlane(t, self.plane);
		}

		public static ExpVector NormalAtInPlane(this IEntity self, Exp t, IPlane plane) {
			if(self.plane != null) {
				var tang = self.TangentAt(t);
				if(tang == null) return null;
				var n = ExpVector.Cross(tang, Vector3.forward);
				if(plane == self.plane) return n;
				return plane.DirToFrom(n, self.plane);
			}

			Param p = new Param("pOn");
			var pt = self.PointOn(p);
			var result = new ExpVector(pt.x.Deriv(p).Deriv(p), pt.y.Deriv(p).Deriv(p), pt.z.Deriv(p).Deriv(p));
			result.x.Substitute(p, t);
			result.y.Substitute(p, t);
			result.z.Substitute(p, t);
			if(plane == null) return result;
			return plane.DirToPlane(result);
		}

		public static bool IsCircular(this IEntity e) {
			return e.Radius() != null && e.Center() != null;
		}

		public static bool IsSameAs(this IEntity e0, IEntity e1) {
			if(e0 == null) return e1 == null;
			if(e1 == null) return e0 == null;
			return e0 == e1 || e0.type == e1.type && e0.id == e1.id;
		}

		public static ExpVector PointExpInPlane(this IEntity entity, IPlane plane) {
			var it = entity.PointsInPlane(plane).GetEnumerator();
			it.MoveNext();
			return it.Current;
			//return entity.PointsInPlane(plane).Single();
		}

		public static Vector3 GetPointPos(this IEntity entity, IPlane plane = null) {
			var it = entity.PointsInPlane(plane).GetEnumerator();
			it.MoveNext();
			return it.Current.Eval();
			//return entity.PointsInPlane(plane).Single();
		}

		public static ExpVector CenterInPlane(this IEntity entity, IPlane plane) {
			var c = entity.Center();
			if(c == null) return null;
			return plane.ToFrom(c, entity.plane);
		}

		public static IEnumerable<ExpVector> PointsInPlane(this IEntity entity, IPlane plane) {
			if(plane == entity.plane) {
				for(var it = entity.points.GetEnumerator(); it.MoveNext();) {
					yield return it.Current;
				}
			}
			for(var it = entity.points.GetEnumerator(); it.MoveNext();) {
				yield return plane.ToFrom(it.Current, entity.plane);
			}
		}

		public static IEnumerable<IEnumerable<Vector3>> SegmentsInPlane(this IEntity entity, IPlane plane) {
			foreach(var lp in entity.segments) {
				yield return plane.ToFrom(lp, entity.plane);
			}
		}

		public static ExpVector PointOnInPlane(this IEntity entity, Exp t, IPlane plane) {
			if(plane == entity.plane) {
				return entity.PointOn(t);
			}
			return plane.ToFrom(entity.PointOn(t), entity.plane);
		}

		public static ExpVector TangentAtInPlane(this IEntity entity, Exp t, IPlane plane) {
			if(plane == entity.plane) {
				return entity.TangentAt(t);
			}
			return plane.DirToFrom(entity.TangentAt(t), entity.plane);
		}

		public static ExpVector OffsetAtInPlane(this IEntity e, Exp t, Exp offset, IPlane plane) {
			if(plane == e.plane) {
				return e.PointOn(t) + e.NormalAt(t).Normalized() * offset;
			}
			return e.PointOnInPlane(t, plane) + e.NormalAtInPlane(t, plane).Normalized() * offset;
		}

		public static ExpVector GetDirectionInPlane(this IEntity entity, IPlane plane) {
			var points = entity.points.GetEnumerator();
			points.MoveNext();
			var p0 = plane.ToFrom(points.Current, entity.plane);
			points.MoveNext();
			var p1 = plane.ToFrom(points.Current, entity.plane);
			return p1 - p0;
		}

		public static ExpVector GetPointAtInPlane(this IEntity entity, int index, IPlane plane) {
			var points = entity.points.GetEnumerator();
			int curIndex = -1;
			while(curIndex++ < index && points.MoveNext());
			return plane.ToFrom(points.Current, entity.plane);
		}

		public static Vector3 GetPointPosAtInPlane(this IEntity entity, int index, IPlane plane) {
			var points = entity.points.GetEnumerator();
			int curIndex = -1;
			while(curIndex++ < index && points.MoveNext());
			return plane.ToFrom(points.Current.Eval(), entity.plane);
		}

		public static ExpVector GetLineP0(this IEntity entity, IPlane plane) {
			var points = entity.points.GetEnumerator();
			points.MoveNext();
			return plane.ToFrom(points.Current, entity.plane);
		}

		public static ExpVector GetLineP1(this IEntity entity, IPlane plane) {
			var points = entity.points.GetEnumerator();
			points.MoveNext();
			points.MoveNext();
			return plane.ToFrom(points.Current, entity.plane);

		}

		public static void ForEachSegment(IEnumerable<Vector3> points, Action<Vector3, Vector3> action) {
			bool first = true;
			Vector3 prev = Vector3.zero;
			foreach(var ep in points) {
				if(!first) {
					action(prev, ep);
				}
				first = false;
				prev = ep;
			}
		}
		public static void ForEachSegment(this IEntity entity, Action<Vector3, Vector3> action) {
			foreach(var lp in entity.segments) {
				ForEachSegment(lp, action);
			}
		}

		public static double Hover(this IEntity entity, Vector3 mouse, Camera camera, Matrix4x4 tf) {
			if(entity.type == IEntityType.Point) return PointEntity.IsSelected(entity.PointExpInPlane(null).Eval(), mouse, camera, tf);
			double minDist = -1.0;
			entity.ForEachSegment((a, b) => {
				var ap = camera.WorldToScreenPoint(tf.MultiplyPoint(a));
				var bp = camera.WorldToScreenPoint(tf.MultiplyPoint(b));
				var dist = Mathf.Abs(GeomUtils.DistancePointSegment2D(mouse, ap, bp));
				if(minDist < 0.0 || dist < minDist) {
					minDist = dist;
				}
			});
			return minDist;
		}

		public static ExpVector OffsetAt(this IEntity e, Exp t, Exp offset) {
			return e.PointOn(t) + e.NormalAt(t).Normalized() * offset;
		}

		public static ExpVector OffsetTangentAt(this IEntity e, Exp t, Exp offset) {
			Param p = new Param("pOn");
			var pt = e.OffsetAt(p, offset);
			var result = new ExpVector(pt.x.Deriv(p), pt.y.Deriv(p), pt.z.Deriv(p));
			result.x.Substitute(p, t);
			result.y.Substitute(p, t);
			result.z.Substitute(p, t);
			return result;
		}

		public static void DrawParamRange(this IEntity e, ICanvas canvas, double offset, double begin, double end, double step, IPlane plane) {
			Vector3 prev = Vector3.zero;
			bool first = true;
			int count = (int)Math.Ceiling(Math.Abs(end - begin) / step);
			Param t = new Param("t");
			var PointOn = e.OffsetAtInPlane(t, offset, plane);
			for(int i = 0; i <= count; i++) {
				t.value = begin + (end - begin) * i / count;
				var p = PointOn.Eval();
				if(!first) {
					canvas.DrawLine(prev, p);
				}
				first = false;
				prev = p;
			}
		}

		public static void DrawExtend(this IEntity e, ICanvas canvas, double t, double step) {
			if(t < 0.0) {
				e.DrawParamRange(canvas, 0.0, t, 0.0, step, null);
			} else
			if(t > 1.0) {
				e.DrawParamRange(canvas, 0.0, 1.0, t, step, null);
			}
		}

	}

	public abstract partial class Entity : SketchObject, IEntity {

		protected List<Constraint> usedInConstraints = new List<Constraint>();
		List<Entity> children = new List<Entity>();
		public Entity parent { get; private set; }
		public Func<ExpVector, ExpVector> transform = null;
		public IEnumerable<Constraint> constraints { get { return usedInConstraints.AsEnumerable(); } }
		public virtual IEnumerable<PointEntity> points { get { yield break; } }
		public virtual BBox bbox { get { return new BBox(Vector3.zero, Vector3.zero); } }
		public abstract IEntityType type { get; }

		public bool isConstruction {
			get {
				return style == null ? false : style.construction;
			}
		}

		public IPlane plane {
			get {
				return sketch.plane;
			}
		}

		public int GetChildrenCount() {
			return children.Count;
		}

		IEnumerable<ExpVector> IEntity.points {
			get {
				for(var it = points.GetEnumerator(); it.MoveNext(); ) {
					yield return it.Current.exp;
				}
			}
		}

		public virtual IEnumerable<IEnumerable<Vector3>> segments {
			get {
				if(this is ISegmentaryEntity) return (this as ISegmentaryEntity).segmentPoints;
				if(this is ILoopEntity) return (this as ILoopEntity).loopPoints;
				return Enumerable.Empty<IEnumerable<Vector3>>();
			}
		}

		protected IEnumerable<Vector3> getSegmentsUsingPointOn(int subdiv) {
			Param pOn = new Param("pOn");
			var on = PointOn(pOn);
			for(int i = 0; i <= subdiv; i++) {
				pOn.value = (double)i / subdiv;
				yield return on.Eval();
			}
		}

		protected IEnumerable<Vector3> getSegments(int subdiv, Func<double, Vector3> pointOn) {
			for (int i = 0; i <= subdiv; i++) {
				yield return pointOn((double)i / subdiv);
			}
		}

		public abstract ExpVector PointOn(Exp t);

		public T AddChild<T>(T e) where T : Entity {
			children.Add(e);
			e.parent = this;
			return e;
		}

		public Entity(Sketch sketch) : base(sketch) {
			sketch.AddEntity(this);
		}

		protected override void OnDrag(Vector3 delta) {
			foreach(var p in points) {
				p.Drag(delta);
			}
		}

		public override void Destroy() {
			if(isDestroyed) return;
			while(usedInConstraints.Count > 0) {
				usedInConstraints[0].Destroy();
			}
			base.Destroy();
			if(parent != null) {
				parent.Destroy();
			}
			while(children.Count > 0) {
				children[0].Destroy();
				children.RemoveAt(0);
			}
		}

		public override void Write(Writer xml) {
			xml.WriteBeginArrayElement("entity");
			xml.WriteAttribute("type", this.GetType().Name);
			base.Write(xml);
			xml.WriteBeginFakeArray("children");
			if(children.Count > 0) {
				foreach(var c in children) {
					c.Write(xml);
				}
			}
			xml.WriteEndFakeArray();
			xml.WriteEndArrayElement();
		}

		public override void Read(XmlNode xml) {
			base.Read(xml);
			int i = 0;
			foreach(XmlNode xmlChild in xml.ChildNodes) {
				if(children.Count <= i) {
					var type = xmlChild.Attributes["type"].Value;
					AddChild(New(type, sketch));
				}
				children[i].Read(xmlChild);
				i++;
			}
		}

		public IEnumerable<Vector3> GetIntersections(Entity e, bool refine = false, bool includeTouches = false) {
			var boxZero = new BBox(Vector3.zero, Vector3.zero);
			if(!e.bbox.Overlaps(bbox) && !e.bbox.Equals(boxZero) && !bbox.Equals(boxZero)) {
				yield break;
			}
			if(this is ISegmentProvider && e is ISegmentProvider) {
				var self = this as ISegmentProvider;
				var entity = e as ISegmentProvider;

				Vector3 selfPrev = Vector3.zero;
				bool selfFirst = true;
				foreach(var spl in self.segmentPoints) foreach(var sp in spl) {
					if(!selfFirst) {
						Vector3 otherPrev = Vector3.zero;
						bool otherFirst = true;
						foreach(var lp in entity.segmentPoints) foreach(var ep in lp) {
							if(!otherFirst) {
								Vector3 itr = Vector3.zero;
								var cross = GeomUtils.isSegmentsCrossed(selfPrev, sp, otherPrev, ep, ref itr, 1e-6f);
								if(cross == GeomUtils.Cross.INTERSECTION || includeTouches && cross == GeomUtils.Cross.TOUCH) {
									// this is located here because we also want to check self intersection, but don't want to waste
									// time checking it every segment for checking different entities intersection
									if(this as Entity == e && selfPrev == otherPrev && sp == ep) continue;
									if (refine && !RefineIntersection(this, e, ref itr)) {
										// no actual intesection happens?
										continue;
									}
									yield return itr;
								}
							}
							otherFirst = false;
							otherPrev = ep;
						}
					}
					selfFirst = false;
					selfPrev = sp;
				}
			}
		}

		public bool ForEachSegment(Func<Vector3, Vector3, bool> action) {
			foreach(var lp in segments) {
				if(!ForEachSegment(lp, action)) return false;
			}
			return true;
		}

		public static bool ForEachSegment(IEnumerable<Vector3> points, Func<Vector3, Vector3, bool> action) {
			if(points == null) return true;
			Vector3 prev = Vector3.zero;
			bool first = true;
			foreach(var ep in points) {
				if(!first) {
					if(!action(prev, ep)) {
						return false;
					}
				}
				first = false;
				prev = ep;
			}
			return true;
		}

		public bool IsEnding(PointEntity p) {
			if(!(this is ISegmentaryEntity)) return false;
			var se = this as ISegmentaryEntity;
			return se.begin == p || se.end == p;
		}

		protected virtual Entity OnSplit(Vector3 position) {
			return null;
		}

		public Entity Split(Vector3 position) {
			var part = OnSplit(position);
			if(part != null) {
				if(style != null) part.style = style;
				if(this is ISegmentaryEntity && part is ISegmentaryEntity) {
					sketch.ReplaceEntityInConstraints((this as ISegmentaryEntity).end, (part as ISegmentaryEntity).end);
				}
			}
			return part;
		}

		public virtual double FindParameter(Vector3 pos, int subdiv = 32) {
			Param pOn = new Param("pOn");
			var on = PointOn(pOn);
			double best_t = 0.0;
			double best_dist = double.MaxValue;
			for(int i = 0; i <= subdiv; i++) {
				pOn.value = (double)i / subdiv;
				var p = on.Eval();
				var d = (p - pos).sqrMagnitude;
				if(d < best_dist) {
					best_dist = d;
					best_t = pOn.value;
				}
			}
			double lo = Math.Max(0.0, best_t - 1.0 / subdiv);
			double hi = Math.Min(1.0, best_t + 1.0 / subdiv);
			for(int iter = 0; iter < 20; iter++) {
				double tl = lo + (hi - lo) / 3.0;
				double tr = lo + 2.0 * (hi - lo) / 3.0;
				pOn.value = tl;
				double dl = (on.Eval() - pos).sqrMagnitude;
				pOn.value = tr;
				double dr = (on.Eval() - pos).sqrMagnitude;
				if(dl < dr) {
					hi = tr;
				} else {
					lo = tl;
				}
				if (Math.Abs(dl - dr) < GaussianMethod.epsilon) {
					break;
				}

			}
			return (lo + hi) / 2.0;
		}

		// Refine a rough segment-segment intersection using EquationSystem Newton steps.
		// Solves entityA.PointOn(sa) == entityB.PointOn(tb) with 2 parameters and 2 equations.
		// revertWhenNotConverged=false keeps the last Newton iterate (closest approach) rather than
		// snapping back to the rough initial point when the solver hasn't fully converged.
		static private bool RefineIntersection(Entity entityA, Entity entityB, ref Vector3 roughPt) {
			Param sa = new Param("sa");
			Param tb = new Param("tb");
			sa.value = entityA.FindParameter(roughPt);
			tb.value = entityB.FindParameter(roughPt);
			var ptA = entityA.PointOn(sa);
			var ptB = entityB.PointOn(tb);
			var diff = ptA - ptB;
			var sys = new EquationSystem();
			sys.revertWhenNotConverged = false;
			sys.AddParameter(sa);
			sys.AddParameter(tb);
			sys.AddEquation(diff.x);
			sys.AddEquation(diff.y);
			var result = sys.Solve() == EquationSystem.SolveResult.OKAY;
			if(result) {
				roughPt.x = (float)ptA.x.Eval();
				roughPt.y = (float)ptA.y.Eval();
			}
			return result;
		}

		protected override double OnSelect(Vector3 mouse, Camera camera, Matrix4x4 tf) {
			double minDist = -1.0;
			ForEachSegment((a, b) => {
				var ap = camera.WorldToScreenPoint(tf.MultiplyPoint(a));
				var bp = camera.WorldToScreenPoint(tf.MultiplyPoint(b));
				var dist = Mathf.Abs(GeomUtils.DistancePointSegment2D(mouse, ap, bp));
				if(minDist < 0.0 || dist < minDist) {
					minDist = dist;
				}
				return true;
			});
			return minDist;
		}

		protected static bool MarqueeSelectSegment(Rect rect, bool wholeObject, Vector3 ap, Vector3 bp) {
			if(wholeObject) {
				if(rect.Contains(ap) && rect.Contains(bp)) {
					return true;
				}
			} else {
				if(rect.Contains(ap) || rect.Contains(bp)) {
					return true;
				}
				var line = Rect.MinMaxRect(
					Mathf.Min(ap.x, bp.x),
					Mathf.Min(ap.y, bp.y),
					Mathf.Max(ap.x, bp.x),
					Mathf.Max(ap.y, bp.y)
				);
				if(!rect.Overlaps(line)) {
					return false;
				}
				Vector3 res = Vector3.zero;
				Vector3[] points = {
					new Vector3(rect.xMin, rect.yMin),
					new Vector3(rect.xMax, rect.yMin),
					new Vector3(rect.xMax, rect.yMax),
					new Vector3(rect.xMin, rect.yMax)
				};
				for(int i = 0; i < points.Length; i++) {
					if(GeomUtils.isSegmentsCrossed(ap, bp, points[i], points[(i + 1) % points.Length], ref res, 1e-6f) == GeomUtils.Cross.INTERSECTION) {
						return true;
					}
				}
				return false;
			}
			return false;
		}

		protected override bool OnMarqueeSelect(Rect rect, bool wholeObject, Camera camera, Matrix4x4 tf) {
			var any = false;
			var whole = true;
			ForEachSegment((a, b) => {
				Vector2 ap = camera.WorldToScreenPoint(tf.MultiplyPoint(a));
				Vector2 bp = camera.WorldToScreenPoint(tf.MultiplyPoint(b));
				var segSelected = MarqueeSelectSegment(rect, wholeObject, ap, bp);
				any = any || segSelected;
				if(!wholeObject && any) {
					// break for each loop
					return false;
				}
				whole = whole && segSelected;
				if(wholeObject && !whole) {
					// break for each loop
					return false;
				}
				// continue for each loop
				return true;
			});
			return wholeObject && whole || !wholeObject && any;
		}

		protected override void OnDraw(ICanvas canvas) {
			if(isError) {
				canvas.SetStyle("error");
			} else {
				if(style == null) {
					canvas.SetStyle("entities");
				} else {
					canvas.SetStyle(style);
				}
			}
			ForEachSegment((a, b) => {
				canvas.DrawLine(a, b);
				return true;
			});
		}

		public virtual ExpVector TangentAt(Exp t) {
			Param p = new Param("pOn");
			var pt = PointOn(p);
			var result = new ExpVector(pt.x.Deriv(p), pt.y.Deriv(p), pt.z.Deriv(p));
			result.x.Substitute(p, t);
			result.y.Substitute(p, t);
			result.z.Substitute(p, t);
			return result;
		}

		public abstract Exp Length();
		public abstract Exp Radius();

		public virtual ExpVector Center() {
			return null;
		}

		public static Entity New(string typeName, Sketch sk) {
			Type[] types = { typeof(Sketch) };
			object[] param = { sk };
			var type = Type.GetType(typeName);
			if(type == null) {
				type = Type.GetType("NoteCAD." + typeName);
			}
			if(type == null) {
				Debug.LogError("Can't create entity of type " + typeName);
				return null;
			}
			return type.GetConstructor(types).Invoke(param) as Entity;
		}

	}

	public interface ISegmentProvider {
		IEnumerable<IEnumerable<Vector3>> segmentPoints { get; }
	}

	public interface ISegmentaryEntity : ISegmentProvider {
		PointEntity begin { get; }
		PointEntity end { get; }
	}

	public interface ILoopEntity : ISegmentProvider {
		IEnumerable<IEnumerable<Vector3>> loopPoints { get; }
	}
}
