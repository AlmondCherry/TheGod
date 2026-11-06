using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content.NPCs.GodEye.summon
{
    public class GodEyeSummonItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.rare = ItemRarityID.Purple;
            Item.consumable = false;
            Item.maxStack = 1;
        }

        public override bool CanUseItem(Player player)
        {
            return !GodEyeSummonSystem.SummonSequenceActive
                   && !NPC.AnyNPCs(ModContent.NPCType<GodEyeBoss>());
        }

        public override bool? UseItem(Player player)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                SkyManager.Instance.Activate("GodEyeMod:SummonSky");
            }

            SoundEngine.PlaySound(SoundID.Roar, player.Center);
            GodEyeSummonSystem.StartSummonSequence(player);

            return true;
        }
    }
}