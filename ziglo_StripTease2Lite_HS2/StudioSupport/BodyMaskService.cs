using System;
using AIChara;
using UnityEngine;

namespace StripTease2.StudioSupport
{
	// Token: 0x02000004 RID: 4
	internal static class BodyMaskService
	{
		// Token: 0x06000003 RID: 3 RVA: 0x000020C4 File Offset: 0x000002C4
		public static bool Apply(ChaControl character, bool topEnabled, bool bottomEnabled, out string message)
		{
			if (character == null)
			{
				message = "No character loaded.";
				return false;
			}
			bool flag;
			try
			{
				byte b = ((topEnabled > false) ? 1 : 0);
				byte b2 = ((bottomEnabled > false) ? 1 : 0);
				character.ChangeAlphaMask(new byte[] { b, b2 });
				BodyMaskService.SetHs2BraMask(character, topEnabled);
				message = string.Format("Body masks set (top {0}, bottom {1}).", topEnabled ? "ON" : "OFF", bottomEnabled ? "ON" : "OFF");
				flag = true;
			}
			catch (Exception ex)
			{
				message = "Could not change the body mask: " + ex.Message;
				flag = false;
			}
			return flag;
		}

		// Token: 0x06000004 RID: 4 RVA: 0x00002164 File Offset: 0x00000364
		private static void SetHs2BraMask(ChaControl character, bool enabled)
		{
			Renderer[] rendBra = character.rendBra;
			if (rendBra == null)
			{
				return;
			}
			foreach (Renderer renderer in rendBra)
			{
				if (!(renderer == null))
				{
					Material material = renderer.material;
					if (!(material == null))
					{
						float num = (enabled ? 1f : 0f);
						if (material.HasProperty(ChaShader.alpha_a))
						{
							material.SetFloat(ChaShader.alpha_a, num);
						}
						if (material.HasProperty(ChaShader.alpha_b))
						{
							material.SetFloat(ChaShader.alpha_b, num);
						}
					}
				}
			}
		}
	}
}
