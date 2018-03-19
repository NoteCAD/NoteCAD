using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsUI : MonoBehaviour {

	public Dropdown dropdown;

	// Use this for initialization
	void Start() {
		dropdown.onValueChanged.AddListener(changeValue);
	}

	void changeValue(int value) {
		var mf = DetailEditor.instance.activeFeature as MeshFeature;
		if(mf != null) {
			mf.operation = dropdown.options[value].text.ToEnum<CombineOp>();
		}

	}
	
	// Update is called once per frame
	void Update () {
		var mf = DetailEditor.instance.activeFeature as MeshFeature;
		bool shouldShow = (mf != null && !DetailEditor.instance.IsFirstMeshFeature(mf));
		dropdown.gameObject.SetActive(shouldShow);
		if(shouldShow) {
			var cur = mf.operation.ToString();
			var index = dropdown.options.FindIndex(o => o.text == cur);
			if(dropdown.value != index) {
				dropdown.value = index;
			}
		}
	}
}
