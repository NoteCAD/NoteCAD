using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EntityConfig : MonoBehaviour {

	public static EntityConfig instance;
	public Text labelPrefab;
	public Material meshMaterial;
	public Material loopMaterial;
	public LineCanvas lineCanvas;
	public StrokeStyles styles;

	void Start () {
		instance = this;
	}
}
