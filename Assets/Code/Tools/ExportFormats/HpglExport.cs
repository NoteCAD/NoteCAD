using System.Collections.Generic;
using System.Text;
using UnityEngine;
using NoteCAD;

public class HpglExport : ICanvas {
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
					phase = 0f;
					dash = 0;
				}

			}
		}
		return builder.ToString();
	}
}
