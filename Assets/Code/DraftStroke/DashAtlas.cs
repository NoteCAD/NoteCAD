using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public struct DashPattern {
	public float[] pattern;

	public DashPattern(float[] p) {
		pattern = new float[p.Length];
		Array.Copy(p, pattern, p.Length);
	}

	public static bool operator==(DashPattern a, DashPattern b) {
		if(a.pattern.Length != b.pattern.Length) return false;
		for(int i = 0; i < a.pattern.Length; i++) {
			if(a.pattern[i] != b.pattern[i]) return false;
		}
		return true;
	}

	public override bool Equals(object obj) {
		return this == (DashPattern)obj;
	}

	public override int GetHashCode() {
		if(pattern == null) return 0;
		return pattern.Length + (int)(StrokeStyle.GetPatternLength(pattern) * 1000.0);
	}

	public static bool operator!=(DashPattern a, DashPattern b) {
		return !(a == b);
	}

	public static bool operator<(DashPattern a, DashPattern b) {
		if(a != b) {
			if(a.pattern.Length != b.pattern.Length) return a.pattern.Length < b.pattern.Length;
			for(int i = 0; i < a.pattern.Length; i++) {
				if(a.pattern[i] != b.pattern[i]) return a.pattern[i] < b.pattern[i];
			}
		}
		return false;
	}

	public static bool operator>(DashPattern a, DashPattern b) {
		return b < a;
	}

}

public static class DashAtlas {

	static Dictionary<DashPattern, Texture2D> cache = new Dictionary<DashPattern, Texture2D>();

	public static Texture2D GetAtlas(float[] dashes) {
		var dash = new DashPattern(dashes);
		if(cache.ContainsKey(dash)) return cache[dash];
		var atlas = GenerateAtlas(dashes);
		cache[dash] = atlas;
		return atlas;
	}

	static double Frac(double x) {
		return x - Math.Floor(x);
	}

	static Color EncodeLengthAsFloat(double v) {
		v = Math.Max(0.0f, Math.Min(1.0f, v));
		double er = v;
		double eg = Frac(255.0 * v);
		double eb = Frac(65025.0 * v);
		double ea = Frac(160581375.0 * v);

		var r = (float)(er - eg / 255.0);
		var g = (float)(eg - eb / 255.0);
		var b = (float)(eb - ea / 255.0);
		return new Color(r, g, b, (float)ea);
	}

	static Texture2D GenerateAtlas(float[] pattern) {
		double patternLen = StrokeStyle.GetPatternLength(pattern);

		Texture2D texture = new Texture2D(SystemInfo.maxTextureSize, 1);
		var size = texture.width;

		int mipCount = (int)Math.Log(texture.width, 2.0) + 1;
		for(int mip = 0; mip < mipCount; mip++) {
			Color[] textureData = new Color[size];
			int dashI = 0;
			double dashT = 0.0;
			for(int i = 0; i < size; i++) {
				if(pattern.Length == 0) {
					textureData[i] = EncodeLengthAsFloat(0.0);
					continue;
				}

				var t = (double)i / (size - 1);
				while(t - 1e-6 > dashT + pattern[dashI] / patternLen) {
					dashT += pattern[dashI] / patternLen;
					dashI++;
				}
				double dashW = pattern[dashI] / patternLen;
				if(dashI % 2 == 0) {
					textureData[i] = EncodeLengthAsFloat(0.0);
				} else {
					double value;
					if(t - dashT < pattern[dashI] / patternLen / 2.0) {
						value = t - dashT;
					} else {
						value = dashT + dashW - t;
					}
					//value = value * patternLen;
					textureData[i] = EncodeLengthAsFloat(value);
				}
			}
			texture.SetPixels(textureData, mip);
			size /= 2;
		}
		texture.Apply();
		return texture;
	}

	public static Sprite GeneratePreview(float[] dashes, Color color, float width) {
		int w = 128;
		int h = 32;
		var preview = Sprite.Create(new Texture2D(w, h), new Rect(0, 0, w, h), Vector2.zero);
		var texture = preview.texture;
		var atlas = GetAtlas(dashes);
		var len = dashes.Sum() * 3f;
		if(len == 0f) len = 1f;
		if(width < 1f) width = 1f;
		for(int i = 0; i < texture.width; i++) {
			for(int j = 0; j < texture.height; j++) {
				var pix = atlas.GetPixelBilinear((float)i / (texture.width - 1) * 3f, 0.5f);
				var sc = width == 0f ? 1f : width;
				var u = Vector4.Dot(new Vector4(pix.a, pix.r, pix.g, pix.b), new Vector4(1f / 160581375f, 1f / 65025f, 1f / 255f, 1f)) * sc / len;
				u = u > 0f ? width * 2f : 0f;
				float v =  j - texture.height / 2f;
				float d = (new Vector2(u, v)).magnitude - width;
				float k = Mathf.SmoothStep(-2f, 2f, d);
				var c = Color.Lerp(color, Color.black, k);
				texture.SetPixel(i, j, c);
			}
		}
		texture.Apply(true, false);
		return preview;
	}
}
