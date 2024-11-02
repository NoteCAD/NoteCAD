using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LineTool : Tool {

	enum SnapType
	{
		None,
		Horizontal,
		Vertical,
		Parallel,
		ParallelOther,
		Perpendicular,
	}

	Constraint snapConstraint;
	Constraint displayLink;
	PointsDistance dimension;

	SnapType snapType;

	LineEntity current;
	LineEntity prev;
	bool canCreate = true;

	LineTool() {
		enableHoverFilter = true;
	}

	protected override bool OnTryHover(Constraint c) {
		return false;
	}

	protected override bool OnTryHover(IEntity e) {
		if(current != null && current.p0.IsCoincidentWith(e)) return false;
		return CanConstrainCoincident(e);
	}

	protected override void OnMouseDown(Vector3 pos, ICADObject sko) {

		if(current != null) {
			//if(!canCreate) return;
			setCurrentPosition(pos, sko);
			current.p1.isSelectable = true;
			current.p0.isSelectable = true;
			current.isSelectable = true;
			prev = current;
			displayLink?.Destroy();
			displayLink = null;
			dimension?.Destroy();
			dimension = null;
			if (snapConstraint != null) {
				snapConstraint.enabled = true;
				snapConstraint.drawLink = false;
				snapConstraint = null;
				snapType = SnapType.None;
			}
			if(AutoConstrainCoincident(current.p1, sko as IEntity)) {
				current = null;
				StopTool();
				return;
			}
		}
		var sk = DetailEditor.instance.currentSketch;
		if(sk == null) return;
		editor.PushUndo();
		var newLine = SpawnEntity(new LineEntity(sk.GetSketch()));
		newLine.p0.SetPosition(pos);
		newLine.p1.SetPosition(pos);
		if (editor.GetDetail().settings.drawingDimensions) {
			dimension = new PointsDistance(newLine.sketch, newLine.p0, newLine.p1);
			dimension.labelY = 0.001f;
			dimension.enabled = false;
		}
		if(current == null) {
			AutoConstrainCoincident(newLine.p0, sko as IEntity);
		} else {
			newLine.p0.pos = current.p1.pos;
			new PointsCoincident(current.sketch, current.p1, newLine.p0);
		}

		current = newLine;
		current.isSelectable = false;
		current.p0.isSelectable = false;
		current.p1.isSelectable = false;
	}

	class SnapData {
		public SnapType type = SnapType.None;
		public Vector3 dir;
		public IEntity entity;
		public double dist;

		public SnapData(SnapType type, Vector3 dir, IEntity entity = null, double dist = 0) {
			this.type = type;
			this.dir = dir;
			this.entity = entity;
			this.dist = dist;
		}

		public static bool checkDirAngle(Vector3 dir0, Vector3 dir1, out float diff) {
			var angle = Mathf.Abs(GeomUtils.GetAngle(dir0, dir1)) * Mathf.Rad2Deg;
		
			diff = angle;
			if(diff < 3f) {
				return true;
			}
			diff = Mathf.Abs(angle - 180f);
			if(diff < 3f) {
				return true;
			}
			return false;
		}

		public static bool checkPointLine(Vector3 pt0, Vector3 pt1, Vector3 dir1, out float diff) {
			diff = Mathf.Abs(GeomUtils.DistancePointLine2D(pt0, pt1, pt1 + dir1));
			return diff < Sketch.hoverRadius * Constraint.getPixelSize();
		}

		public bool check(Vector3 prev, Vector3 mouse, out float diff) {
			var curDir = mouse - prev;
			if(!checkDirAngle(curDir, dir, out diff)) {
				return false;
			}
			var res = checkPointLine(mouse, prev, dir, out diff);
			if(!res) {
				return false;
			}
			if (type == SnapType.Horizontal || type == SnapType.Vertical) {
				diff -= 0.2f;
			}
			if (type == SnapType.Parallel || type == SnapType.Perpendicular) {
				diff -= 0.1f;
			}
			return true;
		}

	}

	Constraint createSnapConstraint(SnapData snap) {
		switch(snap.type) {
			case SnapType.Horizontal: {
				var c = new HVConstraint(current.sketch, current);
				c.orientation = HVOrientation.OY;
				return c;
			}
			case SnapType.Vertical: {
				var c = new HVConstraint(current.sketch, current);
				c.orientation = HVOrientation.OX;
				return c;
			}
			case SnapType.Parallel: {
				return new Parallel(current.sketch, current, prev);
			}
			case SnapType.Perpendicular: {
				return new Perpendicular(current.sketch, current, prev);
			}
			case SnapType.ParallelOther: {
				var c = new Parallel(current.sketch, current, snap.entity);
				c.drawLink = true;
				return c;
			}
		}
		return null;
	}
	
	void setCurrentPosition(Vector3 pos, ICADObject entity)
	{
		if(current == null) {
			return;
		}
		
		var newPos = pos;
		Constraint newSnapConstraint = null;
		displayLink?.Destroy();
		displayLink = null;
		SnapData snap = null;
		var sk = DetailEditor.instance.currentSketch.GetSketch();
		
		if (editor.GetDetail().settings.autoconstraining && !sk.is3d) {
			var snaps = new List<SnapData>();

			// snap to horizontality / verticality
			snaps.Add(new SnapData(SnapType.Horizontal, Vector3.right));
			snaps.Add(new SnapData(SnapType.Vertical, Vector3.up));
		
			// snap to previous segemnt
			if(prev != null) {	
				var pdir = prev.p1.GetPosition() - prev.p0.GetPosition();
				snaps.Add(new SnapData(SnapType.Parallel, pdir));
				var pperp = new Vector3(-pdir.y, pdir.x, pdir.z);
				snaps.Add(new SnapData(SnapType.Perpendicular, pperp));
			}
		
			// snap parallelity to existing sketch segments
			var dirs = new Dictionary<Vector3, SnapData>();
			foreach(var e in current.sketch.entityList) {
				if(e.type != IEntityType.Line) {
					continue;
				}
				if(e == current) {
					continue;
				}
				var eP0 = e.GetLineP0(null).Eval();
				var eDir = e.GetLineP1(null).Eval() - eP0;
				if (eDir == Vector3.zero) {
					continue;
				}
				var eDirN = eDir.normalized;
				if(dirs.TryGetValue(eDirN, out var dir) || dirs.TryGetValue(-eDirN, out dir)) {
					var ePos = eP0 + eDir / 2f;
					var eDist = (pos - ePos).sqrMagnitude;
					if (dir.dist < eDist) {
						continue;
					} else {
						dir.dist = eDist;
						dir.entity = e;
					}
				} else {
					dirs.Add(eDirN, new SnapData(SnapType.ParallelOther, eDir, e, (pos - (eP0 + eDir / 2f)).sqrMagnitude));
				}
			}
			snaps.AddRange(dirs.Values);
		
			var p0 = current.p0.GetPointPos();

			snap = snaps
				.Where(s => s.check(p0, pos, out var _))
				.OrderBy(s => { s.check(p0, pos, out var diff); return diff; })
				.FirstOrDefault();
			
			if(snap != null) {
				var proj = GeomUtils.projectPointToLine(pos, p0, p0 + snap.dir);
				newPos = proj;
				
				if(snapType != snap.type) {
					snapType = snap.type;
					newSnapConstraint = createSnapConstraint(snap);
					newSnapConstraint.enabled = false;
					
					// find the second snapping for constraining perpendicular alignment to points
					var perp = new Vector3(-snap.dir.y, snap.dir.x, snap.dir.z);

					float minDiff = -1f;
					float minDist = -1f;
					IEntity snapPoint = null;

					foreach(var e in current.sketch.entityList) {
						if(e.type != IEntityType.Point) {
							continue;
						}
						if(e == current.p0 || e == current.p1) {
							continue;
						}
						var pt = e.GetPointPos();

						if(SnapData.checkPointLine(newPos, pt, perp, out var diff)) {
							if(minDiff < 0f || diff < minDiff + Mathf.Epsilon) {
								if(Mathf.Abs(diff - minDiff) > Mathf.Epsilon || minDist < 0f || (newPos - pt).sqrMagnitude < minDist)
								{
									minDiff = diff;
									minDist = (newPos - pt).sqrMagnitude;
									snapPoint = e;
								}
							}
						}
					}

					if(snapPoint != null) {
						var snapPt = snapPoint.GetPointPos();
						newPos = GeomUtils.projectPointToLine(newPos, snapPt, snapPt + perp);
						displayLink = new DisplayLink(current.sketch, snapPoint, current.p1);
					}
				}
			}

		}
		if (snap == null || newSnapConstraint != null) {
			snapConstraint?.Destroy();
			snapConstraint = null;
			snapType = SnapType.None;
		}
		snapConstraint = newSnapConstraint;
		current.p1.SetPosition(newPos);
	}

	protected override void OnMouseMove(Vector3 pos, ICADObject entity) {
		if(current != null) {
			setCurrentPosition(pos, entity);
			//var itr = new Vector3();
			canCreate = true;//!current.sketch.IsCrossed(current, ref itr);
			current.isError = !canCreate;
		} else {
			canCreate = true;
		}
	}

	protected override void OnDeactivate() {
		if(current != null) {
			current.Destroy();
			current = null;
			editor.PopUndo();
		}
		canCreate = true;
		prev = null;
		displayLink?.Destroy();
		displayLink = null;
		dimension?.Destroy();
		dimension = null;
	}

	protected override string OnGetDescription() {
		return "click where you want to create the beginning and the ending points of the line";
	}

}
