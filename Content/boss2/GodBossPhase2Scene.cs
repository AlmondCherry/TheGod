using Terraria;
using Terraria.ModLoader;

namespace GodEyeMod.Content.boss2
{
    public class GodBossPhase2Scene : ModSceneEffect
    {
        public override int Music => MusicLoader.GetMusicSlot(Mod, "Content/Music/theFINAL");
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override bool IsSceneEffectActive(Player player)
        {
            return NPC.AnyNPCs(ModContent.NPCType<GodBossPhase2>());
        }
    }
}