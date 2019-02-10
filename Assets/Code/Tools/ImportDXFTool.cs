using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using netDxf;
using System.IO;

public class ImportDXFTool : Tool, IPointerDownHandler {

	protected override void OnActivate() {
		StopTool();
	}

	public void OnPointerDown(PointerEventData eventData) {
		NoteCADJS.LoadBinaryData(DataLoaded, "dxf");
	}

	void AutoConstrain(Entity e) {
		foreach(var p in e.points) {
			var other = p.sketch.GetOtherPointByPoint(p, 1e-4f);
			if(other == null) continue;
			new PointsCoincident(p.sketch, p, other);
		}
	}

	void AddLine(netDxf.Entities.Line l) {
		var s = l.StartPoint;
		var e = l.EndPoint;
		LineEntity line = new LineEntity(DetailEditor.instance.currentSketch.GetSketch());
		line.p0.SetPosition(new UnityEngine.Vector3((float)s.X, (float)s.Y, (float)s.Z));
		line.p1.SetPosition(new UnityEngine.Vector3((float)e.X, (float)e.Y, (float)e.Z));
		AutoConstrain(line);
	}

	void AddArc(netDxf.Entities.Arc a) {
		var c = new UnityEngine.Vector3((float)a.Center.X, (float)a.Center.Y, (float)a.Center.Z);
		var e = a.StartAngle;
		float sa = (float)a.StartAngle * Mathf.Deg2Rad;
		float ea = (float)a.EndAngle * Mathf.Deg2Rad;
		float r = (float)a.Radius;
		var rvs = new UnityEngine.Vector3(r * Mathf.Cos(sa), r * Mathf.Sin(sa), c.z) + c;
		var rve = new UnityEngine.Vector3(r *  Mathf.Cos(ea), r * Mathf.Sin(ea), c.z) + c;

		ArcEntity arc = new ArcEntity(DetailEditor.instance.currentSketch.GetSketch());
		arc.c.SetPosition(c);
		arc.p0.SetPosition(rvs);
		arc.p1.SetPosition(rve);
		AutoConstrain(arc);
	}

	void AddCircle(netDxf.Entities.Circle c) {
		var ce = c.Center;
		CircleEntity circle = new CircleEntity(DetailEditor.instance.currentSketch.GetSketch());
		circle.c.SetPosition(new UnityEngine.Vector3((float)ce.X, (float)ce.Y, (float)ce.Z));
		circle.radius.value = c.Radius;
		AutoConstrain(circle);
	}

	void DataLoaded(byte[] data) {
		MemoryStream stream = new MemoryStream(data);
		DxfDocument doc = DxfDocument.Load(stream);
		editor.PushUndo();
		foreach(var l in doc.Lines) {
			AddLine(l);
		}

		foreach(var pl in doc.Polylines) {
			foreach(netDxf.Entities.Line l in pl.Explode()) {
				AddLine(l);
			}
		}

		foreach(var pl in doc.LwPolylines) {
			foreach(var e in pl.Explode()) {
				if(e is netDxf.Entities.Line) AddLine((netDxf.Entities.Line)e);
				if(e is netDxf.Entities.Arc) AddArc((netDxf.Entities.Arc)e);
			}
		}

		foreach(var spl in doc.Splines) {
			var vertices = spl.PolygonalVertexes(32);
			for(int i = 0; i < vertices.Count - 1; i++) {
				var s = vertices[i];
				var e = vertices[i + 1];
				LineEntity line = new LineEntity(DetailEditor.instance.currentSketch.GetSketch());
				line.p0.SetPosition(new UnityEngine.Vector3((float)s.X, (float)s.Y, (float)s.Z));
				line.p1.SetPosition(new UnityEngine.Vector3((float)e.X, (float)e.Y, (float)e.Z));
				AutoConstrain(line);
			}
		}

		foreach(var c in doc.Circles) {
			AddCircle(c);
		}

		foreach(var a in doc.Arcs) {
			AddArc(a);
		}
	}
}
