using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AIChara;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using UnityEngine;

namespace StripTease2.Serialization
{
	// Token: 0x02000005 RID: 5
	internal sealed class CharacterDataController : CharaCustomFunctionController
	{
		// Token: 0x06000005 RID: 5 RVA: 0x000021EC File Offset: 0x000003EC
		protected override void OnCardBeingSaved(GameMode currentGameMode)
		{
			try
			{
				if (currentGameMode == 2)
				{
					base.SetExtendedData(null);
				}
				else
				{
					DeformationRegistry.SyncSessionsToRecords();
					DeformationRegistry.Prune();
					byte[] array = CharacterDataController.WriteBlob(base.ChaControl);
					if (array == null)
					{
						base.SetExtendedData(null);
					}
					else
					{
						PluginData pluginData = new PluginData
						{
							version = 1
						};
						pluginData.data["garments"] = array;
						base.SetExtendedData(pluginData);
					}
				}
			}
			catch (Exception ex)
			{
				ManualLogSource source = PluginLog.Source;
				string text = "Failed to save garment deformations to the character: ";
				Exception ex2 = ex;
				source.LogError(text + ((ex2 != null) ? ex2.ToString() : null));
			}
		}

		// Token: 0x06000006 RID: 6 RVA: 0x00002284 File Offset: 0x00000484
		protected override void OnReload(GameMode currentGameMode, bool maintainState)
		{
			if (maintainState)
			{
				return;
			}
			base.StopAllCoroutines();
			if (currentGameMode == 2 && SceneDataController.HasSceneAuthority(base.ChaControl))
			{
				return;
			}
			DeformationRegistry.RemoveCharacter(base.ChaControl);
			int num = CharacterDataController.InvalidatePendingRestore(base.ChaControl);
			try
			{
				PluginData extendedData = base.GetExtendedData();
				if (extendedData != null)
				{
					object obj;
					if (extendedData.data.TryGetValue("garments", out obj))
					{
						byte[] array = obj as byte[];
						if (array != null)
						{
							CharacterDataController.CharacterBlock characterBlock = CharacterDataController.ReadBlob(array);
							if (characterBlock != null)
							{
								if (characterBlock.Records.Count > 0)
								{
									base.StartCoroutine(this.RestoreWhenRenderersAreReady(characterBlock.Records, num));
								}
								if (characterBlock.HasBodyMaskState)
								{
									base.StartCoroutine(this.RestoreBodyMasksWhenReady(characterBlock, num));
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				ManualLogSource source = PluginLog.Source;
				string text = "Failed to load garment deformations from the character: ";
				Exception ex2 = ex;
				source.LogError(text + ((ex2 != null) ? ex2.ToString() : null));
			}
		}

		// Token: 0x06000007 RID: 7 RVA: 0x00002378 File Offset: 0x00000578
		protected override void OnDestroy()
		{
			base.StopAllCoroutines();
			CharacterDataController.InvalidatePendingRestore(base.ChaControl);
			DeformationRegistry.RemoveCharacter(base.ChaControl);
			base.OnDestroy();
		}

		// Token: 0x06000008 RID: 8 RVA: 0x000023A0 File Offset: 0x000005A0
		internal static int InvalidatePendingRestore(ChaControl character)
		{
			if (character == null)
			{
				return 0;
			}
			int num;
			CharacterDataController.RestoreGenerations.TryGetValue(character, out num);
			num++;
			CharacterDataController.RestoreGenerations[character] = num;
			return num;
		}

		// Token: 0x06000009 RID: 9 RVA: 0x000023D8 File Offset: 0x000005D8
		private static bool IsRestoreCurrent(ChaControl character, int generation)
		{
			int num;
			return character != null && CharacterDataController.RestoreGenerations.TryGetValue(character, out num) && num == generation;
		}

		// Token: 0x0600000A RID: 10 RVA: 0x00002404 File Offset: 0x00000604
		private static byte[] WriteBlob(ChaControl character)
		{
			if (character == null)
			{
				return null;
			}
			CharacterDataController.CharacterBlock characterBlock = new CharacterDataController.CharacterBlock();
			Dictionary<string, GarmentEntry> dictionary = DeformationRegistry.ForCharacter(character, false);
			if (dictionary != null)
			{
				foreach (KeyValuePair<string, GarmentEntry> keyValuePair in dictionary)
				{
					GarmentRecord record = keyValuePair.Value.Record;
					if (CharacterDataController.HasPersistentState(record))
					{
						characterBlock.Records.Add(record);
					}
				}
			}
			characterBlock.HasBodyMaskState = DeformationRegistry.TryGetBodyMaskState(character, out characterBlock.TopMaskOn, out characterBlock.BottomMaskOn);
			if (characterBlock.Records.Count == 0 && !characterBlock.HasBodyMaskState)
			{
				return null;
			}
			byte[] array;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
				{
					binaryWriter.Write(1);
					binaryWriter.Write(characterBlock.HasBodyMaskState);
					if (characterBlock.HasBodyMaskState)
					{
						binaryWriter.Write(characterBlock.TopMaskOn);
						binaryWriter.Write(characterBlock.BottomMaskOn);
					}
					binaryWriter.Write(characterBlock.Records.Count);
					for (int i = 0; i < characterBlock.Records.Count; i++)
					{
						characterBlock.Records[i].Write(binaryWriter);
					}
					binaryWriter.Flush();
					array = memoryStream.ToArray();
				}
			}
			return array;
		}

		// Token: 0x0600000B RID: 11 RVA: 0x00002588 File Offset: 0x00000788
		private static CharacterDataController.CharacterBlock ReadBlob(byte[] blob)
		{
			CharacterDataController.CharacterBlock characterBlock;
			using (MemoryStream memoryStream = new MemoryStream(blob))
			{
				using (BinaryReader binaryReader = new BinaryReader(memoryStream))
				{
					int num = binaryReader.ReadInt32();
					if (num != 1)
					{
						PluginLog.Source.LogWarning("Character garment data uses unknown version " + num.ToString() + "; skipping.");
						characterBlock = null;
					}
					else
					{
						CharacterDataController.CharacterBlock characterBlock2 = new CharacterDataController.CharacterBlock();
						characterBlock2.HasBodyMaskState = binaryReader.ReadBoolean();
						if (characterBlock2.HasBodyMaskState)
						{
							characterBlock2.TopMaskOn = binaryReader.ReadBoolean();
							characterBlock2.BottomMaskOn = binaryReader.ReadBoolean();
						}
						int num2 = binaryReader.ReadInt32();
						for (int i = 0; i < num2; i++)
						{
							characterBlock2.Records.Add(GarmentRecord.Read(binaryReader));
						}
						characterBlock = characterBlock2;
					}
				}
			}
			return characterBlock;
		}

		// Token: 0x0600000C RID: 12 RVA: 0x00002668 File Offset: 0x00000868
		private static bool HasPersistentState(GarmentRecord record)
		{
			return record != null && (record.HasAnyDelta() || record.HasAnyPin() || record.HasAnyFreeze() || record.HasAnyTriangleRemoval() || record.HasAnyFakeButton() || record.HasOrderedOverlap() || record.HasAuthorDefinitionState() || record.HasBodyMaskState || record.HasSubdividedTopology() || record.HasCustomWeights());
		}

		// Token: 0x0600000D RID: 13 RVA: 0x000026CA File Offset: 0x000008CA
		private IEnumerator RestoreBodyMasksWhenReady(CharacterDataController.CharacterBlock block, int restoreGeneration)
		{
			CharacterDataController.<RestoreBodyMasksWhenReady>d__15 <RestoreBodyMasksWhenReady>d__ = new CharacterDataController.<RestoreBodyMasksWhenReady>d__15(0);
			<RestoreBodyMasksWhenReady>d__.<>4__this = this;
			<RestoreBodyMasksWhenReady>d__.block = block;
			<RestoreBodyMasksWhenReady>d__.restoreGeneration = restoreGeneration;
			return <RestoreBodyMasksWhenReady>d__;
		}

		// Token: 0x0600000E RID: 14 RVA: 0x000026E7 File Offset: 0x000008E7
		private IEnumerator RestoreWhenRenderersAreReady(List<GarmentRecord> records, int restoreGeneration)
		{
			CharacterDataController.<RestoreWhenRenderersAreReady>d__16 <RestoreWhenRenderersAreReady>d__ = new CharacterDataController.<RestoreWhenRenderersAreReady>d__16(0);
			<RestoreWhenRenderersAreReady>d__.<>4__this = this;
			<RestoreWhenRenderersAreReady>d__.records = records;
			<RestoreWhenRenderersAreReady>d__.restoreGeneration = restoreGeneration;
			return <RestoreWhenRenderersAreReady>d__;
		}

		// Token: 0x04000006 RID: 6
		private const int BlobVersion = 1;

		// Token: 0x04000007 RID: 7
		private const string BlobKey = "garments";

		// Token: 0x04000008 RID: 8
		private const int StableRendererFrames = 2;

		// Token: 0x04000009 RID: 9
		private const int RestoreRetryFrames = 120;

		// Token: 0x0400000A RID: 10
		private static readonly Dictionary<ChaControl, int> RestoreGenerations = new Dictionary<ChaControl, int>();

		// Token: 0x02000015 RID: 21
		private sealed class PendingRestore
		{
			// Token: 0x0400008A RID: 138
			public GarmentRecord Record;

			// Token: 0x0400008B RID: 139
			public SkinnedMeshRenderer LastRenderer;

			// Token: 0x0400008C RID: 140
			public Mesh LastMesh;

			// Token: 0x0400008D RID: 141
			public int StableFrames;
		}

		// Token: 0x02000016 RID: 22
		private sealed class CharacterBlock
		{
			// Token: 0x0400008E RID: 142
			public readonly List<GarmentRecord> Records = new List<GarmentRecord>();

			// Token: 0x0400008F RID: 143
			public bool HasBodyMaskState;

			// Token: 0x04000090 RID: 144
			public bool TopMaskOn;

			// Token: 0x04000091 RID: 145
			public bool BottomMaskOn;
		}
	}
}
