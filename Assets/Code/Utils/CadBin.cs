using SharpFont;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;
using static SharpFont.TextLayout;

class CadBinRec {

	public int name = 0;
	public byte dim = 0;
	public int type = 0;
	public int size = 0;
	public byte[] data;

	public void alloc(int newSize) {
		data = new byte[newSize];
		size = newSize;
	}
};

class CadBin {

	static int MAGIC(string str) {
		return (byte)str[0] | (byte)str[1] << 8 | (byte)str[2] << 16 | str[3] << 24;
 	}

	//enum DataType {
	public static readonly int TypeNone		= 0x0;
	public static readonly int TypeUI16		= MAGIC("ui16");
	public static readonly int TypeI16		= MAGIC("i16\0");
	public static readonly int TypeUI32		= MAGIC("ui32");
	public static readonly int TypeI32		= MAGIC("i32\0");
	public static readonly int TypeF32 		= MAGIC("f32\0");
	public static readonly int TypeF64 		= MAGIC("f64\0");
	public static readonly int TypeStr		= MAGIC("str\0");
	//};

	//enum NameType : uint32_t {
	public static readonly int NameVertices		= MAGIC("vert");
	public static readonly int NameNormals		= MAGIC("norm");
	public static readonly int NameTriangles	= MAGIC("tris");
	public static readonly int NameEdges		= MAGIC("edge");
	public static readonly int NameSurfaces		= MAGIC("surf");
	public static readonly int NameCurves		= MAGIC("curv");
	public static readonly int NameQueries		= MAGIC("quer");
	public static readonly int NameCoords0		= MAGIC("uv0\0");
	public static readonly int NameCoords1		= MAGIC("uv1\0");
	//};


	byte[] header = new byte[64];

	List<CadBinRec> records = new();

	CadBinRec addRec() {
		var rec = new CadBinRec();
		records.Add(rec);
		return rec;
	}

	CadBinRec addRec(int name, byte dim, int type) {
		var rec = addRec();
		rec.name = name;
		rec.dim = dim;
		rec.type = type;
		return rec;
	}


	static bool bread(ref int to, byte[] data, ref int available)
	{
		var size = sizeof(int);
		if (available < size) {
			return false;
		}
		to = BitConverter.ToInt32(data, data.Length - available);
		available -= size;
		return true;
	}

	static bool bread(ref byte to, byte[] data, ref int available)
	{
		var size = sizeof(byte);
		if (available < size) {
			return false;
		}
		to = data[data.Length - available];
		available -= size;
		return true;
	}

	static bool bread(ref float to, byte[] data, ref int available)
	{
		var size = sizeof(float);
		if (available < size) {
			return false;
		}
		to = BitConverter.ToSingle(data, data.Length - available);
		available -= size;
		return true;
	}

	static bool bread(ref byte[] to, byte[] data, ref int available)
	{
		var size = to.Length;
		if (available < size) {
			return false;
		}
		Array.Copy(data, data.Length - available, to, 0, size);
		available -= size;
		return true;
	}

	static bool bread(ref Vector3[] to, byte[] data, ref int available)
	{
		var size = to.Length * 3 * sizeof(float);
		if (available < size) {
			return false;
		}
		for(int i = 0; i < to.Length; i++) {
			bread(ref to[i].x, data, ref available);
			bread(ref to[i].y, data, ref available);
			bread(ref to[i].z, data, ref available);
		}
		return true;
	}

	static bool bread(ref int[] to, byte[] data, ref int available)
	{
		var size = to.Length * sizeof(int);
		if (available < size) {
			return false;
		}
		for(int i = 0; i < to.Length; i++) {
			bread(ref to[i], data, ref available);
		}
		return true;
	}

	public bool read(byte[] data) {
		records.Clear();
		int available = data.Length;
		bread(ref header, data, ref available);
		bool result = true;
		while (available > 0) {
			CadBinRec rec = addRec();
			result &= bread(ref rec.name, data, ref available);
			result &= bread(ref rec.dim, data, ref available);
			result &= bread(ref rec.type, data, ref available);
			result &= bread(ref rec.size, data, ref available);
			rec.alloc(rec.size);
			result &= bread(ref rec.data, data, ref available);
		}
		return result;
	}

	/*
	void write(StreamBuffer &buffer) const {
		buffer.write(header);
		for (const CadBinRec &rec : records) {
			buffer.write(rec.name);
			buffer.write(rec.dim);
			buffer.write(rec.type);
			buffer.write(rec.size);
			buffer.write(rec.data, rec.size);
		}
	}
	*/

	int sizeOf(int type) {
		if(type == TypeUI16) return 2;
		if(type == TypeI16)	 return 2;
		if(type == TypeUI32) return 4;
		if(type == TypeI32)	 return 4;
		if(type == TypeF32)  return 4;
		if(type == TypeF64)  return 8;
		return -1;
	}

	public bool toMesh(Mesh mesh, LineCanvas canvas) {
		mesh.Clear();
		int submesh = 0;
		Vector3[] vertices = null;
		foreach(var rec in records) {
			int available = rec.data.Length;
			int typeSize = sizeOf(rec.type);
			var count = rec.size / (typeSize * rec.dim);
			if(rec.name == NameVertices) {
				if(rec.type != TypeF32) {
					continue;
				}
				vertices = new Vector3[count];
				if(!bread(ref vertices, rec.data, ref available)) {
					return false;
				}
			} else
			if(rec.name == NameEdges) {
				if(rec.type != TypeUI32) {
					continue;
				}
				var edges = new int[count * rec.dim];
				if(!bread(ref edges, rec.data, ref available)) {
					return false;
				}

				for(int i = 0; i < edges.Length; i+=rec.dim) {
					canvas.DrawLine(vertices[edges[i]], vertices[edges[i + 1]]);
				}
			} else
			if(rec.name == NameTriangles) {
				if(rec.type != TypeUI32) {
					continue;
				}
				var triangles = new int[count * rec.dim];
				if(!bread(ref triangles, rec.data, ref available)) {
					return false;
				}

				var newVertices = new Vector3[triangles.Length];

				for(int i = 0; i < triangles.Length; i++) {
					newVertices[i] = vertices[triangles[i]];
					triangles[i] = i;
				}

				mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
				mesh.SetVertices(newVertices);
				mesh.SetTriangles(triangles, submesh);
				mesh.RecalculateBounds();
				mesh.RecalculateNormals();
				mesh.RecalculateTangents();
				submesh++;
			}
		}
		return true;
	}


};
