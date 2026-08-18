using System;
using System.Collections.Generic;
using StripTease2.Geometry;
using UnityEngine;

namespace StripTease2.Cloth
{
	// Token: 0x02000010 RID: 16
	internal sealed class GarmentBinding : IDisposable
	{
		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600005B RID: 91 RVA: 0x00005731 File Offset: 0x00003931
		// (set) Token: 0x0600005C RID: 92 RVA: 0x00005739 File Offset: 0x00003939
		public SkinnedMeshRenderer Renderer { get; private set; }

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600005D RID: 93 RVA: 0x00005742 File Offset: 0x00003942
		// (set) Token: 0x0600005E RID: 94 RVA: 0x0000574A File Offset: 0x0000394A
		public Mesh SourceMesh { get; private set; }

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600005F RID: 95 RVA: 0x00005753 File Offset: 0x00003953
		// (set) Token: 0x06000060 RID: 96 RVA: 0x0000575B File Offset: 0x0000395B
		public Mesh OriginalMesh { get; private set; }

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000061 RID: 97 RVA: 0x00005764 File Offset: 0x00003964
		// (set) Token: 0x06000062 RID: 98 RVA: 0x0000576C File Offset: 0x0000396C
		public Mesh WorkingMesh { get; private set; }

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000063 RID: 99 RVA: 0x00005775 File Offset: 0x00003975
		// (set) Token: 0x06000064 RID: 100 RVA: 0x0000577D File Offset: 0x0000397D
		public Vector3[] BindVertices { get; private set; }

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000065 RID: 101 RVA: 0x00005786 File Offset: 0x00003986
		public int VertexCount
		{
			get
			{
				return this._vertexCount;
			}
		}

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000066 RID: 102 RVA: 0x0000578E File Offset: 0x0000398E
		// (set) Token: 0x06000067 RID: 103 RVA: 0x00005796 File Offset: 0x00003996
		public int SourceVertexCount { get; private set; }

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000068 RID: 104 RVA: 0x0000579F File Offset: 0x0000399F
		// (set) Token: 0x06000069 RID: 105 RVA: 0x000057A7 File Offset: 0x000039A7
		public byte SubdivisionPasses { get; private set; }

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x0600006A RID: 106 RVA: 0x000057B0 File Offset: 0x000039B0
		public bool SubdivideLargeTriangles
		{
			get
			{
				return this.SubdivisionPasses > 0;
			}
		}

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x0600006B RID: 107 RVA: 0x000057BB File Offset: 0x000039BB
		// (set) Token: 0x0600006C RID: 108 RVA: 0x000057C3 File Offset: 0x000039C3
		public int SourceTriangleCount { get; private set; }

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x0600006D RID: 109 RVA: 0x000057CC File Offset: 0x000039CC
		// (set) Token: 0x0600006E RID: 110 RVA: 0x000057D4 File Offset: 0x000039D4
		public int SubdividedTriangleCount { get; private set; }

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x0600006F RID: 111 RVA: 0x000057DD File Offset: 0x000039DD
		public BoneWeight[] BoneWeights
		{
			get
			{
				return this._boneWeights;
			}
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x06000070 RID: 112 RVA: 0x000057E5 File Offset: 0x000039E5
		// (set) Token: 0x06000071 RID: 113 RVA: 0x000057ED File Offset: 0x000039ED
		public bool DoubleSided { get; private set; }

		// Token: 0x06000072 RID: 114 RVA: 0x000057F8 File Offset: 0x000039F8
		public GarmentBinding(SkinnedMeshRenderer renderer, byte subdivisionPasses = 0)
		{
			if (renderer == null || renderer.sharedMesh == null)
			{
				throw new ArgumentException("The garment renderer has no mesh.");
			}
			Mesh sharedMesh = renderer.sharedMesh;
			if (!sharedMesh.isReadable)
			{
				throw new ArgumentException("The garment mesh '" + sharedMesh.name + "' is not CPU-readable; this tool cannot deform it.");
			}
			this.Renderer = renderer;
			this.SourceMesh = sharedMesh;
			this.SourceVertexCount = sharedMesh.vertexCount;
			this.SubdivisionPasses = subdivisionPasses;
			if (this.SubdivisionPasses > 0)
			{
				GarmentSubdivisionResult garmentSubdivisionResult = GarmentMeshSubdivider.Build(sharedMesh, (int)this.SubdivisionPasses);
				this.OriginalMesh = garmentSubdivisionResult.Mesh;
				this._ownsOriginalMesh = true;
				this._subdivisionSourceA = garmentSubdivisionResult.SourceA;
				this._subdivisionSourceB = garmentSubdivisionResult.SourceB;
				this.SourceTriangleCount = garmentSubdivisionResult.SourceTriangleCount;
				this.SubdividedTriangleCount = garmentSubdivisionResult.SubdividedTriangleCount;
			}
			else
			{
				this.OriginalMesh = sharedMesh;
				this.SourceTriangleCount = sharedMesh.triangles.Length / 3;
				this.SubdividedTriangleCount = this.SourceTriangleCount;
			}
			Mesh originalMesh = this.OriginalMesh;
			this.BindVertices = originalMesh.vertices;
			this._vertexCount = this.BindVertices.Length;
			this._originalBoneWeights = originalMesh.boneWeights;
			Transform[] bones = renderer.bones;
			Matrix4x4[] bindposes = originalMesh.bindposes;
			if (this.BindVertices == null || this.BindVertices.Length == 0)
			{
				throw new ArgumentException("The garment mesh has no vertices.");
			}
			if (this._originalBoneWeights == null || this._originalBoneWeights.Length != this.BindVertices.Length || bones == null || bones.Length == 0 || bindposes == null || bindposes.Length == 0)
			{
				throw new ArgumentException("The garment mesh has no usable skinning data.");
			}
			this._rendererOriginalBones = (Transform[])bones.Clone();
			this._rendererOriginalLocalBounds = renderer.localBounds;
			int num = Mathf.Max(bones.Length, bindposes.Length);
			this._originalBones = new Transform[num];
			this._originalBindposes = new Matrix4x4[num];
			Array.Copy(bones, this._originalBones, bones.Length);
			Array.Copy(bindposes, this._originalBindposes, bindposes.Length);
			for (int i = bindposes.Length; i < num; i++)
			{
				Transform transform = this._originalBones[i];
				this._originalBindposes[i] = ((transform != null) ? (transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix) : Matrix4x4.identity);
			}
			this._bindposes = (Matrix4x4[])this._originalBindposes.Clone();
			this._boneWeights = (BoneWeight[])this._originalBoneWeights.Clone();
			this._weightSourceIndices = new int[this.BindVertices.Length];
			for (int j = 0; j < this._weightSourceIndices.Length; j++)
			{
				this._weightSourceIndices[j] = j;
			}
			this.WorkingMesh = Object.Instantiate<Mesh>(originalMesh);
			this.WorkingMesh.name = originalMesh.name + " [ST2]";
			this.WorkingMesh.MarkDynamic();
			this.WorkingMesh.bindposes = this._bindposes;
			renderer.sharedMesh = this.WorkingMesh;
			this.AssignRendererBonesIfChanged(this._originalBones);
			if (bones.Length != bindposes.Length && PluginLog.Source != null)
			{
				PluginLog.Source.LogWarning(string.Format("{0}: normalized mismatched skin palette ({1:N0} bones, {2:N0} bind poses) to {3:N0} entries.", new object[] { originalMesh.name, bones.Length, bindposes.Length, num }));
			}
			this._workingVertices = this.WorkingMesh.vertices;
			Vector3[] normals = originalMesh.normals;
			this._hasNormals = normals != null && normals.Length == this.BindVertices.Length;
			this.RefreshSkinning();
		}

		// Token: 0x06000073 RID: 115 RVA: 0x00005B84 File Offset: 0x00003D84
		public int[] ExpandSourceVertexSelection(int[] vertices)
		{
			if (!this.SubdivideLargeTriangles || this._subdivisionSourceA == null || this._subdivisionSourceB == null || vertices == null || vertices.Length == 0)
			{
				return vertices;
			}
			bool[] array = new bool[this.VertexCount];
			foreach (int num in vertices)
			{
				if (num >= 0 && num < array.Length)
				{
					array[num] = true;
				}
			}
			for (int j = this.SourceVertexCount; j < this._subdivisionSourceA.Length; j++)
			{
				if (array[this._subdivisionSourceA[j]] && array[this._subdivisionSourceB[j]])
				{
					array[j] = true;
				}
			}
			List<int> list = new List<int>(vertices.Length * 2);
			for (int k = 0; k < array.Length; k++)
			{
				if (array[k])
				{
					list.Add(k);
				}
			}
			return list.ToArray();
		}

		// Token: 0x06000074 RID: 116 RVA: 0x00005C4C File Offset: 0x00003E4C
		public void ExpandSourceVertexGroups(int[] groups)
		{
			if (!this.SubdivideLargeTriangles || groups == null || groups.Length != this.VertexCount || this._subdivisionSourceA == null || this._subdivisionSourceB == null)
			{
				return;
			}
			for (int i = this.SourceVertexCount; i < groups.Length; i++)
			{
				int num = groups[this._subdivisionSourceA[i]];
				int num2 = groups[this._subdivisionSourceB[i]];
				groups[i] = ((num != 0 && num == num2) ? num : 0);
			}
		}

		// Token: 0x06000075 RID: 117 RVA: 0x00005CB7 File Offset: 0x00003EB7
		public int ToSourceVertex(int vertex)
		{
			if (vertex < 0)
			{
				return -1;
			}
			if (vertex < this.SourceVertexCount)
			{
				return vertex;
			}
			while (vertex >= this.SourceVertexCount)
			{
				if (this._subdivisionSourceA == null || vertex >= this._subdivisionSourceA.Length)
				{
					return -1;
				}
				vertex = this._subdivisionSourceA[vertex];
			}
			return vertex;
		}

		// Token: 0x06000076 RID: 118 RVA: 0x00005CF4 File Offset: 0x00003EF4
		public void PrepareForEditing()
		{
			if (!this._compactedForDisplay)
			{
				return;
			}
			if (this.Renderer == null || this.OriginalMesh == null)
			{
				throw new InvalidOperationException("The garment binding is no longer available.");
			}
			Mesh sharedMesh = this.Renderer.sharedMesh;
			if (!this.IsCompatibleRendererMesh(sharedMesh))
			{
				throw new InvalidOperationException("Another plugin replaced this garment's mesh; re-equip it before editing the saved deformation.");
			}
			if (sharedMesh != this.WorkingMesh && PluginLog.Source != null)
			{
				PluginLog.Source.LogInfo(string.Format("Reacquiring compatible mesh '{0}' before editing the saved deformation.", sharedMesh.name));
			}
			Mesh workingMesh = this.WorkingMesh;
			this.WorkingMesh = Object.Instantiate<Mesh>(this.OriginalMesh);
			this.WorkingMesh.name = this.OriginalMesh.name + " [ST2]";
			this.WorkingMesh.MarkDynamic();
			this._bindposes = (Matrix4x4[])this._originalBindposes.Clone();
			this.WorkingMesh.bindposes = this._bindposes;
			this.Renderer.sharedMesh = this.WorkingMesh;
			this.AssignRendererBonesIfChanged(this._originalBones);
			if (workingMesh != null)
			{
				Object.Destroy(workingMesh);
			}
			this.BindVertices = this.OriginalMesh.vertices;
			this._originalBoneWeights = this.OriginalMesh.boneWeights;
			this._boneWeights = (BoneWeight[])this._originalBoneWeights.Clone();
			this._weightSourceIndices = new int[this._vertexCount];
			for (int i = 0; i < this._weightSourceIndices.Length; i++)
			{
				this._weightSourceIndices[i] = i;
			}
			this._usesDirectWeightOverrides = false;
			this._workingVertices = this.WorkingMesh.vertices;
			this._removedSubmeshTriangles = null;
			this._hasNormals = this.OriginalMesh.normals != null && this.OriginalMesh.normals.Length == this._vertexCount;
			this.DoubleSided = false;
			this._compactedForDisplay = false;
			this.RefreshSkinning();
		}

		// Token: 0x06000077 RID: 119 RVA: 0x00005EDC File Offset: 0x000040DC
		public void CompactForDisplay()
		{
			if (this._compactedForDisplay || this.WorkingMesh == null)
			{
				return;
			}
			this.BindVertices = null;
			this._originalBoneWeights = null;
			this._boneWeights = null;
			this._weightSourceIndices = null;
			this._skin = null;
			this._skinInverse = null;
			this._skinValid = null;
			this._workingVertices = null;
			this._normalScratch = null;
			this._particleNormalScratch = null;
			this._meshVertexScratch = null;
			this._meshNormalScratch = null;
			this._meshBoneWeightScratch = null;
			this._removedSubmeshTriangles = null;
			this._compactedForDisplay = true;
		}

		// Token: 0x06000078 RID: 120 RVA: 0x00005F69 File Offset: 0x00004169
		public static bool HasStaleWorkingMesh(SkinnedMeshRenderer renderer)
		{
			return renderer != null && renderer.sharedMesh != null && renderer.sharedMesh.name.EndsWith(" [ST2]");
		}

		// Token: 0x06000079 RID: 121 RVA: 0x00005F9C File Offset: 0x0000419C
		private bool IsCompatibleRendererMesh(Mesh mesh)
		{
			if (mesh == null)
			{
				return false;
			}
			if (mesh == this.WorkingMesh || mesh == this.OriginalMesh || mesh == this.SourceMesh)
			{
				return true;
			}
			if ((mesh.name ?? string.Empty).EndsWith(" [ST2]"))
			{
				return mesh.vertexCount == this._vertexCount || mesh.vertexCount == this._vertexCount * 2;
			}
			return mesh.vertexCount == this.SourceVertexCount && mesh.subMeshCount == this.SourceMesh.subMeshCount;
		}

		// Token: 0x0600007A RID: 122 RVA: 0x00006040 File Offset: 0x00004240
		public void RefreshSkinning()
		{
			Matrix4x4[] array = this.BuildBoneMatrices();
			int num = array.Length;
			int num2 = this.BindVertices.Length;
			if (this._skin == null)
			{
				this._skin = new Matrix4x4[num2];
				this._skinInverse = new Matrix4x4[num2];
				this._skinValid = new bool[num2];
			}
			for (int i = 0; i < num2; i++)
			{
				BoneWeight boneWeight = this._boneWeights[i];
				Matrix4x4 matrix4x = default(Matrix4x4);
				float num3 = 0f;
				num3 += GarmentBinding.Accumulate(ref matrix4x, array, num, boneWeight.boneIndex0, boneWeight.weight0);
				num3 += GarmentBinding.Accumulate(ref matrix4x, array, num, boneWeight.boneIndex1, boneWeight.weight1);
				num3 += GarmentBinding.Accumulate(ref matrix4x, array, num, boneWeight.boneIndex2, boneWeight.weight2);
				num3 += GarmentBinding.Accumulate(ref matrix4x, array, num, boneWeight.boneIndex3, boneWeight.weight3);
				if (num3 < 1E-06f)
				{
					this._skin[i] = Matrix4x4.identity;
					this._skinInverse[i] = Matrix4x4.identity;
					this._skinValid[i] = false;
				}
				else
				{
					if (Mathf.Abs(num3 - 1f) > 0.0001f)
					{
						GarmentBinding.ScaleMatrix(ref matrix4x, 1f / num3);
					}
					Matrix4x4 inverse = matrix4x.inverse;
					bool flag = !float.IsNaN(inverse.m00) && !float.IsInfinity(inverse.m00);
					this._skin[i] = matrix4x;
					this._skinInverse[i] = (flag ? inverse : Matrix4x4.identity);
					this._skinValid[i] = flag;
				}
			}
		}

		// Token: 0x0600007B RID: 123 RVA: 0x000061E1 File Offset: 0x000043E1
		public bool IsSkinValid(int vertex)
		{
			return this._skinValid[vertex];
		}

		// Token: 0x0600007C RID: 124 RVA: 0x000061EB File Offset: 0x000043EB
		public Vector3 OriginalWorldPosition(int vertex)
		{
			return this._skin[vertex].MultiplyPoint3x4(this.BindVertices[vertex]);
		}

		// Token: 0x0600007D RID: 125 RVA: 0x0000620A File Offset: 0x0000440A
		public Vector3 CurrentWorldPosition(int vertex)
		{
			return this._skin[vertex].MultiplyPoint3x4(this._workingVertices[vertex]);
		}

		// Token: 0x0600007E RID: 126 RVA: 0x0000622C File Offset: 0x0000442C
		public Vector3[] GetAuthoredWorldNormals()
		{
			Vector3[] normals = this.OriginalMesh.normals;
			if (normals == null || normals.Length != this.VertexCount)
			{
				return null;
			}
			Vector3[] array = new Vector3[this.VertexCount];
			for (int i = 0; i < this.VertexCount; i++)
			{
				Vector3 vector = this._skinInverse[i].transpose.MultiplyVector(normals[i]);
				array[i] = ((vector.sqrMagnitude > 1E-16f) ? vector.normalized : Vector3.zero);
			}
			return array;
		}

		// Token: 0x0600007F RID: 127 RVA: 0x000062B8 File Offset: 0x000044B8
		public int RepaintWeights(Vector3[] particleWorldPositions, ClothTopology topology, out float meanDistance, out float maxDistance)
		{
			this.RefreshSkinning();
			Vector3[] array = new Vector3[this.VertexCount];
			this.GetOriginalReferenceWorldPositions(array);
			return this.RepaintWeightsFromReference(particleWorldPositions, topology, array, this._originalBoneWeights, true, out meanDistance, out maxDistance);
		}

		// Token: 0x06000080 RID: 128 RVA: 0x000062F4 File Offset: 0x000044F4
		public int RepaintWeightsFromReference(Vector3[] particleWorldPositions, ClothTopology topology, Vector3[] referenceWorldPositions, BoneWeight[] referenceWeights, bool referenceIsOriginalMesh, out float meanDistance, out float maxDistance)
		{
			if (referenceWorldPositions == null || referenceWeights == null || referenceWorldPositions.Length == 0 || referenceWorldPositions.Length != referenceWeights.Length)
			{
				throw new ArgumentException("The skin-weight reference mesh is invalid.");
			}
			NearestVertexIndex nearestVertexIndex = new NearestVertexIndex(referenceWorldPositions);
			int[] array = new int[topology.ParticleCount];
			double num = 0.0;
			maxDistance = 0f;
			for (int i = 0; i < topology.ParticleCount; i++)
			{
				float num3;
				int num2 = nearestVertexIndex.FindNearest(particleWorldPositions[i], out num3);
				array[i] = num2;
				float num4 = Mathf.Sqrt(num3);
				num += (double)num4;
				if (num4 > maxDistance)
				{
					maxDistance = num4;
				}
			}
			meanDistance = ((topology.ParticleCount > 0) ? ((float)(num / (double)topology.ParticleCount)) : 0f);
			int num5 = 0;
			for (int j = 0; j < topology.ParticleCount; j++)
			{
				int num6 = topology.ParticleToFirstVertex[j];
				BoneWeight boneWeight = this._boneWeights[num6];
				BoneWeight boneWeight2 = referenceWeights[array[j]];
				if (!GarmentBinding.SameBoneWeight(boneWeight, boneWeight2))
				{
					num5++;
				}
			}
			for (int k = 0; k < this.VertexCount; k++)
			{
				int num7 = array[topology.VertexToParticle[k]];
				BoneWeight boneWeight3 = referenceWeights[num7];
				if (referenceIsOriginalMesh && GarmentBinding.SameBoneWeight(this._originalBoneWeights[k], boneWeight3))
				{
					num7 = k;
				}
				this._weightSourceIndices[k] = (referenceIsOriginalMesh ? num7 : (-1));
				this._boneWeights[k] = boneWeight3;
			}
			this._usesDirectWeightOverrides = !referenceIsOriginalMesh;
			if (referenceIsOriginalMesh)
			{
				this.ResetBonePalette();
			}
			this.PushBoneWeightsToMesh();
			this.RefreshSkinning();
			this.WriteBack(particleWorldPositions, topology, true);
			return num5;
		}

		// Token: 0x06000081 RID: 129 RVA: 0x0000648A File Offset: 0x0000468A
		public int[] CaptureWeightSources()
		{
			if (this._usesDirectWeightOverrides || !this.HasCustomWeightSources())
			{
				return null;
			}
			return (int[])this._weightSourceIndices.Clone();
		}

		// Token: 0x06000082 RID: 130 RVA: 0x000064AE File Offset: 0x000046AE
		public BoneWeight[] CaptureWeightOverrides()
		{
			if (!this._usesDirectWeightOverrides)
			{
				return null;
			}
			return (BoneWeight[])this._boneWeights.Clone();
		}

		// Token: 0x06000083 RID: 131 RVA: 0x000064CC File Offset: 0x000046CC
		public string[] CaptureAddedBonePaths()
		{
			int num = this._bindposes.Length - this._originalBindposes.Length;
			if (num <= 0)
			{
				return null;
			}
			string[] array = new string[num];
			Transform rootBone = this.Renderer.rootBone;
			Transform[] bones = this.Renderer.bones;
			for (int i = 0; i < num; i++)
			{
				array[i] = GarmentBinding.GetRelativeBonePath(rootBone, bones[this._originalBones.Length + i]);
			}
			return array;
		}

		// Token: 0x06000084 RID: 132 RVA: 0x00006538 File Offset: 0x00004738
		public Matrix4x4[] CaptureAddedBindposes()
		{
			int num = this._bindposes.Length - this._originalBindposes.Length;
			if (num <= 0)
			{
				return null;
			}
			Matrix4x4[] array = new Matrix4x4[num];
			Array.Copy(this._bindposes, this._originalBindposes.Length, array, 0, num);
			return array;
		}

		// Token: 0x06000085 RID: 133 RVA: 0x0000657C File Offset: 0x0000477C
		public void ApplyExpandedBonePalette(Transform[] bones, Matrix4x4[] bindposes)
		{
			if (bones == null || bindposes == null || bones.Length != bindposes.Length || bones.Length < this._originalBones.Length)
			{
				throw new ArgumentException("The expanded bone palette is invalid.");
			}
			for (int i = 0; i < this._originalBones.Length; i++)
			{
				if (bones[i] != this._originalBones[i] && (bones[i] == null || this._originalBones[i] == null || bones[i].name != this._originalBones[i].name))
				{
					throw new ArgumentException("The expanded bone palette does not match this garment.");
				}
			}
			this.SetBonePalette((Transform[])bones.Clone(), (Matrix4x4[])bindposes.Clone());
		}

		// Token: 0x06000086 RID: 134 RVA: 0x00006634 File Offset: 0x00004834
		public void ApplyAddedBonePalette(string[] paths, Matrix4x4[] bindposes)
		{
			if (paths == null || paths.Length == 0)
			{
				this.ResetBonePalette();
				return;
			}
			if (bindposes == null || paths.Length != bindposes.Length)
			{
				throw new ArgumentException("The saved added-bone palette is invalid.");
			}
			Transform[] array = new Transform[this._originalBones.Length + paths.Length];
			Matrix4x4[] array2 = new Matrix4x4[this._originalBindposes.Length + bindposes.Length];
			Array.Copy(this._originalBones, array, this._originalBones.Length);
			Array.Copy(this._originalBindposes, array2, this._originalBindposes.Length);
			Transform rootBone = this.Renderer.rootBone;
			for (int i = 0; i < paths.Length; i++)
			{
				Transform transform = GarmentBinding.ResolveRelativeBonePath(rootBone, paths[i]);
				if (transform == null)
				{
					throw new ArgumentException("Could not find the saved garment bone '" + paths[i] + "'.");
				}
				array[this._originalBones.Length + i] = transform;
				array2[this._originalBindposes.Length + i] = bindposes[i];
			}
			this.SetBonePalette(array, array2);
		}

		// Token: 0x06000087 RID: 135 RVA: 0x00006728 File Offset: 0x00004928
		public bool HasCustomWeightSources()
		{
			for (int i = 0; i < this._weightSourceIndices.Length; i++)
			{
				if (this._weightSourceIndices[i] != i)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000088 RID: 136 RVA: 0x00006758 File Offset: 0x00004958
		public void ApplyWeightSources(int[] sources)
		{
			if (sources != null && sources.Length != this.VertexCount)
			{
				throw new ArgumentException("The saved skin-weight mapping has the wrong vertex count.");
			}
			for (int i = 0; i < this.VertexCount; i++)
			{
				int num = ((sources != null) ? sources[i] : i);
				if (num < 0 || num >= this.VertexCount)
				{
					throw new ArgumentException("The saved skin-weight mapping contains an invalid vertex index.");
				}
				this._weightSourceIndices[i] = num;
				this._boneWeights[i] = this._originalBoneWeights[num];
			}
			this._usesDirectWeightOverrides = false;
			this.PushBoneWeightsToMesh();
			this.RefreshSkinning();
		}

		// Token: 0x06000089 RID: 137 RVA: 0x000067E8 File Offset: 0x000049E8
		public void ApplyWeightOverrides(BoneWeight[] weights)
		{
			if (weights == null)
			{
				return;
			}
			if (weights.Length != this.VertexCount)
			{
				throw new ArgumentException("The saved skin-weight overrides have the wrong vertex count.");
			}
			int num = this.Renderer.bones.Length;
			for (int i = 0; i < this.VertexCount; i++)
			{
				GarmentBinding.ValidateBoneWeight(weights[i], num);
				this._boneWeights[i] = weights[i];
				this._weightSourceIndices[i] = -1;
			}
			this._usesDirectWeightOverrides = true;
			this.PushBoneWeightsToMesh();
			this.RefreshSkinning();
		}

		// Token: 0x0600008A RID: 138 RVA: 0x0000686A File Offset: 0x00004A6A
		private void ResetBonePalette()
		{
			this.SetBonePalette((Transform[])this._originalBones.Clone(), (Matrix4x4[])this._originalBindposes.Clone());
		}

		// Token: 0x0600008B RID: 139 RVA: 0x00006892 File Offset: 0x00004A92
		private void SetBonePalette(Transform[] bones, Matrix4x4[] bindposes)
		{
			this.AssignRendererBonesIfChanged(bones);
			this._bindposes = bindposes;
			this.WorkingMesh.bindposes = this._bindposes;
		}

		// Token: 0x0600008C RID: 140 RVA: 0x000068B4 File Offset: 0x00004AB4
		private static string GetRelativeBonePath(Transform root, Transform bone)
		{
			if (root == null || bone == null)
			{
				throw new ArgumentException("An added garment bone is missing from the character skeleton.");
			}
			if (bone == root)
			{
				return string.Empty;
			}
			List<string> list = new List<string>();
			Transform transform = bone;
			while (transform != null && transform != root)
			{
				list.Add(transform.name);
				transform = transform.parent;
			}
			if (transform != root)
			{
				throw new ArgumentException("An added garment bone is outside the renderer's skeleton.");
			}
			list.Reverse();
			return string.Join("/", list.ToArray());
		}

		// Token: 0x0600008D RID: 141 RVA: 0x00006946 File Offset: 0x00004B46
		private static Transform ResolveRelativeBonePath(Transform root, string path)
		{
			if (root == null)
			{
				return null;
			}
			if (!string.IsNullOrEmpty(path))
			{
				return root.Find(path);
			}
			return root;
		}

		// Token: 0x0600008E RID: 142 RVA: 0x00006964 File Offset: 0x00004B64
		private void GetOriginalReferenceWorldPositions(Vector3[] destination)
		{
			Matrix4x4[] array = this.BuildBoneMatrices();
			int num = array.Length;
			for (int i = 0; i < this.VertexCount; i++)
			{
				Matrix4x4 matrix4x;
				destination[i] = (GarmentBinding.TryBuildSkinMatrix(this._originalBoneWeights[i], array, num, out matrix4x) ? matrix4x.MultiplyPoint3x4(this.BindVertices[i]) : this.OriginalWorldPosition(i));
			}
		}

		// Token: 0x0600008F RID: 143 RVA: 0x000069C8 File Offset: 0x00004BC8
		public int RemoveTriangles(bool[] removedTriangles, ClothTopology topology)
		{
			if (removedTriangles == null || removedTriangles.Length != topology.Triangles.Length / 3)
			{
				throw new ArgumentException("The removal mask must match the solver triangle count.");
			}
			if (this._removedSubmeshTriangles == null || this._removedSubmeshTriangles.Length != this.OriginalMesh.subMeshCount)
			{
				this._removedSubmeshTriangles = new bool[this.OriginalMesh.subMeshCount][];
			}
			HashSet<GarmentBinding.TriangleVertexKey> hashSet = new HashSet<GarmentBinding.TriangleVertexKey>();
			for (int i = 0; i < removedTriangles.Length; i++)
			{
				if (removedTriangles[i])
				{
					int num = i * 3;
					hashSet.Add(new GarmentBinding.TriangleVertexKey(topology.TriangleVertices[num], topology.TriangleVertices[num + 1], topology.TriangleVertices[num + 2]));
				}
			}
			for (int j = 0; j < this.OriginalMesh.subMeshCount; j++)
			{
				int[] triangles = this.OriginalMesh.GetTriangles(j);
				int num2 = triangles.Length / 3;
				bool[] array = this._removedSubmeshTriangles[j];
				if (array == null || array.Length != num2)
				{
					array = (this._removedSubmeshTriangles[j] = new bool[num2]);
				}
				int num3 = 0;
				while (num3 + 2 < triangles.Length)
				{
					int num4 = num3 / 3;
					if (!array[num4])
					{
						array[num4] = hashSet.Contains(new GarmentBinding.TriangleVertexKey(triangles[num3], triangles[num3 + 1], triangles[num3 + 2]));
					}
					num3 += 3;
				}
			}
			return this.ApplyVisibleTriangles();
		}

		// Token: 0x06000090 RID: 144 RVA: 0x00006B14 File Offset: 0x00004D14
		public byte[] CaptureRemovedTriangles()
		{
			if (this._removedSubmeshTriangles == null)
			{
				return null;
			}
			int num = 0;
			for (int i = 0; i < this.OriginalMesh.subMeshCount; i++)
			{
				num += this.OriginalMesh.GetTriangles(i).Length / 3;
			}
			byte[] array = new byte[num];
			bool flag = false;
			int num2 = 0;
			for (int j = 0; j < this.OriginalMesh.subMeshCount; j++)
			{
				int num3 = this.OriginalMesh.GetTriangles(j).Length / 3;
				bool[] array2 = ((j < this._removedSubmeshTriangles.Length) ? this._removedSubmeshTriangles[j] : null);
				for (int k = 0; k < num3; k++)
				{
					bool flag2 = array2 != null && k < array2.Length && array2[k];
					array[num2++] = ((flag2 > false) ? 1 : 0);
					flag = flag || flag2;
				}
			}
			if (!flag)
			{
				return null;
			}
			return array;
		}

		// Token: 0x06000091 RID: 145 RVA: 0x00006BEC File Offset: 0x00004DEC
		public bool[] ApplyRemovedTriangles(byte[] removedTriangles, ClothTopology topologyOrNull)
		{
			if (removedTriangles == null)
			{
				return null;
			}
			int num = 0;
			for (int i = 0; i < this.OriginalMesh.subMeshCount; i++)
			{
				num += this.OriginalMesh.GetTriangles(i).Length / 3;
			}
			if (removedTriangles.Length != num)
			{
				throw new ArgumentException("The saved triangle-removal mask does not match the garment mesh.");
			}
			this._removedSubmeshTriangles = new bool[this.OriginalMesh.subMeshCount][];
			HashSet<GarmentBinding.TriangleVertexKey> hashSet = ((topologyOrNull != null) ? new HashSet<GarmentBinding.TriangleVertexKey>() : null);
			int num2 = 0;
			for (int j = 0; j < this.OriginalMesh.subMeshCount; j++)
			{
				int[] triangles = this.OriginalMesh.GetTriangles(j);
				int num3 = triangles.Length / 3;
				bool[] array = new bool[num3];
				this._removedSubmeshTriangles[j] = array;
				for (int k = 0; k < num3; k++)
				{
					if (removedTriangles[num2 + k] != 0)
					{
						array[k] = true;
						if (hashSet != null)
						{
							int num4 = k * 3;
							hashSet.Add(new GarmentBinding.TriangleVertexKey(triangles[num4], triangles[num4 + 1], triangles[num4 + 2]));
						}
					}
				}
				num2 += num3;
			}
			this.ApplyVisibleTriangles();
			if (topologyOrNull == null)
			{
				return null;
			}
			bool[] array2 = new bool[topologyOrNull.Triangles.Length / 3];
			for (int l = 0; l < array2.Length; l++)
			{
				int num5 = l * 3;
				array2[l] = hashSet.Contains(new GarmentBinding.TriangleVertexKey(topologyOrNull.TriangleVertices[num5], topologyOrNull.TriangleVertices[num5 + 1], topologyOrNull.TriangleVertices[num5 + 2]));
			}
			return array2;
		}

		// Token: 0x06000092 RID: 146 RVA: 0x00006D5C File Offset: 0x00004F5C
		private int ApplyVisibleTriangles()
		{
			int num = 0;
			int vertexCount = this.VertexCount;
			this.WorkingMesh.subMeshCount = this.OriginalMesh.subMeshCount;
			for (int i = 0; i < this.OriginalMesh.subMeshCount; i++)
			{
				int num2;
				int[] visibleSubmeshTriangles = this.GetVisibleSubmeshTriangles(i, out num2);
				num += num2;
				if (!this.DoubleSided)
				{
					this.WorkingMesh.SetTriangles(visibleSubmeshTriangles, i);
				}
				else
				{
					int[] array = new int[visibleSubmeshTriangles.Length * 2];
					Array.Copy(visibleSubmeshTriangles, array, visibleSubmeshTriangles.Length);
					int num3 = 0;
					while (num3 + 2 < visibleSubmeshTriangles.Length)
					{
						array[visibleSubmeshTriangles.Length + num3] = visibleSubmeshTriangles[num3] + vertexCount;
						array[visibleSubmeshTriangles.Length + num3 + 1] = visibleSubmeshTriangles[num3 + 2] + vertexCount;
						array[visibleSubmeshTriangles.Length + num3 + 2] = visibleSubmeshTriangles[num3 + 1] + vertexCount;
						num3 += 3;
					}
					this.WorkingMesh.SetTriangles(array, i);
				}
			}
			return num;
		}

		// Token: 0x06000093 RID: 147 RVA: 0x00006E44 File Offset: 0x00005044
		private int[] GetVisibleSubmeshTriangles(int submesh, out int removedTriangleCount)
		{
			int[] triangles = this.OriginalMesh.GetTriangles(submesh);
			removedTriangleCount = 0;
			if (this._removedSubmeshTriangles == null || submesh < 0 || submesh >= this._removedSubmeshTriangles.Length || this._removedSubmeshTriangles[submesh] == null)
			{
				return triangles;
			}
			bool[] array = this._removedSubmeshTriangles[submesh];
			List<int> list = new List<int>(triangles.Length);
			int num = 0;
			while (num + 2 < triangles.Length)
			{
				if (array[num / 3])
				{
					removedTriangleCount++;
				}
				else
				{
					list.Add(triangles[num]);
					list.Add(triangles[num + 1]);
					list.Add(triangles[num + 2]);
				}
				num += 3;
			}
			return list.ToArray();
		}

		// Token: 0x06000094 RID: 148 RVA: 0x00006EDC File Offset: 0x000050DC
		public void WriteBack(Vector3[] particleWorldPositions, ClothTopology topology, bool updateNormals)
		{
			for (int i = 0; i < this._workingVertices.Length; i++)
			{
				if (this._skinValid[i])
				{
					Vector3 vector = particleWorldPositions[topology.VertexToParticle[i]];
					this._workingVertices[i] = this._skinInverse[i].MultiplyPoint3x4(vector);
				}
			}
			this.PushVerticesToMesh();
			this.UpdateWorkingBounds();
			if (updateNormals)
			{
				this.UpdateNormals(particleWorldPositions, topology);
			}
		}

		// Token: 0x06000095 RID: 149 RVA: 0x00006F4A File Offset: 0x0000514A
		public void RefreshNormals(Vector3[] particleWorldPositions, ClothTopology topology)
		{
			this.UpdateNormals(particleWorldPositions, topology);
		}

		// Token: 0x06000096 RID: 150 RVA: 0x00006F54 File Offset: 0x00005154
		public void GetBindDeltas(Vector3[] deltas)
		{
			for (int i = 0; i < this._workingVertices.Length; i++)
			{
				deltas[i] = this._workingVertices[i] - this.BindVertices[i];
			}
		}

		// Token: 0x06000097 RID: 151 RVA: 0x00006F98 File Offset: 0x00005198
		public void ApplyBindDeltas(Vector3[] deltas, ClothTopology topologyOrNull)
		{
			for (int i = 0; i < this._workingVertices.Length; i++)
			{
				this._workingVertices[i] = this.BindVertices[i] + deltas[i];
			}
			this.PushVerticesToMesh();
			this.UpdateWorkingBounds();
			if (topologyOrNull != null)
			{
				Vector3[] array = new Vector3[topologyOrNull.ParticleCount];
				for (int j = 0; j < array.Length; j++)
				{
					array[j] = this.CurrentWorldPosition(topologyOrNull.ParticleToFirstVertex[j]);
				}
				this.UpdateNormals(array, topologyOrNull);
			}
		}

		// Token: 0x06000098 RID: 152 RVA: 0x00007024 File Offset: 0x00005224
		public bool HasAnyDelta()
		{
			for (int i = 0; i < this._workingVertices.Length; i++)
			{
				if ((this._workingVertices[i] - this.BindVertices[i]).sqrMagnitude > 1E-12f)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000099 RID: 153 RVA: 0x00007074 File Offset: 0x00005274
		private void UpdateNormals(Vector3[] particleWorldPositions, ClothTopology topology)
		{
			if (!this._hasNormals)
			{
				return;
			}
			if (this._particleNormalScratch == null || this._particleNormalScratch.Length != topology.ParticleCount)
			{
				this._particleNormalScratch = new Vector3[topology.ParticleCount];
			}
			else
			{
				Array.Clear(this._particleNormalScratch, 0, this._particleNormalScratch.Length);
			}
			if (this._normalScratch == null)
			{
				this._normalScratch = new Vector3[this._workingVertices.Length];
			}
			Vector3[] particleNormalScratch = this._particleNormalScratch;
			int[] triangles = topology.Triangles;
			int num = 0;
			while (num + 2 < triangles.Length)
			{
				Vector3 vector = particleWorldPositions[triangles[num]];
				Vector3 vector2 = particleWorldPositions[triangles[num + 1]];
				Vector3 vector3 = particleWorldPositions[triangles[num + 2]];
				Vector3 vector4 = Vector3.Cross(vector2 - vector, vector3 - vector);
				particleNormalScratch[triangles[num]] += vector4;
				particleNormalScratch[triangles[num + 1]] += vector4;
				particleNormalScratch[triangles[num + 2]] += vector4;
				num += 3;
			}
			Vector3[] normalScratch = this._normalScratch;
			for (int i = 0; i < normalScratch.Length; i++)
			{
				Vector3 vector5 = particleNormalScratch[topology.VertexToParticle[i]];
				if (vector5.sqrMagnitude < 1E-16f)
				{
					normalScratch[i] = Vector3.up;
				}
				else
				{
					Vector3 vector6 = this._skin[i].transpose.MultiplyVector(vector5);
					normalScratch[i] = ((vector6.sqrMagnitude > 1E-16f) ? vector6.normalized : Vector3.up);
				}
			}
			this.PushNormalsToMesh(normalScratch);
		}

		// Token: 0x0600009A RID: 154 RVA: 0x00007228 File Offset: 0x00005428
		private void PushVerticesToMesh()
		{
			if (!this.DoubleSided)
			{
				this.WorkingMesh.vertices = this._workingVertices;
				return;
			}
			int num = this._workingVertices.Length;
			for (int i = 0; i < num; i++)
			{
				this._meshVertexScratch[i] = this._workingVertices[i];
				Vector3 vector = ((this._meshNormalScratch != null) ? this._meshNormalScratch[i] : Vector3.zero);
				this._meshVertexScratch[i + num] = ((vector.sqrMagnitude > 1E-12f) ? (this._workingVertices[i] - vector.normalized * 0.0002f) : this._workingVertices[i]);
			}
			this.WorkingMesh.vertices = this._meshVertexScratch;
		}

		// Token: 0x0600009B RID: 155 RVA: 0x000072FC File Offset: 0x000054FC
		private void PushNormalsToMesh(Vector3[] normals)
		{
			if (!this.DoubleSided)
			{
				this.WorkingMesh.normals = normals;
				return;
			}
			int num = normals.Length;
			for (int i = 0; i < num; i++)
			{
				this._meshNormalScratch[i] = normals[i];
				this._meshNormalScratch[i + num] = -normals[i];
			}
			this.WorkingMesh.normals = this._meshNormalScratch;
		}

		// Token: 0x0600009C RID: 156 RVA: 0x0000736C File Offset: 0x0000556C
		private void PushBoneWeightsToMesh()
		{
			if (!this.DoubleSided)
			{
				this.WorkingMesh.boneWeights = this._boneWeights;
				return;
			}
			int vertexCount = this.VertexCount;
			if (this._meshBoneWeightScratch == null || this._meshBoneWeightScratch.Length != vertexCount * 2)
			{
				this._meshBoneWeightScratch = new BoneWeight[vertexCount * 2];
			}
			for (int i = 0; i < vertexCount; i++)
			{
				this._meshBoneWeightScratch[i] = this._boneWeights[i];
				this._meshBoneWeightScratch[i + vertexCount] = this._boneWeights[i];
			}
			this.WorkingMesh.boneWeights = this._meshBoneWeightScratch;
		}

		// Token: 0x0600009D RID: 157 RVA: 0x0000740C File Offset: 0x0000560C
		public bool SetDoubleSided(bool enabled, out string message)
		{
			message = string.Empty;
			if (enabled == this.DoubleSided || this.WorkingMesh == null)
			{
				return true;
			}
			Mesh workingMesh = this.WorkingMesh;
			int vertexCount = this.VertexCount;
			int subMeshCount = this.OriginalMesh.subMeshCount;
			int[][] array = new int[subMeshCount][];
			for (int i = 0; i < subMeshCount; i++)
			{
				int num;
				array[i] = this.GetVisibleSubmeshTriangles(i, out num);
			}
			if (enabled)
			{
				Vector3[] array2 = (this._hasNormals ? workingMesh.normals : null);
				Vector4[] tangents = this.OriginalMesh.tangents;
				Vector2[] uv = this.OriginalMesh.uv;
				Vector2[] uv2 = this.OriginalMesh.uv2;
				Color32[] colors = this.OriginalMesh.colors32;
				this._meshVertexScratch = new Vector3[vertexCount * 2];
				this._meshNormalScratch = (this._hasNormals ? new Vector3[vertexCount * 2] : null);
				BoneWeight[] array3 = new BoneWeight[vertexCount * 2];
				for (int j = 0; j < vertexCount; j++)
				{
					array3[j] = this._boneWeights[j];
					array3[j + vertexCount] = this._boneWeights[j];
				}
				workingMesh.Clear(false);
				if (vertexCount * 2 > 65535)
				{
					workingMesh.indexFormat = 1;
				}
				for (int k = 0; k < vertexCount; k++)
				{
					this._meshVertexScratch[k] = this._workingVertices[k];
					Vector3 vector = ((array2 != null && array2.Length == vertexCount) ? array2[k] : Vector3.zero);
					this._meshVertexScratch[k + vertexCount] = ((vector.sqrMagnitude > 1E-12f) ? (this._workingVertices[k] - vector.normalized * 0.0002f) : this._workingVertices[k]);
				}
				workingMesh.vertices = this._meshVertexScratch;
				if (this._hasNormals && array2 != null && array2.Length == vertexCount)
				{
					for (int l = 0; l < vertexCount; l++)
					{
						this._meshNormalScratch[l] = array2[l];
						this._meshNormalScratch[l + vertexCount] = -array2[l];
					}
					workingMesh.normals = this._meshNormalScratch;
				}
				if (tangents != null && tangents.Length == vertexCount)
				{
					Vector4[] array4 = new Vector4[vertexCount * 2];
					for (int m = 0; m < vertexCount; m++)
					{
						array4[m] = tangents[m];
						Vector4 vector2 = tangents[m];
						vector2.w = -vector2.w;
						array4[m + vertexCount] = vector2;
					}
					workingMesh.tangents = array4;
				}
				if (uv != null && uv.Length == vertexCount)
				{
					workingMesh.uv = GarmentBinding.DoubleArray<Vector2>(uv);
				}
				if (uv2 != null && uv2.Length == vertexCount)
				{
					workingMesh.uv2 = GarmentBinding.DoubleArray<Vector2>(uv2);
				}
				if (colors != null && colors.Length == vertexCount)
				{
					workingMesh.colors32 = GarmentBinding.DoubleArray<Color32>(colors);
				}
				workingMesh.boneWeights = array3;
				workingMesh.bindposes = this._bindposes;
				workingMesh.subMeshCount = subMeshCount;
				for (int n = 0; n < subMeshCount; n++)
				{
					int[] array5 = array[n];
					int[] array6 = new int[array5.Length * 2];
					Array.Copy(array5, array6, array5.Length);
					int num2 = 0;
					while (num2 + 2 < array5.Length)
					{
						array6[array5.Length + num2] = array5[num2] + vertexCount;
						array6[array5.Length + num2 + 1] = array5[num2 + 2] + vertexCount;
						array6[array5.Length + num2 + 2] = array5[num2 + 1] + vertexCount;
						num2 += 3;
					}
					workingMesh.SetTriangles(array6, n);
				}
				GarmentBinding.CopyBlendShapes(this.OriginalMesh, workingMesh, true);
				this.DoubleSided = true;
			}
			else
			{
				Vector3[] array7 = null;
				if (this._hasNormals)
				{
					Vector3[] normals = workingMesh.normals;
					array7 = new Vector3[vertexCount];
					if (normals != null && normals.Length >= vertexCount)
					{
						Array.Copy(normals, array7, vertexCount);
					}
				}
				workingMesh.Clear(false);
				workingMesh.vertices = this._workingVertices;
				if (array7 != null)
				{
					workingMesh.normals = array7;
				}
				Vector4[] tangents2 = this.OriginalMesh.tangents;
				if (tangents2 != null && tangents2.Length == vertexCount)
				{
					workingMesh.tangents = tangents2;
				}
				Vector2[] uv3 = this.OriginalMesh.uv;
				if (uv3 != null && uv3.Length == vertexCount)
				{
					workingMesh.uv = uv3;
				}
				Vector2[] uv4 = this.OriginalMesh.uv2;
				if (uv4 != null && uv4.Length == vertexCount)
				{
					workingMesh.uv2 = uv4;
				}
				Color32[] colors2 = this.OriginalMesh.colors32;
				if (colors2 != null && colors2.Length == vertexCount)
				{
					workingMesh.colors32 = colors2;
				}
				workingMesh.boneWeights = this._boneWeights;
				workingMesh.bindposes = this._bindposes;
				workingMesh.subMeshCount = subMeshCount;
				for (int num3 = 0; num3 < subMeshCount; num3++)
				{
					workingMesh.SetTriangles(array[num3], num3);
				}
				GarmentBinding.CopyBlendShapes(this.OriginalMesh, workingMesh, false);
				this._meshVertexScratch = null;
				this._meshNormalScratch = null;
				this._meshBoneWeightScratch = null;
				this.DoubleSided = false;
			}
			this.UpdateWorkingBounds();
			return true;
		}

		// Token: 0x0600009E RID: 158 RVA: 0x00007904 File Offset: 0x00005B04
		private static void CopyBlendShapes(Mesh source, Mesh destination, bool doubled)
		{
			if (source == null || destination == null || source.blendShapeCount == 0)
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
					Array.Clear(array, 0, vertexCount);
					Array.Clear(array2, 0, vertexCount);
					Array.Clear(array3, 0, vertexCount);
					source.GetBlendShapeFrameVertices(i, j, array, array2, array3);
					if (!doubled)
					{
						destination.AddBlendShapeFrame(blendShapeName, source.GetBlendShapeFrameWeight(i, j), array, array2, array3);
					}
					else
					{
						Vector3[] array4 = new Vector3[vertexCount * 2];
						Vector3[] array5 = new Vector3[vertexCount * 2];
						Vector3[] array6 = new Vector3[vertexCount * 2];
						for (int k = 0; k < vertexCount; k++)
						{
							array4[k] = array[k];
							array4[k + vertexCount] = array[k];
							array5[k] = array2[k];
							array5[k + vertexCount] = -array2[k];
							array6[k] = array3[k];
							array6[k + vertexCount] = array3[k];
						}
						destination.AddBlendShapeFrame(blendShapeName, source.GetBlendShapeFrameWeight(i, j), array4, array5, array6);
					}
				}
			}
		}

		// Token: 0x0600009F RID: 159 RVA: 0x00007A88 File Offset: 0x00005C88
		private void UpdateWorkingBounds()
		{
			if (this.WorkingMesh == null || this.OriginalMesh == null || this._workingVertices == null)
			{
				return;
			}
			Bounds bounds = this.OriginalMesh.bounds;
			bounds.Encapsulate(this._rendererOriginalLocalBounds.min);
			bounds.Encapsulate(this._rendererOriginalLocalBounds.max);
			for (int i = 0; i < this._workingVertices.Length; i++)
			{
				bounds.Encapsulate(this._workingVertices[i]);
			}
			float num = Mathf.Max(bounds.size.magnitude * 0.05f, 0.0001f);
			bounds.Expand(num * 2f);
			this.WorkingMesh.bounds = bounds;
			if (this.Renderer != null && this.Renderer.sharedMesh == this.WorkingMesh)
			{
				this.Renderer.localBounds = bounds;
				this._lastAssignedLocalBounds = bounds;
				this._assignedLocalBounds = true;
			}
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x00007B8C File Offset: 0x00005D8C
		private static T[] DoubleArray<T>(T[] source)
		{
			T[] array = new T[source.Length * 2];
			Array.Copy(source, array, source.Length);
			Array.Copy(source, 0, array, source.Length, source.Length);
			return array;
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x00007BC0 File Offset: 0x00005DC0
		private Matrix4x4[] BuildBoneMatrices()
		{
			Transform[] bones = this.Renderer.bones;
			int num = Mathf.Min(bones.Length, this._bindposes.Length);
			Matrix4x4[] array = new Matrix4x4[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = ((bones[i] != null) ? (bones[i].localToWorldMatrix * this._bindposes[i]) : Matrix4x4.identity);
			}
			return array;
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x00007C30 File Offset: 0x00005E30
		private void AssignRendererBonesIfChanged(Transform[] bones)
		{
			if (this.Renderer == null || bones == null)
			{
				return;
			}
			Transform[] bones2 = this.Renderer.bones;
			if (GarmentBinding.SameTransforms(bones2, bones))
			{
				if (this._lastAssignedBones != null && GarmentBinding.SameTransforms(bones2, this._lastAssignedBones))
				{
					this._lastAssignedBones = (Transform[])bones.Clone();
				}
				return;
			}
			Transform[] array = (Transform[])bones.Clone();
			this.Renderer.bones = array;
			this._lastAssignedBones = array;
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x00007CAC File Offset: 0x00005EAC
		private static bool SameTransforms(Transform[] a, Transform[] b)
		{
			if (a == null || b == null || a.Length != b.Length)
			{
				return false;
			}
			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] != b[i])
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x00007CE8 File Offset: 0x00005EE8
		private static bool TryBuildSkinMatrix(BoneWeight weight, Matrix4x4[] boneMatrices, int boneCount, out Matrix4x4 matrix)
		{
			matrix = default(Matrix4x4);
			float num = 0f;
			num += GarmentBinding.Accumulate(ref matrix, boneMatrices, boneCount, weight.boneIndex0, weight.weight0);
			num += GarmentBinding.Accumulate(ref matrix, boneMatrices, boneCount, weight.boneIndex1, weight.weight1);
			num += GarmentBinding.Accumulate(ref matrix, boneMatrices, boneCount, weight.boneIndex2, weight.weight2);
			num += GarmentBinding.Accumulate(ref matrix, boneMatrices, boneCount, weight.boneIndex3, weight.weight3);
			if (num < 1E-06f)
			{
				return false;
			}
			if (Mathf.Abs(num - 1f) > 0.0001f)
			{
				GarmentBinding.ScaleMatrix(ref matrix, 1f / num);
			}
			return true;
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x00007D94 File Offset: 0x00005F94
		private static bool SameBoneWeight(BoneWeight a, BoneWeight b)
		{
			return a.boneIndex0 == b.boneIndex0 && a.boneIndex1 == b.boneIndex1 && a.boneIndex2 == b.boneIndex2 && a.boneIndex3 == b.boneIndex3 && Mathf.Abs(a.weight0 - b.weight0) < 1E-06f && Mathf.Abs(a.weight1 - b.weight1) < 1E-06f && Mathf.Abs(a.weight2 - b.weight2) < 1E-06f && Mathf.Abs(a.weight3 - b.weight3) < 1E-06f;
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x00007E58 File Offset: 0x00006058
		private static void ValidateBoneWeight(BoneWeight weight, int boneCount)
		{
			if ((weight.weight0 > 0f && (weight.boneIndex0 < 0 || weight.boneIndex0 >= boneCount)) || (weight.weight1 > 0f && (weight.boneIndex1 < 0 || weight.boneIndex1 >= boneCount)) || (weight.weight2 > 0f && (weight.boneIndex2 < 0 || weight.boneIndex2 >= boneCount)) || (weight.weight3 > 0f && (weight.boneIndex3 < 0 || weight.boneIndex3 >= boneCount)))
			{
				throw new ArgumentException("The saved skin weights reference a bone that this garment does not use.");
			}
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x00007EF8 File Offset: 0x000060F8
		public void Dispose()
		{
			if (this.Renderer != null && this.Renderer.sharedMesh == this.WorkingMesh)
			{
				Transform[] bones = this.Renderer.bones;
				if (this._lastAssignedBones != null && GarmentBinding.SameTransforms(bones, this._lastAssignedBones))
				{
					this.Renderer.bones = this._rendererOriginalBones ?? this._originalBones;
				}
				if (this._assignedLocalBounds && GarmentBinding.SameBounds(this.Renderer.localBounds, this._lastAssignedLocalBounds))
				{
					this.Renderer.localBounds = this._rendererOriginalLocalBounds;
				}
				this.Renderer.sharedMesh = this.SourceMesh;
			}
			if (this.WorkingMesh != null)
			{
				Object.Destroy(this.WorkingMesh);
				this.WorkingMesh = null;
			}
			if (this._ownsOriginalMesh && this.OriginalMesh != null)
			{
				Object.Destroy(this.OriginalMesh);
				this.OriginalMesh = null;
			}
			this.SourceMesh = null;
			this.Renderer = null;
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x00008004 File Offset: 0x00006204
		private static bool SameBounds(Bounds a, Bounds b)
		{
			return (a.center - b.center).sqrMagnitude < 1E-12f && (a.size - b.size).sqrMagnitude < 1E-12f;
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x00008058 File Offset: 0x00006258
		private static float Accumulate(ref Matrix4x4 target, Matrix4x4[] boneMatrices, int boneCount, int bone, float weight)
		{
			if (weight <= 0f || bone < 0 || bone >= boneCount)
			{
				return 0f;
			}
			Matrix4x4 matrix4x = boneMatrices[bone];
			target.m00 += matrix4x.m00 * weight;
			target.m01 += matrix4x.m01 * weight;
			target.m02 += matrix4x.m02 * weight;
			target.m03 += matrix4x.m03 * weight;
			target.m10 += matrix4x.m10 * weight;
			target.m11 += matrix4x.m11 * weight;
			target.m12 += matrix4x.m12 * weight;
			target.m13 += matrix4x.m13 * weight;
			target.m20 += matrix4x.m20 * weight;
			target.m21 += matrix4x.m21 * weight;
			target.m22 += matrix4x.m22 * weight;
			target.m23 += matrix4x.m23 * weight;
			target.m30 += matrix4x.m30 * weight;
			target.m31 += matrix4x.m31 * weight;
			target.m32 += matrix4x.m32 * weight;
			target.m33 += matrix4x.m33 * weight;
			return weight;
		}

		// Token: 0x060000AA RID: 170 RVA: 0x000081B8 File Offset: 0x000063B8
		private static void ScaleMatrix(ref Matrix4x4 m, float s)
		{
			m.m00 *= s;
			m.m01 *= s;
			m.m02 *= s;
			m.m03 *= s;
			m.m10 *= s;
			m.m11 *= s;
			m.m12 *= s;
			m.m13 *= s;
			m.m20 *= s;
			m.m21 *= s;
			m.m22 *= s;
			m.m23 *= s;
			m.m30 *= s;
			m.m31 *= s;
			m.m32 *= s;
			m.m33 *= s;
		}

		// Token: 0x04000055 RID: 85
		private const string WorkingMeshMarker = " [ST2]";

		// Token: 0x04000056 RID: 86
		private const float DoubleSidedSurfaceOffset = 0.0002f;

		// Token: 0x04000057 RID: 87
		private const float BoundsPaddingFraction = 0.05f;

		// Token: 0x04000062 RID: 98
		private BoneWeight[] _boneWeights;

		// Token: 0x04000063 RID: 99
		private BoneWeight[] _originalBoneWeights;

		// Token: 0x04000064 RID: 100
		private int[] _weightSourceIndices;

		// Token: 0x04000065 RID: 101
		private bool _usesDirectWeightOverrides;

		// Token: 0x04000066 RID: 102
		private int _vertexCount;

		// Token: 0x04000067 RID: 103
		private bool _compactedForDisplay;

		// Token: 0x04000068 RID: 104
		private Transform[] _originalBones;

		// Token: 0x04000069 RID: 105
		private Transform[] _rendererOriginalBones;

		// Token: 0x0400006A RID: 106
		private Transform[] _lastAssignedBones;

		// Token: 0x0400006B RID: 107
		private Bounds _rendererOriginalLocalBounds;

		// Token: 0x0400006C RID: 108
		private Bounds _lastAssignedLocalBounds;

		// Token: 0x0400006D RID: 109
		private bool _assignedLocalBounds;

		// Token: 0x0400006E RID: 110
		private Matrix4x4[] _originalBindposes;

		// Token: 0x0400006F RID: 111
		private Matrix4x4[] _bindposes;

		// Token: 0x04000070 RID: 112
		private Matrix4x4[] _skin;

		// Token: 0x04000071 RID: 113
		private Matrix4x4[] _skinInverse;

		// Token: 0x04000072 RID: 114
		private bool[] _skinValid;

		// Token: 0x04000073 RID: 115
		private Vector3[] _workingVertices;

		// Token: 0x04000074 RID: 116
		private Vector3[] _normalScratch;

		// Token: 0x04000075 RID: 117
		private Vector3[] _particleNormalScratch;

		// Token: 0x04000076 RID: 118
		private Vector3[] _meshVertexScratch;

		// Token: 0x04000077 RID: 119
		private Vector3[] _meshNormalScratch;

		// Token: 0x04000078 RID: 120
		private BoneWeight[] _meshBoneWeightScratch;

		// Token: 0x04000079 RID: 121
		private bool[][] _removedSubmeshTriangles;

		// Token: 0x0400007A RID: 122
		private bool _hasNormals;

		// Token: 0x0400007B RID: 123
		private bool _ownsOriginalMesh;

		// Token: 0x0400007C RID: 124
		private int[] _subdivisionSourceA;

		// Token: 0x0400007D RID: 125
		private int[] _subdivisionSourceB;

		// Token: 0x02000020 RID: 32
		private struct TriangleVertexKey : IEquatable<GarmentBinding.TriangleVertexKey>
		{
			// Token: 0x060000DF RID: 223 RVA: 0x000099F9 File Offset: 0x00007BF9
			public TriangleVertexKey(int a, int b, int c)
			{
				this.A = a;
				this.B = b;
				this.C = c;
			}

			// Token: 0x060000E0 RID: 224 RVA: 0x00009A10 File Offset: 0x00007C10
			public bool Equals(GarmentBinding.TriangleVertexKey other)
			{
				return this.A == other.A && this.B == other.B && this.C == other.C;
			}

			// Token: 0x060000E1 RID: 225 RVA: 0x00009A3E File Offset: 0x00007C3E
			public override bool Equals(object obj)
			{
				return obj is GarmentBinding.TriangleVertexKey && this.Equals((GarmentBinding.TriangleVertexKey)obj);
			}

			// Token: 0x060000E2 RID: 226 RVA: 0x00009A56 File Offset: 0x00007C56
			public override int GetHashCode()
			{
				return (((this.A * 397) ^ this.B) * 397) ^ this.C;
			}

			// Token: 0x040000BA RID: 186
			public readonly int A;

			// Token: 0x040000BB RID: 187
			public readonly int B;

			// Token: 0x040000BC RID: 188
			public readonly int C;
		}
	}
}
