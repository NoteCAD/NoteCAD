using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.Text;
using NoteCAD;

public class ExportSTLTool : Tool {

	enum FileType {
		Stl,
		Obj,
		Hpgl,
		Replay,
		Svg,
		GCode,
		Dxf,
		Dwg
	};

	[Serializable]
	class ExportSettings {
		ExportSTLTool tool;
		public FileType fileType;
		public bool constraints = false;
		public bool dimensions = false;
		public bool construction = true;

		public ExportSettings(ExportSTLTool t) {
			tool = t;
		}

		[RuntimeInspectorNamespace.RuntimeInspectorButton("TextFillupISP", false, RuntimeInspectorNamespace.ButtonVisibility.InitializedObjects)]
		public void TextFillup() {
			if (DetailEditor.instance.currentSketch == null) {
				return;
			}
			NoteCADJS.LoadData(FillupLoaded, "isp");
		}

		[RuntimeInspectorNamespace.RuntimeInspectorButton("TextFillupCSV", false, RuntimeInspectorNamespace.ButtonVisibility.InitializedObjects)]
		public void TextFillupCSV() {
			if (DetailEditor.instance.currentSketch == null) {
				return;
			}
			NoteCADJS.LoadData(FillupCSVLoaded, "csv");
		}


		void DoFillup(Dictionary<string, string> fillup) {
			DetailEditor.instance.PushUndo();
			foreach(var e in DetailEditor.instance.currentSketch.GetSketch().entityList) {
				if (e is TextEntity text) {
					var str = text.text;
					int ampIndex;
					while ((ampIndex = str.IndexOf("&")) != -1) {
						var endIndex = str.IndexOfAny(new char[] {' ', '\n', '\r', '#'}, ampIndex);
						if(endIndex == -1) {
							endIndex = str.Length;
						}
						var key = str.Substring(ampIndex + 1, endIndex - ampIndex - 1);
						if(!fillup.ContainsKey(key)) {
							Debug.Log($"Unknown fillup key \"{key}\"");
							var sb = new StringBuilder(str);
							sb[ampIndex] = '?';
							str = sb.ToString();
							continue;
						}
						var replacer = fillup[key];//.Replace('3', '5');
						str = str.Replace("&" + key, replacer);
					}
					text.text = str;
				}
			}
			DetailEditor.instance.currentSketch.GetSketch().MarkDirtySketch(topo:true);
		}

		void FillupCSVLoaded(string data) {
			if (DetailEditor.instance.currentSketch == null) {
				return;
			}
			var lines = data.Split('\n');
			var fillup = new Dictionary<string, string>();
			foreach(var l in lines) {
				var line = l.Split('\t');
				if (line.Length < 2) {
					continue;
				}
				var key = line[0];
				var value = line[1];
				Debug.Log($"{key} = {value}");
				fillup[key] = value;
			}

			DoFillup(fillup);

		}

		void FillupLoaded(string data) {
			if (DetailEditor.instance.currentSketch == null) {
				return;
			}
			DetailEditor.instance.PushUndo();
			var lines = data.Split('\n');
			var fillup = new Dictionary<string, string>();
			foreach(var l in lines) {
				var assignIndex = l.IndexOf("=");
				if(assignIndex == -1) {
					continue;
				}
				var key = l.Substring(0, assignIndex).Trim();
				var endIndex = l.IndexOfAny(new char[] {'\n', '\r', '#'});
				if(endIndex == -1) {
					endIndex = l.Length;
				}
				var value = l.Substring(assignIndex + 1, endIndex - assignIndex - 1).Trim();
				Debug.Log($"{key} = {value}");
				fillup[key] = value;
			}

			DoFillup(fillup);
		}

