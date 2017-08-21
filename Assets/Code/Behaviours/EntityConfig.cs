using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityConfig : MonoBehaviour {

	public PointBehaviour pointPrefab;
	public LineBehaviour linePrefab;
	public static EntityConfig instance;

	void Start () {
		instance = this;
	}
}
