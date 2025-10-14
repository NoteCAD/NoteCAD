using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.IO;
using System;
using System.Text;
using System.Linq;
using NoteCAD;

using ACadSharp;
using ACadSharp.IO;

public class ImportDXFTool : Tool, IPointerDownHandler {

	enum FileType {
		Dxf,
		Dwg,
		Hpgl,
		Slvs,
		Replay
	};

	[Serializable]
	class Settings {
		ImportDXFTool tool;
		public FileType fileType;
		public bool autoconstrain = false;
		public bool chooseLayers = false;
		[NonSerialized]
		public bool activated = false;

		public Settings(ImportDXFTool t) {
			tool = t;
		}

		[RuntimeInspectorNamespace.RuntimeInspectorButton("Import", false, RuntimeInspectorNamespace.ButtonVisibility.InitializedObjects)]
		public void Import() {
			activated = true;
		}

	}
	
	[Serializable]
	class Layer {
		public string name;
		public bool import = true;
	}
	
	[Serializable]
	class Layers {
		ImportDXFTool tool;
		CadDocument document;

		public List<Layer> layers = new();
		
		[NonSerialized]
		public Dictionary<string, Layer> map = new();

		public Layers(ImportDXFTool t) {
			tool = t;
		}

		public void SetDocument(CadDocument doc) {
			document = doc;
			layers.Clear();
			map.Clear();
			foreach(var l in doc.Layers) {
				var layer = new Layer { name = l.Name, import = true };
				layers.Add(layer);
				map.Add(layer.name, layer);
			}
		}

		[RuntimeInspectorNamespace.RuntimeInspectorButton("Import", false, RuntimeInspectorNamespace.ButtonVisibility.InitializedObjects)]
		public void Import() {
			tool.ImportDocument(document);
			tool.StopTool();
		}
	}

	Settings settings;
	Layers layers;
	Style currentStyle = null;
	Dictionary<string, Style> styles = new();


	ImportDXFTool() {
		settings = new Settings(this);
		layers = new Layers(this);
	}

	protected override void OnActivate() {
		Inspect(settings);
	}

	void Clear() {
		settings.activated = false;
		layers.layers.Clear();
		styles.Clear();
		currentStyle = null;
	}
	
	T Spawn<T>(T e) where T: Entity {
		if(currentStyle != null) {
			e.style = currentStyle;
		}
		return e;
	}

	public void LateUpdate() {
		if(!DetailEditor.instance.isActiveAndEnabled) return;
		if(!settings.activated) return;
		settings.activated = false;
		switch(settings.fileType) {
			case FileType.Dxf: {
				NoteCADJS.LoadBinaryData(DxfDataLoaded, "dxf");
				break;
			}
			case FileType.Dwg: {
				NoteCADJS.LoadBinaryData(DwgDataLoaded, "dwg");
				break;
			}
			case FileType.Hpgl: {
				NoteCADJS.LoadBinaryData(HpglDataLoaded, "hpgl");
				StopTool();
				break;
			}
			case FileType.Slvs: {
				NoteCADJS.LoadBinaryData(SlvsDataLoaded, "slvs");
				StopTool();
				break;
			}
			case FileType.Replay: {
				NoteCADJS.LoadBinaryData(ReplayDataLoaded, "replay");
				StopTool();
				break;
			}
		}
	}

