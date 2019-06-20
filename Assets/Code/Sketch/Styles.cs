using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using RuntimeInspectorNamespace;

[Serializable]
public class Style {

	public Id guid { get; internal set; }

	public string name {
		get {
			return stroke.name;
		}

		set {
			stroke.name = value;
		}
	}

	public bool construction = false;
	public bool export = true;
	public int pen = 0;
	public StrokeStyle stroke = new StrokeStyle();


	public void Read(XmlNode xml) {
		stroke.name = xml.Attributes["name"].Value;
		stroke.width = xml.Attributes["width"].Value.ToFloat();
		//stroke.stippleWidth = xml.Attributes["stipple"].Value.ToFloat();
		stroke.depthTest = Convert.ToBoolean(xml.Attributes["depth"].Value);
		stroke.inPixels = Convert.ToBoolean(xml.Attributes["inPixels"].Value);
		if(xml.Attributes["dashesInPixels"] != null) {
			stroke.dashesInPixels = Convert.ToBoolean(xml.Attributes["dashesInPixels"].Value);
		} else {
			stroke.dashesInPixels = stroke.inPixels;
		}
		stroke.queue = Convert.ToInt32(xml.Attributes["queue"].Value);
		stroke.color.r = xml.Attributes["r"].Value.ToFloat();
		stroke.color.g = xml.Attributes["g"].Value.ToFloat();
		stroke.color.b = xml.Attributes["b"].Value.ToFloat();
		stroke.color.a = xml.Attributes["a"].Value.ToFloat();
		if(xml.Attributes["pen"] != null) pen = Convert.ToInt32(xml.Attributes["pen"].Value);
		if(xml.Attributes["construction"] != null) construction = Convert.ToBoolean(xml.Attributes["construction"].Value);
		if(xml.Attributes["export"] != null) export = Convert.ToBoolean(xml.Attributes["export"].Value);
		var sep = new char[1] { ' ' };
		var dashes = xml.Attributes["dashes"].Value.Split(sep, StringSplitOptions.RemoveEmptyEntries);
		stroke.dashes = new float[dashes.Length];
		for (int i = 0; i < dashes.Length; i++) {
			stroke.dashes[i] = dashes[i].ToFloat();
		}
	}

	public void Write(XmlTextWriter xml) {
		xml.WriteStartElement("style");
		xml.WriteAttributeString("name", stroke.name);
		xml.WriteAttributeString("id", guid.ToString());
		xml.WriteAttributeString("width", stroke.width.ToStr());
		//xml.WriteAttributeString("stipple", stroke.stippleWidth.ToStr());
		xml.WriteAttributeString("depth", stroke.depthTest.ToString());
		xml.WriteAttributeString("inPixels", stroke.inPixels.ToString());
		xml.WriteAttributeString("dashesInPixels", stroke.dashesInPixels.ToString());
		xml.WriteAttributeString("queue", stroke.queue.ToString());
		xml.WriteAttributeString("r", stroke.color.r.ToStr());
		xml.WriteAttributeString("g", stroke.color.g.ToStr());
		xml.WriteAttributeString("b", stroke.color.b.ToStr());
		xml.WriteAttributeString("a", stroke.color.a.ToStr());
		xml.WriteAttributeString("pen", pen.ToString());
		xml.WriteAttributeString("construction", construction.ToString());
		xml.WriteAttributeString("export", export.ToString());
		string dashes = "";
		for(int i = 0; i < stroke.dashes.Length; i++) {
			if(i != 0) dashes += " ";
			dashes += stroke.dashes[i].ToStr();
		}
		xml.WriteAttributeString("dashes", dashes);
		xml.WriteEndElement();
	}

}

public class Styles {
	Dictionary<Id, Style> styles = new Dictionary<Id, Style>();
	IdGenerator idGenerator = new IdGenerator();

	public Style GetStyle(string name) {
		return styles.Values.FirstOrDefault(s => s.stroke.name == name);
	}
	public Style AddStyle() {
		var s = new Style();
		s.guid = idGenerator.New();
		styles.Add(s.guid, s);
		return s;
	}

	public void RemoveStyle(Id id) {
		styles.Remove(id);
	}

	public Style GetStyle(Id id) {
		return styles[id];
	}

	public IEnumerable<Style> GetStyles() {
		return styles.Values;
	}

	public void Read(XmlNode xml) {
		styles.Clear();
		foreach(XmlNode xmlChild in xml.ChildNodes) {
			var style = new Style();
			style.Read(xmlChild);
			style.guid = idGenerator.Create(xmlChild.Attributes["id"].Value);
			styles.Add(style.guid, style);
		}
	}

	public void Write(XmlTextWriter xml) {
		xml.WriteStartElement("styles");
		foreach(var style in styles.Values) {
			style.Write(xml);
		}
		xml.WriteEndElement();
	}
}
