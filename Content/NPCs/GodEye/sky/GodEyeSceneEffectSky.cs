using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

using GodEyeMod.Content.Items;

namespace GodEyeMod.Content.NPCs.GodEye
{
    public class GodEyeSceneEffect : ModSceneEffect
    {
        public override int Music => MusicLoader.GetMusicSlot(Mod, "Content/Music/GodEyeTheme");

        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override bool IsSceneEffectActive(Player player)
        {
            return NPC.AnyNPCs(ModContent.NPCType<GodEyeBoss>());
        }

        public override void SpecialVisuals(Player player, bool isActive)
        {
            if (SkyManager.Instance["GodEyeMod:GodEyeSky"] == null) return;

            if (isActive)
            {
                if (!SkyManager.Instance["GodEyeMod:GodEyeSky"].IsActive())
                    SkyManager.Instance.Activate("GodEyeMod:GodEyeSky", player.Center);
            }
            else
            {
                var modPlayer = player.GetModPlayer<GodEyeSkyPlayer>();
                bool anyAccessoryActive = modPlayer.voidSkyActive
                    || modPlayer.cosmicSkyActive
                    || modPlayer.omniscientSkyActive;

                if (!anyAccessoryActive && SkyManager.Instance["GodEyeMod:GodEyeSky"].IsActive())
                    SkyManager.Instance.Deactivate("GodEyeMod:GodEyeSky");
            }
        }
    }
}