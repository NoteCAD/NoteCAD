using System;
using UnityEngine;
using ACadSharp;
using ACadSharp.Tables;
using NoteCAD;

public abstract class CadExportBase : ICanvas {
	// Use R2000 (AC1015) for maximum compatibility with QCAD, LibreCAD, etc.
	protected CadDocument document = new CadDocument(ACadVersion.AC1015);
	protected Style currentStyle;

	// ICanvas - fallback for entities without a native counterpart
	public void DrawLine(Vector3 a, Vector3 b) {
		var line = new ACadSharp.Entities.Line {
			StartPoint = new CSMath.XYZ(a.x, a.y, a.z),
			EndPoint = new CSMath.XYZ(b.x, b.y, b.z)
		};
		SetEntityLayer(line);
		document.Entities.Add(line);
	}

	public void DrawPoint(Vector3 pt) {
	}

	public void SetStyle(Style style) {
		currentStyle = style;
	}

	// Export a NoteCAD sketch entity as its native CAD counterpart.
	// Falls back to line segments via Draw(this) for unrecognised types.
	public void AddSketchEntity(Entity e) {
		if (e.style != null) currentStyle = e.style;
		switch (e) {
			case LineEntity l:
				AddNativeLine(l);
				break;
			case ArcEntity a:
				AddNativeArc(a);
				break;
			case CircleEntity c:
				AddNativeCircle(c);
				break;
			case NoteCAD.TextEntity t:
				AddNativeText(t);
				break;
			case PointEntity p when p.parent == null:
				AddNativePoint(p);
				break;
			default:
				if (e.style != null) SetStyle(e.style);
				e.Draw(this);
				break;
		}
	}

	// Export a dimension (ValueConstraint) as a native CAD dimension.
	public void AddDimension(ValueConstraint c) {
		switch (c) {
			case Length l:
				AddLengthDimension(l);
				break;
			case Diameter d:
				AddDiameterDimension(d);
				break;
		}
	}

	// --- Private helpers ---

	void AddNativeLine(LineEntity l) {
		var line = new ACadSharp.Entities.Line {
			StartPoint = ToXYZ(l.p0.pos),
			EndPoint = ToXYZ(l.p1.pos)
		};
		SetEntityLayer(line);
		document.Entities.Add(line);
	}

	void AddNativeArc(ArcEntity a) {
		var center = a.c.pos;
		var start = a.p0.pos;
		var end = a.p1.pos;
		double radius = (start - center).magnitude;
		double startAngle = Math.Atan2(start.y - center.y, start.x - center.x);
		double endAngle = Math.Atan2(end.y - center.y, end.x - center.x);
		var arc = new ACadSharp.Entities.Arc {
			Center = ToXYZ(center),
			Radius = radius,
			StartAngle = startAngle,
			EndAngle = endAngle
		};
		SetEntityLayer(arc);
		document.Entities.Add(arc);
	}

	void AddNativeCircle(CircleEntity c) {
		var circle = new ACadSharp.Entities.Circle {
			Center = ToXYZ(c.c.pos),
			Radius = Math.Abs(c.r.value)
		};
		SetEntityLayer(circle);
		document.Entities.Add(circle);
	}

	void AddNativeText(NoteCAD.TextEntity t) {
		var text = new ACadSharp.Entities.TextEntity {
			InsertPoint = ToXYZ(t.p[0].pos),
			Height = t.fontSize,
			Value = t.text
		};
		SetEntityLayer(text);
		document.Entities.Add(text);
	}

	void AddNativePoint(PointEntity p) {
		var point = new ACadSharp.Entities.Point {
			Location = ToXYZ(p.pos)
		};
		SetEntityLayer(point);
		document.Entities.Add(point);
	}

	void AddLengthDimension(Length l) {
		var e = l.GetEntity(0);
		Vector3 p0 = e.PointOnInPlane(0.0, null).Eval();
		Vector3 p1 = e.PointOnInPlane(1.0, null).Eval();
		Vector3 labelPos = l.pos;

		var dim = new ACadSharp.Entities.DimensionAligned {
			FirstPoint = ToXYZ(p0),
			SecondPoint = ToXYZ(p1),
			TextMiddlePoint = ToXYZ(labelPos)
		};
		dim.DefinitionPoint = ToXYZ(labelPos);
		document.Entities.Add(dim);
	}

	void AddDiameterDimension(Diameter d) {
		var e = d.GetEntity(0);
		Vector3 center = e.CenterInPlane(null).Eval();
		float r = (float)d.GetValue() / 2f;
		Vector3 labelPos = d.pos;
		Vector3 dir = (labelPos - center).normalized;

		if (d.showAsRadius) {
			var dim = new ACadSharp.Entities.DimensionRadius {
				DefinitionPoint = ToXYZ(center + dir * r),
				AngleVertex = ToXYZ(center),
				TextMiddlePoint = ToXYZ(labelPos)
			};
			document.Entities.Add(dim);
		} else {
			var dim = new ACadSharp.Entities.DimensionDiameter {
				DefinitionPoint = ToXYZ(center + dir * r),
				AngleVertex = ToXYZ(center - dir * r),
				TextMiddlePoint = ToXYZ(labelPos)
			};
			document.Entities.Add(dim);
		}
	}

	protected Layer GetOrCreateLayer(Style style) {
		var name = style.name;
		if (document.Layers.Contains(name)) {
			return document.Layers[name];
		}
		var c = style.stroke.color;
		var layer = new Layer(name) {
			Color = new ACadSharp.Color(
				(byte)Mathf.RoundToInt(c.r * 255f),
				(byte)Mathf.RoundToInt(c.g * 255f),
				(byte)Mathf.RoundToInt(c.b * 255f))
		};
		document.Layers.Add(layer);
		return layer;
	}

	void SetEntityLayer(ACadSharp.Entities.Entity entity) {
		if (currentStyle != null) {
			entity.Layer = GetOrCreateLayer(currentStyle);
		}
	}

	static CSMath.XYZ ToXYZ(Vector3 v) {
		return new CSMath.XYZ(v.x, v.y, v.z);
	}

	public abstract byte[] GetResult();
}
