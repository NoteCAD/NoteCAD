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
			var typeName = feature_.GetType().Name;
			var name = typeName.Replace("Feature", "");
			GetComponent<Text>().text = Trans.late(typeName + "@history", name);
		}
	}

	void OnClick() {
		if(feature is IPlane) {
			if(DetailEditor.instance.activeFeature == feature) {
				CameraController.instance.AnimateToPlane(feature as IPlane);
			}
		}
		DetailEditor.instance.ActivateFeature(feature);
	}

	float oldDownTime = 0;
	int oldDownFrame = 0;

	public void OnPointerDown(PointerEventData eventData) {
		var currentTime = Time.realtimeSinceStartup;
		var currentFrame = Time.frameCount;
		if(currentFrame - oldDownFrame == 1 || currentTime - oldDownTime < 0.3f) {
			OnClick();
		}
		oldDownTime = currentTime;
		oldDownFrame = currentFrame;
	}

}
