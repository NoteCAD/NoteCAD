using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

public class ExtrusionFeature : MeshFeature {
	Param extrude = new Param("e", 5.0);
	Mesh mesh = new Mesh();
	public Detail detail;
	GameObject go;

	public override GameObject gameObject {
		get {
			return null;
		}
	}

	protected override void OnUpdate() {
	}

	protected override Mesh OnGenerateMesh() {
		MeshUtils.CreateMeshExtrusion(Sketch.GetPolygons((source as SketchFeature).GetLoops()), (float)extrude.value, ref mesh);
		return mesh;
	}

	protected override void OnUpdateDirty() {
		GameObject.Destroy(go);
		go = new GameObject("ExtrusionFeature");
		var bottom = GameObject.Instantiate(source.gameObject, go.transform);
		bottom.SetActive(true);
		var top = GameObject.Instantiate(source.gameObject, go.transform);
		top.SetActive(true);
		top.transform.localPosition = new Vector3(0, 0, (float)extrude.value);
		go.SetActive(visible);
	}

	protected override void OnShow(bool state) {
		if(go != null) {
			go.SetActive(state);
		}
	}

	protected override void OnWrite(XmlTextWriter xml) {

	}
}
	