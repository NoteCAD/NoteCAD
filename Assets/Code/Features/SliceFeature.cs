using Csg;
using g3;
using gs;
using gs.info;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEngine;

public class SliceFeature : Feature {
	GameObject go;
	SingleMaterialFFFSettings settings;
	PrintMeshAssembly meshes;

	public SliceFeature(Mesh input) {
		MeshCheck meshCheck = new MeshCheck();
		meshCheck.setMesh(input);
		DMesh3 mesh = meshCheck.ToUnityWatertightMesh().ToDMesh3();
		if(!mesh.IsClosed()) {
			return;
		}
        // center mesh above origin
        AxisAlignedBox3d bounds = mesh.CachedBounds;
        Vector3d baseCenterPt = bounds.Center - bounds.Extents.z*Vector3d.AxisZ;
        MeshTransforms.Translate(mesh, -baseCenterPt);

        // create print mesh set
        meshes = new PrintMeshAssembly();
		meshes.AddMesh(mesh, PrintMeshOptions.Default());

        // create settings
        //MakerbotSettings settings = new MakerbotSettings(Makerbot.Models.Replicator2);
        //PrintrbotSettings settings = new PrintrbotSettings(Printrbot.Models.Plus);
        //MonopriceSettings settings = new MonopriceSettings(Monoprice.Models.MP_Select_Mini_V2);
        settings = new RepRapSettings(RepRap.Models.Unknown);
	}

	public void DrawLayers() {
		/*
		GenericGCodeParser parser = new GenericGCodeParser();
		GCodeFile gcode;
		using (TextReader reader = new StreamReader(GenerateGCode())) {
			gcode = parser.Parse(reader);
		}

		GCodeToToolpaths converter = new GCodeToToolpaths();
		MakerbotInterpreter interpreter = new MakerbotInterpreter();
		interpreter.AddListener(converter);
		InterpretArgs interpArgs = new InterpretArgs();
		interpreter.Interpret(gcode, interpArgs);

		ToolpathSet Paths = converter.PathSet;
		//View.SetPaths(Paths);
		*/
	}

	float Percent(SingleMaterialFFFPrintGenerator gen) {
		int curProgress = 0;
		int maxProgress = 1;
		gen.GetProgress(out curProgress, out maxProgress);
		return 100f * curProgress / maxProgress;
	}
	
	float Percent(MeshPlanarSlicer gen) {
		int curProgress = gen.Progress;
		int maxProgress = gen.TotalCompute;
		return 100f * curProgress / maxProgress;
	}

	GCodeFile gcode;
	public IEnumerable<Progress> GenerateGCodeFile() {
        // run print generator
        // do slicing
        var slicer = new MeshPlanarSlicer() { LayerHeightMM = settings.LayerHeightMM };
        slicer.Add(meshes);
        foreach(var i in slicer.Compute()) {
			yield return i;
		}
		PlanarSliceStack slices = slicer.Result;

        var printGen = new SingleMaterialFFFPrintGenerator(meshes, slices, settings);
		foreach(var i in printGen.Generate()) {
			yield return i;
		}
        gcode = printGen.Result;
	}

	public IEnumerator GenerateGCode(Action<Progress> progress, Action<string> result) {
		foreach(var i in GenerateGCodeFile()) {
			progress(i);
			yield return null;
		}

        if(gcode != null) {
            // export gcode
			MemoryStream ms = new MemoryStream();
			string data = "";
            using (StreamWriter w = new StreamWriter(ms)) {
                StandardGCodeWriter writer = new StandardGCodeWriter();
				foreach(var i in writer.WriteFileEnumerator(gcode, w)) {
					progress(i);
					yield return i;
				}
				gcode = null;
				writer = null;
				yield return null;

				w.Flush();
				ms.Seek(0, SeekOrigin.Begin);
				using(var reader = new StreamReader(ms)) {
					data = reader.ReadToEnd();
				}
				ms.Dispose();
				yield return null;
            }
			result(data);
        }
	}

	public override GameObject gameObject {
		get {
			return go;
		}
	}

	public override ICADObject GetChild(Id guid) {
		return null;
		/*
		var entity = sketch.GetEntity(guid.WithoutSecond());
		if(guid.second == 2) return new ExtrudedPointEntity(entity as PointEntity, this);
		return new ExtrudedEntity(entity, this, guid.second);
		*/
	}

	protected override void OnUpdate() {
	}

	LineCanvas canvas;

	Vector3 CalculateNormal(int tri, int e, Vector3[] vert, int[] tris) {
		var v0 = vert[tris[tri + e]];
		var v1 = vert[tris[tri + (e + 1) % 3]];
		var v2 = vert[tris[tri + (e + 2) % 3]];
		return Vector3.Cross(v0 - v1, v2 - v1).normalized;
	}