		[RuntimeInspectorNamespace.RuntimeInspectorButton("Export", false, RuntimeInspectorNamespace.ButtonVisibility.InitializedObjects)]
		public void Export() {
			tool.StopTool();
			switch(fileType) {
				case FileType.Stl: {
					var data = DetailEditor.instance.ExportSTL(); 
					NoteCADJS.SaveData(data, "NoteCADExport.stl", "stl");
					break;
				}
				case FileType.Obj: {
					var data = DetailEditor.instance.ExportOBJ(); 
					NoteCADJS.SaveData(data, "NoteCADExport.obj", "obj");
					break;
				}
				case FileType.Hpgl: {
					var data = tool.ExportHpgl(); 
					NoteCADJS.SaveData(data, "NoteCADExport.hpgl", "hpgl");
					break;
				}
				case FileType.Replay: {
					var data = tool.ExportReplay(); 
					NoteCADJS.SaveData(data, "NoteCADExport.replay", "replay");
					break;
				}
				case FileType.Svg: {
					var data = tool.ExportSvg(); 
					NoteCADJS.SaveData(data, "NoteCADExport.svg", "svg");
					break;
				}
				case FileType.GCode: {
					var data = tool.ExportGCode(); 
					NoteCADJS.SaveData(data, "NoteCADExport.nc", "nc");
					break;
				}
				case FileType.Dxf: {
					var data = tool.ExportDxf();
					NoteCADJS.SaveBinaryData(data, "NoteCADExport.dxf", "dxf");
					break;
				}
				case FileType.Dwg: {
					var data = tool.ExportDwg();
					NoteCADJS.SaveBinaryData(data, "NoteCADExport.dwg", "dwg");
					break;
				}
			}
		}

	}

	class HpglCanvas : HpglExport { }

	class SvgCanvas : SvgExport { }

	class GCodeCanvas : GCodeExport { }

	string ExportHpgl() {
		var canvas = new HpglCanvas();
		editor.currentSketch.DrawEntities(canvas, e => settings.construction || !e.isConstruction);
		if(settings.constraints || settings.dimensions) {
			editor.currentSketch.DrawConstraints(canvas, c => settings.constraints && !c.IsDimension || settings.dimensions && c.IsDimension);
		}
		return canvas.GetResult();
	}

	string ExportSvg() {
		var canvas = new SvgCanvas();
		editor.currentSketch.DrawEntities(canvas, e => settings.construction || !e.isConstruction);
		if(settings.constraints || settings.dimensions) {
			editor.currentSketch.DrawConstraints(canvas, c => settings.constraints && !c.IsDimension || settings.dimensions && c.IsDimension);
		}
		return canvas.GetResult();
	}

	string ExportGCode() {
		var canvas = new GCodeCanvas();
		editor.currentSketch.DrawEntities(canvas, e => settings.construction || !e.isConstruction);
		if(settings.constraints || settings.dimensions) {
			editor.currentSketch.DrawConstraints(canvas, c => settings.constraints && !c.IsDimension || settings.dimensions && c.IsDimension);
		}
		return canvas.GetResult();
	}

	byte[] ExportDxf() {
		var exporter = new DxfExport();
		ExportNativeEntities(exporter);
		return exporter.GetResult();
	}

	byte[] ExportDwg() {
		var exporter = new DwgExport();
		ExportNativeEntities(exporter);
		return exporter.GetResult();
	}

	void ExportNativeEntities(CadExportBase exporter) {
		foreach (var e in editor.currentSketch.GetSketch().entityList) {
			if (!e.isVisible) continue;
			if (!settings.construction && e.isConstruction) continue;
			exporter.AddSketchEntity(e);
		}
		if (settings.dimensions) {
			foreach (var c in editor.currentSketch.GetSketch().constraintList) {
				if (!c.isVisible) continue;
				if (c is ValueConstraint vc && vc.IsDimension) {
					exporter.AddDimension(vc);
				}
			}
		}
	}

	string ExportReplay() {
		return Poisson.ReplaySerializer.SaveToJson(editor.currentSketch.GetSketch());
	}

	ExportSettings settings;

	ExportSTLTool() {
		settings = new ExportSettings(this);
	}

	protected override void OnActivate() {
		Inspect(settings);
	}
}
