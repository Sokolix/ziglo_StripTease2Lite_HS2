using System;
using System.Collections.Generic;
using UnityEngine;

namespace StripTease2.Geometry
{
	// Token: 0x0200000C RID: 12
	internal sealed class NearestVertexIndex
	{
		// Token: 0x06000048 RID: 72 RVA: 0x0000479C File Offset: 0x0000299C
		public NearestVertexIndex(Vector3[] points)
		{
			if (points == null || points.Length == 0)
			{
				throw new ArgumentException("Nearest-vertex lookup needs at least one point.");
			}
			this._points = points;
			this._order = new int[points.Length];
			this._nodes = new NearestVertexIndex.Node[points.Length];
			this._comparer = new NearestVertexIndex.AxisComparer(points);
			for (int i = 0; i < points.Length; i++)
			{
				this._order[i] = i;
			}
			this._root = this.Build(0, points.Length);
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00004818 File Offset: 0x00002A18
		public int FindNearest(Vector3 query, out float distanceSquared)
		{
			int num = -1;
			distanceSquared = float.MaxValue;
			this.Search(this._root, query, ref num, ref distanceSquared);
			return num;
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00004840 File Offset: 0x00002A40
		private int Build(int start, int count)
		{
			Bounds bounds;
			bounds..ctor(this._points[this._order[start]], Vector3.zero);
			for (int i = start + 1; i < start + count; i++)
			{
				bounds.Encapsulate(this._points[this._order[i]]);
			}
			Vector3 size = bounds.size;
			int num = ((size.x >= size.y && size.x >= size.z) ? 0 : ((size.y >= size.z) ? 1 : 2));
			this._comparer.Axis = num;
			Array.Sort<int>(this._order, start, count, this._comparer);
			int num2 = start + count / 2;
			int nodeCount = this._nodeCount;
			this._nodeCount = nodeCount + 1;
			int num3 = nodeCount;
			NearestVertexIndex.Node node = new NearestVertexIndex.Node
			{
				Point = this._order[num2],
				Left = -1,
				Right = -1,
				Axis = num
			};
			int num4 = num2 - start;
			int num5 = start + count - num2 - 1;
			if (num4 > 0)
			{
				node.Left = this.Build(start, num4);
			}
			if (num5 > 0)
			{
				node.Right = this.Build(num2 + 1, num5);
			}
			this._nodes[num3] = node;
			return num3;
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00004990 File Offset: 0x00002B90
		private void Search(int nodeIndex, Vector3 query, ref int best, ref float bestDistanceSquared)
		{
			if (nodeIndex < 0)
			{
				return;
			}
			NearestVertexIndex.Node node = this._nodes[nodeIndex];
			Vector3 vector = query - this._points[node.Point];
			float sqrMagnitude = vector.sqrMagnitude;
			if (sqrMagnitude < bestDistanceSquared)
			{
				bestDistanceSquared = sqrMagnitude;
				best = node.Point;
			}
			float num = NearestVertexIndex.Coordinate(vector, node.Axis);
			int num2 = ((num <= 0f) ? node.Left : node.Right);
			int num3 = ((num <= 0f) ? node.Right : node.Left);
			this.Search(num2, query, ref best, ref bestDistanceSquared);
			if (num * num < bestDistanceSquared)
			{
				this.Search(num3, query, ref best, ref bestDistanceSquared);
			}
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00004A3A File Offset: 0x00002C3A
		private static float Coordinate(Vector3 value, int axis)
		{
			if (axis == 0)
			{
				return value.x;
			}
			if (axis != 1)
			{
				return value.z;
			}
			return value.y;
		}

		// Token: 0x0400003D RID: 61
		private readonly Vector3[] _points;

		// Token: 0x0400003E RID: 62
		private readonly int[] _order;

		// Token: 0x0400003F RID: 63
		private readonly NearestVertexIndex.Node[] _nodes;

		// Token: 0x04000040 RID: 64
		private readonly NearestVertexIndex.AxisComparer _comparer;

		// Token: 0x04000041 RID: 65
		private int _nodeCount;

		// Token: 0x04000042 RID: 66
		private readonly int _root;

		// Token: 0x0200001E RID: 30
		private struct Node
		{
			// Token: 0x040000B4 RID: 180
			public int Point;

			// Token: 0x040000B5 RID: 181
			public int Left;

			// Token: 0x040000B6 RID: 182
			public int Right;

			// Token: 0x040000B7 RID: 183
			public int Axis;
		}

		// Token: 0x0200001F RID: 31
		private sealed class AxisComparer : IComparer<int>
		{
			// Token: 0x060000DD RID: 221 RVA: 0x00009999 File Offset: 0x00007B99
			public AxisComparer(Vector3[] points)
			{
				this._points = points;
			}

			// Token: 0x060000DE RID: 222 RVA: 0x000099A8 File Offset: 0x00007BA8
			public int Compare(int x, int y)
			{
				int num = NearestVertexIndex.Coordinate(this._points[x], this.Axis).CompareTo(NearestVertexIndex.Coordinate(this._points[y], this.Axis));
				if (num == 0)
				{
					return x.CompareTo(y);
				}
				return num;
			}

			// Token: 0x040000B8 RID: 184
			private readonly Vector3[] _points;

			// Token: 0x040000B9 RID: 185
			public int Axis;
		}
	}
}
