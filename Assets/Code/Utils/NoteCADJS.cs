using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;

public class NoteCADJS : MonoBehaviour {

#if UNITY_EDITOR
	public static void SaveData(string data, string filename) {
		var path = UnityEditor.EditorUtility.SaveFilePanel("Save NoteCAD file", "", filename, "xml");
		System.IO.File.WriteAllText(path, data);
	}
#elif UNITY_WEBGL
	[DllImport("__Internal")]
	public static extern void SaveData(string data, string filename);
#else
	public static void SaveData(string data, string filename) {
		
	}
#endif

#if UNITY_EDITOR
	public static void LoadData(Action<string> callback) {
		var path = UnityEditor.EditorUtility.OpenFilePanel("Load NoteCAD file", "", "xml");
		callback(System.IO.File.ReadAllText(path));
	}
#elif UNITY_WEBGL
	[DllImport("__Internal")]
	private static extern string LoadDataInternal();
	private static Action<string> loadCallback;
	public static void LoadData(Action<string> callback) {
		loadCallback = callback;
		LoadDataInternal();
	}
	public void LoadDataCallback(string d) {
		if(loadCallback != null) {
			loadCallback(d);
			loadCallback = null;
		}
	}
#else
	public static void LoadData(Action<string> callback) {
	}
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
	[DllImport("__Internal")]
	public static extern string GetParam(string name);
#else
	public static string GetParam(string name) {
		return "";
	}
#endif


}
