using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content.Items
{
    public class AllSeeingEmblem : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.rare = ItemRarityID.Expert;
            Item.value = Item.buyPrice(platinum: 10);
            Item.expert = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<GodEyeSkyPlayer>().omniscientSkyActive = true;
        }

        public override void UpdateVanity(Player player)
        {
            player.GetModPlayer<GodEyeSkyPlayer>().omniscientSkyActive = true;
        }
    }
}