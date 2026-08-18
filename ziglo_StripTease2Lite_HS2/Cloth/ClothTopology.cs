using System;
using System.Collections.Generic;
using UnityEngine;

namespace StripTease2.Cloth
{
	// Token: 0x0200000F RID: 15
	internal sealed class ClothTopology
	{
		// Token: 0x0600004D RID: 77 RVA: 0x00004A58 File Offset: 0x00002C58
		public static ClothTopology Build(Vector3[] restPositions, int[] meshTriangles, float weldTolerance, bool buildConstraints, bool[] preserveSplitVertices = null, int[] weldGroups = null)
		{
			ClothTopology clothTopology = new ClothTopology();
			float num = Mathf.Max(weldTolerance, 1E-07f);
			Dictionary<long, Dictionary<int, int>> dictionary = new Dictionary<long, Dictionary<int, int>>(restPositions.Length);
			clothTopology.VertexToParticle = new int[restPositions.Length];
			List<int> list = new List<int>(restPositions.Length);
			for (int i = 0; i < restPositions.Length; i++)
			{
				if (preserveSplitVertices != null && i < preserveSplitVertices.Length && preserveSplitVertices[i])
				{
					int count = list.Count;
					list.Add(i);
					clothTopology.VertexToParticle[i] = count;
				}
				else
				{
					long weldKey = ClothTopology.GetWeldKey(restPositions[i], num);
					int num2 = ((weldGroups != null && i < weldGroups.Length) ? weldGroups[i] : 0);
					Dictionary<int, int> dictionary2;
					if (!dictionary.TryGetValue(weldKey, out dictionary2))
					{
						dictionary2 = new Dictionary<int, int>();
						dictionary.Add(weldKey, dictionary2);
					}
					int count2;
					if (!dictionary2.TryGetValue(num2, out count2))
					{
						count2 = list.Count;
						list.Add(i);
						dictionary2.Add(num2, count2);
					}
					clothTopology.VertexToParticle[i] = count2;
				}
			}
			clothTopology.ParticleCount = list.Count;
			clothTopology.ParticleToFirstVertex = list.ToArray();
			List<int> list2 = new List<int>(meshTriangles.Length);
			List<int> list3 = new List<int>(meshTriangles.Length);
			int num3 = 0;
			int num4 = 0;
			while (num4 + 2 < meshTriangles.Length)
			{
				int num5 = meshTriangles[num4];
				int num6 = meshTriangles[num4 + 1];
				int num7 = meshTriangles[num4 + 2];
				if (num5 < 0 || num6 < 0 || num7 < 0 || num5 >= restPositions.Length || num6 >= restPositions.Length || num7 >= restPositions.Length)
				{
					num3++;
				}
				else
				{
					int num8 = clothTopology.VertexToParticle[num5];
					int num9 = clothTopology.VertexToParticle[num6];
					int num10 = clothTopology.VertexToParticle[num7];
					if (num8 == num9 || num9 == num10 || num8 == num10)
					{
						num3++;
					}
					else
					{
						list2.Add(num8);
						list2.Add(num9);
						list2.Add(num10);
						list3.Add(num5);
						list3.Add(num6);
						list3.Add(num7);
					}
				}
				num4 += 3;
			}
			clothTopology.Triangles = list2.ToArray();
			clothTopology.TriangleVertices = list3.ToArray();
			clothTopology.SkippedTriangleCount = num3;
			if (!buildConstraints)
			{
				clothTopology.Edges = new EdgeConstraint[0];
				clothTopology.Bends = new BendConstraint[0];
				clothTopology.Neighbors = new int[0][];
				return clothTopology;
			}
			Dictionary<long, int> dictionary3 = new Dictionary<long, int>(list2.Count);
			List<EdgeConstraint> list4 = new List<EdgeConstraint>(list2.Count);
			List<BendConstraint> list5 = new List<BendConstraint>(list2.Count / 2);
			int num11 = 0;
			while (num11 + 2 < clothTopology.Triangles.Length)
			{
				int num12 = clothTopology.Triangles[num11];
				int num13 = clothTopology.Triangles[num11 + 1];
				int num14 = clothTopology.Triangles[num11 + 2];
				ClothTopology.AddEdge(clothTopology, dictionary3, list4, list5, num12, num13, num14, restPositions, list);
				ClothTopology.AddEdge(clothTopology, dictionary3, list4, list5, num13, num14, num12, restPositions, list);
				ClothTopology.AddEdge(clothTopology, dictionary3, list4, list5, num14, num12, num13, restPositions, list);
				num11 += 3;
			}
			clothTopology.Edges = list4.ToArray();
			clothTopology.Bends = list5.ToArray();
			List<int>[] array = new List<int>[clothTopology.ParticleCount];
			for (int j = 0; j < list4.Count; j++)
			{
				EdgeConstraint edgeConstraint = list4[j];
				if (array[edgeConstraint.A] == null)
				{
					array[edgeConstraint.A] = new List<int>(6);
				}
				if (array[edgeConstraint.B] == null)
				{
					array[edgeConstraint.B] = new List<int>(6);
				}
				array[edgeConstraint.A].Add(edgeConstraint.B);
				array[edgeConstraint.B].Add(edgeConstraint.A);
			}
			clothTopology.Neighbors = new int[clothTopology.ParticleCount][];
			for (int k = 0; k < clothTopology.ParticleCount; k++)
			{
				clothTopology.Neighbors[k] = ((array[k] != null) ? array[k].ToArray() : new int[0]);
			}
			clothTopology._geodesicHeap = new int[clothTopology.ParticleCount];
			clothTopology._geodesicHeapPositions = new int[clothTopology.ParticleCount];
			return clothTopology;
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00004E59 File Offset: 0x00003059
		public void ComputeGeodesicDistances(Vector3[] particleRestPositions, int source, float[] distances)
		{
			this.BeginGeodesicQuery(distances, null);
			if (source < 0 || source >= this.ParticleCount)
			{
				return;
			}
			distances[source] = 0f;
			this.PushOrDecreaseGeodesic(source, distances);
			this.RunGeodesicQuery(particleRestPositions, distances, null);
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00004E8C File Offset: 0x0000308C
		public bool ComputeNearestGeodesicSources(Vector3[] particleRestPositions, float[] sourceWeights, float sourceThreshold, float[] distances, int[] nearestSources)
		{
			this.BeginGeodesicQuery(distances, nearestSources);
			bool flag = false;
			for (int i = 0; i < this.ParticleCount; i++)
			{
				if (sourceWeights[i] > sourceThreshold)
				{
					distances[i] = 0f;
					nearestSources[i] = i;
					this.PushOrDecreaseGeodesic(i, distances);
					flag = true;
				}
			}
			if (flag)
			{
				this.RunGeodesicQuery(particleRestPositions, distances, nearestSources);
			}
			return flag;
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00004EE4 File Offset: 0x000030E4
		private void BeginGeodesicQuery(float[] distances, int[] nearestSources)
		{
			if (this._geodesicHeap == null || this._geodesicHeap.Length != this.ParticleCount)
			{
				this._geodesicHeap = new int[this.ParticleCount];
				this._geodesicHeapPositions = new int[this.ParticleCount];
			}
			this._geodesicHeapCount = 0;
			for (int i = 0; i < this.ParticleCount; i++)
			{
				distances[i] = float.PositiveInfinity;
				this._geodesicHeapPositions[i] = -1;
				if (nearestSources != null)
				{
					nearestSources[i] = -1;
				}
			}
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00004F5C File Offset: 0x0000315C
		private void RunGeodesicQuery(Vector3[] particleRestPositions, float[] distances, int[] nearestSources)
		{
			while (this._geodesicHeapCount > 0)
			{
				int num = this.PopGeodesic(distances);
				float num2 = distances[num];
				foreach (int num3 in this.Neighbors[num])
				{
					float num4 = Vector3.Distance(particleRestPositions[num], particleRestPositions[num3]);
					if (num4 > 1E-07f)
					{
						float num5 = num2 + num4;
						if (num5 < distances[num3])
						{
							distances[num3] = num5;
							if (nearestSources != null)
							{
								nearestSources[num3] = nearestSources[num];
							}
							this.PushOrDecreaseGeodesic(num3, distances);
						}
					}
				}
			}
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00004FE4 File Offset: 0x000031E4
		private void PushOrDecreaseGeodesic(int particle, float[] distances)
		{
			int i = this._geodesicHeapPositions[particle];
			if (i < 0)
			{
				int geodesicHeapCount = this._geodesicHeapCount;
				this._geodesicHeapCount = geodesicHeapCount + 1;
				i = geodesicHeapCount;
				this._geodesicHeap[i] = particle;
				this._geodesicHeapPositions[particle] = i;
			}
			while (i > 0)
			{
				int num = i - 1 >> 1;
				if (distances[this._geodesicHeap[num]] <= distances[particle])
				{
					break;
				}
				this.SwapGeodesicHeap(i, num);
				i = num;
			}
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00005048 File Offset: 0x00003248
		private int PopGeodesic(float[] distances)
		{
			int num = this._geodesicHeap[0];
			this._geodesicHeapPositions[num] = -1;
			this._geodesicHeapCount--;
			if (this._geodesicHeapCount == 0)
			{
				return num;
			}
			int num2 = this._geodesicHeap[this._geodesicHeapCount];
			this._geodesicHeap[0] = num2;
			this._geodesicHeapPositions[num2] = 0;
			int num3 = 0;
			for (;;)
			{
				int num4 = num3 * 2 + 1;
				if (num4 >= this._geodesicHeapCount)
				{
					break;
				}
				int num5 = num4 + 1;
				int num6 = ((num5 < this._geodesicHeapCount && distances[this._geodesicHeap[num5]] < distances[this._geodesicHeap[num4]]) ? num5 : num4);
				if (distances[this._geodesicHeap[num3]] <= distances[this._geodesicHeap[num6]])
				{
					break;
				}
				this.SwapGeodesicHeap(num3, num6);
				num3 = num6;
			}
			return num;
		}

		// Token: 0x06000054 RID: 84 RVA: 0x00005104 File Offset: 0x00003304
		private void SwapGeodesicHeap(int a, int b)
		{
			int num = this._geodesicHeap[a];
			int num2 = this._geodesicHeap[b];
			this._geodesicHeap[a] = num2;
			this._geodesicHeap[b] = num;
			this._geodesicHeapPositions[num] = b;
			this._geodesicHeapPositions[num2] = a;
		}

		// Token: 0x06000055 RID: 85 RVA: 0x00005148 File Offset: 0x00003348
		public void RecomputeRestLengths(Vector3[] particleRestPositions)
		{
			for (int i = 0; i < this.Edges.Length; i++)
			{
				this.Edges[i].Rest = Vector3.Distance(particleRestPositions[this.Edges[i].A], particleRestPositions[this.Edges[i].B]);
			}
			for (int j = 0; j < this.Bends.Length; j++)
			{
				this.Bends[j].Rest = Vector3.Distance(particleRestPositions[this.Bends[j].A], particleRestPositions[this.Bends[j].B]);
			}
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00005204 File Offset: 0x00003404
		public bool[] RemoveTriangles(bool[] removedTriangles, Vector3[] particleRestPositions)
		{
			if (removedTriangles == null || removedTriangles.Length != this.Triangles.Length / 3)
			{
				throw new ArgumentException("The removal mask must match the triangle count.");
			}
			if (particleRestPositions == null || particleRestPositions.Length != this.ParticleCount)
			{
				throw new ArgumentException("Rest positions must match the particle count.");
			}
			List<int> list = new List<int>(this.Triangles.Length);
			List<int> list2 = new List<int>(this.TriangleVertices.Length);
			int num = 0;
			while (num + 2 < this.Triangles.Length)
			{
				int num2 = this.Triangles[num];
				int num3 = this.Triangles[num + 1];
				int num4 = this.Triangles[num + 2];
				if (!removedTriangles[num / 3])
				{
					list.Add(num2);
					list.Add(num3);
					list.Add(num4);
					list2.Add(this.TriangleVertices[num]);
					list2.Add(this.TriangleVertices[num + 1]);
					list2.Add(this.TriangleVertices[num + 2]);
				}
				num += 3;
			}
			this.Triangles = list.ToArray();
			this.TriangleVertices = list2.ToArray();
			Dictionary<long, int> dictionary = new Dictionary<long, int>(this.Triangles.Length);
			List<EdgeConstraint> list3 = new List<EdgeConstraint>(this.Triangles.Length);
			List<BendConstraint> list4 = new List<BendConstraint>(this.Triangles.Length / 2);
			int num5 = 0;
			while (num5 + 2 < this.Triangles.Length)
			{
				int num6 = this.Triangles[num5];
				int num7 = this.Triangles[num5 + 1];
				int num8 = this.Triangles[num5 + 2];
				ClothTopology.AddParticleEdge(dictionary, list3, list4, num6, num7, num8, particleRestPositions);
				ClothTopology.AddParticleEdge(dictionary, list3, list4, num7, num8, num6, particleRestPositions);
				ClothTopology.AddParticleEdge(dictionary, list3, list4, num8, num6, num7, particleRestPositions);
				num5 += 3;
			}
			this.Edges = list3.ToArray();
			this.Bends = list4.ToArray();
			List<int>[] array = new List<int>[this.ParticleCount];
			for (int i = 0; i < this.Edges.Length; i++)
			{
				EdgeConstraint edgeConstraint = this.Edges[i];
				if (array[edgeConstraint.A] == null)
				{
					array[edgeConstraint.A] = new List<int>(6);
				}
				if (array[edgeConstraint.B] == null)
				{
					array[edgeConstraint.B] = new List<int>(6);
				}
				array[edgeConstraint.A].Add(edgeConstraint.B);
				array[edgeConstraint.B].Add(edgeConstraint.A);
			}
			this.Neighbors = new int[this.ParticleCount][];
			for (int j = 0; j < this.ParticleCount; j++)
			{
				this.Neighbors[j] = ((array[j] != null) ? array[j].ToArray() : new int[0]);
			}
			bool[] array2 = new bool[this.ParticleCount];
			for (int k = 0; k < this.ParticleCount; k++)
			{
				array2[k] = this.Neighbors[k].Length == 0;
			}
			return array2;
		}

		// Token: 0x06000057 RID: 87 RVA: 0x000054DC File Offset: 0x000036DC
		private static void AddParticleEdge(Dictionary<long, int> edgeToOpposite, List<EdgeConstraint> edges, List<BendConstraint> bends, int a, int b, int opposite, Vector3[] particleRestPositions)
		{
			long num = ((a < b) ? (((long)a << 32) | (long)((ulong)b)) : (((long)b << 32) | (long)((ulong)a)));
			int num2;
			if (!edgeToOpposite.TryGetValue(num, out num2))
			{
				edgeToOpposite.Add(num, opposite);
				float num3 = Vector3.Distance(particleRestPositions[a], particleRestPositions[b]);
				if (num3 > 1E-07f)
				{
					edges.Add(new EdgeConstraint
					{
						A = a,
						B = b,
						Rest = num3
					});
				}
				return;
			}
			if (num2 >= 0 && num2 != opposite)
			{
				float num4 = Vector3.Distance(particleRestPositions[num2], particleRestPositions[opposite]);
				if (num4 > 1E-07f)
				{
					bends.Add(new BendConstraint
					{
						A = num2,
						B = opposite,
						Rest = num4
					});
				}
				edgeToOpposite[num] = -1;
			}
		}

		// Token: 0x06000058 RID: 88 RVA: 0x000055BC File Offset: 0x000037BC
		private static void AddEdge(ClothTopology topology, Dictionary<long, int> edgeToOpposite, List<EdgeConstraint> edges, List<BendConstraint> bends, int a, int b, int opposite, Vector3[] restPositions, List<int> firstVertex)
		{
			long num = ((a < b) ? (((long)a << 32) | (long)((ulong)b)) : (((long)b << 32) | (long)((ulong)a)));
			int num2;
			if (!edgeToOpposite.TryGetValue(num, out num2))
			{
				edgeToOpposite.Add(num, opposite);
				float num3 = Vector3.Distance(restPositions[firstVertex[a]], restPositions[firstVertex[b]]);
				if (num3 > 1E-07f)
				{
					edges.Add(new EdgeConstraint
					{
						A = a,
						B = b,
						Rest = num3
					});
				}
				return;
			}
			if (num2 >= 0 && num2 != opposite)
			{
				float num4 = Vector3.Distance(restPositions[firstVertex[num2]], restPositions[firstVertex[opposite]]);
				if (num4 > 1E-07f)
				{
					bends.Add(new BendConstraint
					{
						A = num2,
						B = opposite,
						Rest = num4
					});
				}
				edgeToOpposite[num] = -1;
			}
		}

		// Token: 0x06000059 RID: 89 RVA: 0x000056BC File Offset: 0x000038BC
		internal static long GetWeldKey(Vector3 p, float cellSize)
		{
			long num = (long)Mathf.Floor(p.x / cellSize) + 1048576L;
			long num2 = (long)Mathf.Floor(p.y / cellSize) + 1048576L;
			long num3 = (long)Mathf.Floor(p.z / cellSize) + 1048576L;
			return ((num & 2097151L) << 42) | ((num2 & 2097151L) << 21) | (num3 & 2097151L);
		}

		// Token: 0x04000049 RID: 73
		public int ParticleCount;

		// Token: 0x0400004A RID: 74
		public int[] VertexToParticle;

		// Token: 0x0400004B RID: 75
		public int[] ParticleToFirstVertex;

		// Token: 0x0400004C RID: 76
		public EdgeConstraint[] Edges;

		// Token: 0x0400004D RID: 77
		public BendConstraint[] Bends;

		// Token: 0x0400004E RID: 78
		public int[] Triangles;

		// Token: 0x0400004F RID: 79
		public int[] TriangleVertices;

		// Token: 0x04000050 RID: 80
		public int[][] Neighbors;

		// Token: 0x04000051 RID: 81
		public int SkippedTriangleCount;

		// Token: 0x04000052 RID: 82
		private int[] _geodesicHeap;

		// Token: 0x04000053 RID: 83
		private int[] _geodesicHeapPositions;

		// Token: 0x04000054 RID: 84
		private int _geodesicHeapCount;
	}
}
