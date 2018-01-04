using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

public class SketchFeature : Feature {
	EquationSystem sys = new EquationSystem();
	List<List<Entity>> loops = new List<List<Entity>>();
	protected LineCanvas canvas;
	Sketch sketch;

	public Sketch GetSketch() {
		return sketch;
	}

	public SketchFeature() {
		sketch = new Sketch();
		canvas = GameObject.Instantiate(EntityConfig.instance.lineCanvas);
	}

	public void AddDrag(Exp drag) {
		sys.AddEquation(drag);
	}

	public void RemoveDrag(Exp drag) {
		sys.RemoveEquation(drag);
	}

	void UpdateSystem() {
		sys.Clear();
		sketch.GenerateEquations(sys);
	}

	protected override void OnUpdateDirty() {
		if(sketch.topologyChanged || sketch.constraintsTopologyChanged) {
			UpdateSystem();
		}

		if(sketch.topologyChanged) {
			loops = sketch.GenerateLoops();
		}

		if(sketch.IsConstraintsChanged()) {
			canvas.ClearStyle("constraints");
			canvas.SetStyle("constraints");
			foreach(var c in sketch.constraintList) {
				c.Draw(canvas);
			}
		}

		if(sketch.IsEntitiesChanged()) {
			canvas.ClearStyle("entities");
			canvas.SetStyle("entities");
			foreach(var e in sketch.entityList) {
				e.Draw(canvas);
			}

			canvas.ClearStyle("error");
			canvas.SetStyle("error");
			foreach(var e in sketch.entityList) {
				if(!e.isError) continue;
				e.Draw(canvas);
			}
		}

		var loopsChanged = loops.Any(l => l.Any(e => e.IsChanged()));
		if(loopsChanged || sketch.topologyChanged) {
			CreateLoops();
		}
		sketch.MarkUnchanged();
	}

	protected override SketchObject OnHover(Vector3 mouse, Camera camera, ref double objDist) {
		return sketch.Hover(mouse, camera, ref objDist);
	}

	protected override void OnUpdate() {
		string result = sys.Solve().ToString();
		result += "\n" + sys.stats;
		DetailEditor.instance.resultText.text = result.ToString();

		if(sketch.IsConstraintsChanged() || sketch.IsEntitiesChanged() || sketch.IsDirty()) {
			MarkDirty();
		}
	}

	List<GameObject> loopsObjects = new List<GameObject>();
	Mesh mainMesh;

	void CreateLoops() {
		foreach(var obj in loopsObjects) {
			GameObject.Destroy(obj.GetComponent<MeshFilter>().mesh);
			GameObject.Destroy(obj);
		}
		loopsObjects.Clear();
		var itr = new Vector3();
		foreach(var loop in loops) {
			loop.ForEach(e => e.isError = false);
			foreach(var e0 in loop) {
				foreach(var e1 in loop) {
					if(e0 == e1) continue;
					var cross = e0.IsCrossed(e1, ref itr);
					e0.isError = e0.isError || cross;
					e1.isError = e1.isError || cross;
				}
			}
		}
		var polygons = Sketch.GetPolygons(loops.Where(l => l.All(e => !e.isError)).ToList());
		GameObject.Destroy(mainMesh);
		mainMesh = MeshUtils.CreateMeshRegion(polygons);
		var go = new GameObject();
		var mf = go.AddComponent<MeshFilter>();
		var mr = go.AddComponent<MeshRenderer>();
		mf.mesh = mainMesh;
		mr.material = EntityConfig.instance.meshMaterial;
		loopsObjects.Add(go);
	}

	protected override void OnWrite(XmlTextWriter xml) {
		sketch.Write(xml);
	}

	protected override void OnRead(XmlNode xml) {
		sketch.Read(xml);
	}


	protected override void OnClear() {
		sketch.Clear();
		Update();
	}

	public List<List<Entity>> GetLoops() {
		return loops;
	}
}
