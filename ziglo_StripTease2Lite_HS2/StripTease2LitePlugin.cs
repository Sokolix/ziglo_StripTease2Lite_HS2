using System;
using BepInEx;
using BepInEx.Bootstrap;
using KKAPI.Chara;
using KKAPI.Studio;
using KKAPI.Studio.SaveLoad;
using StripTease2.Serialization;

namespace StripTease2
{
	// Token: 0x02000003 RID: 3
	[BepInPlugin("com.ziglo.striptease2.lite", "StripTease2 Lite", "1.2.2")]
	[BepInDependency("marco.kkapi", 1)]
	[BepInDependency("com.bepis.bepinex.sideloader", 1)]
	[BepInProcess("HoneySelect2")]
	[BepInProcess("StudioNEOV2")]
	public sealed class StripTease2LitePlugin : BaseUnityPlugin
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		private void Awake()
		{
			PluginLog.Source = base.Logger;
			if (Chainloader.PluginInfos.ContainsKey("com.ziglo.striptease2"))
			{
				base.Logger.LogWarning("StripTease2 Full is installed; StripTease2 Lite will remain disabled.");
				base.enabled = false;
				return;
			}
			CharacterApi.RegisterExtraBehaviour<CharacterDataController>("com.ziglo.striptease2");
			if (StudioAPI.InsideStudio)
			{
				StudioSaveLoadApi.RegisterExtraBehaviour<SceneDataController>("com.ziglo.striptease2");
			}
			base.Logger.LogInfo("StripTease2 Lite 1.2.2 loaded (viewer only).");
		}

		// Token: 0x04000002 RID: 2
		public const string PluginGuid = "com.ziglo.striptease2.lite";

		// Token: 0x04000003 RID: 3
		public const string PluginName = "StripTease2 Lite";

		// Token: 0x04000004 RID: 4
		public const string PluginVersion = "1.2.2";

		// Token: 0x04000005 RID: 5
		public const string DataGuid = "com.ziglo.striptease2";
	}
}
