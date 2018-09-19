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

}