	protected override void OnDeactivate() {
		Clear();
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

	LineEntity AddLine(ACadSharp.Entities.Line l) {
		var s = l.StartPoint;
		var e = l.EndPoint;
		LineEntity line = new LineEntity(DetailEditor.instance.currentSketch.GetSketch());
		line.p0.SetPosition(new UnityEngine.Vector3((float)s.X, (float)s.Y, (float)s.Z));
		line.p1.SetPosition(new UnityEngine.Vector3((float)e.X, (float)e.Y, (float)e.Z));
		AutoConstrain(line);
		return Spawn(line);
	}

	ArcEntity AddArc(Vector3 c, Vector3 p0, Vector3 p1) {
		ArcEntity arc = new ArcEntity(DetailEditor.instance.currentSketch.GetSketch());
		arc.c.SetPosition(c);
		arc.p0.SetPosition(p0);
		arc.p1.SetPosition(p1);
		AutoConstrain(arc);
		return Spawn(arc);
	}

	Vector3 ToVec(CSMath.XYZ xyz) {
		return new Vector3((float)xyz.X, (float)xyz.Y, (float)xyz.Z);
	}
	
	Vector3 ToVecOrt(CSMath.XYZ xyz) {
		return new Vector3(-(float)xyz.Y, (float)xyz.X, (float)xyz.Z);
	}

	TextEntity AddText(ACadSharp.Entities.IText text) {
		var result = new TextEntity(DetailEditor.instance.currentSketch.GetSketch());
		result.p[0].SetPosition(ToVec(text.InsertPoint));
		//var dir = ToVecOrt(text.AlignmentPoint);
		var dir = Vector3.up;
		result.p[3].SetPosition(result.p[0].GetPosition() + dir);
		result.fontSize = text.Height;
		result.text = text.Value;
		result.alignment = TextEntity.Alignment.Fit;
		result.UpdatePoints();
		return Spawn(result);
	}

	ArcEntity AddArc(ACadSharp.Entities.Arc a) {
		var c = ToVec(a.Center);
		a.GetEndVertices(out var start, out var end);

		ArcEntity arc = new ArcEntity(DetailEditor.instance.currentSketch.GetSketch());
		arc.c.SetPosition(c);
		arc.p0.SetPosition(ToVec(start));
		arc.p1.SetPosition(ToVec(end));
		AutoConstrain(arc);
		return Spawn(arc);
	}

	CircleEntity AddCircle(Vector3 pos, double r) {
		CircleEntity circle = new CircleEntity(DetailEditor.instance.currentSketch.GetSketch());
		circle.c.SetPosition(pos);
		circle.r.value = r;
		AutoConstrain(circle);
		return Spawn(circle);
	}

	CircleEntity AddCircle(ACadSharp.Entities.Circle c) {
		var ce = c.Center;
		CircleEntity circle = new CircleEntity(DetailEditor.instance.currentSketch.GetSketch());
		circle.c.SetPosition(new Vector3((float)ce.X, (float)ce.Y, (float)ce.Z));
		circle.r.value = c.Radius;
		AutoConstrain(circle);
		return Spawn(circle);
	}

	void AddPolyline(ACadSharp.Entities.IPolyline pl) {
		foreach(var ple in ACadSharp.Entities.Polyline2D.Explode(pl)) {
			if(ple is ACadSharp.Entities.Line pll) AddLine(pll);
			if(ple is ACadSharp.Entities.Arc pla) AddArc(pla);
		}
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

	PointEntity AddPoint(Vector3 pt) {
		var point = new PointEntity(DetailEditor.instance.currentSketch.GetSketch());
		point.SetPosition(pt);
		AutoConstrain(point);
		return Spawn(point);
	}

	LineEntity AddLine(Vector3 p0, Vector3 p1) {
		LineEntity line = new LineEntity(DetailEditor.instance.currentSketch.GetSketch());
		line.p0.SetPosition(p0);
		line.p1.SetPosition(p1);
		AutoConstrain(line);
		return Spawn(line);
	}

	void AddSpline(ACadSharp.Entities.Spline spl) {
		var vertices = spl.PolygonalVertexes(32);
		for(int i = 0; i < vertices.Count - 1; i++) {
			var s = vertices[i];
			var e = vertices[i + 1];
			LineEntity line = new LineEntity(DetailEditor.instance.currentSketch.GetSketch());
			line.p0.SetPosition(new UnityEngine.Vector3((float)s.X, (float)s.Y, (float)s.Z));
			line.p1.SetPosition(new UnityEngine.Vector3((float)e.X, (float)e.Y, (float)e.Z));
			AutoConstrain(line);
			Spawn(line);
		}
	}

	void ImportDocumentLayers(CadDocument doc) {
		if(settings.chooseLayers) {
			layers.SetDocument(doc);
			Inspect(layers);
		} else {
			ImportDocument(doc);
			StopTool();
		}
	}

	void AddEntity(ACadSharp.Entities.Entity e) {
		var detail = DetailEditor.instance.GetDetail();
		if(styles.TryGetValue(e.Layer.Name, out var style)) {
			currentStyle = style;
		} else {
			currentStyle = detail.styles.AddStyle();
			currentStyle.name = e.Layer.Name;
			currentStyle.stroke.color = new UnityEngine.Color(e.Layer.Color.R / 255.0f, e.Layer.Color.G / 255.0f, e.Layer.Color.B / 255.0f);
			currentStyle.stroke.width = 0.7f;
			styles.Add(e.Layer.Name, currentStyle);
			
		}

		switch(e) {
			case ACadSharp.Entities.Line l:
				AddLine(l); 
				break;

			case ACadSharp.Entities.Polyline2D pl:  
				AddPolyline(pl);
				break;

			case ACadSharp.Entities.LwPolyline lwpl:
				AddPolyline(lwpl);
				break;

			case ACadSharp.Entities.Spline spl:
				AddSpline(spl);
				break;

			case ACadSharp.Entities.Arc a:
				AddArc(a);
				break;

			case ACadSharp.Entities.Circle c:
				AddCircle(c);
				break;

			case ACadSharp.Entities.IText t:
				AddText(t);
				break;
			case ACadSharp.Entities.Insert i:
				AddInsert(i);
				break;
		}
	}

	void ImportDocument(CadDocument doc) {
		editor.PushUndo();
		DetailEditor.instance.GetDetail().settings.suppressSolver = true;
		DetailEditor.instance.GetDetail().settings.detectContours = false;
		DetailEditor.instance.GetDetail().settings.displayPoints = DetailSettings.DisplayPoints.None;
		foreach(var ent in doc.Entities) {
			if (settings.chooseLayers && layers.map.ContainsKey(ent.Layer.Name) && !layers.map[ent.Layer.Name].import) {
				continue;
			}
			AddEntity(ent);
		}
	}

	void AddInsert(ACadSharp.Entities.Insert i) {
		foreach(var e in i.Explode()) {
			Debug.Log("AddInsert " + e.GetType().Name);
			AddEntity(e);
		}
	}

	void DwgDataLoaded(byte[] data) {
		MemoryStream stream = new MemoryStream(data);
		using (DwgReader reader = new DwgReader(stream))
		{
			var doc = reader.Read();
			ImportDocumentLayers(doc);
		}
	}

	void DxfDataLoaded(byte[] data) {
		MemoryStream stream = new MemoryStream(data);
		using (DxfReader reader = new DxfReader(stream))
		{
			var doc = reader.Read();
			ImportDocumentLayers(doc);
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

	enum SlvsConstraintType {
        /**/POINTS_COINCIDENT      =  20,
        /**/PT_PT_DISTANCE         =  30,
        //PT_PLANE_DISTANCE      =  31,
        /**/PT_LINE_DISTANCE       =  32,
        //PT_FACE_DISTANCE       =  33,
        //PROJ_PT_DISTANCE       =  34,
        //PT_IN_PLANE            =  41,
        /**/PT_ON_LINE             =  42,
        //PT_ON_FACE             =  43,
        /**/EQUAL_LENGTH_LINES     =  50,
        /**/LENGTH_RATIO           =  51,
        //EQ_LEN_PT_LINE_D       =  52,
        //EQ_PT_LN_DISTANCES     =  53,
        EQUAL_ANGLE            =  54,
        /**/EQUAL_LINE_ARC_LEN     =  55,
        //LENGTH_DIFFERENCE      =  56,
        //SYMMETRIC              =  60,
        //SYMMETRIC_HORIZ        =  61,
        //SYMMETRIC_VERT         =  62,
        //SYMMETRIC_LINE         =  63,
        AT_MIDPOINT            =  70,
        /**/HORIZONTAL             =  80,
        /**/VERTICAL               =  81,
        DIAMETER               =  90,
        /**/PT_ON_CIRCLE           = 100,
        //SAME_ORIENTATION       = 110,
        /**/ANGLE                  = 120,
        /**/PARALLEL               = 121,
        /**/PERPENDICULAR          = 122,
        /**/ARC_LINE_TANGENT       = 123,
        /**/CUBIC_LINE_TANGENT     = 124,
        /**/CURVE_CURVE_TANGENT    = 125,
        /**/EQUAL_RADIUS           = 130,
        //WHERE_DRAGGED          = 200,
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
		public string workplanehv;
	}

	class SlvsConstraint {
		public string hv;
		public SlvsConstraintType type;
		public string workplanehv;
		public string ptAhv = "";
		public string ptBhv = "";
		public string entityAhv = "";
		public string entityBhv = "";
		public string entityChv = "";
		public string entityDhv = "";
		public bool other;
		public bool other2;
		public bool reference;
		public UnityEngine.Vector3 offset;
		public double valA;
	}

	void SlvsDataLoaded(byte[] data) {
		editor.PushUndo();
		var str = Encoding.UTF8.GetString(data, 0, data.Length);
		var sep = new char[] { '\n', '\r' };
		var sep1 = new char[] { '=' };
		var commands = str.Split(sep, StringSplitOptions.RemoveEmptyEntries);
		UnityEngine.Vector3 pos = UnityEngine.Vector3.zero;
		string firstGroup = "";
		string firstWorkplane = "";
		
		var parameters = new Dictionary<string, SlvsParam>();
		SlvsParam curParam = new SlvsParam();

		var slvsEntities = new Dictionary<string, SlvsEntity>();
		SlvsEntity curEntity = new SlvsEntity();
		var ncadEntities = new Dictionary<string, Entity>();

		var slvsConstraints = new Dictionary<string, SlvsConstraint>();
		SlvsConstraint curConstraint = new SlvsConstraint();
		
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
					}
					break;
				case "Group.activeWorkplane.v":
					if(firstGroup != "" && firstWorkplane == "") {
						firstWorkplane = value;
					}
					break;
				
				case "Param.h.v.": curParam.hv = value; break;
				case "Param.val": curParam.value = value.ToDouble(); break;
				case "AddParam": parameters.Add(curParam.hv, curParam); curParam = new SlvsParam(); break;

				case "Entity.h.v": curEntity.hv = value; break;
				case "Entity.workplane.v": curEntity.workplanehv = value; break;
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
				case "AddEntity": slvsEntities.Add(curEntity.hv, curEntity); curEntity = new SlvsEntity(); break;

				case "Constraint.h.v": curConstraint.hv = value; break;
				case "Constraint.workplane.v": curConstraint.workplanehv = value; break;
				case "Constraint.type": curConstraint.type = (SlvsConstraintType)Convert.ToInt32(value); break;
				case "Constraint.reference": curConstraint.reference = Convert.ToInt32(value) != 0; break;
				case "Constraint.other": curConstraint.other = Convert.ToInt32(value) != 0; break;
				case "Constraint.other2": curConstraint.other2 = Convert.ToInt32(value) != 0; break;
				case "Constraint.valA": curConstraint.valA = value.ToDouble(); break;
				case "Constraint.disp.offset.x": curConstraint.offset.x = value.ToFloat(); break;
				case "Constraint.disp.offset.y": curConstraint.offset.y = value.ToFloat(); break;
				case "Constraint.disp.offset.z": curConstraint.offset.z = value.ToFloat(); break;
				case "Constraint.entityA.v": curConstraint.entityAhv = value; break;
				case "Constraint.entityB.v": curConstraint.entityBhv = value; break;
				case "Constraint.entityC.v": curConstraint.entityChv = value; break;
				case "Constraint.entityD.v": curConstraint.entityDhv = value; break;
				case "Constraint.ptA.v": curConstraint.ptAhv = value; break;
				case "Constraint.ptB.v": curConstraint.ptBhv = value; break;
				case "AddConstraint": slvsConstraints.Add(curConstraint.hv, curConstraint); curConstraint = new SlvsConstraint(); break;
			}
		}

		foreach(var e in slvsEntities.Values) {
			if(e.workplanehv != firstWorkplane) continue;
			switch(e.type) {
				case SlvsEntityType.LINE_SEGMENT: {
					var p0 = slvsEntities[e.pointshv[0]];
					var p1 = slvsEntities[e.pointshv[1]];
					var line = AddLine(p0.actPoint, p1.actPoint);
					ncadEntities.Add(e.hv, line);
					ncadEntities.Add(e.pointshv[0], line.p0);
					ncadEntities.Add(e.pointshv[1], line.p1);
					break;
				}
				case SlvsEntityType.CIRCLE: {
					var c = slvsEntities[e.pointshv[0]];
					var d = slvsEntities[e.distancehv];
					var circle = AddCircle(c.actPoint, d.actDistance);
					ncadEntities.Add(e.hv, circle);
					ncadEntities.Add(e.pointshv[0], circle.center);
					break;
				}
				case SlvsEntityType.ARC_OF_CIRCLE: {
					var c = slvsEntities[e.pointshv[0]];
					var p0 = slvsEntities[e.pointshv[1]];
					var p1 = slvsEntities[e.pointshv[2]];
					var arc = AddArc(c.actPoint, p0.actPoint, p1.actPoint);
					ncadEntities.Add(e.hv, arc);
					ncadEntities.Add(e.pointshv[0], arc.c);
					ncadEntities.Add(e.pointshv[1], arc.p0);
					ncadEntities.Add(e.pointshv[2], arc.p1);
					break;
				}
			}
		}

		bool GetEntityNCAD(string hv, out Entity e) {
			return ncadEntities.TryGetValue(hv, out e);
		}

		bool GetPointNCAD(string hv, out Entity e) {
			if(!ncadEntities.TryGetValue(hv, out e)) {
				if(slvsEntities.TryGetValue(hv, out var slvsPoint)) {
					e = AddPoint(slvsPoint.actPoint);
					ncadEntities.Add(hv, e);
					return true;
				}
				return false;
			}
			return true;
		}

		foreach(var c in slvsConstraints.Values) {
			if(c.workplanehv != firstWorkplane) continue;
			
			switch(c.type) {
				
				case SlvsConstraintType.POINTS_COINCIDENT: {
					if (GetPointNCAD(c.ptAhv, out var ptA) && 
						GetPointNCAD(c.ptBhv, out var ptB)
					) {
						new PointsCoincident(ptA.sketch, ptA, ptB);
					}
					break;
				}
				
				case SlvsConstraintType.HORIZONTAL:
				case SlvsConstraintType.VERTICAL: {
					HVConstraint hvc = null; 
					if (GetPointNCAD(c.ptAhv, out var ptA) && 
						GetPointNCAD(c.ptBhv, out var ptB)
					) {
						hvc = new HVConstraint(ptA.sketch, ptA, ptB);
					} else 
					if (GetEntityNCAD(c.entityAhv, out var eA)) {
						hvc = new HVConstraint(eA.sketch, eA);
					}
					if(hvc != null) {
						hvc.orientation = c.type == SlvsConstraintType.HORIZONTAL ? HVOrientation.OY : HVOrientation.OX;
					}
					break;
				}

				case SlvsConstraintType.AT_MIDPOINT:
				case SlvsConstraintType.PT_ON_LINE:
				case SlvsConstraintType.PT_ON_CIRCLE: {
					if (GetPointNCAD(c.ptAhv, out var ptA) && 
						GetEntityNCAD(c.entityAhv, out var eA)
					) {
						var pon = new PointOn(ptA.sketch, ptA, eA);
						if(c.type == SlvsConstraintType.AT_MIDPOINT) {
							pon.reference = false;
							pon.SetValue(0.5);
						}
					}
					break;
				}

				case SlvsConstraintType.PT_PT_DISTANCE: {
					if (GetPointNCAD(c.ptAhv, out var ptA) && 
						GetPointNCAD(c.ptBhv, out var ptB)
					) {
						var ppd = new PointsDistance(ptA.sketch, ptA, ptB);
						ppd.pos = (ptA.GetPointPos() + ptB.GetPointPos()) / 2f + c.offset;
					}
					break;
				}

				case SlvsConstraintType.PT_LINE_DISTANCE: {
					if (GetPointNCAD(c.ptAhv, out var ptA) && 
						GetEntityNCAD(c.entityAhv, out var eA)
					) {
						var pld = new PointLineDistance(ptA.sketch, ptA, eA);
						var pt = ptA.GetPointPos();
						var ponl = GeomUtils.projectPointToLine(pt, eA.GetLineP0(null).Eval(), eA.GetLineP1(null).Eval());
						pld.pos = (pt + ponl) / 2f + c.offset;
					}
					break;
				}

				case SlvsConstraintType.EQUAL_LENGTH_LINES:
				case SlvsConstraintType.LENGTH_RATIO:
				case SlvsConstraintType.EQUAL_LINE_ARC_LEN:
				case SlvsConstraintType.EQUAL_RADIUS: {
					if (GetEntityNCAD(c.entityAhv, out var entityA) && 
						GetEntityNCAD(c.entityBhv, out var entityB)
					) {
						var eq = new Equal(entityA.sketch, entityA, entityB);
						switch(c.type) {
							case SlvsConstraintType.LENGTH_RATIO:
								eq.Satisfy();
								break;
							case SlvsConstraintType.EQUAL_LINE_ARC_LEN:
								eq.FirstLengthType = Equal.LengthType.Length;
								eq.SecondLengthType = Equal.LengthType.Length;
								break;
							case SlvsConstraintType.EQUAL_RADIUS:
								eq.FirstLengthType = Equal.LengthType.Radius; 
								eq.SecondLengthType = Equal.LengthType.Radius; 
								break;
						}
					}
					break;
				}

				case SlvsConstraintType.ANGLE: {
					if (GetEntityNCAD(c.entityAhv, out var eA) && 
						GetEntityNCAD(c.entityBhv, out var eB)
					) {
						var eq = new AngleConstraint(eA.sketch, eA, eB);
						eq.supplementary = !c.other;
						var basis = eq.GetBasis();
						eq.pos = (UnityEngine.Vector3)basis.GetColumn(3) + c.offset;
					}
					break;
				}

				case SlvsConstraintType.ARC_LINE_TANGENT:
				case SlvsConstraintType.CURVE_CURVE_TANGENT:
				case SlvsConstraintType.CUBIC_LINE_TANGENT: {
					if (GetEntityNCAD(c.entityAhv, out var eA) && 
						GetEntityNCAD(c.entityBhv, out var eB)
					) {
						var eq = new Tangent(eA.sketch, eA, eB);
					}
					break;
				}

				case SlvsConstraintType.PARALLEL: {
					if (GetEntityNCAD(c.entityAhv, out var eA) && 
						GetEntityNCAD(c.entityBhv, out var eB)
					) {
						var eq = new Parallel(eA.sketch, eA, eB);
					}
					break;
				}

				case SlvsConstraintType.PERPENDICULAR: {
					if (GetEntityNCAD(c.entityAhv, out var eA) && 
						GetEntityNCAD(c.entityBhv, out var eB)
					) {
						var eq = new Perpendicular(eA.sketch, eA, eB);
					}
					break;
				}
				
				case SlvsConstraintType.DIAMETER: {
					if (GetEntityNCAD(c.entityAhv, out var eA)) {
						var eq = new Diameter(eA.sketch, eA);
						eq.pos = eA.Center().Eval() + c.offset;
					}
					break;
				}
			}
		}


	}

	void ReplayDataLoaded(byte[] data) {
		var replay = Poisson.ReplaySerializer.Load(data);
		var sk = DetailEditor.instance.currentSketch.GetSketch();
		replay.Output.CreateTo(sk);
	}
}
