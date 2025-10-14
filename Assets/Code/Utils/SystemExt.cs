using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

public static class SystemExt {
	static NumberFormatInfo nfi = new NumberFormatInfo();

	static SystemExt() {
		nfi.NumberDecimalSeparator = ".";
	}

	public static double ToDouble(this string str) {
		return double.Parse(str, nfi);
	}

	public static float ToFloat(this string str) {
		return float.Parse(str, nfi);
	}

	public static string ToStr(this double value) {
		return value.ToString(nfi);
	}

	public static string ToStr(this float value) {
		return value.ToString(nfi);
	}

	public static string ToLStr(this double value) {
		return value.ToString(nfi).ToLower();
	}

	public static string ToLStr(this float value) {
		return value.ToString(nfi).ToLower();
	}

	public static void ToEnum<T>(this string str, ref T e) where T: Enum {
		e = (T)Enum.Parse(e.GetType(), str);
	}

	public static T ToEnum<T>(this string str) where T: Enum {
		return (T)Enum.Parse(typeof(T), str);
	}

	public static void ForEach<T>(this IEnumerable<T> e, Action<T> action) {
		foreach(var i in e) {
			action(i);
		}
	}

	public static void Swap<T>(ref T a, ref T b) {
		var t = a;
		a = b;
		b = t;
	}

	public static void GetAttribute(this XmlNode xml, string key, ref bool value) {
		if(xml.Attributes[key] != null) {
			value = Convert.ToBoolean(xml.Attributes[key].Value);
		}
	}

	public static void GetAttribute(this XmlNode xml, string key, ref int value) {
		if(xml.Attributes[key] != null) {
			value = Convert.ToInt32(xml.Attributes[key].Value);
		}
	}

	public static void GetAttribute(this XmlNode xml, string key, ref double value) {
		if(xml.Attributes[key] != null) {
			value = Convert.ToDouble(xml.Attributes[key].Value);
		}
	
	}

	public static void GetAttribute(this XmlNode xml, string key, ref string value) {
		if(xml.Attributes[key] != null) {
			value = xml.Attributes[key].Value;
		}
	}

	public static void GetAttribute<T>(this XmlNode xml, string key, ref T value) where T: Enum {
		if(xml.Attributes[key] != null) {
			xml.Attributes[key].Value.ToEnum(ref value);
		}
	}

}
