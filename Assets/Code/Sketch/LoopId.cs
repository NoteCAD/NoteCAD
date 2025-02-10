
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class LoopId {
	
	static readonly char[] loopSeparator = new char[] {'|'};
	private List<IdPath> loopPaths = new List<IdPath>();

	public LoopId(List<IEntity> loop) {
		loopPaths = loop.Select(l => l.id).ToList();
	}
	
	public LoopId(string str) {
		Parse(str);
	}

	public static bool operator==(LoopId a, LoopId b) {
		if(ReferenceEquals(a, b)) {
			return true;
		}
		if(a.loopPaths.Count != b.loopPaths.Count) {
			return false;
		}
		if(a.loopPaths.Count == 0) {
			return true;
		}
		var count = a.loopPaths.Count;
		var f = b.loopPaths.First();
		int index = -1;
		while(
			index + 1 < count &&
			(index = a.loopPaths.FindIndex(index + 1, lp => lp == f)) != -1
		) {
			bool foundNonEqual = false;
			for(int i = 0/*1*/; i < count; i++) {
				if (b.loopPaths[i] != a.loopPaths[(i + index) % count]) {
					foundNonEqual = true;
					break;
				}
			}
			if(!foundNonEqual) {
				return true;
			}
		}

		return false;
	}

	public override bool Equals(object obj) {
		return this == obj as LoopId;
	}

	public static bool operator!=(LoopId a, LoopId b) {
		return !(a == b);
	}

	public override string ToString() {
		var sb = new StringBuilder();
		foreach(var p in loopPaths) {
			sb.Append(p.ToString());
			sb.Append(loopSeparator);
		}
		sb.Remove(sb.Length - 1, 1);
		return sb.ToString();
	}

	public void Parse(string str) {
		var ids = str.Split(loopSeparator, StringSplitOptions.RemoveEmptyEntries);
		loopPaths.Clear();
		for(int i = 0; i < ids.Length; i++) {
			var path = new IdPath();
			path.Parse(ids[i]);
			loopPaths.Add(path);
		}
	}

	public void Shift(int distance) {
		var count = loopPaths.Count;
		distance %= count;
		if (distance == 0) {
			return;
		}
		var newPaths = new List<IdPath>(count);
		for (int i = 0; i < count; i++) {
			newPaths.Add(loopPaths[(i + distance + count) % count]);
		}
		loopPaths = newPaths;
	}

	public static bool Test(List<IEntity> loop) {
		var loopId = new LoopId(loop);
		var str = loopId.ToString();
		Debug.Log("loop original = " + str);
		var loopParsed = new LoopId(str);
		Debug.Log("loop parsed = " + loopParsed.ToString());
		bool loopsStrEquals = (str == loopParsed.ToString());
		bool loopsEquals = (loopId == loopParsed);
		Debug.Log("loops str equals = " + loopsStrEquals.ToString());
		Debug.Log("loops equals = " + loopsEquals.ToString());
		loopParsed.Shift(1);
		Debug.Log("loop shifted by 1 = " + loopParsed.ToString());
		bool loopsShiftedStrEquals = (str == loopParsed.ToString());
		var loopsShiftedEquals = (loopId == loopParsed);
		Debug.Log("loops shifted str equals = " + loopsShiftedStrEquals.ToString());
		Debug.Log("loops shifted equals = " + loopsShiftedEquals.ToString());
		return loopsStrEquals && loopsEquals && !loopsShiftedStrEquals && loopsShiftedEquals;
	}

}
