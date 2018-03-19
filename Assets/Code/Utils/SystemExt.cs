using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

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

	public static void ToEnum<T>(this string str, ref T e) {
		e = (T)Enum.Parse(typeof(T), str);
	}

	public static T ToEnum<T>(this string str) {
		return (T)Enum.Parse(typeof(T), str);
	}
}
