using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Poisson {

	[Serializable]
	public class ReplayObject {
		public double[] dir;
		public double[] loc;
		public double[] cen;
		public int[] points;
		public double rad;
		public string type;
		public string label;
	}

	[Serializable]
	public class ReplayConstraint {
		public string alignment = "Current";
		public int[] arguments;
		public string orientation = "Current";
		public string type;
		public double value;
	}

	[Serializable]
	public class DragTransform {
		public int objectId;
		public double[] transformMatrix;
		public double[][] matrix;

		void InitIdentity() {
			matrix = new double[3][];
			matrix[0] = new double[3] {1.0, 0.0, 0.0 };
			matrix[1] = new double[3] {0.0, 1.0, 0.0 };
			matrix[2] = new double[3] {0.0, 0.0, 1.0 };
		}
		public DragTransform() {
			InitIdentity();
		}

		public DragTransform(Vector3 translate) {
			InitIdentity();
			matrix[0][2] = translate.x;
			matrix[1][2] = translate.y;
		}
	}

	[Serializable]
	public class ReplaySketch {
		public ReplayObject[] objects;
		public ReplayConstraint[] constraints;
		public double LinearTolerance;
		public double AngularTolerance;
		public DragTransform[] dragTransforms;
		public string Replay = "Solve";

		static Vector3 toVec(double[] array) {
			return new Vector3((float)array[0], (float)array[1], 0.0f);
		}
		
		static double[] fromVec(Vector3 vec, bool is3d) {
			if (is3d) {
				return new double[3] { vec.x, vec.y, vec.z };
			}
			return new double[2] { vec.x, vec.y };
		}

		public static ReplaySketch From(Sketch sk) {
			var replay = new ReplaySketch();
			replay.Replay = sk.is3d ? "Solve3D" : "Solve";
			replay.LinearTolerance = 1e-06;
			replay.AngularTolerance = 1e-09;
			var objects = new List<ReplayObject>();
			var constraints = new List<ReplayConstraint>();
			var map = new Dictionary<ICADObject, int>();
			var pointsToLine = new Dictionary<PointEntity, Dictionary<PointEntity, LineEntity>>();
			Action<PointEntity, PointEntity, LineEntity> addPointToLine = (p0, p1, l) => {
				if (pointsToLine.TryGetValue(p0, out var dict)) {
					dict[p1] = l;
				} else {
					pointsToLine[p0] = new Dictionary<PointEntity, LineEntity> { [p1] = l };
				}
			};

			string D = sk.is3d ? "3D" : "2D";

			foreach(var e in sk.entityList) {
				ReplayObject obj = null;
				switch(e) {
					case PointEntity p: {
						// skip arc points
						if(p.parent == null || p.parent.type != IEntityType.Arc) {
						//if(p.x.name == "draggingPoint") {
						//} else {
							obj = new ReplayObject{ type = "Point" + D, loc = fromVec(p.GetPosition(), sk.is3d)};
						//}
						}
						break;
					}
					case ArcEntity a: {
						// write arc points in the right order
						PointEntity[] points = { a.p0, a.p1, a.c };
						foreach(var p in points) {
							var pObj = new ReplayObject{ type = "Point" + D, loc = fromVec(p.GetPosition(), sk.is3d)};
							pObj.label = e.guid.ToString();
							objects.Add(pObj);
							map.Add(p, objects.Count - 1);
						}
						continue;
					}
				}
				if(obj == null) {
					continue;
				}
				obj.label = e.guid.ToString();
				objects.Add(obj);
				map.Add(e, objects.Count - 1);
			}

			foreach(var e in sk.entityList) {
				ReplayObject obj = null;
				switch(e) {
					case CircleEntity c: {
						obj = new ReplayObject{ type = "Circle" + D, cen = fromVec(c.center.GetPosition(), sk.is3d), rad = c.radius };
						break;
					}
					case LineEntity l: {
						if (!sk.is3d)
						{
							obj = new ReplayObject{
								type = "Segment" + D, 
								points = new int[2] { map[l.p0], map[l.p1] }
							};
						} else {
							obj = new ReplayObject{ 
								type = "Line" + D, 
								loc = fromVec(l.p0.GetPosition(), sk.is3d), 
								dir = fromVec(Vector3.Normalize(l.p1.GetPosition() - l.p0.GetPosition()), sk.is3d)
							};
						}
						addPointToLine(l.p0, l.p1, l);
						addPointToLine(l.p1, l.p0, l);
						
						break;
					}
					case ArcEntity a: {
						obj = new ReplayObject{ type = "Circle" + D, cen = fromVec(a.center.GetPosition(), sk.is3d), rad = a.radius };
						break;
					}
				}
				if(obj == null) {
					continue;
				}
				obj.label = e.guid.ToString();
				objects.Add(obj);
				map.Add(e, objects.Count - 1);
			}
			
			/*
			foreach(var e in sk.entityList) {
				ReplayObject obj = null;
				switch(e) {
					case PointEntity p: {
						
						if(p.x.name == "draggingPoint") {
							foreach(var e1 in sk.entityList) {
								if(e == e1) {
									continue;
								}
								PointOn pOn = null;
								if(p.IsCoincidentWith(e1) || p.IsCoincidentWithCurve(e1, ref pOn)) {
									if (map.ContainsKey(e1)) {
										replay.Replay = "Drag";
										replay.dragTransforms = new DragTransform[1] { new DragTransform(p.GetPosition()) };
										replay.dragTransforms[0].objectId = map[e1];
									}
								}
							}
						}
						break;
					}
				}
			}
			*/

			foreach(var e in map) {
				switch(e.Key) {
					case PointEntity p: {
						if(p.x.name == "draggingPoint") {
							constraints.Add(new ReplayConstraint{ type = "Fixation" + D, arguments = new int[]{e.Value}});
						}
						break;
					}
					case LineEntity l: {
						constraints.Add(new ReplayConstraint{ type = "Incidence" + D, arguments = new int[]{ map[l.p0], e.Value}});
						constraints.Add(new ReplayConstraint{ type = "Incidence" + D, arguments = new int[]{ map[l.p1], e.Value}});
						break;
					}
					case CircleEntity c: {
						constraints.Add(new ReplayConstraint{ type = "Concentricity" + D, arguments = new int[]{ map[c.center], e.Value}});
						break;
					}
					case ArcEntity a: {
						constraints.Add(new ReplayConstraint{ type = "Incidence" + D, arguments = new int[]{ map[a.p0], e.Value}});
						constraints.Add(new ReplayConstraint{ type = "Incidence" + D, arguments = new int[]{ map[a.p1], e.Value}});
						constraints.Add(new ReplayConstraint{ type = "Concentricity" + D, arguments = new int[]{ map[a.c], e.Value}});
						break;
					}
				}
			}

			foreach(var c in sk.constraintList) {
				if (c.objects.Any(o => !map.ContainsKey(o))) {
					if(c is PointsCoincident) {
						var p = c.objects.FirstOrDefault(o => map.ContainsKey(o));
						if(p != null) {
							var fix = new ReplayConstraint{ type = "Fixation" + D, arguments = new int[]{map[p]}};
							constraints.Add(fix);
						}
					}
					continue;
				}
				ReplayConstraint con = null;
				var args = c.objects.Select(o => map[o]).ToArray();

				string pos  = "Positive";
				string neg  = "Negative";

				switch(c) {
					case PointsDistance ptsDist: {
						if(args.Length == 2) {
							con = new ReplayConstraint{ type = "Distance" + D, arguments = args, value = ptsDist.GetValue() };
						} else if (ptsDist.GetEntity(0) is LineEntity l) {
							con = new ReplayConstraint{ type = "Distance" + D, arguments = new int[]{map[l.p0], map[l.p1]}, value = ptsDist.GetValue() };
						}
						break;
					}
					case PointLineDistance ptLD: {
						con = new ReplayConstraint{ type = "Distance" + D, arguments = args, value = ptLD.GetValue() };
						break;
					}
					case PointOn pOn: {
						con = new ReplayConstraint{ type = "Incidence" + D, arguments = args, value = pOn.GetValue() };
						break;
					}
					case PointsCoincident ptsCo: {
						con = new ReplayConstraint{ type = "Incidence" + D, arguments = args};
						break;
					}
					case Tangent tan: {
						con = new ReplayConstraint{ type = "Tangency" + D, arguments = args};
						con.alignment = tan.option == Tangent.Option.Codirected ? pos : neg;
						
						break;
					}
					case Parallel par: {
						con = new ReplayConstraint{ type = "Parallelism" + D, arguments = args};
						break;
					}
					case Perpendicular per: {
						con = new ReplayConstraint{ type = "Perpendicularity" + D, arguments = args};
						//con.alignment = per.option == Perpendicular.Option.LeftHand ? pos : neg;
						con.alignment = "Current";
						break;
					}
					case AngleConstraint ang: {
						if (ang.HasEntitiesOfType(IEntityType.Point, 4)) {
							var p = new PointEntity[4];
							for (int i = 0; i < 4; i++) {
								p[i] = ang.GetEntityOfType(IEntityType.Point, i) as PointEntity;
							}
							args = new int[2];
							args[0] = map[pointsToLine[p[0]][p[1]]];
							args[1] = map[pointsToLine[p[2]][p[3]]];
						}
						con = new ReplayConstraint{ type = "Angle" + D, arguments = args, value = Math.Abs(ang.GetValueExp().Eval())};
						con.alignment = ang.GetValue() > 0.0 ? "Positive" : "Negative";
						//con.alignment = "Current";
						break;
					}
					case HVConstraint hv: {
						if(args.Length == 2) {
							continue;
							/*
							var p0 = hv.GetEntityOfType(IEntityType.Point, 0) as PointEntity;
							var p1 = hv.GetEntityOfType(IEntityType.Point, 1) as PointEntity;
							var obj = new ReplayObject{ 
								type = "Line" + D, 
								loc = fromVec(p0.GetPosition()), 
								dir = fromVec(Vector3.Normalize(p1.GetPosition() - p0.GetPosition()))
							};
							objects.Add(obj);
							args = new int[] { objects.Count - 1 };
							*/
						}
						if(hv.orientation == HVOrientation.OY) {
							con = new ReplayConstraint{ type = "Horizontality" + D, arguments = args };
						} else
						if(hv.orientation == HVOrientation.OX) {
							con = new ReplayConstraint{ type = "Verticality" + D, arguments = args };
						}
						break;
					}

					case Diameter d: {
						con = new ReplayConstraint{ type = "Radius" + D, arguments = args, value = d.GetValueExp().Eval() / 2.0 };
						break;
					}
				}
				if(con == null) {
					continue;
				}
				constraints.Add(con);
			}
			replay.objects = objects.ToArray();
			replay.constraints = constraints.ToArray();
			return replay;
		}

		public void CreateTo(Sketch sk) {
			var entities = new List<Entity>();
			foreach(var obj in objects) {
				Entity e = null;
				switch(obj.type) {
					case "Point2D": {
						var p = new PointEntity(sk);
						p.SetPosition(toVec(obj.loc));
						e = p;
						break;
					}
					case "Circle2D": {
						var c = new CircleEntity(sk);
						c.center.SetPosition(toVec(obj.cen));
						c.radius = obj.rad;
						e = c;
						break;
					}
					case "Line2D": {
						var l = new LineEntity(sk);
						l.p0.SetPosition(toVec(obj.loc));
						l.p1.SetPosition(l.p0.GetPosition() + toVec(obj.dir));
						e = l;
						break;
					}
				}
				entities.Add(e);
			}
			foreach(var con in constraints) {
				Constraint c = null;
				if(con.arguments.Length == 2) {
					var ct = con.type;
					var et0 = objects[con.arguments[0]].type;
					var et1 = objects[con.arguments[1]].type;
					var e0 = entities[con.arguments[0]];
					var e1 = entities[con.arguments[1]];
					c = (ct, et0, et1) switch
					{
						("Incidence2D", "Line2D", "Point2D") => new PointOn(sk, e1, e0),
						("Incidence2D", "Point2D", "Line2D") => new PointOn(sk, e0, e1),
						("Incidence2D", "Point2D", "Point2D") => new PointsCoincident(sk, e0, e1),
						("Incidence2D", "Point2D", "Circle2D") => new PointOn(sk, e0, e1),
						("Incidence2D", "Circle2D", "Point2D") => new PointOn(sk, e1, e0),
						("Concentricity2D", "Point2D", "Circle2D") => new PointsCoincident(sk, e0, e1.points.First()),
						("Concentricity2D", "Circle2D", "Point2D") => new PointsCoincident(sk, e0.points.First(), e1),
						("Concentricity2D", "Circle2D", "Circle2D") => new PointsCoincident(sk, e0.points.First(), e1.points.First()),
						("Concentricity2D", "Point2D", "Point2D") => new PointsCoincident(sk, e0, e1),
						("Tangency2D", "Circle2D", "Line2D") => new Tangent(sk, e0, e1),
						("Tangency2D", "Line2D", "Circle2D") => new Tangent(sk, e0, e1),
						("Perpendicularity2D", "Line2D", "Line2D") => new Perpendicular(sk, e0, e1),
						("Parallelism2D", "Line2D", "Line2D") => new Parallel(sk, e0, e1),
						("Distance2D", "Point2D", "Point2D") => new PointsDistance(sk, e0, e1),
						("Distance2D", "Circle2D", "Circle2D") => new CirclesDistance(sk, e0, e1),
						("Distance2D", "Point2D", "Line2D") => new PointLineDistance(sk, e0, e1),
						("Distance2D", "Line2D", "Point2D") => new PointLineDistance(sk, e1, e0),
						("Distance2D", "Circle2D", "Line2D") => new LineCircleDistance(sk, e1, e0),
						("Distance2D", "Line2D", "Circle2D") => new LineCircleDistance(sk, e0, e1),
						(_, _, _) => null
					};
				}
				if(c == null) {
					string message = "Constraint " + con.type + 
						"(" + con.arguments.Aggregate("", (a, b) => a + objects[b].type) + 
						") is not supported!\n";
					Debug.Log(message);
				}
			}
		}

		public void ApplyTo(Sketch sk) {
			foreach(var obj in objects) {
				Entity e = sk.GetChild(IdGenerator.Parse(obj.label)) as Entity;
				switch(obj.type) {
					case "Point2D": {
						var p = e as PointEntity;
						p.SetPosition(toVec(obj.loc));
						break;
					}
					case "Circle2D": {
						if(e is CircleEntity c) {
							c.center.SetPosition(toVec(obj.cen));
							c.radius = obj.rad;
						}
						break;
					}
					case "Line2D": {
						/*
						var l = e as LineEntity;
						l.p0.SetPosition(toVec(obj.loc));
						l.p1.SetPosition(l.p0.GetPosition() + toVec(obj.dir));
						*/
						break;
					}
				}
			}
		}
	}

	public class ReplayData {
		public string Replay;
		public ReplaySketch Input;
		public ReplaySketch Output;
		public string Result = "Success";
	}

	public class ReplaySerializer {
		
		public static ReplayData Load(byte[] data) {
			var str = Encoding.UTF8.GetString(data, 0, data.Length);
			return Load(str);
		}

		public static ReplayData Load(string str) {
			return JsonUtility.FromJson<ReplayData>(str);
		}

		public static ReplayData LoadFromFile(string filename) {
			var str = System.IO.File.ReadAllText(filename);
			return Load(str);
		}

		public static ReplayData Save(Sketch sk) {
			var data = new ReplayData();
			data.Input = ReplaySketch.From(sk);
			data.Output = data.Input;
			data.Replay = data.Input.Replay;
			return data;
		}

		public static string SaveToJson(Sketch sk) {
			var replay = Save(sk);
			var str = JsonUtility.ToJson(replay, true);
			/*
			if(replay.Input.dragTransforms != null && replay.Input.dragTransforms.Length > 0) {
				var strMatrix = "\"transformMatrix\": [";
				var matrix = replay.Input.dragTransforms[0].matrix;
				
				for(int i = 0; i < matrix.Length; i++) {
					string line = "[";
					for(int j = 0; j < matrix[i].Length; j++) {
						line += matrix[i][j].ToString();
						if(j < matrix[i].Length - 1) {
							line += ", ";
						}
					}
					line += "]";
					if(i < matrix.Length - 1) {
						line += ", ";
					}
					strMatrix += line;
				}
				strMatrix += "]";
				str = str.Replace("\"transformMatrix\": []", strMatrix);
			}
			*/
			return str;
		}

	}
}