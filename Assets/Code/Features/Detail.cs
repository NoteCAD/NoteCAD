using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;

public class Detail : Feature {
	public List<Feature> features = new List<Feature>();

	protected override void OnUpdate() {
		foreach(var f in features) {
			f.Update();
		}
	}

	public void AddFeature(Feature feature) {
		features.Add(feature);
	}

	void Clear() {
		foreach(var f in features) {
			f.Clear();
		}
		features.Clear();
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



}
