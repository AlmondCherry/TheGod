using Terraria;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;

namespace GodEyeMod.Content.NPCs.GodEye
{
    public class GodEyeLootSaveSystem : ModSystem
    {
        public static bool PendingBagDrop = false;
        public static bool PendingNormalLoot = false;
        public static bool PendingCoins = false;

        private static int _entryTimer = 0;
        private static bool _lootDelivered = false;

        public override void SaveWorldData(TagCompound tag)
        {
            tag["GodEye_PendingBag"] = PendingBagDrop;
            tag["GodEye_PendingNormal"] = PendingNormalLoot;
            tag["GodEye_PendingCoins"] = PendingCoins;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            PendingBagDrop = tag.GetBool("GodEye_PendingBag");
            PendingNormalLoot = tag.GetBool("GodEye_PendingNormal");
            PendingCoins = tag.GetBool("GodEye_PendingCoins");

            _entryTimer = 0;
            _lootDelivered = false;
        }

        public override void PostUpdateWorld()
        {
            if (Main.gameMenu) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            if (!PendingBagDrop && !PendingNormalLoot && !PendingCoins) return;

            if (_lootDelivered) return;

            Player player = Main.LocalPlayer;
            if (player == null || !player.active || player.dead) return;

            _entryTimer++;
            if (_entryTimer < 180) return;

            _lootDelivered = true;
            var source = new EntitySource_WorldEvent();

            if (PendingBagDrop)
            {
                PendingBagDrop = false;

                int bagType = ModContent.ItemType<GodEyeBag>();
                if (bagType > 0)
                {
                    if (!TryGiveToPlayer(player, bagType, 1))
                    {
                        int idx = Item.NewItem(source, player.Center, Vector2.Zero, bagType);
                        if (Main.netMode == NetmodeID.Server && idx >= 0)
                            NetMessage.SendData(MessageID.SyncItem, -1, -1, null, idx);
                    }

                    if (Main.netMode != NetmodeID.Server)
                        Main.NewText("A treasure bag materializes before you...", new Color(180, 0, 220));
                }
            }

            if (PendingNormalLoot)
            {
                PendingNormalLoot = false;

                DropNearPlayer(player, source, ItemID.LunarBar, Main.rand.Next(30, 60));
                DropNearPlayer(player, source, ItemID.FragmentSolar, Main.rand.Next(20, 40));
                DropNearPlayer(player, source, ItemID.FragmentVortex, Main.rand.Next(20, 40));
                DropNearPlayer(player, source, ItemID.FragmentNebula, Main.rand.Next(20, 40));
                DropNearPlayer(player, source, ItemID.FragmentStardust, Main.rand.Next(20, 40));
                DropNearPlayer(player, source, ItemID.SuperHealingPotion, Main.rand.Next(10, 20));

                if (Main.rand.NextBool(3))
                    DropNearPlayer(player, source, ModContent.ItemType<Items.CorruptedLens>());
                if (Main.rand.NextBool(4))
                    DropNearPlayer(player, source, ModContent.ItemType<Items.EyeOfTheVoid>());

                if (Main.netMode != NetmodeID.Server)
                    Main.NewText("The remnants of a god scatter at your feet...", new Color(150, 0, 200));
            }

            if (PendingCoins)
            {
                PendingCoins = false;
                DropNearPlayer(player, source, ItemID.PlatinumCoin, 50);
            }
        }

        private void DropNearPlayer(Player player, IEntitySource source, int type, int stack = 1)
        {
            if (type <= 0) return;

            if (!TryGiveToPlayer(player, type, stack))
            {
                int idx = Item.NewItem(source, player.Center, Vector2.Zero, type, stack);
                if (Main.netMode == NetmodeID.Server && idx >= 0)
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, idx);
            }
        }

        private bool TryGiveToPlayer(Player player, int type, int stack)
        {
            if (player == null || !player.active) return false;

            for (int i = 0; i < 50; i++)
            {
                if (player.inventory[i].type == type &&
                    player.inventory[i].stack < player.inventory[i].maxStack)
                {
                    int canAdd = player.inventory[i].maxStack - player.inventory[i].stack;
                    int toAdd = System.Math.Min(canAdd, stack);
                    player.inventory[i].stack += toAdd;
                    stack -= toAdd;
                    if (stack <= 0) return true;
                }
            }

            for (int i = 0; i < 50; i++)
            {
                if (player.inventory[i].IsAir)
                {
                    player.inventory[i].SetDefaults(type);
                    player.inventory[i].stack = stack;
                    return true;
                }
            }

            return false;
        }

        public static void MarkPendingLoot(bool expertMode)
        {
        }
    }
}