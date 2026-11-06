using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace GodEyeMod.Content.Items
{
    public class GodEyeSkyPlayer : ModPlayer
    {
        public bool voidSkyActive;
        public bool cosmicSkyActive;
        public bool omniscientSkyActive;

        public override void ResetEffects()
        {
            voidSkyActive = false;
            cosmicSkyActive = false;
            omniscientSkyActive = false;
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer) return;
            if (BossIsAlive()) return;

            bool anyActive = voidSkyActive || cosmicSkyActive || omniscientSkyActive;

            if (anyActive)
            {
                GodEyeSkyAccessoryMode mode = GodEyeSkyAccessoryMode.None;

                if (omniscientSkyActive)
                    mode = GodEyeSkyAccessoryMode.Omniscient;
                else if (cosmicSkyActive)
                    mode = GodEyeSkyAccessoryMode.Cosmic;
                else if (voidSkyActive)
                    mode = GodEyeSkyAccessoryMode.Void;

                GodEyeSkyAccessoryBridge.CurrentMode = mode;

                if (SkyManager.Instance["GodEyeMod:GodEyeSky"] != null &&
                    !SkyManager.Instance["GodEyeMod:GodEyeSky"].IsActive())
                {
                    SkyManager.Instance.Activate("GodEyeMod:GodEyeSky", Player.Center);
                }
            }
            else
            {
                GodEyeSkyAccessoryBridge.CurrentMode = GodEyeSkyAccessoryMode.None;

                if (SkyManager.Instance["GodEyeMod:GodEyeSky"] != null &&
                    SkyManager.Instance["GodEyeMod:GodEyeSky"].IsActive())
                {
                    SkyManager.Instance.Deactivate("GodEyeMod:GodEyeSky");
                }
            }
        }

        private bool BossIsAlive()
        {
            int bossType = ModContent.NPCType<NPCs.GodEye.GodEyeBoss>();
            for (int i = 0; i < Main.maxNPCs; i++)
                if (Main.npc[i].active && Main.npc[i].type == bossType) return true;
            return false;
        }
    }

    public enum GodEyeSkyAccessoryMode
    {
        None,
        Void,
        Cosmic,
        Omniscient
    }

    public static class GodEyeSkyAccessoryBridge
    {
        public static GodEyeSkyAccessoryMode CurrentMode = GodEyeSkyAccessoryMode.None;
    }
}