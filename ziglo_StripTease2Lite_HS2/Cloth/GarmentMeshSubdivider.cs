using System;
using System.Collections.Generic;
using UnityEngine;

namespace StripTease2.Cloth
{
	// Token: 0x02000013 RID: 19
	internal static class GarmentMeshSubdivider
	{
		// Token: 0x060000AD RID: 173 RVA: 0x00008288 File Offset: 0x00006488
		public static GarmentSubdivisionResult Build(Mesh source, int subdivisionPasses)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			if (!source.isReadable)
			{
				throw new ArgumentException("The garment mesh must be CPU-readable.");
			}
			Vector3[] vertices = source.vertices;
			if (vertices == null || vertices.Length == 0)
			{
				throw new ArgumentException("The garment mesh has no vertices.");
			}
			if (subdivisionPasses < 1 || subdivisionPasses > 2)
			{
				throw new ArgumentOutOfRangeException("subdivisionPasses");
			}
			int[][] array = new int[source.subMeshCount][];
			for (int i = 0; i < source.subMeshCount; i++)
			{
				if (source.GetTopology(i) != null)
				{
					throw new ArgumentException("Subdivision supports triangle-list garment submeshes only.");
				}
				int[] triangles = source.GetTriangles(i);
				array[i] = triangles;
			}
			GarmentSubdivisionPlan garmentSubdivisionPlan = GarmentMeshSubdivider.BuildPlan(vertices, array, subdivisionPasses);
			int num = garmentSubdivisionPlan.SourceA.Length;
			Mesh mesh = new Mesh();
			mesh.name = source.name + " [ST2 Subdivided]";
			if (num > 65535)
			{
				mesh.indexFormat = 1;
			}
			int[] sourceA = garmentSubdivisionPlan.SourceA;
			int[] sourceB = garmentSubdivisionPlan.SourceB;
			mesh.vertices = GarmentMeshSubdivider.ExpandVector3(vertices, sourceA, sourceB, false);
			Vector3[] normals = source.normals;
			if (normals != null && normals.Length == vertices.Length)
			{
				mesh.normals = GarmentMeshSubdivider.ExpandVector3(normals, sourceA, sourceB, true);
			}
			Vector4[] tangents = source.tangents;
			if (tangents != null && tangents.Length == vertices.Length)
			{
				mesh.tangents = GarmentMeshSubdivider.ExpandTangents(tangents, sourceA, sourceB);
			}
			Vector2[] uv = source.uv;
			if (uv != null && uv.Length == vertices.Length)
			{
				mesh.uv = GarmentMeshSubdivider.ExpandVector2(uv, sourceA, sourceB);
			}
			Vector2[] uv2 = source.uv2;
			if (uv2 != null && uv2.Length == vertices.Length)
			{
				mesh.uv2 = GarmentMeshSubdivider.ExpandVector2(uv2, sourceA, sourceB);
			}
			GarmentMeshSubdivider.CopyAdditionalUvChannels(source, mesh, sourceA, sourceB);
			Color32[] colors = source.colors32;
			if (colors != null && colors.Length == vertices.Length)
			{
				mesh.colors32 = GarmentMeshSubdivider.ExpandColors(colors, sourceA, sourceB);
			}
			BoneWeight[] boneWeights = source.boneWeights;
			if (boneWeights == null || boneWeights.Length != vertices.Length)
			{
				Object.Destroy(mesh);
				throw new ArgumentException("The garment mesh has no usable skinning weights.");
			}
			mesh.boneWeights = GarmentMeshSubdivider.ExpandBoneWeights(boneWeights, sourceA, sourceB);
			mesh.bindposes = source.bindposes;
			mesh.subMeshCount = garmentSubdivisionPlan.TrianglesBySubmesh.Length;
			for (int j = 0; j < garmentSubdivisionPlan.TrianglesBySubmesh.Length; j++)
			{
				mesh.SetTriangles(garmentSubdivisionPlan.TrianglesBySubmesh[j], j);
			}
			GarmentMeshSubdivider.CopyBlendShapes(source, mesh, sourceA, sourceB);
			mesh.bounds = source.bounds;
			return new GarmentSubdivisionResult
			{
				Mesh = mesh,
				SourceVertexCount = vertices.Length,
				SourceA = sourceA,
				SourceB = sourceB,
				SourceTriangleCount = garmentSubdivisionPlan.SourceTriangleCount,
				SubdividedTriangleCount = garmentSubdivisionPlan.SubdividedTriangleCount
			};
		}

