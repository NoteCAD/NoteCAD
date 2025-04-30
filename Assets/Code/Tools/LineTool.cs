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
	AngleConstraint angle;
	bool dimensionValueNotChanged = true;
	bool angleValueNotChanged = true;
	double dimensionValue = 0.0;
	double angleValue = 0.0;
	bool editAngle = false;

	SnapType snapType;

	LineEntity current;
	LineEntity prev;
	bool canCreate = true;
	public bool editDimensionWhileCreate = true;

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

	void EditValue() {
		if (editAngle && angle == null) {
			editAngle = false;
		}
		if (editAngle) {
			MoveTool.instance.EditConstraintValue(angle, pushUndo: false, dynamicEditing: true, valueChanged: !angleValueNotChanged);
		} else {
			MoveTool.instance.EditConstraintValue(dimension, pushUndo: false, dynamicEditing: true, valueChanged: !dimensionValueNotChanged);
		}
	}

	protected override void OnUpdate()
	{
		if (editDimensionWhileCreate && angle != null && Input.GetKeyDown(KeyCode.Tab)) {
			editAngle = !editAngle;
			EditValue();
		}
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
			if(snapConstraint != null) {
				snapConstraint.enabled = true;
				snapConstraint.drawLink = false;
				snapConstraint = null;
				snapType = SnapType.None;
			}
			if(dimensionValueNotChanged) {
				dimension?.Destroy();
			} else if(dimension != null) {
				dimension.enabled = true;
				dimension.SetValue(dimensionValue);
			}
			if(angleValueNotChanged) {
				angle?.Destroy();
			} else if(dimension != null) {
				angle.enabled = true;
				angle.SetValue(angleValue);
			}
			CleanUp();
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
		if(editor.GetDetail().settings.drawingDimensions) {
			dimension = new PointsDistance(newLine.sketch, newLine.p0, newLine.p1);
			dimension.labelY = -0.001f;
			dimension.enabled = false;
			if (prev != null) {
				angle = new AngleConstraint(newLine.sketch, prev, newLine);
				angle.labelY = 0.001f;
				angle.enabled = false;
			}
			if(editDimensionWhileCreate) {
				EditValue();
			}
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
			if(type == SnapType.Horizontal || type == SnapType.Vertical) {
				diff -= 0.2f;
			}
			if(type == SnapType.Parallel || type == SnapType.Perpendicular) {
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
	

	Vector3 getNewPos(Vector3 p0, Vector3 pos) {
		var sk = DetailEditor.instance.currentSketch.GetSketch();
		var mouseDir = (pos - p0).normalized;
		var mouseLen = (pos - p0).magnitude;
		if(!angleValueNotChanged && prev != null) {
			var pdir = (prev.GetLineP0(sk.plane).Eval() - prev.GetLineP1(sk.plane).Eval()).normalized;
			mouseDir = Matrix4x4.Rotate(Quaternion.Euler(0.0f, 0.0f, (float)angleValue)) * pdir;
		} else
		if(Input.GetKey(KeyCode.LeftControl)) {
			var angle = Mathf.Atan2(mouseDir.y, mouseDir.x);
			var step = Mathf.PI / 4.0f;
			angle = Mathf.Floor(angle / step + 0.5f) * step;
			mouseDir.x = Mathf.Cos(angle);
			mouseDir.y = Mathf.Sin(angle);
		}
		if(!dimensionValueNotChanged) {
			mouseLen = (float)dimensionValue;
		}
		return p0 + mouseDir * mouseLen;
	}

	void setCurrentPosition(Vector3 pos, ICADObject entity)
	{
		if(current == null) {
			return;
		}
		
		var sk = DetailEditor.instance.currentSketch.GetSketch();
		if (editAngle) {
			if (EditValueChanged()) {
				angleValue = MoveTool.instance.GetEditingValue();
			}
			angleValueNotChanged = !EditValueChanged();
		} else {
			if (EditValueChanged()) {
				dimensionValue = MoveTool.instance.GetEditingValue();
			}
			dimensionValueNotChanged = !EditValueChanged();
		}
		var p0 = current.GetLineP0(sk.plane).Eval();
		var newPos = getNewPos(p0, pos);
		Constraint newSnapConstraint = null;
		displayLink?.Destroy();
		displayLink = null;
		SnapData snap = null;
		bool isDirectionFixed = !angleValueNotChanged;
		if(editor.GetDetail().settings.autoconstraining && !sk.is3d) {
			if(!isDirectionFixed) {
				var snaps = new List<SnapData>();

				// snap to horizontality / verticality
				snaps.Add(new SnapData(SnapType.Horizontal, Vector3.right));
				snaps.Add(new SnapData(SnapType.Vertical, Vector3.up));
		
				// snap to previous segment
				if(prev != null) {	
					var pdir = prev.GetLineP0(sk.plane).Eval() - prev.GetLineP1(sk.plane).Eval();
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
					var eP0 = e.GetLineP0(sk.plane).Eval();
					var eDir = e.GetLineP1(sk.plane).Eval() - eP0;
					if(eDir == Vector3.zero) {
						continue;
					}
					var eDirN = eDir.normalized;
					if(dirs.TryGetValue(eDirN, out var dir) || dirs.TryGetValue(-eDirN, out dir)) {
						var ePos = eP0 + eDir / 2f;
						var eDist = (newPos - ePos).sqrMagnitude;
						if(dir.dist < eDist) {
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
		
				snap = snaps
					.Where(s => s.check(p0, newPos, out var _))
					.OrderBy(s => { s.check(p0, newPos, out var diff); return diff; })
					.FirstOrDefault();
			
				if(snap != null) { 
					var proj = GeomUtils.projectPointToLine(newPos, p0, p0 + snap.dir);
					newPos = proj;
					if(snapType != snap.type) {
						snapType = snap.type;
						newSnapConstraint = createSnapConstraint(snap);
						newSnapConstraint.enabled = false;
					}
				}
			}
			// find the second snapping for constraining perpendicular/h/v alignment to points
			var newDir = (newPos - p0).normalized;
			var perp = new Vector3(-newDir.y, newDir.x, newDir.z);
			var secondDirs = new List<Vector3>{perp};
			if(snap == null || snap.type != SnapType.Horizontal) {
				secondDirs.Add(Vector3.right);
			}
			if(snap == null || snap.type != SnapType.Vertical) {
				secondDirs.Add(Vector3.up);
			}

			float minDiff = -1f;
			float minDist = -1f;
			IEntity snapPoint = null;
			Vector3 secondDir = Vector3.zero;

			foreach(var sDir in secondDirs) {
				foreach(var e in current.sketch.entityList) {
					if(e.type != IEntityType.Point) {
						continue;
					}
					if(e == current.p0 || e == current.p1) {
						continue;
					}
					var point = e as PointEntity;
					if(point.GetConicidentPoints().Contains(current.p0)) {
						continue;
					}
					var pt = e.GetPointPos(sk.plane);

					if(SnapData.checkPointLine(newPos, pt, sDir, out var diff)) {
						if(minDiff < 0f || diff < minDiff + Mathf.Epsilon) {
							if(Mathf.Abs(diff - minDiff) > Mathf.Epsilon || minDist < 0f || (newPos - pt).sqrMagnitude < minDist)
							{
								minDiff = diff;
								minDist = (newPos - pt).sqrMagnitude;
								snapPoint = e;
								secondDir = sDir;
							}
						}
					}
				}
			}

			if(snapPoint != null) {
				var snapPt = snapPoint.GetPointPos(sk.plane);
				if(snap == null && !isDirectionFixed) {
					newPos = GeomUtils.projectPointToLine(newPos, snapPt, snapPt + secondDir);
				} else {
					Vector3 itr = Vector3.zero;
					if(GeomUtils.isLinesCrossed(p0, newPos, snapPt, snapPt + secondDir, ref itr, 1e-6f)) {
						newPos = itr;
					}
				}
				displayLink = new DisplayLink(current.sketch, snapPoint, current.p1);
			}

		}
		if(snap == null || newSnapConstraint != null) {
			snapConstraint?.Destroy();
			snapConstraint = null;
			snapType = SnapType.None;
		}
		snapConstraint = newSnapConstraint;
		newPos = getNewPos(p0, newPos);
		current.p1.SetPosition(newPos);
		if(EditValueNotChanged()) {
			MoveTool.instance.UpdateEditingValue();
		}
	}

	bool EditValueChanged() => editDimensionWhileCreate && MoveTool.instance.HasEditingValueChanged();
	bool EditValueNotChanged() => editDimensionWhileCreate && !MoveTool.instance.HasEditingValueChanged();

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

	void CleanUp() {
		dimension = null;
		angle = null;
		angleValueNotChanged = true;
		dimensionValueNotChanged = true;
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
		angle?.Destroy();
		CleanUp();

		if(editDimensionWhileCreate) {
			MoveTool.instance.EditConstraintValue(null);
		}
	}

	protected override string OnGetDescription() {
		return "click where you want to create the beginning and the ending points of the line";
	}

}
