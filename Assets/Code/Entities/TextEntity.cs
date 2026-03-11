using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SharpFont;
using System.Xml;

namespace NoteCAD {

[Serializable]
public class TextEntity : Entity, ILoopEntity {

	[NonSerialized]
	public PointEntity[] p = new PointEntity[4];
	
	[Flags]
	public enum Alignment {
		Scale	= 0 << 0,
		Stretch	= 1 << 1,
		Fit		= 1 << 2,
		
		Left	= 1 << 3,
		Right	= 1 << 4,
		Top		= 1 << 5,
		Bottom	= 1 << 7,

		TopLeft		= Top | Left,
		TopRight	= Top | Right,
		BottomLeft	= Bottom | Left,
		BottomRight	= Bottom | Right,

		Center		= Top | Bottom | Left | Right,
	}


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

	double fontSize_ = 1.0f;
	public double fontSize {
		get {
			return fontSize_;
		}
		set {
			fontSize_ = value;
			sketch.MarkDirtySketch(topo: true);
		}
	}

	double margin_ = 0.0f;
	public double margin {
		get {
			return margin_;
		}
		set {
			margin_ = value;
			sketch.MarkDirtySketch(topo: true);
		}
	}

	Alignment alignment_ = Alignment.Scale;
	public Alignment alignment {
		get {
			return alignment_;
		}
		set {
			alignment_ = value;
			sketch.MarkDirtySketch(topo: true);
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
			//Debug.Log("MoveTo");
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
		var text = this.text;
		if(text.Length == 0) text = " ";
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
		if (alignment_ != Alignment.Scale && alignment != Alignment.Fit) {
			return;
		}
		var v = p[3].pos - p[0].pos;
		if(alignment_ == Alignment.Fit) {
			v = v.normalized * (float)fontSize_;
			p[3].pos = p[0].pos + v;
		}
		var ext = bound.size;
		var u = new Vector3(v.y, -v.x, v.z) * ext.x / ext.y;
		
		p[1].pos = p[0].pos + u;
		p[2].pos = p[0].pos + v + u;
	}

	public override IEnumerable<Exp> equations {
		get {
			if (alignment_ == Alignment.Scale || alignment_ == Alignment.Fit) {
				var u = p[1].exp - p[0].exp;
				var ext = bound.size;
				var v = new ExpVector(-u.y, u.x, u.z) * ext.y / ext.x;
				if(alignment_ == Alignment.Fit) {
					yield return v.Magnitude() - fontSize;
				}
				var eq0 = p[3].exp - (p[0].exp + v);
				yield return eq0.x;
				yield return eq0.y;

				var eq1 = p[2].exp - (p[0].exp + v + u);
				yield return eq1.x;
				yield return eq1.y;
			} else {
				var u = p[1].exp - p[0].exp;
				var v = p[3].exp - p[0].exp;

				var cross = ExpVector.Cross(u, v);
				var dot = ExpVector.Dot(u, v);
				yield return Exp.Atan2(cross.Magnitude(), dot) - Math.PI / 2;

				var eq = p[2].exp - (p[1].exp + v);
				yield return eq.x;
				yield return eq.y;
			}
		}
	}

	float alignCoeff(bool min, bool max) {
		return min ? (max ? 0.5f : 0.0f) : (max ? 1.0f : 0.5f);
	}

	public IEnumerable<IEnumerable<Vector3>> loopPoints {
		get {
			Vector3 pos;
			Vector3 u;
			Vector3 v;
			float m = (float)margin;
			if (alignment_ == Alignment.Scale || alignment_ == Alignment.Stretch || alignment_ == Alignment.Fit) {
				pos = p[0].pos;
				u = (p[1].pos - pos) / (bound.size.x);
				v = (p[3].pos - pos) / (bound.size.y);
			} else {
				var ud = p[1].pos - p[0].pos;
				var vd = p[3].pos - p[0].pos;
				var un = ud.normalized;
				var vn = vd.normalized;
				u = un * (float)fontSize_;
				v = vn * (float)fontSize_;
				
				float ku = alignCoeff(alignment_.HasFlag(Alignment.Left), alignment_.HasFlag(Alignment.Right));
				float kv = alignCoeff(alignment_.HasFlag(Alignment.Bottom), alignment_.HasFlag(Alignment.Top));

				pos = p[0].pos 
					- un * (ku * 2.0f - 1.0f) * m
					- vn * (kv * 2.0f - 1.0f) * m
					+ ud * ku - u * ku * bound.size.x
					+ vd * kv - v * kv * bound.size.y;
			}
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

	protected override void OnWrite(Writer xml) {
		xml.WriteAttribute("text", text_);
		xml.WriteAttribute("fontSize", fontSize_);
		if (margin_ != 0.0) xml.WriteAttribute("margin", margin_);
		if (alignment_ != Alignment.Scale) xml.WriteAttribute("alignment", alignment_.ToString());
	}

	protected override void OnRead(XmlNode xml) {
		text = xml.Attributes["text"].Value;
		if (xml.Attributes["fontSize"] != null) {
			fontSize_ = xml.Attributes["fontSize"].Value.ToDouble();
		}
		if (xml.Attributes["margin"] != null) {
			margin_ = xml.Attributes["margin"].Value.ToDouble();
		}
		if (xml.Attributes["alignment"] != null) {
			xml.Attributes["alignment"].Value.ToEnum(ref alignment_);
		}
	}

}

}