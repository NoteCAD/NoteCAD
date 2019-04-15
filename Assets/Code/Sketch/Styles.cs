using System;
using System.Collections.Generic;
using System.Xml;

public class Style
{
	public Id guid { get; internal set; }
	public StrokeStyle stroke = new StrokeStyle();

	public void Read(XmlNode xml) {
		stroke.name = xml.Attributes["name"].Value;
		stroke.width = xml.Attributes["width"].Value.ToFloat();
		//stroke.stippleWidth = xml.Attributes["stipple"].Value.ToFloat();
		stroke.depthTest = Convert.ToBoolean(xml.Attributes["depth"].Value);
		stroke.inPixels = Convert.ToBoolean(xml.Attributes["inPixels"].Value);
		stroke.queue = Convert.ToInt32(xml.Attributes["queue"].Value);
		stroke.color.r = xml.Attributes["r"].Value.ToFloat();
		stroke.color.g = xml.Attributes["g"].Value.ToFloat();
		stroke.color.b = xml.Attributes["b"].Value.ToFloat();
		stroke.color.a = xml.Attributes["a"].Value.ToFloat();
		if(xml.Attributes["pen"] != null) stroke.pen = Convert.ToInt32(xml.Attributes["pen"].Value);
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
		xml.WriteAttributeString("queue", stroke.queue.ToString());
		xml.WriteAttributeString("r", stroke.color.r.ToStr());
		xml.WriteAttributeString("g", stroke.color.g.ToStr());
		xml.WriteAttributeString("b", stroke.color.b.ToStr());
		xml.WriteAttributeString("a", stroke.color.a.ToStr());
		xml.WriteAttributeString("pen", stroke.pen.ToString());
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
