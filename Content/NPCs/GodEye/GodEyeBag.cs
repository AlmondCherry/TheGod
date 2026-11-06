using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content.NPCs.GodEye
{
    public class GodEyeBag : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.maxStack = 999;
            Item.rare = ItemRarityID.Purple;
            Item.expert = true;
            Item.consumable = true;
        }

        public override bool CanRightClick() => true;

        public override void RightClick(Player player)
        {
            var source = player.GetSource_OpenItem(Type);

            player.QuickSpawnItem(source, ModContent.ItemType<Items.AllSeeingEmblem>());

            player.QuickSpawnItem(source, ItemID.PlatinumCoin, 10);
            player.QuickSpawnItem(source, ItemID.LunarBar, Main.rand.Next(30, 60));
            player.QuickSpawnItem(source, ItemID.FragmentSolar, Main.rand.Next(20, 40));
            player.QuickSpawnItem(source, ItemID.FragmentVortex, Main.rand.Next(20, 40));
            player.QuickSpawnItem(source, ItemID.FragmentNebula, Main.rand.Next(20, 40));
            player.QuickSpawnItem(source, ItemID.FragmentStardust, Main.rand.Next(20, 40));
            player.QuickSpawnItem(source, ItemID.SuperHealingPotion, Main.rand.Next(10, 20));

            if (Main.rand.NextBool(3))
                player.QuickSpawnItem(source, ModContent.ItemType<Items.EyeOfTheVoid>());
            if (Main.rand.NextBool(4))
                player.QuickSpawnItem(source, ModContent.ItemType<Items.CorruptedLens>());

            GodEyeLootSaveSystem.PendingBagDrop = false;
            GodEyeLootSaveSystem.PendingNormalLoot = false;
            GodEyeLootSaveSystem.PendingCoins = false;
        }
    }
}