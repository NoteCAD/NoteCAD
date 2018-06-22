using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UnityExt {
	public static void SetMatrix(this Transform tf, Matrix4x4 mtx) {
		tf.position = mtx.MultiplyPoint3x4(Vector3.zero);
		tf.rotation = Quaternion.LookRotation(mtx.GetColumn(2), mtx.GetColumn(1));
	}
}
