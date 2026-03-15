using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Xml;

namespace NoteCAD {

[Serializable]
public class ImageEntity : Entity, ILoopEntity {

	[NonSerialized]
	public PointEntity[] p = new PointEntity[4];

	string imageData_ = "";
	Texture2D texture_;
	Mesh quadMesh_;
	Material imageMaterial_;

	public string imageData {
		get {
			return imageData_;
		}
		set {
			imageData_ = value;
			UpdateTexture();
			sketch.MarkDirtySketch(topo: true);
		}
	}

	double aspectRatio_ = 1.0;
	public double aspectRatio { get { return aspectRatio_; } }

	public ImageEntity(Sketch sk) : base(sk) {
		for (int i = 0; i < p.Length; i++) {
			p[i] = AddChild(new PointEntity(sk));
		}
	}

	public override IEntityType type { get { return IEntityType.Image; } }

	public override IEnumerable<PointEntity> points {
		get {
			for (int i = 0; i < p.Length; i++) {
				yield return p[i];
			}
		}
	}

	public override bool IsChanged() {
		return p.Any(po => po.IsChanged());
	}

	void UpdateTexture() {
		if (string.IsNullOrEmpty(imageData_)) return;
		try {
			var bytes = Convert.FromBase64String(imageData_);
			if (texture_ == null) {
				texture_ = new Texture2D(2, 2);
			}
			if (texture_.LoadImage(bytes) && texture_.width > 0) {
				aspectRatio_ = (double)texture_.height / texture_.width;
			}
			if (imageMaterial_ != null) {
				imageMaterial_.mainTexture = texture_;
			}
		} catch {
			aspectRatio_ = 1.0;
		}
	}

	void EnsureQuadResources() {
		if (imageMaterial_ == null) {
			imageMaterial_ = new Material(EntityConfig.instance.imageMaterial);
			imageMaterial_.mainTexture = texture_;
		}
		if (quadMesh_ == null) {
			quadMesh_ = new Mesh();
			quadMesh_.name = "ImageQuad";
			// Vertex order: p[0]=bottom-left, p[1]=bottom-right, p[2]=top-right, p[3]=top-left
			quadMesh_.uv = new Vector2[] {
				new Vector2(0f, 0f),
				new Vector2(1f, 0f),
				new Vector2(1f, 1f),
				new Vector2(0f, 1f)
			};
			quadMesh_.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
		}
	}

	void UpdateQuadMesh() {
		if (quadMesh_ == null) return;
		quadMesh_.vertices = new Vector3[] { p[0].pos, p[1].pos, p[2].pos, p[3].pos };
		quadMesh_.RecalculateBounds();
	}

	public void UpdatePoints() {
		var u = p[1].pos - p[0].pos;
		var v = new Vector3(-u.y, u.x, u.z) * (float)aspectRatio_;
		p[3].pos = p[0].pos + v;
		p[2].pos = p[1].pos + v;
	}

	public override IEnumerable<Exp> equations {
		get {
			var u = p[1].exp - p[0].exp;
			var v = new ExpVector(-u.y, u.x, u.z) * aspectRatio_;
			var eq0 = p[3].exp - (p[0].exp + v);
			yield return eq0.x;
			yield return eq0.y;
			var eq1 = p[2].exp - (p[1].exp + v);
			yield return eq1.x;
			yield return eq1.y;
		}
	}

	public IEnumerable<IEnumerable<Vector3>> loopPoints {
		get {
			yield return new[] { p[0].pos, p[1].pos, p[2].pos, p[3].pos };
		}
	}

	public IEnumerable<IEnumerable<Vector3>> segmentPoints =>
		loopPoints.Select(loop => loop.Concat(loop.Take(1)));

	public override ExpVector PointOn(Exp t) {
		return null;
	}

	public override Exp Length() {
		return null;
	}

	public override Exp Radius() {
		return null;
	}

	public override ExpVector Center() {
		return null;
	}

	protected override void OnDraw(ICanvas canvas) {
		EnsureQuadResources();
		UpdateQuadMesh();
		base.OnDraw(canvas);
	}

	public override void DrawExtras(UnityEngine.Matrix4x4 worldTF) {
		if (quadMesh_ == null || imageMaterial_ == null || texture_ == null) return;
		Graphics.DrawMesh(quadMesh_, worldTF, imageMaterial_, 0);
	}

	protected override void OnDestroy() {
		base.OnDestroy();
		if (texture_ != null) { UnityEngine.Object.Destroy(texture_); texture_ = null; }
		if (imageMaterial_ != null) { UnityEngine.Object.Destroy(imageMaterial_); imageMaterial_ = null; }
		if (quadMesh_ != null) { UnityEngine.Object.Destroy(quadMesh_); quadMesh_ = null; }
	}

	protected override void OnWrite(Writer xml) {
		xml.WriteAttribute("aspectRatio", aspectRatio_);
		xml.WriteAttribute("imageData", imageData_);
	}

	protected override void OnRead(XmlNode xml) {
		if (xml.Attributes["aspectRatio"] != null) {
			aspectRatio_ = xml.Attributes["aspectRatio"].Value.ToDouble();
		}
		if (xml.Attributes["imageData"] != null) {
			imageData_ = xml.Attributes["imageData"].Value;
			UpdateTexture();
		}
	}

}

}
