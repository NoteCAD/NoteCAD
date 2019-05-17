using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SharpFont;
using System.Xml;

[Serializable]
public class TextEntity : Entity, ILoopEntity {

	[NonSerialized]
	public PointEntity[] p = new PointEntity[4];
	
	string text_ = "";
	public string text {
		get {
			return text_;
		}

		set {
			text_ = value;
			UpdateText();
		}

	}

	public TextEntity(Sketch sk) : base(sk) {
		for(int i = 0; i < p.Length; i++) {
			p[i] = AddChild(new PointEntity(sk));
		}
	}

	public override IEntityType type { get { return IEntityType.Sketch; } }

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

	class FontRenderer : IFontRenderer {
		public List<List<Vector3>> loops = new List<List<Vector3>>();
		List<Vector3> points;
		public Vector3 shift;
		public float charHeight = 0f;

		Vector3 cur;

		Vector3 toUnity(System.Numerics.Vector2 point) {
			return new UnityEngine.Vector3(point.X, point.Y, 0f) + shift;
		}

		public void Clear() {
			loops.Clear();
			points = null;
			shift = Vector3.zero;
		}

		public void LineTo(System.Numerics.Vector2 point) {
			var old = cur;
			cur = toUnity(point);
			points.Add(cur);
		}

		public void MoveTo(System.Numerics.Vector2 point) {
			//if(points != null) points.Add(cur);
			cur = toUnity(point);
			points = new List<Vector3>();
			loops.Add(points);
			points.Add(cur);
			Debug.Log("MoveTo");
		}
		
		UnityEngine.Vector3 Bezier(UnityEngine.Vector3 p0, UnityEngine.Vector3 p1, UnityEngine.Vector3 p2, float t) {
			var omt = 1f - t;
			var omt2 = omt * omt;
			return omt2 * p0 + 2f * t * omt * p1 + t * t * p2;
		}

		UnityEngine.Vector3 BezierCubic(UnityEngine.Vector3 p0, UnityEngine.Vector3 p1, UnityEngine.Vector3 p2, UnityEngine.Vector3 p3, float t) {
			var omt = 1f - t;
			var omt2 = omt * omt;
			var omt3 = omt2 * omt;
			return omt3 * p0 + 3f * t * omt2 * p1 + 3f * t * t * omt * p2 + t * t * t * p3;
		}

		public void QuadraticCurveTo(System.Numerics.Vector2 control, System.Numerics.Vector2 point) {
			var old = cur;
			cur = toUnity(point);
			var ctl = toUnity(control);
			for(int i = 1; i < 8; i++) {
				var c = Bezier(old, ctl, cur, i / 7f);
				points.Add(c);
			}
		}
	}

	FontRenderer fontRenderer = new FontRenderer();

	Vector3 ToUnity(System.Numerics.Vector2 point) {
		return new Vector3(point.X, point.Y, 0f);
	}

	Bounds GetCBox(Glyph glyph) {
		Bounds result = new Bounds();
		if(glyph.points.Length == 0) return result;
		result.min = ToUnity(glyph.points[0]);
		result.max = result.min;
		for(int i = 1; i < glyph.points.Length; i++) {
			result.Encapsulate(ToUnity(glyph.points[i]));
		}
		return result;
	}

	IEnumerable<Vector3> CirclePoints(Vector3 cp, float radius) {
		var rv = Vector3.left * radius;
		int subdiv = 16;
		var vz = Vector3.forward;
		for(int i = 0; i < subdiv; i++) {
			var nrv = Quaternion.AngleAxis(360.0f / (subdiv - 1) * i, vz) * rv;
			yield return nrv + cp;
		}
	}

	Bounds bound;

	void UpdateText() {
		fontRenderer.Clear();
		var font = EntityConfig.instance.fontFace;
		Vector3 pos = Vector3.zero;
		CodePoint old = new CodePoint();
		if(fontRenderer.charHeight == 0f) {
			var cur = new CodePoint('A');
			var glyph = font.GetUnhitedGlyph(cur, 1f);
			var box = GetCBox(glyph);
			fontRenderer.charHeight = box.size.y;
		}
		for(int i = 0; i < text.Length; i++) {
			var cur = new CodePoint(text[i]);
			var glyph = font.GetUnhitedGlyph(cur, 1f);
			if(glyph == null) continue;
			//var kern = font.GetKerning(old, cur, size);
			/*
			var box = GetCBox(glyph);
			box.min = box.min + pos;
			box.max = box.max + pos;
			if(i == 0) {
				bound = box;
			} else {
				bound.Encapsulate(box);
			}
			*/
			//Debug.Log(box.min);
			//var bearing = new Vector3(glyph.HorizontalMetrics.Bearing.X, glyph.HorizontalMetrics.Bearing.Y, 0f);
			//Debug.Log(text[i] + " " + bearing.ToStr());
			fontRenderer.shift = pos;
			glyph.RenderTo(fontRenderer);
			//fontRenderer.loops.Add(CirclePoints(box.min, 0.1f).Select(p => p + fontRenderer.shift).ToList());
			//fontRenderer.loops.Add(CirclePoints(box.max, 0.1f).Select(p => p + fontRenderer.shift).ToList());
			//if(text[i] != ' ') fontRenderer.loops.Add(CirclePoints(bearing, 0.1f).Select(p => p + fontRenderer.shift).ToList());
			pos.x += glyph.HorizontalMetrics.LinearAdvance;
			old = cur;
		}
		bound.min = Vector3.zero;
		bound.max = new Vector3(pos.x, fontRenderer.charHeight, 0f);
		UpdatePoints();
		sketch.MarkDirtySketch(topo: true);
	}

	IEnumerable<Vector3> transform(Vector3 u, Vector3 v, Vector3 p, IEnumerable<Vector3> input) {
		foreach(var pt in input) {
			yield return u * pt.x + v * pt.y + p;
		}
	}

	public void UpdatePoints() {
		var v = p[3].pos - p[0].pos;
		var ext = bound.size;
		var u = new Vector3(v.y, -v.x, v.z) * ext.x / ext.y;
		
		p[1].pos = p[0].pos + u;
		p[2].pos = p[0].pos + v + u;
	}

	public override IEnumerable<Exp> equations {
		get {
			var u = p[1].exp - p[0].exp;
			var ext = bound.size;
			var v = new ExpVector(-u.y, u.x, u.z) * ext.y / ext.x;

			var eq0 = p[3].exp - (p[0].exp + v);
			yield return eq0.x;
			yield return eq0.y;

			var eq1 = p[2].exp - (p[0].exp + v + u);
			yield return eq1.x;
			yield return eq1.y;
		}
	}

	public IEnumerable<IEnumerable<Vector3>> loopPoints {
		get {
			var pos = p[0].pos;
			var u = (p[1].pos - pos) / (bound.size.x);
			var v = (p[3].pos - pos) / (bound.size.y);
			foreach(var l in fontRenderer.loops) {
				yield return transform(u, v, pos, l);
			}
		}
	}

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

	protected override void OnWrite(XmlTextWriter xml) {
		xml.WriteAttributeString("text", text_);
	}

	protected override void OnRead(XmlNode xml) {
		text = xml.Attributes["text"].Value;
	}

}
