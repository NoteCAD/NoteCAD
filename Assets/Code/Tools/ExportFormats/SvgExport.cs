using System.Collections.Generic;
using System.Text;
using UnityEngine;
using NoteCAD;

public class SvgExport : ICanvas {
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
		currentStyle = style;
		currentLines = allLines[style];
	}

	Vector3 pos;
	bool canContinue = false;
	bool wasPD = false;
	void SP(StringBuilder builder, Style style) {
		wasPD = false;
	}

	void PU(StringBuilder builder, Vector3 pt) {
		canContinue = (pos == pt);
		pos = pt;
	}

	void PD(StringBuilder builder, Vector3 pt) {
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
