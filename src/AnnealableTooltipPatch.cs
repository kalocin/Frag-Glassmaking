using System.Text;
using GlassMaking.Common;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace GlassMaking
{
	/// <summary>
	/// Appends a warning to the held-item tooltip of any raw (un-annealed) glass: it must be
	/// annealed or it will shatter. Keys off the glassmaking:anneal attribute, so it covers all
	/// annealable glass regardless of source mod.
	///
	/// Block and Item each fully reimplement GetHeldItemInfo (neither calls the CollectibleObject
	/// base), so both are patched. Class-level [HarmonyPatch] targets match the mod's existing
	/// patch convention and are reliably picked up by PatchAll.
	/// </summary>
	internal static class AnnealTooltip
	{
		public static void Append(ItemSlot inSlot, StringBuilder dsc)
		{
			if(GlassShatter.IsRawGlass(inSlot?.Itemstack?.Collectible))
			{
				dsc.AppendLine(Lang.Get("glassmaking:Needs annealing or will shatter"));
			}
		}
	}

	[HarmonyPatch(typeof(Block), nameof(Block.GetHeldItemInfo))]
	internal static class BlockAnnealTooltipPatch
	{
		[HarmonyPostfix]
		private static void Postfix(ItemSlot inSlot, StringBuilder dsc) => AnnealTooltip.Append(inSlot, dsc);
	}

	[HarmonyPatch(typeof(Item), nameof(Item.GetHeldItemInfo))]
	internal static class ItemAnnealTooltipPatch
	{
		[HarmonyPostfix]
		private static void Postfix(ItemSlot inSlot, StringBuilder dsc) => AnnealTooltip.Append(inSlot, dsc);
	}
}
