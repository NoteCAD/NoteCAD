using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using UnityEngine;

public class Detail : Feature {

	public List<Feature> features = new List<Feature>();

	public override GameObject gameObject {
		get {
			return null;
		}
	}

	public override CADObject GetChild(Guid guid) {
		return features.Find(f => f.guid == guid);
	}

	protected override void OnUpdate() {
		foreach(var f in features) {
			f.Update();
		}
		if(features.Any(f => f.dirty)) {
			MarkDirty();
		}
	}

	public void AddFeature(Feature feature) {
		feature.detail = this;
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
			AddFeature(item);
			item.Read(node);
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

	public ISketchObject HoverUntil(Vector3 mouse, Camera camera, Matrix4x4 tf, ref double objDist, Feature feature) {
		double min = -1.0;
		ISketchObject result = null;
		foreach(var f in features) {
			if(!f.ShouldHoverWhenInactive() && !f.active) {
				continue;
			}
			double dist = -1.0;
			var hovered = f.Hover(mouse, camera, tf, ref dist);

			if(dist >= 0.0 && dist < 5.0 && (min < 0.0 || dist < min)) {
				result = hovered;
				min = dist;
			}
			if(f == feature) break;
		}
		objDist = min;
		return result;
	}

	protected override ISketchObject OnHover(Vector3 mouse, Camera camera, Matrix4x4 tf, ref double objDist) {
		return HoverUntil(mouse, camera, tf, ref objDist, features.Last());
	}

	public Feature GetFeature(Guid guid) {
		return features.Find(f => f.guid == guid);
	}

}
