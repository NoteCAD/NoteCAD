using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EntityConfig : MonoBehaviour {

	public static EntityConfig instance;
	public ConstraintBehaviour constraint;
	public Text labelPrefab;
	public Material meshMaterial;
	public LineCanvas lineCanvas;

	void Start () {
		instance = this;
	}
}
