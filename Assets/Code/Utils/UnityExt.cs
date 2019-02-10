using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UnityExt {
	public static void SetMatrix(this Transform tf, Matrix4x4 mtx) {
		tf.position = mtx.MultiplyPoint3x4(Vector3.zero);
		tf.rotation = Quaternion.LookRotation(mtx.GetColumn(2), mtx.GetColumn(1));
	}

	public static Matrix4x4 Basis(Vector3 x, Vector3 y, Vector3 z, Vector3 p) {
		Matrix4x4 result = Matrix4x4.identity;
		result.SetColumn(0, x);
		result.SetColumn(1, y);
		result.SetColumn(2, z);
		Vector4 pp = p;
		pp.w = 1;
		result.SetColumn(3, pp);
		return result;
	}

	public static Vector3 ToVector3(this string str) {
		var values = str.Split(' ');
		var vec = new Vector3(values[0].ToFloat(), values[1].ToFloat(), values[2].ToFloat());
		return vec;
	}

	public static Quaternion ToQuaternion(this string str) {
		var values = str.Split(' ');
		var q = Quaternion.identity;
		q.x = values[0].ToFloat();
		q.y = values[1].ToFloat();
		q.z = values[2].ToFloat();
		q.w = values[3].ToFloat();
		return q;
	}

	public static string ToStr(this Vector3 v) {
		return v.x.ToStr() + " " + v.y.ToStr() + " " + v.z.ToStr();
	}

	public static string ToStr(this Quaternion q) {
		return q.x.ToStr() + " " + q.y.ToStr() + " " + q.z.ToStr() + " " + q.w.ToStr();
	}

	public static Vector3 WorldToGuiPoint(this Camera camera, Vector3 position) {
		var result = camera.WorldToScreenPoint(position);
		result.y = camera.pixelHeight - result.y;
		return result;
	}
}