		// Token: 0x060000AE RID: 174 RVA: 0x00008524 File Offset: 0x00006724
		private static GarmentSubdivisionPlan BuildPlan(Vector3[] sourceVertices, int[][] trianglesBySubmesh, int subdivisionPasses)
		{
			if (sourceVertices == null || sourceVertices.Length == 0)
			{
				throw new ArgumentException("The garment mesh has no vertices.");
			}
			if (trianglesBySubmesh == null)
			{
				throw new ArgumentNullException("trianglesBySubmesh");
			}
			if (subdivisionPasses < 1 || subdivisionPasses > 2)
			{
				throw new ArgumentOutOfRangeException("subdivisionPasses");
			}
			int num = 0;
			foreach (int[] array in trianglesBySubmesh)
			{
				if (array == null || array.Length % 3 != 0)
				{
					throw new ArgumentException("Subdivision requires complete triangle lists.");
				}
				int num2 = 0;
				while (num2 + 2 < array.Length)
				{
					int num3 = array[num2];
					int num4 = array[num2 + 1];
					int num5 = array[num2 + 2];
					GarmentMeshSubdivider.ValidateVertex(num3, sourceVertices.Length);
					GarmentMeshSubdivider.ValidateVertex(num4, sourceVertices.Length);
					GarmentMeshSubdivider.ValidateVertex(num5, sourceVertices.Length);
					num++;
					num2 += 3;
				}
			}
			if (num == 0)
			{
				throw new ArgumentException("The garment mesh has no triangles.");
			}
			List<Vector3> list = new List<Vector3>(sourceVertices);
			List<int> list2 = new List<int>(sourceVertices.Length * 2);
			List<int> list3 = new List<int>(sourceVertices.Length * 2);
			for (int j = 0; j < sourceVertices.Length; j++)
			{
				list2.Add(j);
				list3.Add(j);
			}
			int[][] array2 = GarmentMeshSubdivider.ApplyPass(list, trianglesBySubmesh, 1, 2, list2, list3);
			if (subdivisionPasses >= 2)
			{
				array2 = GarmentMeshSubdivider.ApplyPass(list, array2, 1, 4, list2, list3);
			}
			int num6 = 0;
			for (int k = 0; k < array2.Length; k++)
			{
				num6 += array2[k].Length / 3;
			}
			return new GarmentSubdivisionPlan
			{
				SourceA = list2.ToArray(),
				SourceB = list3.ToArray(),
				TrianglesBySubmesh = array2,
				SourceTriangleCount = num,
				SubdividedTriangleCount = num6
			};
		}

