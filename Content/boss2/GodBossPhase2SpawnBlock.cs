using Terraria;
using Terraria.ModLoader;

namespace GodEyeMod.Content.boss2
{
    public class GodBossPhase2SpawnBlock : GlobalNPC
    {
        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (NPC.AnyNPCs(ModContent.NPCType<GodBossPhase2>()))
            {
                maxSpawns = 0;
                spawnRate = int.MaxValue;
            }
        }
    }
}