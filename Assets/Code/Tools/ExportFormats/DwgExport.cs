using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.IO;
using ACadSharp.Tables;

public class DwgExport : ICanvas {
	CadDocument document = new CadDocument();
	Style currentStyle;

	public void DrawLine(Vector3 a, Vector3 b) {
		var line = new ACadSharp.Entities.Line {
			StartPoint = new CSMath.XYZ(a.x, a.y, a.z),
			EndPoint = new CSMath.XYZ(b.x, b.y, b.z)
		};
		if (currentStyle != null) {
			line.Layer = GetOrCreateLayer(currentStyle);
		}
		document.Entities.Add(line);
	}

	public void DrawPoint(Vector3 pt) {
	}

	public void SetStyle(Style style) {
		currentStyle = style;
	}

	Layer GetOrCreateLayer(Style style) {
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

	public byte[] GetResult() {
		using (var stream = new MemoryStream()) {
			using (var writer = new DwgWriter(stream, document)) {
				writer.Configuration.CloseStream = false;
				writer.Write();
			}
			return stream.ToArray();
		}
	}
}
