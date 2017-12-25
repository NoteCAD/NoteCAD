using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointBehaviour : EntityBehaviour {

	public void LateUpdate() {
		var point = entity as PointEntity;
		transform.position = point.GetPosition();
	}

}
