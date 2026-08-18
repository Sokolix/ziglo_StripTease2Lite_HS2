using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AIChara;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using Studio;
using UnityEngine;

namespace StripTease2.Serialization
{
	// Token: 0x0200000B RID: 11
	internal sealed class SceneDataController : SceneCustomFunctionController
	{
		// Token: 0x0600003B RID: 59 RVA: 0x00003FCD File Offset: 0x000021CD
		internal static bool HasSceneAuthority(ChaControl character)
		{
			return character != null && SceneDataController.SceneAuthority.Contains(character);
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00003FE8 File Offset: 0x000021E8
		protected override void OnSceneSave()
		{
			try
			{
				DeformationRegistry.SyncSessionsToRecords();
				DeformationRegistry.Prune();
				PluginData pluginData = new PluginData
				{
					version = 2
				};
				byte[] array = SceneDataController.WriteBlob();
				if (array != null)
				{
					pluginData.data["garments"] = array;
				}
				base.SetExtendedData(pluginData);
			}
			catch (Exception ex)
			{
				ManualLogSource source = PluginLog.Source;
				string text = "Failed to save garment deformations: ";
				Exception ex2 = ex;
				source.LogError(text + ((ex2 != null) ? ex2.ToString() : null));
			}
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00004064 File Offset: 0x00002264
		protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
		{
			if (operation == 2)
			{
				base.StopAllCoroutines();
				SceneDataController.ClearSceneAuthority();
				DeformationRegistry.Clear();
				return;
			}
			try
			{
				if (operation == null)
				{
					base.StopAllCoroutines();
					SceneDataController.ClearSceneAuthority();
					DeformationRegistry.Clear();
				}
				PluginData extendedData = base.GetExtendedData();
				if (extendedData != null)
				{
					object obj;
					if (extendedData.data.TryGetValue("garments", out obj))
					{
						byte[] array = obj as byte[];
						if (array != null)
						{
							List<SceneDataController.PendingBodyMaskRestore> list2;
							List<SceneDataController.PendingRestore> list = SceneDataController.ReadBlob(array, loadedItems, out list2);
							for (int i = 0; i < list.Count; i++)
							{
								SceneDataController.ClaimSceneAuthority(list[i].Character);
							}
							for (int j = 0; j < list2.Count; j++)
							{
								SceneDataController.ClaimSceneAuthority(list2[j].Character);
							}
							if (list.Count > 0)
							{
								base.StartCoroutine(this.RestoreWhenRenderersAreReady(list));
							}
							if (list2.Count > 0)
							{
								base.StartCoroutine(SceneDataController.RestoreBodyMasksWhenReady(list2));
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				ManualLogSource source = PluginLog.Source;
				string text = "Failed to load garment deformations: ";
				Exception ex2 = ex;
				source.LogError(text + ((ex2 != null) ? ex2.ToString() : null));
			}
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00004194 File Offset: 0x00002394
		protected override void OnObjectDeleted(ObjectCtrlInfo objectCtrlInfo)
		{
			OCIChar ocichar = objectCtrlInfo as OCIChar;
			if (ocichar != null && ocichar.charInfo != null)
			{
				SceneDataController.SceneAuthority.Remove(ocichar.charInfo);
				CharacterDataController.InvalidatePendingRestore(ocichar.charInfo);
				DeformationRegistry.RemoveCharacter(ocichar.charInfo);
			}
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000041E1 File Offset: 0x000023E1
		private static void ClaimSceneAuthority(ChaControl character)
		{
			if (character == null)
			{
				return;
			}
			SceneDataController.SceneAuthority.Add(character);
			CharacterDataController.InvalidatePendingRestore(character);
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00004200 File Offset: 0x00002400
		private static void ClearSceneAuthority()
		{
			foreach (ChaControl chaControl in SceneDataController.SceneAuthority)
			{
				CharacterDataController.InvalidatePendingRestore(chaControl);
			}
			SceneDataController.SceneAuthority.Clear();
		}

		// Token: 0x06000041 RID: 65 RVA: 0x0000425C File Offset: 0x0000245C
		private static byte[] WriteBlob()
		{
			List<SceneDataController.CharacterBlock> list = new List<SceneDataController.CharacterBlock>();
			foreach (KeyValuePair<ChaControl, Dictionary<string, GarmentEntry>> keyValuePair in DeformationRegistry.All)
			{
				if (!(keyValuePair.Key == null))
				{
					int num = SceneDataController.FindSceneKey(keyValuePair.Key);
					if (num >= 0)
					{
						SceneDataController.CharacterBlock characterBlock = new SceneDataController.CharacterBlock
						{
							SceneKey = num
						};
						foreach (KeyValuePair<string, GarmentEntry> keyValuePair2 in keyValuePair.Value)
						{
							GarmentRecord record = keyValuePair2.Value.Record;
							if (record != null && (record.HasAnyDelta() || record.HasAnyPin() || record.HasAnyFreeze() || record.HasAnyTriangleRemoval() || record.HasAnyFakeButton() || record.HasOrderedOverlap() || record.HasAuthorDefinitionState() || record.HasBodyMaskState || record.HasSubdividedTopology() || record.HasCustomWeights()))
							{
								characterBlock.Records.Add(record);
							}
						}
						characterBlock.HasBodyMaskState = DeformationRegistry.TryGetBodyMaskState(keyValuePair.Key, out characterBlock.TopMaskOn, out characterBlock.BottomMaskOn);
						if (characterBlock.Records.Count > 0 || characterBlock.HasBodyMaskState)
						{
							list.Add(characterBlock);
						}
					}
				}
			}
			if (list.Count == 0)
			{
				return null;
			}
			byte[] array;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
				{
					binaryWriter.Write(2);
					binaryWriter.Write(list.Count);
					for (int i = 0; i < list.Count; i++)
					{
						SceneDataController.CharacterBlock characterBlock2 = list[i];
						binaryWriter.Write(characterBlock2.SceneKey);
						binaryWriter.Write(characterBlock2.HasBodyMaskState);
						if (characterBlock2.HasBodyMaskState)
						{
							binaryWriter.Write(characterBlock2.TopMaskOn);
							binaryWriter.Write(characterBlock2.BottomMaskOn);
						}
						binaryWriter.Write(characterBlock2.Records.Count);
						for (int j = 0; j < characterBlock2.Records.Count; j++)
						{
							characterBlock2.Records[j].Write(binaryWriter);
						}
					}
					binaryWriter.Flush();
					array = memoryStream.ToArray();
				}
			}
			return array;
		}

		// Token: 0x06000042 RID: 66 RVA: 0x0000452C File Offset: 0x0000272C
		private static List<SceneDataController.PendingRestore> ReadBlob(byte[] blob, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems, out List<SceneDataController.PendingBodyMaskRestore> pendingMasks)
		{
			List<SceneDataController.PendingRestore> list = new List<SceneDataController.PendingRestore>();
			pendingMasks = new List<SceneDataController.PendingBodyMaskRestore>();
			using (MemoryStream memoryStream = new MemoryStream(blob))
			{
				using (BinaryReader binaryReader = new BinaryReader(memoryStream))
				{
					int num = binaryReader.ReadInt32();
					if (num < 1 || num > 2)
					{
						PluginLog.Source.LogWarning("Garment deformation data uses unknown version " + num.ToString() + "; skipping.");
						return list;
					}
					int num2 = binaryReader.ReadInt32();
					for (int i = 0; i < num2; i++)
					{
						int num3 = binaryReader.ReadInt32();
						bool flag = false;
						bool flag2 = true;
						bool flag3 = true;
						if (num >= 2 && binaryReader.ReadBoolean())
						{
							flag = true;
							flag2 = binaryReader.ReadBoolean();
							flag3 = binaryReader.ReadBoolean();
						}
						int num4 = binaryReader.ReadInt32();
						ObjectCtrlInfo objectCtrlInfo;
						OCIChar ocichar = (loadedItems.TryGetValue(num3, ref objectCtrlInfo) ? (objectCtrlInfo as OCIChar) : null);
						if (flag && ocichar != null && ocichar.charInfo != null)
						{
							pendingMasks.Add(new SceneDataController.PendingBodyMaskRestore
							{
								Character = ocichar.charInfo,
								TopMaskOn = flag2,
								BottomMaskOn = flag3
							});
						}
						for (int j = 0; j < num4; j++)
						{
							GarmentRecord garmentRecord = GarmentRecord.Read(binaryReader);
							if (ocichar != null && ocichar.charInfo != null)
							{
								list.Add(new SceneDataController.PendingRestore
								{
									Character = ocichar.charInfo,
									Record = garmentRecord
								});
							}
						}
					}
				}
			}
			return list;
		}

		// Token: 0x06000043 RID: 67 RVA: 0x000046D4 File Offset: 0x000028D4
		private static IEnumerator RestoreBodyMasksWhenReady(List<SceneDataController.PendingBodyMaskRestore> pending)
		{
			SceneDataController.<RestoreBodyMasksWhenReady>d__16 <RestoreBodyMasksWhenReady>d__ = new SceneDataController.<RestoreBodyMasksWhenReady>d__16(0);
			<RestoreBodyMasksWhenReady>d__.pending = pending;
			return <RestoreBodyMasksWhenReady>d__;
		}

		// Token: 0x06000044 RID: 68 RVA: 0x000046E3 File Offset: 0x000028E3
		private IEnumerator RestoreWhenRenderersAreReady(List<SceneDataController.PendingRestore> pending)
		{
			SceneDataController.<RestoreWhenRenderersAreReady>d__17 <RestoreWhenRenderersAreReady>d__ = new SceneDataController.<RestoreWhenRenderersAreReady>d__17(0);
			<RestoreWhenRenderersAreReady>d__.pending = pending;
			return <RestoreWhenRenderersAreReady>d__;
		}

		// Token: 0x06000045 RID: 69 RVA: 0x000046F4 File Offset: 0x000028F4
		private static int FindSceneKey(ChaControl character)
		{
			global::Studio.Studio instance = Singleton<global::Studio.Studio>.Instance;
			if (instance == null || instance.dicObjectCtrl == null)
			{
				return -1;
			}
			foreach (KeyValuePair<int, ObjectCtrlInfo> keyValuePair in instance.dicObjectCtrl)
			{
				OCIChar ocichar = keyValuePair.Value as OCIChar;
				if (ocichar != null && ocichar.charInfo == character)
				{
					return keyValuePair.Key;
				}
			}
			return -1;
		}

		// Token: 0x04000038 RID: 56
		private const int BlobVersion = 2;

		// Token: 0x04000039 RID: 57
		private const string BlobKey = "garments";

		// Token: 0x0400003A RID: 58
		private const int StableRendererFrames = 2;

		// Token: 0x0400003B RID: 59
		private const int RestoreRetryFrames = 120;

		// Token: 0x0400003C RID: 60
		private static readonly HashSet<ChaControl> SceneAuthority = new HashSet<ChaControl>();

		// Token: 0x02000019 RID: 25
		private sealed class PendingRestore
		{
			// Token: 0x0400009F RID: 159
			public ChaControl Character;

			// Token: 0x040000A0 RID: 160
			public GarmentRecord Record;

			// Token: 0x040000A1 RID: 161
			public SkinnedMeshRenderer LastRenderer;

			// Token: 0x040000A2 RID: 162
			public Mesh LastMesh;

			// Token: 0x040000A3 RID: 163
			public int StableFrames;
		}

		// Token: 0x0200001A RID: 26
		private sealed class PendingBodyMaskRestore
		{
			// Token: 0x040000A4 RID: 164
			public ChaControl Character;

			// Token: 0x040000A5 RID: 165
			public bool TopMaskOn;

			// Token: 0x040000A6 RID: 166
			public bool BottomMaskOn;
		}

		// Token: 0x0200001B RID: 27
		private sealed class CharacterBlock
		{
			// Token: 0x040000A7 RID: 167
			public int SceneKey;

			// Token: 0x040000A8 RID: 168
			public readonly List<GarmentRecord> Records = new List<GarmentRecord>();

			// Token: 0x040000A9 RID: 169
			public bool HasBodyMaskState;

			// Token: 0x040000AA RID: 170
			public bool TopMaskOn;

			// Token: 0x040000AB RID: 171
			public bool BottomMaskOn;
		}
	}
}
