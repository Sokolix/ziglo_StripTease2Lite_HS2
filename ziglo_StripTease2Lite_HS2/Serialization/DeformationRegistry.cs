using System;
using System.Collections.Generic;
using AIChara;
using UnityEngine;

namespace StripTease2.Serialization
{
	// Token: 0x02000008 RID: 8
	internal static class DeformationRegistry
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000013 RID: 19 RVA: 0x00002728 File Offset: 0x00000928
		public static IEnumerable<KeyValuePair<ChaControl, Dictionary<string, GarmentEntry>>> All
		{
			get
			{
				return DeformationRegistry.Entries;
			}
		}

		// Token: 0x06000014 RID: 20 RVA: 0x00002730 File Offset: 0x00000930
		public static Dictionary<string, GarmentEntry> ForCharacter(ChaControl character, bool createIfMissing)
		{
			Dictionary<string, GarmentEntry> dictionary;
			if (!DeformationRegistry.Entries.TryGetValue(character, out dictionary) && createIfMissing)
			{
				dictionary = new Dictionary<string, GarmentEntry>();
				DeformationRegistry.Entries.Add(character, dictionary);
			}
			return dictionary;
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002764 File Offset: 0x00000964
		public static GarmentEntry GetOrCreate(ChaControl character, string key)
		{
			Dictionary<string, GarmentEntry> dictionary = DeformationRegistry.ForCharacter(character, true);
			GarmentEntry garmentEntry;
			if (!dictionary.TryGetValue(key, out garmentEntry))
			{
				garmentEntry = new GarmentEntry();
				dictionary.Add(key, garmentEntry);
			}
			return garmentEntry;
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002794 File Offset: 0x00000994
		public static void SetBodyMaskState(ChaControl character, bool topMaskOn, bool bottomMaskOn)
		{
			if (character == null)
			{
				return;
			}
			CharacterBodyMaskState characterBodyMaskState;
			if (!DeformationRegistry.BodyMaskStates.TryGetValue(character, out characterBodyMaskState))
			{
				characterBodyMaskState = new CharacterBodyMaskState();
				DeformationRegistry.BodyMaskStates.Add(character, characterBodyMaskState);
			}
			characterBodyMaskState.TopMaskOn = topMaskOn;
			characterBodyMaskState.BottomMaskOn = bottomMaskOn;
		}

		// Token: 0x06000017 RID: 23 RVA: 0x000027DC File Offset: 0x000009DC
		public static bool TryGetBodyMaskState(ChaControl character, out bool topMaskOn, out bool bottomMaskOn)
		{
			CharacterBodyMaskState characterBodyMaskState;
			if (character != null && DeformationRegistry.BodyMaskStates.TryGetValue(character, out characterBodyMaskState))
			{
				topMaskOn = characterBodyMaskState.TopMaskOn;
				bottomMaskOn = characterBodyMaskState.BottomMaskOn;
				return true;
			}
			topMaskOn = true;
			bottomMaskOn = true;
			return false;
		}

		// Token: 0x06000018 RID: 24 RVA: 0x0000281C File Offset: 0x00000A1C
		public static bool TryGetByRenderer(ChaControl character, SkinnedMeshRenderer renderer, out string key, out GarmentEntry entry)
		{
			key = null;
			entry = null;
			Dictionary<string, GarmentEntry> dictionary = DeformationRegistry.ForCharacter(character, false);
			if (dictionary == null || renderer == null)
			{
				return false;
			}
			foreach (KeyValuePair<string, GarmentEntry> keyValuePair in dictionary)
			{
				if (keyValuePair.Value.Binding != null && keyValuePair.Value.Binding.Renderer == renderer)
				{
					key = keyValuePair.Key;
					entry = keyValuePair.Value;
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000019 RID: 25 RVA: 0x000028C0 File Offset: 0x00000AC0
		public static void ReleaseSession(GarmentEntry entry)
		{
		}

		// Token: 0x0600001A RID: 26 RVA: 0x000028C2 File Offset: 0x00000AC2
		public static void SyncSessionsToRecords()
		{
		}

		// Token: 0x0600001B RID: 27 RVA: 0x000028C4 File Offset: 0x00000AC4
		public static void RemoveEntry(ChaControl character, string key)
		{
			Dictionary<string, GarmentEntry> dictionary;
			if (!DeformationRegistry.Entries.TryGetValue(character, out dictionary))
			{
				return;
			}
			GarmentEntry garmentEntry;
			if (!dictionary.TryGetValue(key, out garmentEntry))
			{
				return;
			}
			DeformationRegistry.DisposeEntry(garmentEntry);
			dictionary.Remove(key);
		}

		// Token: 0x0600001C RID: 28 RVA: 0x000028FC File Offset: 0x00000AFC
		public static void RemoveCharacter(ChaControl character)
		{
			Dictionary<string, GarmentEntry> dictionary;
			if (DeformationRegistry.Entries.TryGetValue(character, out dictionary))
			{
				DeformationRegistry.DisposeEntries(dictionary);
				DeformationRegistry.Entries.Remove(character);
			}
			DeformationRegistry.BodyMaskStates.Remove(character);
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00002938 File Offset: 0x00000B38
		public static void Prune()
		{
			List<ChaControl> list = new List<ChaControl>();
			foreach (KeyValuePair<ChaControl, Dictionary<string, GarmentEntry>> keyValuePair in DeformationRegistry.Entries)
			{
				if (keyValuePair.Key == null)
				{
					list.Add(keyValuePair.Key);
				}
				else
				{
					List<string> list2 = new List<string>();
					foreach (KeyValuePair<string, GarmentEntry> keyValuePair2 in keyValuePair.Value)
					{
						GarmentEntry value = keyValuePair2.Value;
						if (value.Binding != null && value.Binding.Renderer == null)
						{
							DeformationRegistry.DisposeEntry(value);
							if (value.Record == null || (!value.Record.HasAnyDelta() && !value.Record.HasAnyPin() && !value.Record.HasAnyFreeze() && !value.Record.HasAnyTriangleRemoval() && !value.Record.HasAnyFakeButton() && !value.Record.HasOrderedOverlap() && !value.Record.HasAuthorDefinitionState() && !value.Record.HasBodyMaskState && !value.Record.HasSubdividedTopology() && !value.Record.HasCustomWeights()))
							{
								list2.Add(keyValuePair2.Key);
							}
						}
					}
					for (int i = 0; i < list2.Count; i++)
					{
						keyValuePair.Value.Remove(list2[i]);
					}
				}
			}
			for (int j = 0; j < list.Count; j++)
			{
				DeformationRegistry.DisposeEntries(DeformationRegistry.Entries[list[j]]);
				DeformationRegistry.BodyMaskStates.Remove(list[j]);
				DeformationRegistry.Entries.Remove(list[j]);
			}
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002B6C File Offset: 0x00000D6C
		public static void Clear()
		{
			foreach (KeyValuePair<ChaControl, Dictionary<string, GarmentEntry>> keyValuePair in DeformationRegistry.Entries)
			{
				DeformationRegistry.DisposeEntries(keyValuePair.Value);
			}
			DeformationRegistry.Entries.Clear();
			DeformationRegistry.BodyMaskStates.Clear();
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00002BD8 File Offset: 0x00000DD8
		private static void DisposeEntries(Dictionary<string, GarmentEntry> map)
		{
			foreach (KeyValuePair<string, GarmentEntry> keyValuePair in map)
			{
				DeformationRegistry.DisposeEntry(keyValuePair.Value);
			}
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002C2C File Offset: 0x00000E2C
		private static void DisposeEntry(GarmentEntry entry)
		{
			if (entry.Binding != null)
			{
				entry.Binding.Dispose();
				entry.Binding = null;
			}
		}

		// Token: 0x0400000F RID: 15
		private static readonly Dictionary<ChaControl, Dictionary<string, GarmentEntry>> Entries = new Dictionary<ChaControl, Dictionary<string, GarmentEntry>>();

		// Token: 0x04000010 RID: 16
		private static readonly Dictionary<ChaControl, CharacterBodyMaskState> BodyMaskStates = new Dictionary<ChaControl, CharacterBodyMaskState>();
	}
}
