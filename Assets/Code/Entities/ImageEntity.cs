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
	public string imageData {
		get {
			return imageData_;
		}
		set {
			imageData_ = value;
			UpdateAspectRatio();
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

	void UpdateAspectRatio() {
		if (string.IsNullOrEmpty(imageData_)) return;
		try {
			var bytes = Convert.FromBase64String(imageData_);
			var tex = new Texture2D(2, 2);
			if (tex.LoadImage(bytes) && tex.width > 0) {
				aspectRatio_ = (double)tex.height / tex.width;
			}
			UnityEngine.Object.Destroy(tex);
		} catch {
			aspectRatio_ = 1.0;
		}
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
			if (xml.Attributes["aspectRatio"] == null) {
				UpdateAspectRatio();
			}
		}
	}

}

}
