using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using NoteCAD;

[Serializable]
public class SketchFeature : SketchFeatureBase, IPlane {
	List<List<IEntity>> loops = new List<List<IEntity>>();
	Mesh mainMesh;
	GameObject loopObj;

	Matrix4x4 transform_;
	public override Matrix4x4 transform {
		get {
			if(transformDirty || sourceChanged) {
				transform_ = CalculateTransform();
			}
			return transform_;
		}
	}
	bool transformDirty = true;
	
	IdPath uId = new IdPath();
	IdPath vId = new IdPath();
	IdPath pId = new IdPath();

	public IEntity u {
		get {
			return detail.GetObjectById(uId) as IEntity;
		}
		set {
			uId = value.id;
			transformDirty = true;
		}
	}

	public IEntity v {
		get {
			return detail.GetObjectById(vId) as IEntity;
		}
		set {
			vId = value.id;
			transformDirty = true;
		}
	}
	
	public IEntity p {
		get {
			return detail.GetObjectById(pId) as IEntity;
		}
		set {
			pId = value.id;
			transformDirty = true;
		}
	}

	Vector3 IPlane.u {
		get {
			return transform.GetColumn(0);
		}
	}

	Vector3 IPlane.v {
		get {
			return transform.GetColumn(1);
		}
	}

	Vector3 IPlane.n {
		get {
			return transform.GetColumn(2);
		}
	}

	Vector3 IPlane.o {
		get {
			return transform.GetColumn(3);
		}
	}

	public Matrix4x4 defaultTransfrom = Matrix4x4.identity;

	Matrix4x4 CalculateTransform() {
		if(u == null) {
			transformDirty = false;
			return defaultTransfrom;
		}
		var ud = u.GetDirectionInPlane(null).Eval().normalized;
		var vd = v.GetDirectionInPlane(null).Eval();
		var nd = Vector3.Cross(ud, vd).normalized;
		vd = Vector3.Cross(nd, ud).normalized;
		transformDirty = false;
		return UnityExt.Basis(ud, vd, nd, p.points.First().Eval());
	}

	public SketchFeature() {
		sketch.plane = this;
		sketch.is3d = false;
		shouldHoverWhenInactive = false;
		mainMesh = new Mesh();
		loopObj = new GameObject("loops");
		var mf = loopObj.AddComponent<MeshFilter>();
		var mr = loopObj.AddComponent<MeshRenderer>();
		loopObj.transform.SetParent(canvas.gameObject.transform, false);
		loopObj.SetActive(false);
		mf.mesh = mainMesh;
		mr.material = EntityConfig.instance.loopMaterial;
	}

	protected virtual List<List<IEntity>> OnGenerateLoops() {
		return sketch.GenerateLoops().Select(el => el.Select(e => e as IEntity).ToList()).ToList();
	}

	protected override void OnUpdateDirty() {
		transformDirty = true;
		if(sketch.topologyChanged) {
			loops = OnGenerateLoops();
		}

		var loopsChanged = loops.Any(l => l.OfType<Entity>().Any(e => e.IsChanged()));
		if(loopsChanged || sketch.topologyChanged) {
			CreateLoops();
		}
	}

	void CreateLoops() {
		if(detail.settings.checkSketchErrors) {
			var itr = new Vector3();
			foreach(var l in loops) {
				var loop = l.OfType<Entity>();
				loop.ForEach(e => e.isError = false);
				foreach(var e0 in loop) {
					foreach(var e1 in loop) {
						var cross = e0.IsCrossed(e1, ref itr);
						e0.isError = e0.isError || cross;
						e1.isError = e1.isError || cross;
					}
				}
			}
		}

		mainMesh.Clear();
		if(detail.settings.detectContours) {
			List<List<IdPath>> ids = null;
			var polygons = Sketch.GetPolygons(loops.Where(l => l.All(e => !(e is Entity) || !(e as Entity).isError)).ToList(), ref ids);
			MeshUtils.CreateMeshRegion(polygons, ref mainMesh);
		}
	}

	protected virtual void OnWriteSketchFeature(Writer xml) { }
	protected override sealed void OnWriteSketchFeatureBase(Writer xml) {
		OnWriteSketchFeature(xml);
		xml.WriteBeginElement("generated");
		xml.WriteBeginElement("transform");
		var plane = this as IPlane;
		xml.WriteAttribute("u", plane.u.ToStr());
		xml.WriteAttribute("v", plane.v.ToStr());
		xml.WriteAttribute("n", plane.n.ToStr());
		xml.WriteAttribute("o", plane.o.ToStr());
		xml.WriteEndElement();
		xml.WriteEndElement();
		if(uId.Empty() && vId.Empty() && pId.Empty()) return;
		xml.WriteBeginArray("references");
		uId.Write(xml, "u");
		vId.Write(xml, "v");
		pId.Write(xml, "o");
		xml.WriteEndArray();
	}

	protected virtual void OnReadSketchFeature(XmlNode xml) { }
	protected override sealed void OnReadSketchFeatureBase(XmlNode xml) {
		foreach(XmlNode nodeKind in xml.ChildNodes) {
			if(nodeKind.Name == "references") {
				foreach(XmlNode idNode in nodeKind.ChildNodes) {
					var name = idNode.Attributes["name"].Value;
					switch(name) {
						case "u": uId.Read(idNode); break;
						case "v": vId.Read(idNode); break;
						case "o": pId.Read(idNode); break;
					}
				}
			} else
			if(nodeKind.Name == "generated") {
				foreach(XmlNode tfNode in nodeKind.ChildNodes) {
					if(tfNode.Name != "transform") continue;
					var u = tfNode.Attributes["u"].Value.ToVector3();
					var v = tfNode.Attributes["v"].Value.ToVector3();
					var n = tfNode.Attributes["n"].Value.ToVector3();
					var o = tfNode.Attributes["o"].Value.ToVector3();
					defaultTransfrom = UnityExt.Basis(u, v, n, o);
				}
			}
		}
		OnReadSketchFeature(xml);
	}

	public List<List<IEntity>> GetLoops() {
		return loops;
	}

	protected override void OnActivate(bool state) {
		loopObj.SetActive(state && visible);
	}

	protected override void OnShow(bool state) {
		loopObj.SetActive(state && active);
	}

	public void DrawTriangulation(ICanvas canvas) {
		/*
		var ids = new List<List<Id>>();
		var polygons = Sketch.GetPolygons(loops.Where(l => l.All(e => !e.isError)).ToList(), ref ids);
		MeshUtils.DrawTriangulation(polygons, canvas);
		*/
	}

}
