using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.Text;

public class ExportSTLTool : Tool {

	enum FileType {
		Stl,
		Obj,
		Hpgl,
		Replay,
		Svg,
		GCode
	};

	[Serializable]
	class Settings {
		ExportSTLTool tool;
		public FileType fileType;
		public bool constraints = false;
		public bool dimensions = false;
		public bool construction = true;

		public Settings(ExportSTLTool t) {
			tool = t;
		}

		[RuntimeInspectorNamespace.RuntimeInspectorButton("TextFillup", false, RuntimeInspectorNamespace.ButtonVisibility.InitializedObjects)]
		public void TextFillup() {
			if (DetailEditor.instance.currentSketch == null) {
				return;
			}
			NoteCADJS.LoadData(FillupLoaded, "isp");
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
			}
		}

	}

	class HpglCanvas : ICanvas {
		struct Line {
			public Vector3 a;
			public Vector3 b;
			public void Swap() {
				Vector3 t = a;
				a = b;
				b = t;
			}
		}
		Dictionary<Style, List<Line>> allLines = new Dictionary<Style, List<Line>>();

		Style currrentStyle;
		List<Line> currentLines;
		
		public void DrawLine(Vector3 a, Vector3 b) {
			currentLines.Add(new Line { a = a, b = b });
		}

		public void DrawPoint(Vector3 pt) {
		}

		public void SetStyle(Style style) {
			if(!allLines.ContainsKey(style)) {
				allLines[style] = new List<Line>();
			}
			currrentStyle = style;
			currentLines = allLines[style];
		}

		int ToHpgl(float x) {
			return (int)(x * 40f);
		}

		string ToHpgl(Vector3 v) {
			return ToHpgl(v.x).ToString() + "," + ToHpgl(v.y).ToString();
		}
		
		public bool HpglEquals(Vector3 v0, Vector3 v1) {
			return ToHpgl(v0.x) == ToHpgl(v1.x) && ToHpgl(v0.y) == ToHpgl(v1.y);
		}

		void SP(StringBuilder builder, Style style) {
			builder.Append("SP" + style.pen.ToString() + ";\n");
			//builder.Append("PW" + style.widthMm.ToStr() + ";\n");
		}

		void PU(StringBuilder builder, Vector3 pt) {
			builder.Append("PU" + ToHpgl(pt) + ";\n");
		}

		void PD(StringBuilder builder, Vector3 pt) {
			builder.Append("PD" + ToHpgl(pt) + ";\n");
		}

		public string GetResult() {
			
			StringBuilder builder = new StringBuilder();
			builder.Append("IN;\nPA0,0;\n");
			var pixel = DraftStroke.getGlobalPixelSize();
			foreach(var style in allLines.Keys) {
				if(!style.export) continue;
				var lines = allLines[style];
				SP(builder, style);
				float phase = 0f;
				int dash = 0;
				for(int i = 0; i < lines.Count; i++) {
					var l = lines[i];
					bool needZeroPhase = false;
					if(i < lines.Count - 1) {
						var nl = lines[i + 1];
						if(l.b == nl.b) { 
							nl.Swap();
							lines[i + 1] = nl;
						} else 
						if(l.a == nl.b) {
							nl.Swap();
							lines[i + 1] = nl;
							l.Swap();
						} else
						if(l.a == nl.a) {
							l.Swap();
						}
						if(l.b != nl.a) needZeroPhase = true;
					}
					var t = l.b - l.a;
					var dir = t.normalized;
					var len = t.magnitude;

					if(style.stroke.dashes.Length > 1) {
						Vector3 pos = l.a;
						while(len > 0f) {
							var delta = style.stroke.dashes[dash] * (float)style.stroke.dashesScaleMm(pixel) - phase;
							if(delta > len) {
								phase += len;
								delta = len;
							} else {
								phase = 0f;
							}
							if(dash % 2 == 0) PU(builder, pos);
							pos += dir * delta;
							if(dash % 2 == 0) PD(builder, pos);
							len -= delta;
							if(phase == 0f) {
								dash = (dash + 1) % style.stroke.dashes.Length;
							}
						}
					} else {
						PU(builder, l.a);
						PD(builder, l.b);
					}

					if(needZeroPhase) {
						//Debug.LogFormat("clear phase {0} {1}", l.b.ToStr(), nl.a.ToStr());
						phase = 0f;
						dash = 0;
					}

				}
			}
			return builder.ToString();
		}
	}

	class SvgCanvas : ICanvas {
		struct Line {
			public Vector3 a;
			public Vector3 b;
			public void Swap() {
				Vector3 t = a;
				a = b;
				b = t;
			}
		}
		Dictionary<Style, List<Line>> allLines = new Dictionary<Style, List<Line>>();

		Style currrentStyle;
		List<Line> currentLines;
		
		public void DrawLine(Vector3 a, Vector3 b) {
			a.y = -a.y;
			b.y = -b.y;
			currentLines.Add(new Line { a = a, b = b });
		}

		public void DrawPoint(Vector3 pt) {
		}

		public void SetStyle(Style style) {
			if(!allLines.ContainsKey(style)) {
				allLines[style] = new List<Line>();
			}
			currrentStyle = style;
			currentLines = allLines[style];
		}

		Vector3 pos;
		bool canContinue = false;
		bool wasPD = false;
		void SP(StringBuilder builder, Style style) {
			//builder.Append("SP" + style.pen.ToString() + ";\n");
			//builder.Append("PW" + style.widthMm.ToStr() + ";\n");
			wasPD = false;
		}

		void PU(StringBuilder builder, Vector3 pt) {
			canContinue = (pos == pt);
			pos = pt;
		}

		void PD(StringBuilder builder, Vector3 pt) {
			//builder.AppendLine($"\t<line stroke=\"black\" x1=\"{pos.x.ToStr()}\" y1=\"{pos.y.ToStr()}\" x2=\"{pt.x.ToStr()}\" y2=\"{pt.y.ToStr()}\"/>");
			if (canContinue && wasPD) {
				builder.Append($"L{pt.x.ToLStr()} {pt.y.ToLStr()} ");
			} else {
				builder.Append($"M{pos.x.ToLStr()} {pos.y.ToLStr()} L{pt.x.ToLStr()} {pt.y.ToLStr()} ");
				wasPD = true;
			}
			pos = pt;
		}

		public string GetResult() {
			
			StringBuilder builder = new StringBuilder();

			Vector3 min = Vector3.zero;
			Vector3 max = Vector3.zero;
			bool first = true;

			foreach(var style in allLines.Keys) {
				var lines = allLines[style];
				for(int i = 0; i < lines.Count; i++) {
					var l = lines[i];
					if(first) {
						min = l.a;
						max = l.a;
						first = false;
					}
					min = Vector3.Min(min, Vector3.Min(l.a, l.b));
					max = Vector3.Max(max, Vector3.Max(l.a, l.b));
				}
			}

			var size = max - min;
			builder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>");
			builder.AppendLine("<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">");
			builder.AppendLine($"<svg width=\"{size.x.ToLStr()}\" height=\"{size.y.ToLStr()}\" viewBox=\"{min.x.ToLStr()} {min.y.ToLStr()} {size.x.ToLStr()} {size.y.ToLStr()}\" xmlns=\"http://www.w3.org/2000/svg\">");

			var pixel = DraftStroke.getGlobalPixelSize();
			foreach(var style in allLines.Keys) {
				if(!style.export) continue;
				var lines = allLines[style];
				SP(builder, style);
				builder.Append("\t<path fill=\"none\" stroke=\"black\" d=\"");
				float phase = 0f;
				int dash = 0;
				for(int i = 0; i < lines.Count; i++) {
					var l = lines[i];
					bool needZeroPhase = false;
					if(i < lines.Count - 1) {
						var nl = lines[i + 1];
						if(l.b == nl.b) { 
							nl.Swap();
							lines[i + 1] = nl;
						} else 
						if(l.a == nl.b) {
							nl.Swap();
							lines[i + 1] = nl;
							l.Swap();
						} else
						if(l.a == nl.a) {
							l.Swap();
						}
						if(l.b != nl.a) needZeroPhase = true;
					}
					var t = l.b - l.a;
					var dir = t.normalized;
					var len = t.magnitude;

					if(style.stroke.dashes.Length > 1) {
						Vector3 pos = l.a;
						while(len > 0f) {
							var delta = style.stroke.dashes[dash] * (float)style.stroke.dashesScaleMm(pixel) - phase;
							if(delta > len) {
								phase += len;
								delta = len;
							} else {
								phase = 0f;
							}
							if(dash % 2 == 0) PU(builder, pos);
							pos += dir * delta;
							if(dash % 2 == 0) PD(builder, pos);
							len -= delta;
							if(phase == 0f) {
								dash = (dash + 1) % style.stroke.dashes.Length;
							}
						}
					} else {
						PU(builder, l.a);
						PD(builder, l.b);
					}

					if(needZeroPhase) {
						//Debug.LogFormat("clear phase {0} {1}", l.b.ToStr(), nl.a.ToStr());
						phase = 0f;
						dash = 0;
					}

				}
				builder.Append("\"/>\n");
			}
			builder.AppendLine("</svg>");
			return builder.ToString();
		}
	}

	class GCodeCanvas : ICanvas {
		struct Line {
			public Vector3 a;
			public Vector3 b;
			public void Swap() {
				Vector3 t = a;
				a = b;
				b = t;
			}
		}
		Dictionary<Style, List<Line>> allLines = new Dictionary<Style, List<Line>>();

		Style currentStyle;
		List<Line> currentLines;
		public int defaultSpeed = 500;
		public int repeats = -2;
		public float repeatStep = 0.15f;
		Vector3 shift = Vector3.zero;

		
		public void DrawLine(Vector3 a, Vector3 b) {
			currentLines.Add(new Line { a = a, b = b });
		}

		public void DrawPoint(Vector3 pt) {
		}

		public void SetStyle(Style style) {
			if(!allLines.ContainsKey(style)) {
				allLines[style] = new List<Line>();
			}
			currentStyle = style;
			currentLines = allLines[style];
		}

		Vector3 pos;
		bool canContinue = false;
		bool penDown = false;
		int currentSpeed = 0;
		void SP(StringBuilder builder, Style style) {
			//builder.Append("SP" + style.pen.ToString() + ";\n");
			//builder.Append("PW" + style.widthMm.ToStr() + ";\n");
			currentStyle = style;
			penDown = false;
		}

		void PU(StringBuilder builder, Vector3 pt) {
			pt += shift;
			canContinue = (pos - pt).sqrMagnitude < 1e-3;
			pos = pt;
			if (!canContinue) {
				builder.AppendLine("S0");
				builder.AppendLine($"G0 X{pt.x:0.###} Y{pt.y:0.###}");
				penDown = false;
			}
		}

		void PD(StringBuilder builder, Vector3 pt) {
			if (!penDown) {
				builder.AppendLine("S255");
				penDown = true;
			}
			string speedStr = "";
			if (currentSpeed != currentStyle.speed) {
				currentSpeed = currentStyle.speed;
				var actualSpeed = (currentSpeed == 0) ? defaultSpeed : currentSpeed;
				speedStr = $" F{actualSpeed}";
			}
			pt += shift;
			builder.AppendLine($"G1 X{pt.x:0.###} Y{pt.y:0.###}{speedStr}");
			pos = pt;
		}

		public string GetResult() {
			
			StringBuilder builder = new StringBuilder();

			Vector3 min = Vector3.zero;
			Vector3 max = Vector3.zero;
			bool first = true;

			foreach(var style in allLines.Keys) {
				var lines = allLines[style];
				for(int i = 0; i < lines.Count; i++) {
					var l = lines[i];
					if(first) {
						min = l.a;
						max = l.a;
						first = false;
					}
					min = Vector3.Min(min, Vector3.Min(l.a, l.b));
					max = Vector3.Max(max, Vector3.Max(l.a, l.b));
				}
			}

			var size = max - min;
			builder.AppendLine("G90");
			builder.AppendLine("M3 S0");
			builder.AppendLine($"F{defaultSpeed}");
			builder.AppendLine("G0 X0 Y0 Z0");

			for (int x = 0; x < Mathf.Abs(repeats); x++) {
				for (int y = 0; y < Mathf.Abs(repeats); y++) {
					//if (repeats < 0 && x != y) {
						//continue;
					//}
					shift = (new Vector3(x, y, 0.0f)) * repeatStep;
					var pixel = DraftStroke.getGlobalPixelSize();
					foreach(var style in allLines.Keys) {
						if(!style.export) continue;
						var lines = allLines[style];
						SP(builder, style);
						float phase = 0f;
						int dash = 0;
						for(int i = 0; i < lines.Count; i++) {
							var l = lines[i];
							bool needZeroPhase = false;
							if(i < lines.Count - 1) {
								var nl = lines[i + 1];
								if(l.b == nl.b) { 
									nl.Swap();
									lines[i + 1] = nl;
								} else 
								if(l.a == nl.b) {
									nl.Swap();
									lines[i + 1] = nl;
									l.Swap();
								} else
								if(l.a == nl.a) {
									l.Swap();
								}
								if(l.b != nl.a) needZeroPhase = true;
							}
							var t = l.b - l.a;
							var dir = t.normalized;
							var len = t.magnitude;

							if(style.stroke.dashes.Length > 1) {
								Vector3 pos = l.a;
								while(len > 0f) {
									var delta = style.stroke.dashes[dash] * (float)style.stroke.dashesScaleMm(pixel) - phase;
									if(delta > len) {
										phase += len;
										delta = len;
									} else {
										phase = 0f;
									}
									if(dash % 2 == 0) PU(builder, pos);
									pos += dir * delta;
									if(dash % 2 == 0) PD(builder, pos);
									len -= delta;
									if(phase == 0f) {
										dash = (dash + 1) % style.stroke.dashes.Length;
									}
								}
							} else {
								PU(builder, l.a);
								PD(builder, l.b);
							}

							if(needZeroPhase) {
								//Debug.LogFormat("clear phase {0} {1}", l.b.ToStr(), nl.a.ToStr());
								phase = 0f;
								dash = 0;
							}

						}
					}
				}
			}
			builder.AppendLine("S0");
			builder.AppendLine("M5");
			builder.AppendLine("G0 X0 Y0 Z0");
			return builder.ToString();
		}
	}

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

	string ExportReplay() {
		return Poisson.ReplaySerializer.SaveToJson(editor.currentSketch.GetSketch());
	}

	Settings settings;

	ExportSTLTool() {
		settings = new Settings(this);
	}

	protected override void OnActivate() {
		Inspect(settings);
	}
}
