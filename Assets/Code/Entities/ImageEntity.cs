using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

namespace NoteCAD {

[Serializable]
public class ImageEntity : Entity, ILoopEntity {

	[NonSerialized]
	public PointEntity[] p = new PointEntity[4];

	public enum ScaleMode {
		Scale   = 0,
		Stretch = 1,
	}

	ScaleMode scaleMode_ = ScaleMode.Scale;
	public ScaleMode scaleMode {
		get { return scaleMode_; }
		set {
			scaleMode_ = value;
			sketch.MarkDirtySketch(topo: true);
		}
	}

	string imageData_ = "";
	public string imageData {
		get { return imageData_; }
	}

	string imageName_ = "";
	public string imageName {
		get { return imageName_; }
		set { imageName_ = value; }
	}

	[NonSerialized]
	Texture2D texture_;

	public Texture2D texture { get { return texture_; } }

	public int imageWidth  { get { return texture_ != null ? texture_.width  : 1; } }
	public int imageHeight { get { return texture_ != null ? texture_.height : 1; } }

	public ImageEntity(Sketch sk) : base(sk) {
		for(int i = 0; i < p.Length; i++) {
			p[i] = AddChild(new PointEntity(sk));
		}
	}

	public override IEntityType type { get { return IEntityType.Image; } }

	public override IEnumerable<PointEntity> points {
		get {
			for(int i = 0; i < p.Length; i++) {
				yield return p[i];
			}
		}
	}

	public override bool IsChanged() {
		return p.Any(po => po.IsChanged());
	}

	/// <summary>
	/// Set the image from raw bytes (e.g. loaded from file).
	/// </summary>
	public void SetImage(byte[] bytes, string name) {
		if(bytes == null || bytes.Length == 0) return;
		imageData_ = Convert.ToBase64String(bytes);
		imageName_ = name ?? "";
		LoadTextureFromData();
		UpdatePoints();
		sketch.MarkDirtySketch(topo: true);
	}

	/// <summary>
	/// Set the image from an already-loaded Texture2D and raw bytes.
	/// </summary>
	public void SetImage(Texture2D tex, byte[] bytes, string name) {
		texture_ = tex;
		imageData_ = bytes != null ? Convert.ToBase64String(bytes) : "";
		imageName_ = name ?? "";
		UpdatePoints();
		sketch.MarkDirtySketch(topo: true);
	}

	void LoadTextureFromData() {
		if(string.IsNullOrEmpty(imageData_)) return;
		var bytes = Convert.FromBase64String(imageData_);
		texture_ = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
		texture_.LoadImage(bytes);
	}

	public void UpdatePoints() {
		if(scaleMode_ != ScaleMode.Scale) return;
		var v = p[3].pos - p[0].pos;
		float aspect = (float)imageWidth / Mathf.Max(imageHeight, 1);
		var u = new Vector3(v.y, -v.x, v.z) * aspect;
		p[1].pos = p[0].pos + u;
		p[2].pos = p[0].pos + v + u;
	}

	public override IEnumerable<Exp> equations {
		get {
			if(scaleMode_ == ScaleMode.Scale) {
				var u   = p[1].exp - p[0].exp;
				double aspect = (double)imageWidth / Math.Max(imageHeight, 1);
				var v   = new ExpVector(-u.y, u.x, u.z) / aspect;
				var eq0 = p[3].exp - (p[0].exp + v);
				yield return eq0.x;
				yield return eq0.y;
				var eq1 = p[2].exp - (p[0].exp + v + u);
				yield return eq1.x;
				yield return eq1.y;
			} else {
				// Stretch: maintain rectangle (perpendicular sides, p2 = p1 + (p3 - p0))
				var u     = p[1].exp - p[0].exp;
				var v     = p[3].exp - p[0].exp;
				var cross = ExpVector.Cross(u, v);
				var dot   = ExpVector.Dot(u, v);
				yield return Exp.Atan2(cross.Magnitude(), dot) - Math.PI / 2;
				var eq = p[2].exp - (p[1].exp + v);
				yield return eq.x;
				yield return eq.y;
			}
		}
	}

	public IEnumerable<IEnumerable<Vector3>> loopPoints {
		get {
			yield return p.Select(pt => pt.pos).Concat(Enumerable.Repeat(p[0].pos, 1));
		}
	}

	public IEnumerable<IEnumerable<Vector3>> segmentPoints => loopPoints;

	protected override void OnDraw(ICanvas canvas) {
		base.OnDraw(canvas);
		if(texture_ != null && canvas is LineCanvas lc) {
			lc.DrawQuadMesh(p[0].pos, p[1].pos, p[2].pos, p[3].pos, texture_);
		}
	}

	public override ExpVector PointOn(Exp t) { return null; }
	public override Exp Length()             { return null; }
	public override Exp Radius()             { return null; }
	public override ExpVector Center()       { return null; }

	protected override void OnWrite(Writer xml) {
		if(scaleMode_ != ScaleMode.Scale) xml.WriteAttribute("scaleMode", scaleMode_.ToString());
		if(!string.IsNullOrEmpty(imageName_)) xml.WriteAttribute("imageName", imageName_);
		if(!string.IsNullOrEmpty(imageData_)) xml.WriteAttribute("imageData", imageData_);
	}

	protected override void OnRead(XmlNode xml) {
		if(xml.Attributes["scaleMode"] != null) xml.Attributes["scaleMode"].Value.ToEnum(ref scaleMode_);
		if(xml.Attributes["imageName"] != null) imageName_ = xml.Attributes["imageName"].Value;
		if(xml.Attributes["imageData"] != null) {
			imageData_ = xml.Attributes["imageData"].Value;
			LoadTextureFromData();
		}
	}

}

}
