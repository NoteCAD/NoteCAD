using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using UnityEngine;

public abstract class Writer
{

	public abstract void WriteBeginArray(string name);
	public abstract void WriteEndArray();
	public abstract void WriteBeginArrayElement(string name);
	public abstract void WriteEndArrayElement();
	public abstract void WriteBeginElement(string name);
	public abstract void WriteEndElement();
	public abstract void WriteAttributeString(string name, string value);
	public abstract void WriteBeginFakeArray(string name);
	public abstract void WriteEndFakeArray();
}

public class WriterXml : Writer
{
	XmlWriter xml = null;

	public WriterXml(XmlWriter aXml) {
		xml = aXml;
	}

	public override void WriteBeginArray(string name) {
		xml.WriteStartElement(name);
	}
	
	public override void WriteEndArray() {
		xml.WriteEndElement();
	}

	public override void WriteBeginArrayElement(string name) {
		xml.WriteStartElement(name);
	}
	
	public override void WriteEndArrayElement() {
		xml.WriteEndElement();
	}

	public override void WriteBeginElement(string name) {
		xml.WriteStartElement(name);
	}
	
	public override void WriteEndElement() {
		xml.WriteEndElement();
	}

	public override void WriteAttributeString(string name, string value) {
		xml.WriteAttributeString(name, value);
	}

	public override void WriteBeginFakeArray(string v) {
	}
	
	public override void WriteEndFakeArray() {
	}
}

public class WriterJSON : Writer
{
	enum JsonType {
		Root,
		Key,
		Value,
		Object,
		Array
	};

	class Json {
		public JsonType type = JsonType.Value;
		public string str;
		public List<Json> list = new();
		public Json parent;

		public Json(string aStr, JsonType aType, Json aParent) {
			str = aStr;
			type = aType;
			parent = aParent;
			if (parent != null) {
				parent.list.Add(this);
			}
		}

		const char indentChar = '\t';

		public void ToString(StringBuilder builder, string indent) {
			switch(type) {
				case JsonType.Root: {
					builder.Append("{\n");
					break;
				}
				case JsonType.Key: {
					builder.Append(indent);
					builder.Append("\"");
					builder.Append(str);
					builder.Append("\": ");
					break;
				}
				case JsonType.Value: {
					var key = parent.str;
					
					bool nonStr = key != "id" && key != "activeFeature" && 
						(int.TryParse(str, out _) || double.TryParse(str, out _));

					if (!nonStr) builder.Append("\"");
					builder.Append(str);
					if (!nonStr) builder.Append("\"");
					return;
				}
				case JsonType.Array: {
					if (parent.type != JsonType.Key) {
						builder.Append(indent);
					}
					builder.Append("[\n");
					break;
				}
				case JsonType.Object: {
					if (parent.type != JsonType.Key) {
						builder.Append(indent);
					}
					builder.Append("{\n");
					break;
				}
			}

			var nextIndent = indent;
			if (type != JsonType.Key) {
				nextIndent += indentChar;
			}
			for (int i = 0; i < list.Count; i++) {
				list[i].ToString(builder, nextIndent);
				if (i != list.Count - 1) {
					builder.Append(",");
				}
				if (type != JsonType.Key) {
					builder.Append("\n");
				}
			}

			switch(type) {
				case JsonType.Root: {
					builder.Append(indent);
					builder.Append("}\n");
					break;
				}
				case JsonType.Array: {
					builder.Append(indent);
					builder.Append("]");
					break;
				}
				case JsonType.Object: {
					builder.Append(indent);
					builder.Append("}");
					break;
				}
			}
		}
	}

	Json json;
	Json cur;
	
	public WriterJSON() {
		json = new("", JsonType.Root, null);
		cur = json;
	}
 
	public override void WriteBeginArray(string name) {
		Json key = new(name, JsonType.Key, cur);
		Json array = new(name, JsonType.Array, key);
		cur = array;
	}
	
	public override void WriteEndArray() {
		if (cur.type != JsonType.Array || cur.parent == null || cur.parent.parent == null) {
			throw new System.Exception("WriterJSON.WriteEndArray: begin wasn't called");
		}
		cur = cur.parent.parent;
	}

	public override void WriteBeginArrayElement(string name) {
		if (cur.type != JsonType.Array || cur.parent == null) {
			throw new System.Exception("WriterJSON.WriteBeginArrayElement: begin wasn't called");
		}
		Json elem = new(name, JsonType.Object, cur);
		cur = elem;
	}
	
	public override void WriteEndArrayElement() {
		if (cur.type != JsonType.Object || cur.parent == null || cur.parent.type != JsonType.Array) {
			throw new System.Exception("WriterJSON.WriteEndArrayElement: begin wasn't called");
		}
		cur = cur.parent;
	}

	public override void WriteBeginElement(string name) {
		Json key = new(name, JsonType.Key, cur);
		Json elem = new(name, JsonType.Object, key);
		cur = elem;
	}
	
	public override void WriteEndElement() {
		if (cur.type != JsonType.Object || cur.parent == null || cur.parent.parent == null) {
			throw new System.Exception("WriterJSON.WriteEndElement: begin wasn't called");
		}
		cur = cur.parent.parent;
	}

	public override void WriteAttributeString(string name, string value) {
		Json key = new(name, JsonType.Key, cur);
		new Json(value, JsonType.Value, key);
	}

	public override void WriteBeginFakeArray(string name) {
		WriteBeginArray(name);
	}
	
	public override void WriteEndFakeArray() {
		WriteEndArray();
	}

	public override string ToString() {
		StringBuilder builder = new();
		json.ToString(builder, "");
		return builder.ToString();
	}
}


