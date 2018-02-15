using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;

public struct Id {

	public static readonly Id Null = new Id(0);

	long id;
	long secondId;

	internal Id(long v, long s = 0) {
		id = v;
		secondId = s;
	}

	public long value { get { return id; } }
	public long second { get { return secondId; } }

	public Id WithSecond(long s) {
		return new Id(value, s);
	}

	public Id WithoutSecond() {
		return new Id(value, 0);
	}

	public static bool operator==(Id a, Id b) {
		return a.value == b.value && a.second == b.second;
	}

	public static bool operator!=(Id a, Id b) {
		return a.value != b.value || a.second != b.second;
	}

	public override string ToString() {
		if(second == 0) return value.ToString("X");
		return value.ToString("X") + ":" + second.ToString("X");
	}
}

public class IdGenerator {
	long maxId = 0;

	public Id New() {
		return new Id(++maxId);
	}

	public Id Create(long id) {
		maxId = Math.Max(maxId, id);
		return new Id(id);
	}

	public Id Create(string str) {
		long id = long.Parse(str, NumberStyles.HexNumber);
		maxId = Math.Max(maxId, id);
		return new Id(id);
	}

	public void Clear() {
		maxId = 0;
	}
}

public class IdPath {
	public List<Id> path = new List<Id>();

	static readonly char[] pathSeparator = new char[] {'-'};
	static readonly char[] secondSeparator = new char[] {':'};

	public void Parse(string str) {
		var ids = str.Split(pathSeparator, StringSplitOptions.RemoveEmptyEntries);
		path.Clear();
		for(int i = 0; i < ids.Length; i++) {
			var vals = ids[i].Split(secondSeparator);
			long value = long.Parse(vals[0], NumberStyles.HexNumber);
			long second = 0; 
			if(vals.Length > 1) {
				second = long.Parse(vals[1], NumberStyles.HexNumber);
			}
			path.Add(new Id(value, second));
		}
	}

	public static IdPath From(string str) {
		var result = new IdPath();
		result.Parse(str);
		return result;
	}

	public override string ToString() {
		string result = "";
		for(int i = 0; i < path.Count; i++) {
			if(i != 0) result += "-";
			result += path[i].value.ToString("X");
			if(path[i].second != 0) {
				result += ":" + path[i].second.ToString("X");
				// "1-2-3-4:3"
			}
		}
		return result;
	}

	public void Write(XmlTextWriter xml, string name) {
		xml.WriteStartElement("ref");
		xml.WriteAttributeString("name", name);
		xml.WriteAttributeString("path", ToString());
		xml.WriteEndElement();
	}

	public void Read(XmlNode xml) {
		path.Clear();
		Parse(xml.Attributes["path"].Value);
	}

	public static bool operator==(IdPath a, IdPath b) {
		return a.ToString() == b.ToString();
	}

	public static bool operator!=(IdPath a, IdPath b) {
		return a.ToString() != b.ToString();
	}
}
