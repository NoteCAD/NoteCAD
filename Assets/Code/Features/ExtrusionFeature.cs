using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ExtrusionFeature : MeshFeature {
	Param extrude = new Param("e", 5.0);
	Mesh mesh = new Mesh();

	protected override void OnUpdate() {
	}

	protected override Mesh OnGenerateMesh() {
		MeshUtils.CreateMeshExtrusion(Sketch.GetPolygons((source as SketchFeature).GetLoops()), (float)extrude.value, ref mesh);
		return mesh;
	}
}
	