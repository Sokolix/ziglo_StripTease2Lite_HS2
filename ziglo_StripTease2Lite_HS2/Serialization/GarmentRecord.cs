using System;
using System.IO;
using UnityEngine;

namespace StripTease2.Serialization
{
	// Token: 0x02000009 RID: 9
	internal sealed class GarmentRecord
	{
		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000022 RID: 34 RVA: 0x00002C5E File Offset: 0x00000E5E
		public bool SubdivideLargeTriangles
		{
			get
			{
				return this.SubdivisionPasses > 0;
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000023 RID: 35 RVA: 0x00002C69 File Offset: 0x00000E69
		public string Key
		{
			get
			{
				return GarmentRecord.MakeKey(this.Slot, this.RendererName, (this.SourceVertexCount > 0) ? this.SourceVertexCount : this.VertexCount);
			}
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002C93 File Offset: 0x00000E93
		public static string MakeKey(int slot, string rendererName, int vertexCount)
		{
			return string.Concat(new string[]
			{
				slot.ToString(),
				"/",
				rendererName,
				"/",
				vertexCount.ToString()
			});
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002CC8 File Offset: 0x00000EC8
		public bool HasAnyDelta()
		{
			if (this.BindDeltas == null)
			{
				return false;
			}
			for (int i = 0; i < this.BindDeltas.Length; i++)
			{
				if (this.BindDeltas[i].sqrMagnitude > 1E-12f)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002D10 File Offset: 0x00000F10
		public bool HasAnyPin()
		{
			if (this.PinWeights == null)
			{
				return false;
			}
			for (int i = 0; i < this.PinWeights.Length; i++)
			{
				if (this.PinWeights[i] > 0)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00002D48 File Offset: 0x00000F48
		public bool HasAnyFreeze()
		{
			if (this.FrozenParticles == null)
			{
				return false;
			}
			for (int i = 0; i < this.FrozenParticles.Length; i++)
			{
				if (this.FrozenParticles[i] != 0)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00002D80 File Offset: 0x00000F80
		public bool HasAnyTriangleRemoval()
		{
			if (this.RemovedTriangles == null)
			{
				return false;
			}
			for (int i = 0; i < this.RemovedTriangles.Length; i++)
			{
				if (this.RemovedTriangles[i] != 0)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00002DB8 File Offset: 0x00000FB8
		public bool HasAnyFakeButton()
		{
			int num = ((this.FakeButtonA != null) ? this.FakeButtonA.Length : 0);
			return num > 0 && this.FakeButtonB != null && this.FakeButtonB.Length == num && this.FakeButtonRestDistances != null && this.FakeButtonRestDistances.Length == num && this.FakeButtonBreakExtensions != null && this.FakeButtonBreakExtensions.Length == num && this.FakeButtonInfluenceRadii != null && this.FakeButtonInfluenceRadii.Length == num;
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00002E2B File Offset: 0x0000102B
		public bool HasAuthorDefinitionState()
		{
			return !string.IsNullOrEmpty(this.AuthorDefinitionId);
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002E3C File Offset: 0x0000103C
		public bool HasOrderedOverlap()
		{
			if (this.OrderedOverlapOuter == null || this.OrderedOverlapInner == null || this.OrderedOverlapOuter.Length != this.VertexCount || this.OrderedOverlapInner.Length != this.VertexCount)
			{
				return false;
			}
			bool flag = false;
			bool flag2 = false;
			int num = 0;
			while (num < this.VertexCount && (!flag || !flag2))
			{
				flag |= this.OrderedOverlapOuter[num] > 0;
				flag2 |= this.OrderedOverlapInner[num] > 0;
				num++;
			}
			return flag && flag2;
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00002EB5 File Offset: 0x000010B5
		public bool HasCustomWeights()
		{
			return (this.WeightOverrides != null && this.WeightOverrides.Length == this.VertexCount) || this.HasWeightSources();
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00002ED7 File Offset: 0x000010D7
		public bool HasSubdividedTopology()
		{
			return this.SubdivideLargeTriangles;
		}

		// Token: 0x0600002E RID: 46 RVA: 0x00002EE0 File Offset: 0x000010E0
		private bool HasWeightSources()
		{
			if (this.WeightSourceIndices == null || this.WeightSourceIndices.Length != this.VertexCount)
			{
				return false;
			}
			for (int i = 0; i < this.WeightSourceIndices.Length; i++)
			{
				if (this.WeightSourceIndices[i] != i)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00002F28 File Offset: 0x00001128
		public void Write(BinaryWriter writer)
		{
			writer.Write(22);
			writer.Write(this.Slot);
			writer.Write(this.RendererName ?? string.Empty);
			writer.Write(this.VertexCount);
			writer.Write(this.Elasticity);
			writer.Write(this.BendStiffness);
			writer.Write(this.Friction);
			writer.Write(this.SkinOffset);
			writer.Write(this.StretchLimit);
			writer.Write(this.ClothThickness);
			writer.Write(this.CompressionAllowance);
			writer.Write(this.DoubleSided);
			writer.Write(this.SelfCollision);
			writer.Write(this.Shrink);
			writer.Write(this.ShrinkMovedOnly);
			bool flag = this.HasWeightSources();
			writer.Write(flag);
			if (flag)
			{
				for (int i = 0; i < this.WeightSourceIndices.Length; i++)
				{
					writer.Write(this.WeightSourceIndices[i]);
				}
			}
			bool flag2 = this.WeightOverrides != null && this.WeightOverrides.Length == this.VertexCount;
			writer.Write(flag2);
			if (flag2)
			{
				for (int j = 0; j < this.WeightOverrides.Length; j++)
				{
					GarmentRecord.WriteBoneWeight(writer, this.WeightOverrides[j]);
				}
			}
			bool flag3 = this.AddedBonePaths != null && this.AddedBindposes != null && this.AddedBonePaths.Length != 0 && this.AddedBonePaths.Length == this.AddedBindposes.Length;
			writer.Write(flag3);
			if (flag3)
			{
				writer.Write(this.AddedBonePaths.Length);
				for (int k = 0; k < this.AddedBonePaths.Length; k++)
				{
					writer.Write(this.AddedBonePaths[k] ?? string.Empty);
					GarmentRecord.WriteMatrix(writer, this.AddedBindposes[k]);
				}
			}
			bool flag4 = this.HasAnyDelta();
			writer.Write(flag4);
			if (flag4)
			{
				for (int l = 0; l < this.BindDeltas.Length; l++)
				{
					writer.Write(GarmentRecord.Quantize(this.BindDeltas[l].x));
					writer.Write(GarmentRecord.Quantize(this.BindDeltas[l].y));
					writer.Write(GarmentRecord.Quantize(this.BindDeltas[l].z));
				}
			}
			bool flag5 = this.HasAnyPin();
			writer.Write(flag5);
			if (flag5)
			{
				writer.Write(this.PinWeights, 0, this.PinWeights.Length);
			}
			bool flag6 = this.HasAnyFreeze();
			writer.Write(flag6);
			if (flag6)
			{
				writer.Write(this.FrozenParticles, 0, this.FrozenParticles.Length);
			}
			bool flag7 = this.HasAnyTriangleRemoval();
			writer.Write(flag7);
			if (flag7)
			{
				writer.Write(this.RemovedTriangles.Length);
				writer.Write(this.RemovedTriangles, 0, this.RemovedTriangles.Length);
			}
			bool flag8 = this.HasAnyFakeButton();
			writer.Write(flag8);
			if (flag8)
			{
				writer.Write(this.FakeButtonA.Length);
				for (int m = 0; m < this.FakeButtonA.Length; m++)
				{
					writer.Write(this.FakeButtonA[m]);
					writer.Write(this.FakeButtonB[m]);
					writer.Write(this.FakeButtonRestDistances[m]);
					writer.Write(this.FakeButtonBreakExtensions[m]);
					writer.Write(this.FakeButtonInfluenceRadii[m]);
				}
			}
			writer.Write(this.AuthorDefinitionId ?? string.Empty);
			writer.Write(this.HasBodyMaskState);
			if (this.HasBodyMaskState)
			{
				writer.Write(this.TopMaskOn);
				writer.Write(this.BottomMaskOn);
			}
			bool flag9 = this.HasOrderedOverlap();
			writer.Write(flag9);
			if (flag9)
			{
				writer.Write(this.OrderedOverlapGap);
				writer.Write(this.OrderedOverlapActiveDistance);
				writer.Write(this.OrderedOverlapOuter, 0, this.OrderedOverlapOuter.Length);
				writer.Write(this.OrderedOverlapInner, 0, this.OrderedOverlapInner.Length);
			}
			writer.Write((this.SourceVertexCount > 0) ? this.SourceVertexCount : this.VertexCount);
			writer.Write(this.SubdivisionPasses);
		}

		// Token: 0x06000030 RID: 48 RVA: 0x0000334C File Offset: 0x0000154C
		public static GarmentRecord Read(BinaryReader reader)
		{
			byte b = reader.ReadByte();
			if (b < 1 || b > 22)
			{
				throw new InvalidDataException("Unknown garment record version " + b.ToString() + ".");
			}
			GarmentRecord garmentRecord = new GarmentRecord
			{
				Slot = reader.ReadInt32(),
				RendererName = reader.ReadString(),
				VertexCount = reader.ReadInt32(),
				Elasticity = reader.ReadSingle(),
				BendStiffness = reader.ReadSingle(),
				Friction = reader.ReadSingle(),
				SkinOffset = reader.ReadSingle()
			};
			if (b >= 2)
			{
				garmentRecord.StretchLimit = reader.ReadSingle();
			}
			if (b >= 3)
			{
				garmentRecord.ClothThickness = reader.ReadSingle();
			}
			if (b >= 4)
			{
				garmentRecord.CompressionAllowance = reader.ReadSingle();
			}
			if (b >= 5)
			{
				garmentRecord.DoubleSided = reader.ReadBoolean();
			}
			if (b >= 6)
			{
				garmentRecord.SelfCollision = reader.ReadBoolean();
			}
			if (b >= 7)
			{
				garmentRecord.Shrink = reader.ReadSingle();
			}
			if (b >= 8)
			{
				garmentRecord.ShrinkMovedOnly = reader.ReadBoolean();
			}
			if (garmentRecord.VertexCount < 0 || garmentRecord.VertexCount > 4000000)
			{
				throw new InvalidDataException("Garment record vertex count is implausible.");
			}
			if (b >= 9 && reader.ReadBoolean())
			{
				garmentRecord.WeightSourceIndices = new int[garmentRecord.VertexCount];
				for (int i = 0; i < garmentRecord.VertexCount; i++)
				{
					int num = reader.ReadInt32();
					if (num < 0 || num >= garmentRecord.VertexCount)
					{
						throw new InvalidDataException("Garment record contains an invalid skin-weight source index.");
					}
					garmentRecord.WeightSourceIndices[i] = num;
				}
			}
			if (b >= 10 && reader.ReadBoolean())
			{
				garmentRecord.WeightOverrides = new BoneWeight[garmentRecord.VertexCount];
				for (int j = 0; j < garmentRecord.VertexCount; j++)
				{
					garmentRecord.WeightOverrides[j] = GarmentRecord.ReadBoneWeight(reader, b >= 20);
				}
			}
			if (b >= 11 && reader.ReadBoolean())
			{
				int num2 = reader.ReadInt32();
				if (num2 < 0 || num2 > 4096)
				{
					throw new InvalidDataException("Garment record added-bone count is implausible.");
				}
				garmentRecord.AddedBonePaths = new string[num2];
				garmentRecord.AddedBindposes = new Matrix4x4[num2];
				for (int k = 0; k < num2; k++)
				{
					garmentRecord.AddedBonePaths[k] = reader.ReadString();
					garmentRecord.AddedBindposes[k] = GarmentRecord.ReadMatrix(reader);
				}
			}
			if (reader.ReadBoolean())
			{
				garmentRecord.BindDeltas = new Vector3[garmentRecord.VertexCount];
				for (int l = 0; l < garmentRecord.VertexCount; l++)
				{
					garmentRecord.BindDeltas[l] = ((b >= 13) ? new Vector3((float)reader.ReadInt32() / 20000f, (float)reader.ReadInt32() / 20000f, (float)reader.ReadInt32() / 20000f) : new Vector3((float)reader.ReadInt16() / 20000f, (float)reader.ReadInt16() / 20000f, (float)reader.ReadInt16() / 20000f));
				}
			}
			if (reader.ReadBoolean())
			{
				garmentRecord.PinWeights = reader.ReadBytes(garmentRecord.VertexCount);
			}
			if (b >= 12 && reader.ReadBoolean())
			{
				garmentRecord.FrozenParticles = reader.ReadBytes(garmentRecord.VertexCount);
			}
			if (b >= 14 && reader.ReadBoolean())
			{
				int num3 = reader.ReadInt32();
				if (num3 < 0 || num3 > 20000000)
				{
					throw new InvalidDataException("Garment record triangle count is implausible.");
				}
				garmentRecord.RemovedTriangles = reader.ReadBytes(num3);
				if (garmentRecord.RemovedTriangles.Length != num3)
				{
					throw new EndOfStreamException("Garment record triangle mask is truncated.");
				}
			}
			if (b >= 15 && reader.ReadBoolean())
			{
				int num4 = reader.ReadInt32();
				if (num4 < 0 || num4 > 128)
				{
					throw new InvalidDataException("Garment record fake-button count is implausible.");
				}
				garmentRecord.FakeButtonA = new int[num4];
				garmentRecord.FakeButtonB = new int[num4];
				garmentRecord.FakeButtonRestDistances = new float[num4];
				garmentRecord.FakeButtonBreakExtensions = new float[num4];
				garmentRecord.FakeButtonInfluenceRadii = new float[num4];
				for (int m = 0; m < num4; m++)
				{
					garmentRecord.FakeButtonA[m] = reader.ReadInt32();
					garmentRecord.FakeButtonB[m] = reader.ReadInt32();
					garmentRecord.FakeButtonRestDistances[m] = reader.ReadSingle();
					garmentRecord.FakeButtonBreakExtensions[m] = reader.ReadSingle();
					garmentRecord.FakeButtonInfluenceRadii[m] = ((b >= 18) ? reader.ReadSingle() : 0.08f);
					if (garmentRecord.FakeButtonA[m] < 0 || garmentRecord.FakeButtonB[m] < 0 || float.IsNaN(garmentRecord.FakeButtonRestDistances[m]) || float.IsInfinity(garmentRecord.FakeButtonRestDistances[m]) || garmentRecord.FakeButtonRestDistances[m] <= 0f || float.IsNaN(garmentRecord.FakeButtonBreakExtensions[m]) || float.IsInfinity(garmentRecord.FakeButtonBreakExtensions[m]) || garmentRecord.FakeButtonBreakExtensions[m] <= 0f || float.IsNaN(garmentRecord.FakeButtonInfluenceRadii[m]) || float.IsInfinity(garmentRecord.FakeButtonInfluenceRadii[m]) || garmentRecord.FakeButtonInfluenceRadii[m] < 0f)
					{
						throw new InvalidDataException("Garment record contains an invalid fake button.");
					}
				}
			}
			if (b >= 16)
			{
				garmentRecord.AuthorDefinitionId = reader.ReadString();
			}
			if (b >= 17 && reader.ReadBoolean())
			{
				garmentRecord.HasBodyMaskState = true;
				garmentRecord.TopMaskOn = reader.ReadBoolean();
				garmentRecord.BottomMaskOn = reader.ReadBoolean();
			}
			if (b >= 19 && reader.ReadBoolean())
			{
				garmentRecord.OrderedOverlapGap = reader.ReadSingle();
				garmentRecord.OrderedOverlapActiveDistance = reader.ReadSingle();
				garmentRecord.OrderedOverlapOuter = reader.ReadBytes(garmentRecord.VertexCount);
				garmentRecord.OrderedOverlapInner = reader.ReadBytes(garmentRecord.VertexCount);
				if (garmentRecord.OrderedOverlapOuter.Length != garmentRecord.VertexCount || garmentRecord.OrderedOverlapInner.Length != garmentRecord.VertexCount || float.IsNaN(garmentRecord.OrderedOverlapGap) || float.IsInfinity(garmentRecord.OrderedOverlapGap) || garmentRecord.OrderedOverlapGap <= 0f || float.IsNaN(garmentRecord.OrderedOverlapActiveDistance) || float.IsInfinity(garmentRecord.OrderedOverlapActiveDistance) || garmentRecord.OrderedOverlapActiveDistance < garmentRecord.OrderedOverlapGap)
				{
					throw new InvalidDataException("Garment record contains an invalid ordered overlap.");
				}
			}
			if (b >= 21)
			{
				garmentRecord.SourceVertexCount = reader.ReadInt32();
				garmentRecord.SubdivisionPasses = ((b >= 22) ? reader.ReadByte() : ((reader.ReadBoolean() > false) ? 1 : 0));
				if (garmentRecord.SourceVertexCount <= 0 || garmentRecord.SourceVertexCount > garmentRecord.VertexCount)
				{
					throw new InvalidDataException("Garment record source vertex count is invalid.");
				}
				if (garmentRecord.SubdivisionPasses > 2)
				{
					throw new InvalidDataException("Garment record subdivision pass count is invalid.");
				}
				if (!garmentRecord.SubdivideLargeTriangles && garmentRecord.SourceVertexCount != garmentRecord.VertexCount)
				{
					throw new InvalidDataException("Garment record vertex counts require subdivision metadata.");
				}
			}
			else
			{
				garmentRecord.SourceVertexCount = garmentRecord.VertexCount;
			}
			return garmentRecord;
		}

		// Token: 0x06000031 RID: 49 RVA: 0x000039F4 File Offset: 0x00001BF4
		private static int Quantize(float value)
		{
			double num = Math.Round((double)(value * 20000f));
			if (num <= -2147483648.0)
			{
				return int.MinValue;
			}
			if (num >= 2147483647.0)
			{
				return int.MaxValue;
			}
			return (int)num;
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00003A38 File Offset: 0x00001C38
		private static void WriteBoneWeight(BinaryWriter writer, BoneWeight weight)
		{
			writer.Write((ushort)Mathf.Clamp(weight.boneIndex0, 0, 65535));
			writer.Write((ushort)Mathf.Clamp(weight.boneIndex1, 0, 65535));
			writer.Write((ushort)Mathf.Clamp(weight.boneIndex2, 0, 65535));
			writer.Write((ushort)Mathf.Clamp(weight.boneIndex3, 0, 65535));
			writer.Write(weight.weight0);
			writer.Write(weight.weight1);
			writer.Write(weight.weight2);
			writer.Write(weight.weight3);
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00003AE0 File Offset: 0x00001CE0
		private static BoneWeight ReadBoneWeight(BinaryReader reader, bool fullPrecision)
		{
			BoneWeight boneWeight = default(BoneWeight);
			boneWeight.boneIndex0 = (int)reader.ReadUInt16();
			boneWeight.boneIndex1 = (int)reader.ReadUInt16();
			boneWeight.boneIndex2 = (int)reader.ReadUInt16();
			boneWeight.boneIndex3 = (int)reader.ReadUInt16();
			BoneWeight boneWeight2 = boneWeight;
			if (fullPrecision)
			{
				boneWeight2.weight0 = reader.ReadSingle();
				boneWeight2.weight1 = reader.ReadSingle();
				boneWeight2.weight2 = reader.ReadSingle();
				boneWeight2.weight3 = reader.ReadSingle();
			}
			else
			{
				boneWeight2.weight0 = (float)reader.ReadByte() / 255f;
				boneWeight2.weight1 = (float)reader.ReadByte() / 255f;
				boneWeight2.weight2 = (float)reader.ReadByte() / 255f;
				boneWeight2.weight3 = (float)reader.ReadByte() / 255f;
			}
			if (!GarmentRecord.IsFiniteNonNegative(boneWeight2.weight0) || !GarmentRecord.IsFiniteNonNegative(boneWeight2.weight1) || !GarmentRecord.IsFiniteNonNegative(boneWeight2.weight2) || !GarmentRecord.IsFiniteNonNegative(boneWeight2.weight3))
			{
				throw new InvalidDataException("Garment record contains an invalid skin weight.");
			}
			float num = boneWeight2.weight0 + boneWeight2.weight1 + boneWeight2.weight2 + boneWeight2.weight3;
			if (!fullPrecision && num > 1E-06f && Mathf.Abs(num - 1f) > 1E-06f)
			{
				boneWeight2.weight0 /= num;
				boneWeight2.weight1 /= num;
				boneWeight2.weight2 /= num;
				boneWeight2.weight3 /= num;
			}
			return boneWeight2;
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00003C72 File Offset: 0x00001E72
		private static bool IsFiniteNonNegative(float value)
		{
			return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
		}

		// Token: 0x06000035 RID: 53 RVA: 0x00003C94 File Offset: 0x00001E94
		private static void WriteMatrix(BinaryWriter writer, Matrix4x4 matrix)
		{
			for (int i = 0; i < 16; i++)
			{
				writer.Write(matrix[i]);
			}
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00003CBC File Offset: 0x00001EBC
		private static Matrix4x4 ReadMatrix(BinaryReader reader)
		{
			Matrix4x4 matrix4x = default(Matrix4x4);
			for (int i = 0; i < 16; i++)
			{
				matrix4x[i] = reader.ReadSingle();
			}
			return matrix4x;
		}

		// Token: 0x04000011 RID: 17
		private const float QuantizationScale = 20000f;

		// Token: 0x04000012 RID: 18
		private const byte FormatVersion = 22;

		// Token: 0x04000013 RID: 19
		public int Slot;

		// Token: 0x04000014 RID: 20
		public string RendererName = string.Empty;

		// Token: 0x04000015 RID: 21
		public int VertexCount;

		// Token: 0x04000016 RID: 22
		public int SourceVertexCount;

		// Token: 0x04000017 RID: 23
		public byte SubdivisionPasses;

		// Token: 0x04000018 RID: 24
		public float Elasticity;

		// Token: 0x04000019 RID: 25
		public float BendStiffness;

		// Token: 0x0400001A RID: 26
		public float Friction;

		// Token: 0x0400001B RID: 27
		public float SkinOffset;

		// Token: 0x0400001C RID: 28
		public float StretchLimit = 0.03f;

		// Token: 0x0400001D RID: 29
		public float ClothThickness = 0.0035f;

		// Token: 0x0400001E RID: 30
		public bool SelfCollision = true;

		// Token: 0x0400001F RID: 31
		public float CompressionAllowance;

		// Token: 0x04000020 RID: 32
		public float Shrink;

		// Token: 0x04000021 RID: 33
		public bool ShrinkMovedOnly;

		// Token: 0x04000022 RID: 34
		public bool DoubleSided;

		// Token: 0x04000023 RID: 35
		public string[] AddedBonePaths;

		// Token: 0x04000024 RID: 36
		public Matrix4x4[] AddedBindposes;

		// Token: 0x04000025 RID: 37
		public int[] WeightSourceIndices;

		// Token: 0x04000026 RID: 38
		public BoneWeight[] WeightOverrides;

		// Token: 0x04000027 RID: 39
		public Vector3[] BindDeltas;

		// Token: 0x04000028 RID: 40
		public byte[] PinWeights;

		// Token: 0x04000029 RID: 41
		public byte[] FrozenParticles;

		// Token: 0x0400002A RID: 42
		public byte[] RemovedTriangles;

		// Token: 0x0400002B RID: 43
		public int[] FakeButtonA;

		// Token: 0x0400002C RID: 44
		public int[] FakeButtonB;

		// Token: 0x0400002D RID: 45
		public float[] FakeButtonRestDistances;

		// Token: 0x0400002E RID: 46
		public float[] FakeButtonBreakExtensions;

		// Token: 0x0400002F RID: 47
		public float[] FakeButtonInfluenceRadii;

		// Token: 0x04000030 RID: 48
		public string AuthorDefinitionId = string.Empty;

		// Token: 0x04000031 RID: 49
		public bool HasBodyMaskState;

		// Token: 0x04000032 RID: 50
		public bool TopMaskOn;

		// Token: 0x04000033 RID: 51
		public bool BottomMaskOn;

		// Token: 0x04000034 RID: 52
		public byte[] OrderedOverlapOuter;

		// Token: 0x04000035 RID: 53
		public byte[] OrderedOverlapInner;

		// Token: 0x04000036 RID: 54
		public float OrderedOverlapGap;

		// Token: 0x04000037 RID: 55
		public float OrderedOverlapActiveDistance;
	}
}
