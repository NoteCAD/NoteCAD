using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConstraintBehaviour : MonoBehaviour {
	public ValueConstraint constraint;
	public Text text;

	private void Awake() {
		text = GameObject.Instantiate(EntityConfig.instance.labelPrefab, DetailEditor.instance.labelParent.transform);
	}

	public void Update() {
		transform.position = constraint.pos;
		text.transform.position = Camera.main.WorldToScreenPoint(transform.position);
		text.text = Math.Abs(constraint.GetValue()).ToString("0.##");
		Sketch sk = null;
		if(DetailEditor.instance.currentSketch != null) {
			sk = DetailEditor.instance.currentSketch.GetSketch();
		}
		text.gameObject.SetActive(sk == constraint.sketch);
	}
	private void OnDestroy() {
		if(text != null) {
			GameObject.Destroy(text.gameObject);
		}
	}

}
