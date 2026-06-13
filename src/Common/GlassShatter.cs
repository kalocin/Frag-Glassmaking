using System;
using Vintagestory.API.Common;

namespace GlassMaking.Common
{
	/// <summary>
	/// Shared logic for the "unannealed glass shatters when it cools" mechanic.
	///
	/// Any collectible carrying a <c>glassmaking:anneal</c> attribute is considered
	/// raw/un-annealed glass. If such an item cools below <see cref="ShatterTemperature"/>
	/// it shatters into glass shards instead of becoming a usable item. Annealed outputs
	/// have no anneal attribute and so are never affected.
	///
	/// Item temperature in VS is computed lazily on read (see Collectible.GetTemperature),
	/// so callers only need a slot-aware, server-side context to drive the check; no active
	/// per-item cooldown tracking is required here.
	/// </summary>
	public static class GlassShatter
	{
		/// <summary>Below this temperature (°C) raw glass shatters.</summary>
		public const float ShatterTemperature = 300f;

		private const string AnnealKey = "glassmaking:anneal";
		private static readonly AssetLocation ShardsCode = new AssetLocation("glassmaking", "glassshards");

		/// <summary>True if the collectible is raw, un-annealed glass (has an anneal attribute).</summary>
		public static bool IsRawGlass(CollectibleObject? collectible)
		{
			return collectible?.Attributes != null && collectible.Attributes[AnnealKey].Exists;
		}

		/// <summary>True if the stack is raw glass that has cooled below the shatter threshold.</summary>
		public static bool ShouldShatter(IWorldAccessor world, ItemStack? stack)
		{
			return stack != null
				&& IsRawGlass(stack.Collectible)
				&& stack.Collectible.GetTemperature(world, stack) < ShatterTemperature;
		}

		/// <summary>Number of shards produced per raw item; configurable via the anneal attribute.</summary>
		public static int GetShardCount(ItemStack stack)
		{
			var anneal = stack.Collectible.Attributes?[AnnealKey];
			int perItem = anneal != null ? anneal["shatterShards"].AsInt(4) : 4;
			return Math.Max(1, perItem) * Math.Max(1, stack.StackSize);
		}

		/// <summary>Builds a stack of glass shards, or null if the shards item is missing.</summary>
		public static ItemStack? CreateShards(IWorldAccessor world, int count)
		{
			var item = world.GetItem(ShardsCode);
			return item == null ? null : new ItemStack(item, count);
		}

		/// <summary>
		/// Returns the glass code and amount stored in the anneal attribute (via glassCode/glassAmount
		/// fields), or (null, 0) when not specified. Items that carry these fields produce properly-typed
		/// shards proportional to the glass used; items without them fall back to untyped shards.
		/// </summary>
		public static (AssetLocation? code, int amount) GetGlassInfo(CollectibleObject? collectible)
		{
			var anneal = collectible?.Attributes?[AnnealKey];
			if(anneal == null) return (null, 0);
			string? codeStr = anneal["glassCode"].AsString(null);
			int amount = anneal["glassAmount"].AsInt(0);
			return (codeStr != null ? new AssetLocation(codeStr) : null, amount);
		}

		/// <summary>
		/// Returns properly-typed shard stacks for the given raw glass stack. When the anneal attribute
		/// carries glassCode/glassAmount, uses GetShardsList to produce typed shards that can be
		/// re-melted; otherwise falls back to untyped glassshards.
		/// </summary>
		public static IEnumerable<ItemStack> CreateShardStacks(IWorldAccessor world, ItemStack stack)
		{
			var (glassCode, glassAmount) = GetGlassInfo(stack.Collectible);
			if(glassCode != null && glassAmount > 0)
			{
				var mod = world.Api.ModLoader.GetModSystem<GlassMakingMod>();
				if(mod != null)
				{
					int totalAmount = glassAmount * Math.Max(1, stack.StackSize);
					return mod.GetShardsList(world, glassCode, totalAmount, limitStackSize: true);
				}
			}
			int count = GetShardCount(stack);
			var shards = CreateShards(world, count);
			return shards != null ? (IEnumerable<ItemStack>)new[] { shards } : Array.Empty<ItemStack>();
		}
	}
}
