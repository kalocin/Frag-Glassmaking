using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace GlassMaking
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class CastingMoldRecipe
	{
		[JsonProperty(Required = Required.DisallowNull)]
		public CraftingRecipeIngredient Output = default!;
		[JsonProperty(Required = Required.Always)]
		public GlassAmount Recipe = default!;

		[JsonProperty]
		public AssetLocation? Name { get; set; }

		public bool Enabled { get; set; } = true;
		public IRecipeIngredient[] Ingredients => new IRecipeIngredient[] { Recipe };
		public IRecipeOutput? RecipeOutput => Output.ReturnedStack;

		public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
		{
			Dictionary<string, string[]> mappings = new Dictionary<string, string[]>();

			if(!string.IsNullOrEmpty(Recipe.Name))
			{
				int wildcardStartLen = Recipe.Code.Path.IndexOf("*");
				if(wildcardStartLen >= 0)
				{
					List<string> codes = new List<string>();
					int wildcardEndLen = Recipe.Code.Path.Length - wildcardStartLen - 1;
					var mod = world.Api.ModLoader.GetModSystem<GlassMakingMod>();
					foreach(var pair in mod.GetGlassTypes())
					{
						if(WildcardUtil.Match(Recipe.Code, pair.Key))
						{
							string code = pair.Key.Path.Substring(wildcardStartLen);
							string codepart = code.Substring(0, code.Length - wildcardEndLen);
							if(Recipe.AllowedVariants == null || Recipe.AllowedVariants.Contains(codepart))
							{
								codes.Add(codepart);
							}
						}
					}
					mappings[Recipe.Name] = codes.ToArray();
				}
			}

			return mappings;
		}

		public bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
		{
			return Output.Resolve(world, sourceForErrorLogging);
		}

		public CastingMoldRecipe Clone()
		{
			return new CastingMoldRecipe() {
				Output = Output.Clone(),
				Recipe = Recipe.Clone(),
				Name = Name?.Clone()
			};
		}

		[JsonObject]
		public class GlassAmount : IRecipeIngredient
		{
			// "type" is the wildcard name used in GetNameToCodeMapping; the interface requires a settable Name.
			public string? Name { get; set; } = "type";

			[JsonProperty(Required = Required.DisallowNull)]
			public AssetLocation? Code { get; set; } = default!;

			[JsonProperty]
			public string[]? AllowedVariants { get; set; }

			[JsonProperty(Required = Required.Always)]
			public int Amount;

			// IRecipeIngredient / IRecipeIngredientBase stubs — GlassAmount matches glass by code/amount,
			// not via the standard ingredient-matching pipeline; these satisfy the interface only.
			private int _quantity = 1;
			int IRecipeIngredient.Quantity { get => _quantity; set => _quantity = value; }
			private string _id = "";
			string IRecipeIngredient.Id { get => _id; set => _id = value; }
			JsonItemStack? IRecipeIngredient.ReturnedStack { get => null; set { } }
			JsonObject? IRecipeIngredient.RecipeAttributes { get => null; set { } }
			RecipeIngredientConsumeProperties IRecipeIngredient.ConsumeProperties => default;
			void IRecipeIngredient.FillPlaceHolder(string key, string value) { }
			bool IRecipeIngredient.Resolve(IWorldAccessor world, string sourceForErrorLogging) => true;
			// IRecipeIngredientBase stubs
			EnumItemClass IRecipeIngredientBase.Type { get => EnumItemClass.Item; set { } }
			string[]? IRecipeIngredientBase.SkipVariants { get => null; set { } }
			ComplexTagCondition<TagSet> IRecipeIngredientBase.Tags { get => default; set { } }
			EnumRecipeMatchType IRecipeIngredientBase.MatchingType { get => EnumRecipeMatchType.Exact; set { } }
			ItemStack? IRecipeIngredientBase.ResolvedItemStack { get => null; set { } }
			bool IRecipeIngredientBase.SatisfiesAsIngredient(ItemStack inputStack, bool checkStackSize) => false;
			// IByteSerializable
			void IByteSerializable.ToBytes(BinaryWriter writer)
			{
				writer.Write(Code?.ToShortString() ?? "");
				writer.Write(Amount);
				writer.Write(Name ?? "");
				writer.Write(AllowedVariants != null);
				if(AllowedVariants != null)
				{
					writer.Write(AllowedVariants.Length);
					foreach(var v in AllowedVariants) writer.Write(v);
				}
			}
			void IByteSerializable.FromBytes(BinaryReader reader, IWorldAccessor resolver)
			{
				Code = new AssetLocation(reader.ReadString());
				Amount = reader.ReadInt32();
				Name = reader.ReadString();
				if(reader.ReadBoolean())
				{
					AllowedVariants = new string[reader.ReadInt32()];
					for(int i = 0; i < AllowedVariants.Length; i++) AllowedVariants[i] = reader.ReadString();
				}
			}
			object ICloneable.Clone() => Clone();

			public GlassAmount Clone()
			{
				return new GlassAmount() {
					Code = Code?.Clone(),
					Amount = Amount,
					Name = Name,
					AllowedVariants = (string[]?)(AllowedVariants?.Clone())
				};
			}
		}
	}
}