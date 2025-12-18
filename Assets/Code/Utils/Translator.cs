using UnityEngine;

public class Translator : MonoBehaviour {
	public string key = "";

	public void OnDisable() {
		key = "";
	}
}
