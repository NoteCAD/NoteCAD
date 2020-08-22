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
		Hpgl,
		Slvs
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
			case FileType.Slvs: {
				NoteCADJS.LoadBinaryData(SlvsDataLoaded, "slvs");
				break;
			}
		}
	}

	public void OnPointerDown(PointerEventData eventData) {
	}

	void AutoConstrain(Entity e) {
		if(!settings.autoconstrain || settings.fileType == FileType.Slvs) return;
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

	void AddArc(UnityEngine.Vector3 c, UnityEngine.Vector3 p0, UnityEngine.Vector3 p1) {
		ArcEntity arc = new ArcEntity(DetailEditor.instance.currentSketch.GetSketch());
		arc.c.SetPosition(c);
		arc.p0.SetPosition(p0);
		arc.p1.SetPosition(p1);
		AutoConstrain(arc);
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

	void AddCircle(UnityEngine.Vector3 pos, double r) {
		CircleEntity circle = new CircleEntity(DetailEditor.instance.currentSketch.GetSketch());
		circle.c.SetPosition(pos);
		circle.r.value = r;
		AutoConstrain(circle);
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

	enum SlvsEntityType {
        POINT_IN_3D            =  2000,
        POINT_IN_2D            =  2001,
        POINT_N_TRANS          =  2010,
        POINT_N_ROT_TRANS      =  2011,
        POINT_N_COPY           =  2012,
        POINT_N_ROT_AA         =  2013,
        POINT_N_ROT_AXIS_TRANS =  2014,
        POINT_MIRROR           =  2019,

        NORMAL_IN_3D           =  3000,
        NORMAL_IN_2D           =  3001,
        NORMAL_N_COPY          =  3010,
        NORMAL_N_ROT           =  3011,
        NORMAL_N_ROT_AA        =  3012,
        NORMAL_MIRROR           = 3019,

        DISTANCE               =  4000,
        DISTANCE_N_COPY        =  4001,

        FACE_NORMAL_PT         =  5000,
        FACE_XPROD             =  5001,
        FACE_N_ROT_TRANS       =  5002,
        FACE_N_TRANS           =  5003,
        FACE_N_ROT_AA          =  5004,
        FACE_MIRROR            =  5005,

        WORKPLANE              = 10000,
        LINE_SEGMENT           = 11000,
        CUBIC                  = 12000,
        CUBIC_PERIODIC         = 12001,
        CIRCLE                 = 13000,
        ARC_OF_CIRCLE          = 14000,
        TTF_TEXT               = 15000,
        IMAGE                  = 16000
	}

	class SlvsParam {
		public string hv;
		public double value;
	}

	class SlvsEntity {
		public string hv;
		public SlvsEntityType type;
		public bool construction;
		public List<string> pointshv = new List<string>();
		public List<string> normalshv = new List<string>();
		public UnityEngine.Vector3 actPoint;
		public string distancehv;
		public double actDistance;
	}

	void SlvsDataLoaded(byte[] data) {
		editor.PushUndo();
		var str = Encoding.UTF8.GetString(data, 0, data.Length);
		var sep = new char[] { '\n', '\r' };
		var sep1 = new char[] { '=' };
		var commands = str.Split(sep, StringSplitOptions.RemoveEmptyEntries);
		UnityEngine.Vector3 pos = UnityEngine.Vector3.zero;
		string firstGroup = "";
		
		var parameters = new Dictionary<string, SlvsParam>();
		SlvsParam curParam = new SlvsParam();

		var entities = new Dictionary<string, SlvsEntity>();
		SlvsEntity curEntity = new SlvsEntity();
		
		foreach(var com in commands) {
			var keyValue = com.Split(sep1);
			var key = keyValue[0];
			var value = keyValue.Length > 1 ? keyValue[1] : "";

			switch(key) {
				case "Group.h.v":
					if(value == "00000001") {
					} else
					if(firstGroup == "") {
						firstGroup = value;
					} else {
						return;
					}
					break;
				
				case "Param.h.v.": curParam.hv = value; break;
				case "Param.val": curParam.value = value.ToDouble(); break;
				case "AddParam": parameters.Add(curParam.hv, curParam); curParam = new SlvsParam(); break;

				case "Entity.h.v": curEntity.hv = value; break;
				case "Entity.type": curEntity.type = (SlvsEntityType)Convert.ToInt32(value); break;
				case "Entity.construction": curEntity.construction = Convert.ToInt32(value) != 0; break;
				case "Entity.point[0].v": curEntity.pointshv.Add(value); break;
				case "Entity.point[1].v": curEntity.pointshv.Add(value); break;
				case "Entity.point[2].v": curEntity.pointshv.Add(value); break;
				case "Entity.point[3].v": curEntity.pointshv.Add(value); break;
				case "Entity.point[4].v": curEntity.pointshv.Add(value); break;
				case "Entity.point[5].v": curEntity.pointshv.Add(value); break;
				case "Entity.point[6].v": curEntity.pointshv.Add(value); break;
				case "Entity.point[7].v": curEntity.pointshv.Add(value); break;
				case "Entity.point[8].v": curEntity.pointshv.Add(value); break;
				case "Entity.point[9].v": curEntity.pointshv.Add(value); break;
				case "Entity.point[10].v": curEntity.pointshv.Add(value); break;
				case "Entity.point[11].v": curEntity.pointshv.Add(value); break;
				case "Entity.actPoint.x": curEntity.actPoint.x = value.ToFloat(); break;
				case "Entity.actPoint.y": curEntity.actPoint.y = value.ToFloat(); break;
				case "Entity.actPoint.z": curEntity.actPoint.z = value.ToFloat(); break;
				case "Entity.actDistance": curEntity.actDistance = value.ToDouble(); break;
				case "Entity.distance.v": curEntity.distancehv = value; break;
				case "AddEntity": entities.Add(curEntity.hv, curEntity); curEntity = new SlvsEntity(); break;
			}
		}

		foreach(var e in entities.Values) {
			switch(e.type) {
				case SlvsEntityType.LINE_SEGMENT: {
					var p0 = entities[e.pointshv[0]];
					var p1 = entities[e.pointshv[1]];
					AddLine(p0.actPoint, p1.actPoint);
					break;
				}
				case SlvsEntityType.CIRCLE: {
					var c = entities[e.pointshv[0]];
					var d = entities[e.distancehv];
					AddCircle(c.actPoint, d.actDistance);
					break;
				}
				case SlvsEntityType.ARC_OF_CIRCLE: {
					var c = entities[e.pointshv[0]];
					var p0 = entities[e.pointshv[1]];
					var p1 = entities[e.pointshv[2]];
					AddArc(c.actPoint, p0.actPoint, p1.actPoint);
					break;
				}
			}
		}

	}


}