		// Token: 0x060000AF RID: 175 RVA: 0x000086A8 File Offset: 0x000068A8
		private static int[][] ApplyPass(List<Vector3> positions, int[][] trianglesBySubmesh, int selectedNumerator, int selectedDenominator, List<int> sourceA, List<int> sourceB)
		{
			List<GarmentMeshSubdivider.TriangleInfo> list = new List<GarmentMeshSubdivider.TriangleInfo>();
			int num = 0;
			foreach (int[] array in trianglesBySubmesh)
			{
				int num2 = 0;
				while (num2 + 2 < array.Length)
				{
					int num3 = array[num2];
					int num4 = array[num2 + 1];
					int num5 = array[num2 + 2];
					Vector3 vector = Vector3.Cross(positions[num4] - positions[num3], positions[num5] - positions[num3]);
					list.Add(new GarmentMeshSubdivider.TriangleInfo
					{
						A = num3,
						B = num4,
						C = num5,
						Order = num++,
						AreaSquared = vector.sqrMagnitude
					});
					num2 += 3;
				}
			}
			GarmentMeshSubdivider.TriangleInfo[] array2 = list.ToArray();
			Array.Sort<GarmentMeshSubdivider.TriangleInfo>(array2, delegate(GarmentMeshSubdivider.TriangleInfo x, GarmentMeshSubdivider.TriangleInfo y)
			{
				int num8 = y.AreaSquared.CompareTo(x.AreaSquared);
				if (num8 == 0)
				{
					return x.Order.CompareTo(y.Order);
				}
				return num8;
			});
			int num6 = (array2.Length * selectedNumerator + selectedDenominator - 1) / selectedDenominator;
			HashSet<GarmentMeshSubdivider.EdgeKey> hashSet = new HashSet<GarmentMeshSubdivider.EdgeKey>();
			for (int j = 0; j < num6; j++)
			{
				hashSet.Add(GarmentMeshSubdivider.LongestEdge(array2[j], positions));
			}
			Dictionary<GarmentMeshSubdivider.EdgeKey, int> dictionary = new Dictionary<GarmentMeshSubdivider.EdgeKey, int>();
			int[][] array3 = new int[trianglesBySubmesh.Length][];
			for (int k = 0; k < trianglesBySubmesh.Length; k++)
			{
				int[] array4 = trianglesBySubmesh[k];
				List<int> list2 = new List<int>(array4.Length * 2);
				int num7 = 0;
				while (num7 + 2 < array4.Length)
				{
					GarmentMeshSubdivider.EmitTriangle(array4[num7], array4[num7 + 1], array4[num7 + 2], hashSet, dictionary, sourceA, sourceB, positions, list2);
					num7 += 3;
				}
				array3[k] = list2.ToArray();
			}
			return array3;
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x00008858 File Offset: 0x00006A58
		private static void ValidateVertex(int vertex, int vertexCount)
		{
			if (vertex < 0 || vertex >= vertexCount)
			{
				throw new ArgumentException("The garment mesh contains an invalid triangle index.");
			}
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x00008870 File Offset: 0x00006A70
		private static GarmentMeshSubdivider.EdgeKey LongestEdge(GarmentMeshSubdivider.TriangleInfo triangle, IList<Vector3> vertices)
		{
			GarmentMeshSubdivider.EdgeKey edgeKey = new GarmentMeshSubdivider.EdgeKey(triangle.A, triangle.B);
			GarmentMeshSubdivider.EdgeKey edgeKey2 = new GarmentMeshSubdivider.EdgeKey(triangle.B, triangle.C);
			GarmentMeshSubdivider.EdgeKey edgeKey3 = new GarmentMeshSubdivider.EdgeKey(triangle.C, triangle.A);
			float sqrMagnitude = (vertices[triangle.A] - vertices[triangle.B]).sqrMagnitude;
			float sqrMagnitude2 = (vertices[triangle.B] - vertices[triangle.C]).sqrMagnitude;
			float sqrMagnitude3 = (vertices[triangle.C] - vertices[triangle.A]).sqrMagnitude;
			GarmentMeshSubdivider.EdgeKey edgeKey4 = edgeKey;
			float num = sqrMagnitude;
			if (sqrMagnitude2 > num || (Mathf.Approximately(sqrMagnitude2, num) && GarmentMeshSubdivider.CompareEdges(edgeKey2, edgeKey4) < 0))
			{
				edgeKey4 = edgeKey2;
				num = sqrMagnitude2;
			}
			if (sqrMagnitude3 > num || (Mathf.Approximately(sqrMagnitude3, num) && GarmentMeshSubdivider.CompareEdges(edgeKey3, edgeKey4) < 0))
			{
				edgeKey4 = edgeKey3;
			}
			return edgeKey4;
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x00008974 File Offset: 0x00006B74
		private static int CompareEdges(GarmentMeshSubdivider.EdgeKey x, GarmentMeshSubdivider.EdgeKey y)
		{
			int num = x.A.CompareTo(y.A);
			if (num == 0)
			{
				return x.B.CompareTo(y.B);
			}
			return num;
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x000089B0 File Offset: 0x00006BB0
		private static void EmitTriangle(int a, int b, int c, HashSet<GarmentMeshSubdivider.EdgeKey> splitEdges, Dictionary<GarmentMeshSubdivider.EdgeKey, int> midpointIndices, List<int> sourceA, List<int> sourceB, List<Vector3> positions, List<int> output)
		{
			GarmentMeshSubdivider.EdgeKey edgeKey = new GarmentMeshSubdivider.EdgeKey(a, b);
			GarmentMeshSubdivider.EdgeKey edgeKey2 = new GarmentMeshSubdivider.EdgeKey(b, c);
			GarmentMeshSubdivider.EdgeKey edgeKey3 = new GarmentMeshSubdivider.EdgeKey(c, a);
			bool flag = splitEdges.Contains(edgeKey);
			bool flag2 = splitEdges.Contains(edgeKey2);
			bool flag3 = splitEdges.Contains(edgeKey3);
			int num = (((flag > false) + (flag2 > false) + (flag3 > false)) ? 1 : 0);
			if (num == 0)
			{
				GarmentMeshSubdivider.AddTriangle(output, a, b, c);
				return;
			}
			int num2 = (flag ? GarmentMeshSubdivider.Midpoint(edgeKey, midpointIndices, sourceA, sourceB, positions) : (-1));
			int num3 = (flag2 ? GarmentMeshSubdivider.Midpoint(edgeKey2, midpointIndices, sourceA, sourceB, positions) : (-1));
			int num4 = (flag3 ? GarmentMeshSubdivider.Midpoint(edgeKey3, midpointIndices, sourceA, sourceB, positions) : (-1));
			if (num == 1)
			{
				if (flag)
				{
					GarmentMeshSubdivider.AddTriangle(output, a, num2, c);
					GarmentMeshSubdivider.AddTriangle(output, num2, b, c);
					return;
				}
				if (flag2)
				{
					GarmentMeshSubdivider.AddTriangle(output, a, b, num3);
					GarmentMeshSubdivider.AddTriangle(output, a, num3, c);
					return;
				}
				GarmentMeshSubdivider.AddTriangle(output, a, b, num4);
				GarmentMeshSubdivider.AddTriangle(output, num4, b, c);
				return;
			}
			else
			{
				if (num != 2)
				{
					GarmentMeshSubdivider.AddTriangle(output, a, num2, num4);
					GarmentMeshSubdivider.AddTriangle(output, num2, b, num3);
					GarmentMeshSubdivider.AddTriangle(output, num4, num3, c);
					GarmentMeshSubdivider.AddTriangle(output, num2, num3, num4);
					return;
				}
				if (flag && flag2)
				{
					GarmentMeshSubdivider.AddTriangle(output, num2, b, num3);
					GarmentMeshSubdivider.AddTriangle(output, a, num2, num3);
					GarmentMeshSubdivider.AddTriangle(output, a, num3, c);
					return;
				}
				if (flag2 && flag3)
				{
					GarmentMeshSubdivider.AddTriangle(output, num3, c, num4);
					GarmentMeshSubdivider.AddTriangle(output, a, b, num3);
					GarmentMeshSubdivider.AddTriangle(output, a, num3, num4);
					return;
				}
				GarmentMeshSubdivider.AddTriangle(output, a, num2, num4);
				GarmentMeshSubdivider.AddTriangle(output, num2, b, c);
				GarmentMeshSubdivider.AddTriangle(output, num2, c, num4);
				return;
			}
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x00008B58 File Offset: 0x00006D58
		private static int Midpoint(GarmentMeshSubdivider.EdgeKey edge, Dictionary<GarmentMeshSubdivider.EdgeKey, int> midpointIndices, List<int> sourceA, List<int> sourceB, List<Vector3> positions)
		{
			int count;
			if (midpointIndices.TryGetValue(edge, out count))
			{
				return count;
			}
			count = sourceA.Count;
			midpointIndices.Add(edge, count);
			sourceA.Add(edge.A);
			sourceB.Add(edge.B);
			positions.Add((positions[edge.A] + positions[edge.B]) * 0.5f);
			return count;
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x00008BCA File Offset: 0x00006DCA
		private static void AddTriangle(List<int> output, int a, int b, int c)
		{
			output.Add(a);
			output.Add(b);
			output.Add(c);
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x00008BE4 File Offset: 0x00006DE4
		private static Vector3[] ExpandVector3(Vector3[] source, int[] sourceA, int[] sourceB, bool normalize)
		{
			Vector3[] array = new Vector3[sourceA.Length];
			for (int i = 0; i < source.Length; i++)
			{
				array[i] = source[i];
			}
			for (int j = source.Length; j < array.Length; j++)
			{
				Vector3 vector = ((sourceA[j] == sourceB[j]) ? array[sourceA[j]] : ((array[sourceA[j]] + array[sourceB[j]]) * 0.5f));
				array[j] = ((normalize && vector.sqrMagnitude > 1E-16f) ? vector.normalized : vector);
			}
			return array;
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x00008C80 File Offset: 0x00006E80
		private static Vector2[] ExpandVector2(Vector2[] source, int[] sourceA, int[] sourceB)
		{
			Vector2[] array = new Vector2[sourceA.Length];
			for (int i = 0; i < source.Length; i++)
			{
				array[i] = source[i];
			}
			for (int j = source.Length; j < array.Length; j++)
			{
				array[j] = ((sourceA[j] == sourceB[j]) ? array[sourceA[j]] : ((array[sourceA[j]] + array[sourceB[j]]) * 0.5f));
			}
			return array;
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x00008D00 File Offset: 0x00006F00
		private static Vector4[] ExpandTangents(Vector4[] source, int[] sourceA, int[] sourceB)
		{
			Vector4[] array = new Vector4[sourceA.Length];
			for (int i = 0; i < source.Length; i++)
			{
				array[i] = source[i];
			}
			for (int j = source.Length; j < array.Length; j++)
			{
				if (sourceA[j] == sourceB[j])
				{
					array[j] = array[sourceA[j]];
				}
				else
				{
					Vector4 vector = array[sourceA[j]];
					Vector4 vector2 = array[sourceB[j]];
					Vector3 vector3;
					vector3..ctor(vector.x + vector2.x, vector.y + vector2.y, vector.z + vector2.z);
					if (vector3.sqrMagnitude > 1E-16f)
					{
						vector3.Normalize();
					}
					array[j] = new Vector4(vector3.x, vector3.y, vector3.z, vector.w);
				}
			}
			return array;
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x00008DE8 File Offset: 0x00006FE8
		private static void CopyAdditionalUvChannels(Mesh source, Mesh destination, int[] sourceA, int[] sourceB)
		{
			for (int i = 2; i < 8; i++)
			{
				List<Vector4> list = new List<Vector4>();
				source.GetUVs(i, list);
				if (list.Count == source.vertexCount)
				{
					Vector4[] array = GarmentMeshSubdivider.ExpandVector4(list.ToArray(), sourceA, sourceB);
					destination.SetUVs(i, new List<Vector4>(array));
				}
			}
		}

		// Token: 0x060000BA RID: 186 RVA: 0x00008E38 File Offset: 0x00007038
		private static Vector4[] ExpandVector4(Vector4[] source, int[] sourceA, int[] sourceB)
		{
			Vector4[] array = new Vector4[sourceA.Length];
			for (int i = 0; i < source.Length; i++)
			{
				array[i] = source[i];
			}
			for (int j = source.Length; j < array.Length; j++)
			{
				array[j] = ((sourceA[j] == sourceB[j]) ? array[sourceA[j]] : ((array[sourceA[j]] + array[sourceB[j]]) * 0.5f));
			}
			return array;
		}

		// Token: 0x060000BB RID: 187 RVA: 0x00008EB8 File Offset: 0x000070B8
		private static Color32[] ExpandColors(Color32[] source, int[] sourceA, int[] sourceB)
		{
			Color32[] array = new Color32[sourceA.Length];
			for (int i = 0; i < source.Length; i++)
			{
				array[i] = source[i];
			}
			for (int j = source.Length; j < array.Length; j++)
			{
				Color32 color = array[sourceA[j]];
				if (sourceA[j] == sourceB[j])
				{
					array[j] = color;
				}
				else
				{
					Color32 color2 = array[sourceB[j]];
					array[j] = new Color32((color.r + color2.r) / 2, (color.g + color2.g) / 2, (color.b + color2.b) / 2, (color.a + color2.a) / 2);
				}
			}
			return array;
		}

		// Token: 0x060000BC RID: 188 RVA: 0x00008F78 File Offset: 0x00007178
		private static BoneWeight[] ExpandBoneWeights(BoneWeight[] source, int[] sourceA, int[] sourceB)
		{
			BoneWeight[] array = new BoneWeight[sourceA.Length];
			for (int i = 0; i < source.Length; i++)
			{
				array[i] = source[i];
			}
			for (int j = source.Length; j < array.Length; j++)
			{
				array[j] = ((sourceA[j] == sourceB[j]) ? array[sourceA[j]] : GarmentMeshSubdivider.BlendWeights(array[sourceA[j]], array[sourceB[j]]));
			}
			return array;
		}

		// Token: 0x060000BD RID: 189 RVA: 0x00008FEC File Offset: 0x000071EC
		private static BoneWeight BlendWeights(BoneWeight a, BoneWeight b)
		{
			int[] array = new int[8];
			float[] array2 = new float[8];
			int num = 0;
			GarmentMeshSubdivider.AddInfluence(array, array2, ref num, a.boneIndex0, a.weight0 * 0.5f);
			GarmentMeshSubdivider.AddInfluence(array, array2, ref num, a.boneIndex1, a.weight1 * 0.5f);
			GarmentMeshSubdivider.AddInfluence(array, array2, ref num, a.boneIndex2, a.weight2 * 0.5f);
			GarmentMeshSubdivider.AddInfluence(array, array2, ref num, a.boneIndex3, a.weight3 * 0.5f);
			GarmentMeshSubdivider.AddInfluence(array, array2, ref num, b.boneIndex0, b.weight0 * 0.5f);
			GarmentMeshSubdivider.AddInfluence(array, array2, ref num, b.boneIndex1, b.weight1 * 0.5f);
			GarmentMeshSubdivider.AddInfluence(array, array2, ref num, b.boneIndex2, b.weight2 * 0.5f);
			GarmentMeshSubdivider.AddInfluence(array, array2, ref num, b.boneIndex3, b.weight3 * 0.5f);
			for (int i = 0; i < num - 1; i++)
			{
				for (int j = i + 1; j < num; j++)
				{
					if (array2[j] >= array2[i] && (!Mathf.Approximately(array2[j], array2[i]) || array[j] < array[i]))
					{
						float num2 = array2[i];
						array2[i] = array2[j];
						array2[j] = num2;
						int num3 = array[i];
						array[i] = array[j];
						array[j] = num3;
					}
				}
			}
			float num4 = 0f;
			int num5 = Mathf.Min(4, num);
			for (int k = 0; k < num5; k++)
			{
				num4 += array2[k];
			}
			if (num4 <= 1E-08f)
			{
				BoneWeight boneWeight = default(BoneWeight);
				boneWeight.boneIndex0 = 0;
				boneWeight.weight0 = 1f;
				return boneWeight;
			}
			BoneWeight boneWeight2 = default(BoneWeight);
			if (num5 > 0)
			{
				boneWeight2.boneIndex0 = array[0];
				boneWeight2.weight0 = array2[0] / num4;
			}
			if (num5 > 1)
			{
				boneWeight2.boneIndex1 = array[1];
				boneWeight2.weight1 = array2[1] / num4;
			}
			if (num5 > 2)
			{
				boneWeight2.boneIndex2 = array[2];
				boneWeight2.weight2 = array2[2] / num4;
			}
			if (num5 > 3)
			{
				boneWeight2.boneIndex3 = array[3];
				boneWeight2.weight3 = array2[3] / num4;
			}
			return boneWeight2;
		}

		// Token: 0x060000BE RID: 190 RVA: 0x00009228 File Offset: 0x00007428
		private static void AddInfluence(int[] bones, float[] weights, ref int count, int bone, float weight)
		{
			if (weight <= 0f)
			{
				return;
			}
			for (int i = 0; i < count; i++)
			{
				if (bones[i] == bone)
				{
					weights[i] += weight;
					return;
				}
			}
			bones[count] = bone;
			weights[count] = weight;
			count++;
		}

		// Token: 0x060000BF RID: 191 RVA: 0x00009274 File Offset: 0x00007474
		private static void CopyBlendShapes(Mesh source, Mesh destination, int[] sourceA, int[] sourceB)
		{
			if (source.blendShapeCount == 0)
			{
				return;
			}
			int vertexCount = source.vertexCount;
			Vector3[] array = new Vector3[vertexCount];
			Vector3[] array2 = new Vector3[vertexCount];
			Vector3[] array3 = new Vector3[vertexCount];
			for (int i = 0; i < source.blendShapeCount; i++)
			{
				string blendShapeName = source.GetBlendShapeName(i);
				int blendShapeFrameCount = source.GetBlendShapeFrameCount(i);
				for (int j = 0; j < blendShapeFrameCount; j++)
				{
					Array.Clear(array, 0, array.Length);
					Array.Clear(array2, 0, array2.Length);
					Array.Clear(array3, 0, array3.Length);
					source.GetBlendShapeFrameVertices(i, j, array, array2, array3);
					destination.AddBlendShapeFrame(blendShapeName, source.GetBlendShapeFrameWeight(i, j), GarmentMeshSubdivider.ExpandVector3(array, sourceA, sourceB, false), GarmentMeshSubdivider.ExpandVector3(array2, sourceA, sourceB, false), GarmentMeshSubdivider.ExpandVector3(array3, sourceA, sourceB, false));
				}
			}
		}

		// Token: 0x02000021 RID: 33
		private struct EdgeKey : IEquatable<GarmentMeshSubdivider.EdgeKey>
		{
			// Token: 0x060000E3 RID: 227 RVA: 0x00009A78 File Offset: 0x00007C78
			public EdgeKey(int a, int b)
			{
				this.A = ((a < b) ? a : b);
				this.B = ((a < b) ? b : a);
			}

			// Token: 0x060000E4 RID: 228 RVA: 0x00009A96 File Offset: 0x00007C96
			public bool Equals(GarmentMeshSubdivider.EdgeKey other)
			{
				return this.A == other.A && this.B == other.B;
			}

			// Token: 0x060000E5 RID: 229 RVA: 0x00009AB6 File Offset: 0x00007CB6
			public override bool Equals(object obj)
			{
				return obj is GarmentMeshSubdivider.EdgeKey && this.Equals((GarmentMeshSubdivider.EdgeKey)obj);
			}

			// Token: 0x060000E6 RID: 230 RVA: 0x00009ACE File Offset: 0x00007CCE
			public override int GetHashCode()
			{
				return (this.A * 397) ^ this.B;
			}

			// Token: 0x040000BD RID: 189
			public readonly int A;

			// Token: 0x040000BE RID: 190
			public readonly int B;
		}

		// Token: 0x02000022 RID: 34
		private sealed class TriangleInfo
		{
			// Token: 0x040000BF RID: 191
			public int A;

			// Token: 0x040000C0 RID: 192
			public int B;

			// Token: 0x040000C1 RID: 193
			public int C;

			// Token: 0x040000C2 RID: 194
			public int Order;

			// Token: 0x040000C3 RID: 195
			public float AreaSquared;
		}
	}
}
