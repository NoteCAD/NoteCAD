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
		text.text = constraint.GetValue().ToString("#.##");
	}
	private void OnDestroy() {
		if(text != null) {
			GameObject.Destroy(text.gameObject);
		}
	}

}
