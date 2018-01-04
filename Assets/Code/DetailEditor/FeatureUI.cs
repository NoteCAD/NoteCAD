using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FeatureUI : MonoBehaviour, IPointerDownHandler {
	public Feature feature_;
	public Feature feature {
		get { return feature_; }
		set {
			feature_ = value;
			GetComponent<Text>().text = feature_.GetType().Name;
		}
	}

	void OnClick() {
		DetailEditor.instance.ActivateFeature(feature);
	}

	float oldDownTime = 0;

	public void OnPointerDown(PointerEventData eventData) {
		var currentTime = Time.realtimeSinceStartup;
		if(currentTime - oldDownTime < 0.3f) {
			OnClick();
		}
		oldDownTime = currentTime;
	}

}
