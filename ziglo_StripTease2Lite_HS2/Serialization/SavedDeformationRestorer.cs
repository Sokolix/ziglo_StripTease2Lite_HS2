using System;
using AIChara;
using StripTease2.Cloth;
using UnityEngine;

namespace StripTease2.Serialization
{
	// Token: 0x0200000A RID: 10
	internal static class SavedDeformationRestorer
	{
		// Token: 0x06000038 RID: 56 RVA: 0x00003D28 File Offset: 0x00001F28
		public static void Apply(ChaControl character, SkinnedMeshRenderer renderer, GarmentRecord record)
		{
			try
			{
				GarmentEntry orCreate = DeformationRegistry.GetOrCreate(character, record.Key);
				orCreate.Record = record;
				if (orCreate.Binding == null)
				{
					orCreate.Binding = new GarmentBinding(renderer, record.SubdivisionPasses);
				}
				orCreate.Binding.ApplyAddedBonePalette(record.AddedBonePaths, record.AddedBindposes);
				if (record.WeightOverrides != null)
				{
					orCreate.Binding.ApplyWeightOverrides(record.WeightOverrides);
				}
				else
				{
					orCreate.Binding.ApplyWeightSources(record.WeightSourceIndices);
				}
				if (record.BindDeltas != null && record.BindDeltas.Length == orCreate.Binding.VertexCount)
				{
					Vector3[] array = new Vector3[orCreate.Binding.VertexCount];
					for (int i = 0; i < array.Length; i++)
					{
						array[i] = orCreate.Binding.OriginalWorldPosition(i);
					}
					ClothTopology clothTopology = ClothTopology.Build(array, orCreate.Binding.OriginalMesh.triangles, 0.0001f, false, null, null);
					orCreate.Binding.ApplyBindDeltas(record.BindDeltas, clothTopology);
				}
				if (record.RemovedTriangles != null)
				{
					orCreate.Binding.ApplyRemovedTriangles(record.RemovedTriangles, null);
				}
				if (record.DoubleSided && !orCreate.Binding.DoubleSided)
				{
					string text;
					orCreate.Binding.SetDoubleSided(true, out text);
				}
				orCreate.Binding.CompactForDisplay();
				PluginLog.Source.LogInfo(string.Format("Restored saved deformation for '{0}' (slot {1}, {2:N0} verts).", record.RendererName, record.Slot, record.VertexCount));
			}
			catch (Exception ex)
			{
				PluginLog.Source.LogWarning("Could not apply saved deformation for '" + record.RendererName + "': " + ex.Message);
			}
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00003EE8 File Offset: 0x000020E8
		public static SkinnedMeshRenderer FindRenderer(ChaControl character, GarmentRecord record)
		{
			if (character.objClothes == null || record.Slot < 0 || record.Slot >= character.objClothes.Length || character.objClothes[record.Slot] == null)
			{
				return null;
			}
			foreach (SkinnedMeshRenderer skinnedMeshRenderer in character.objClothes[record.Slot].GetComponentsInChildren<SkinnedMeshRenderer>(true))
			{
				if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null && skinnedMeshRenderer.name == record.RendererName && skinnedMeshRenderer.sharedMesh.vertexCount == record.SourceVertexCount && !GarmentBinding.HasStaleWorkingMesh(skinnedMeshRenderer))
				{
					return skinnedMeshRenderer;
				}
			}
			return null;
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00003F9B File Offset: 0x0000219B
		public static void LogMissingGarment(GarmentRecord record)
		{
			PluginLog.Source.LogWarning(string.Format("Saved deformation for '{0}' (slot {1}, {2:N0} verts) has no matching stable garment; skipping.", record.RendererName, record.Slot, record.VertexCount));
		}
	}
}