	Vector3 Snap(Vector3 v, Vector3[] vert) {
		for(int i = 0; i < vert.Length; i++) {
			if((v - vert[i]).sqrMagnitude < 1e-9) return vert[i];
		}
		return v;
	}

	protected override void OnUpdateDirty() {
		GameObject.Destroy(go);
		go = new GameObject("ImportMeshFeature");
		canvas = GameObject.Instantiate(EntityConfig.instance.lineCanvas, go.transform);
		canvas.SetStyle("entities");
		//foreach(var edge in edges) {
		//			canvas.DrawLine(edge.a, edge.b);
		//}
		//meshCheck.drawErrors(canvas);

		/*

		var vertices = mesh.vertices;
		var triangles = mesh.GetTriangles(0);
		var edges = new HashSet<KeyValuePair<Vector3, Vector3>>();
		var antiEdges = new HashSet<KeyValuePair<Vector3, Vector3>>();
		var tris = new Dictionary<KeyValuePair<Vector3, Vector3>, int>();
		for(int i = 0; i < triangles.Length / 3; i++) {
			for(int j = 0; j < 3; j++) {
				var edge = new KeyValuePair<Vector3, Vector3>(
					Snap(vertices[triangles[i * 3 + j]], vertices),
					Snap(vertices[triangles[i * 3 + (j + 1) % 3]], vertices)
				);
				if(edge.Key == edge.Value) continue;
				tris[edge] =  i * 3 + j;
				if(edges.Contains(edge)) continue;
				if(antiEdges.Contains(edge)) continue;
				var antiEdge = new KeyValuePair<Vector3, Vector3>(edge.Value, edge.Key);
				edges.Add(edge);
				antiEdges.Add(antiEdge);
			}
		}

		foreach(var edge in edges) {
			var anti = new KeyValuePair<Vector3, Vector3>(edge.Value, edge.Key);
			if(tris.ContainsKey(edge) && tris.ContainsKey(anti)) {
				var tri0 = tris[edge];
				var edg0 = tri0 % 3;
				tri0 -= edg0;
				var n0 = CalculateNormal(tri0, edg0, vertices, triangles);

				var tri1 = tris[anti];
				var edg1 = tri1 % 3;
				tri1 -= edg1;
				var n1 = CalculateNormal(tri1, edg1, vertices, triangles);

				if((n0 - n1).magnitude < 1e-4) continue;
			} else continue;
			canvas.DrawLine(edge.Key, edge.Value);
		}
		*/
		go.SetActive(visible);
	}

	protected override void OnShow(bool state) {
		if(go != null) {
			go.SetActive(state);
		}
	}

	protected override void OnClear() {
		GameObject.Destroy(go);
	}

	protected override ICADObject OnHover(Vector3 mouse, Camera camera, UnityEngine.Matrix4x4 tf, ref double dist) {
		/*
		var sk = source as SketchFeature;
		k
		double d0 = -1;
		var r0 = sk.Hover(mouse, camera, tf, ref d0);
		if(!(r0 is Entity)) r0 = null;

		UnityEngine.Matrix4x4 move = UnityEngine.Matrix4x4.Translate(Vector3.forward * (float)extrude.value);
		double d1 = -1;
		var r1 = sk.Hover(mouse, camera, tf * move, ref d1);
		if(!(r1 is Entity)) r1 = null;

		if(r1 != null && (r0 == null || d1 < d0)) {
			r0 = new ExtrudedEntity(r1 as Entity, this, 1);
			d0 = d1;
		} else if(r0 != null) {
			r0 = new ExtrudedEntity(r0 as Entity, this, 0);
		}

		var points = sk.GetSketch().entityList.OfType<PointEntity>();
		var dir = extrusionDir.Eval();
		double min = -1.0;
		PointEntity hover = null;
		var sktf = tf * sk.GetTransform();
		foreach(var p in points) {
			Vector3 pp = sktf.MultiplyPoint(p.pos);
			var p0 = camera.WorldToScreenPoint(pp);
			var p1 = camera.WorldToScreenPoint(pp + dir);
			double d = GeomUtils.DistancePointSegment2D(mouse, p0, p1);
			if(d > Sketch.hoverRadius) continue;
			if(min >= 0.0 && d > min) continue;
			min = d;
			hover = p;
		}

		if(hover != null && (r0 == null || d0 > min)) {
			dist = min;
			return new ExtrudedPointEntity(hover, this);
		}

		if(r0 != null) {
			dist = d0;
			return r0;
		}
		*/
		return null;
	}

}
	