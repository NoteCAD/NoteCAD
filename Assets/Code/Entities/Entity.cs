using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Xml;
using System;

public interface IEntity : ICADObject {
	IEnumerable<ExpVector> points { get; }			// enough for dragging
	IEnumerable<Vector3> segments { get; }			// enough for drawing
	ExpVector PointOn(Exp t);						// enough for constraining
	IPlane plane { get; }
}

public static class IEntityUtils {

	public static ExpVector PointExpInPlane(this IEntity entity, IPlane plane) {
		return entity.PointsInPlane(plane).Single();
	}

	public static IEnumerable<ExpVector> PointsInPlane(this IEntity entity, IPlane plane) {
		if(plane == entity.plane) {
			return entity.points;
		}
		return entity.points.Select(p => plane.ToFrom(p, entity.plane));
	}

	public static IEnumerable<Vector3> SegmentsInPlane(this IEntity entity, IPlane plane) {
		return plane.ToFrom(entity.segments, entity.plane);
	}

	public static bool IsPoint(this IEntity entity) {
		return entity.segments.Count() == 1;
	}

	public static ExpVector PointOnInPlane(this IEntity entity, Exp t, IPlane plane) {
		if(plane == entity.plane) {
			return entity.PointOn(t);
		}
		return plane.ToFrom(entity.PointOn(t), entity.plane);
	}

	public static ExpVector GetDirectionInPlane(this IEntity entity, IPlane plane) {
		var points = entity.points.GetEnumerator();
		points.MoveNext();
		var p0 = plane.ToFrom(points.Current, entity.plane);
		points.MoveNext();
		var p1 = plane.ToFrom(points.Current, entity.plane);
		return p1 - p0;
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

	public IPlane plane {
		get {
			return sketch.plane;
		}
	}

	IEnumerable<ExpVector> IEntity.points {
		get {
			return points.Select(p => p.exp);
		}
	}

	public virtual IEnumerable<Vector3> segments {
		get {
			if(this is ISegmentaryEntity) return (this as ISegmentaryEntity).segmentPoints;
			if(this is ILoopEntity) return (this as ILoopEntity).loopPoints;
			return Enumerable.Empty<Vector3>();
		}
	}

	public virtual /*abstract*/ ExpVector PointOn(Exp t) {
		return new ExpVector(0, 0, 0);
	}

	protected T AddChild<T>(T e) where T : Entity {
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
		GameObject.Destroy(gameObject);
	}

	public override void Write(XmlTextWriter xml) {
		xml.WriteStartElement("entity");
		xml.WriteAttributeString("type", this.GetType().Name);
		base.Write(xml);
		if(children.Count > 0) {
			foreach(var c in children) {
				c.Write(xml);
			}
		}
		xml.WriteEndElement();
	}

	public override void Read(XmlNode xml) {
		base.Read(xml);
		int i = 0;
		foreach(XmlNode xmlChild in xml.ChildNodes) {
			children[i].Read(xmlChild);
			i++;
		}
	}

	public virtual bool IsCrossed(Entity e, ref Vector3 itr) {
		if(!e.bbox.Overlaps(bbox)) return false;
		if(this is ISegmentaryEntity && e is ISegmentaryEntity) {
			var self = this as ISegmentaryEntity;
			var entity = e as ISegmentaryEntity;

			Vector3 selfPrev = Vector3.zero;
			bool selfFirst = true;
			foreach(var sp in self.segmentPoints) {
				if(!selfFirst) {
					Vector3 otherPrev = Vector3.zero;
					bool otherFirst = true;
					foreach(var ep in entity.segmentPoints) {
						if(!otherFirst) {
							if(GeomUtils.isSegmentsCrossed(selfPrev, sp, otherPrev, ep, ref itr, 1e-6f) == GeomUtils.Cross.INTERSECTION) {
								return true;
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
		return false;
	}

	public void ForEachSegment(Action<Vector3, Vector3> action) {
		IEnumerable<Vector3> points = null;
		if(this is ISegmentaryEntity) points = (this as ISegmentaryEntity).segmentPoints;
		if(this is ILoopEntity) points = (this as ILoopEntity).loopPoints;
		if(points == null) return;
		Vector3 prev = Vector3.zero;
		bool first = true;
		foreach(var ep in points) {
			if(!first) {
				action(prev, ep);
			}
			first = false;
			prev = ep;
		}
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
		return OnSplit(position);
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
		});
		return minDist;
	}

	protected override void OnDraw(LineCanvas canvas) {
		ForEachSegment((a, b) => {
			canvas.DrawLine(a, b);
		});
	}

}

public interface ISegmentaryEntity {
	PointEntity begin { get; }
	PointEntity end { get; }
	IEnumerable<Vector3> segmentPoints { get; }
}

public interface ILoopEntity {
	IEnumerable<Vector3> loopPoints { get; }
}