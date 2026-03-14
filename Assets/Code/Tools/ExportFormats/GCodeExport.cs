using System.Collections.Generic;
using System.Text;
using UnityEngine;
using NoteCAD;

public class GCodeExport : ICanvas {
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

		builder.AppendLine("G90");
		builder.AppendLine("M3 S0");
		builder.AppendLine($"F{defaultSpeed}");
		builder.AppendLine("G0 X0 Y0 Z0");

		for (int x = 0; x < Mathf.Abs(repeats); x++) {
			for (int y = 0; y < Mathf.Abs(repeats); y++) {
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
