using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content.Items
{
    public class CorruptedLens : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.buyPrice(gold: 50);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<GodEyeSkyPlayer>().voidSkyActive = true;
        }

        public override void UpdateVanity(Player player)
        {
            player.GetModPlayer<GodEyeSkyPlayer>().voidSkyActive = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Lens, 5)
                .AddIngredient(ItemID.SoulofNight, 10)
                .AddIngredient(ItemID.CursedFlame, 15)
                .AddTile(TileID.DemonAltar)
                .Register();
        }
    }
}