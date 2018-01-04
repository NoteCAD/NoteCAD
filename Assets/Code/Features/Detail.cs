using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using UnityEngine;

public class Detail : Feature {
	public List<Feature> features = new List<Feature>();

	protected override void OnUpdate() {
		foreach(var f in features) {
			f.Update();
		}
		if(features.Any(f => f.dirty)) {
			MarkDirty();
		}
	}

	public void AddFeature(Feature feature) {
		features.Add(feature);
	}

	protected override void OnClear() {
		foreach(var f in features) {
			f.Clear();
		}
		features.Clear();
	}

	protected override void OnUpdateDirty() {
		foreach(var f in features) {
			f.UpdateDirty();
		}
	}

	public void ReadXml(string str) {
		Clear();
		var xml = new XmlDocument();
		xml.LoadXml(str);

		foreach(XmlNode node in xml.DocumentElement) {
			if(node.Name != "feature") continue;
			var type = node.Attributes["type"].Value;
			var item = Type.GetType(type).GetConstructor(new Type[0]).Invoke(new object[0]) as Feature;
			item.Read(node);
			features.Add(item);
		}
	}

	public string WriteXml() {
		var text = new StringWriter();
		var xml = new XmlTextWriter(text);
		xml.Formatting = Formatting.Indented;
		xml.IndentChar = '\t';
		xml.Indentation = 1;
		xml.WriteStartDocument();
		xml.WriteStartElement("features");
		foreach(var f in features) {
			f.Write(xml);
		}
		xml.WriteEndElement();
		return text.ToString();
	}

	protected override SketchObject OnHover(Vector3 mouse, Camera camera, ref double objDist) {
		double min = -1.0;
		SketchObject result = null;
		foreach(var f in features) {
			double dist = -1.0;
			var hovered = f.Hover(mouse, camera, ref dist);
			if(dist < 0.0) continue;
			if(dist > 5.0) continue;
			if(min >= 0.0 && dist > min) continue;
			result = hovered;
			min = dist;
		}
		objDist = min;
		return result;
	}

}
