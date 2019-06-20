using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using netDxf;
using System.IO;
using System;
using System.Text;

public class ImportDXFTool : Tool, IPointerDownHandler {

	enum FileType {
		Dxf,
		Hpgl
	};

	[Serializable]
	class Settings {
		ImportDXFTool tool;
		public FileType fileType;
		public bool autoconstrain = true;
		[NonSerialized]
		public bool activated = false;

		public Settings(ImportDXFTool t) {
			tool = t;
		}

		[RuntimeInspectorNamespace.RuntimeInspectorButton("Import", false, RuntimeInspectorNamespace.ButtonVisibility.InitializedObjects)]
		public void Export() {
			activated = true;
		}

	}

	Settings settings;

	ImportDXFTool() {
		settings = new Settings(this);
	}

	protected override void OnActivate() {
		Inspect(settings);
	}

	public void LateUpdate() {
		if(!settings.activated) return;
		settings.activated = false;
		StopTool();
		switch(settings.fileType) {
			case FileType.Dxf: {
				NoteCADJS.LoadBinaryData(DxfDataLoaded, "dxf");
				break;
			}
			case FileType.Hpgl: {
				NoteCADJS.LoadBinaryData(HpglDataLoaded, "hpgl");
				break;
			}
		}
	}

	public void OnPointerDown(PointerEventData eventData) {
	}

	void AutoConstrain(Entity e) {
		if(!settings.autoconstrain) return;
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
		circle.r.value = c.Radius;
		AutoConstrain(circle);
	}
	
	void HpglDataLoaded(byte[] data) {
		editor.PushUndo();
		var str = Encoding.UTF8.GetString(data, 0, data.Length);
		var sep = new char[] { ';', '\n', '\r' };
		var sep1 = new char[] { ',' };
		var commands = str.Split(sep, StringSplitOptions.RemoveEmptyEntries);
		UnityEngine.Vector3 pos = UnityEngine.Vector3.zero;
		foreach(var com in commands) {
			if(com.StartsWith("PU")) {
				var pu = com.Replace("PU", "");
				var coord = pu.Split(sep1);
				pos = new UnityEngine.Vector3(coord[0].ToFloat(), coord[1].ToFloat(), 0f) / 40f;
			}
			if(com.StartsWith("PD")) {
				var pd = com.Replace("PD", "");
				var coord = pd.Split(sep1);
				var newPos = new UnityEngine.Vector3(coord[0].ToFloat(), coord[1].ToFloat(), 0f) / 40f;
				AddLine(pos, newPos);
				pos = newPos;
			}
		}
	}

	void AddLine(UnityEngine.Vector3 p0, UnityEngine.Vector3 p1) {
		LineEntity line = new LineEntity(DetailEditor.instance.currentSketch.GetSketch());
		line.p0.SetPosition(p0);
		line.p1.SetPosition(p1);
		AutoConstrain(line);
	}

	void DxfDataLoaded(byte[] data) {
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
