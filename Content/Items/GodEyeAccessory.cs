using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content.Items
{
    public class EyeOfTheVoid : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(platinum: 5);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<GodEyeSkyPlayer>().cosmicSkyActive = true;
        }

        public override void UpdateVanity(Player player)
        {
            player.GetModPlayer<GodEyeSkyPlayer>().cosmicSkyActive = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.LunarBar, 15)
                .AddIngredient(ItemID.SoulofSight, 10)
                .AddIngredient(ItemID.FragmentSolar, 5)
                .AddIngredient(ItemID.FragmentNebula, 5)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}