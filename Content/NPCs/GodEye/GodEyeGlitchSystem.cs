using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace GodEyeMod.Content.NPCs.GodEye
{
    public class GodEyeGlitchSystem : ModSystem
    {
        public static bool BossActive = false;
        public static int BossPhase = 0;
        public static float ScreenShakeIntensity = 0f;
        public static int KickTimer = -1;

        public static bool DeathChaosActive = false;
        public static int DeathChaosTimer = 0;
        public static float DeathChaosProgress = 0f;

        public static void StartDeathChaos(Vector2 bossCenter)
        {
            DeathChaosActive = true;

            if (Main.netMode != Terraria.ID.NetmodeID.Server)
            {
                if (!ModContent.GetInstance<GodEyeConfig>().DisableChatMessages)
                {
                    Main.NewText("T̸̛H̸̛E̸̛ ̸̛G̸̛Ơ̸D̸̛ ̸̛H̸̛A̸̛S̸̛ ̸̛F̸̛A̸̛L̸̛L̸̛E̸̛N̸̛", Color.Red);
                    Main.NewText("REALITY IS COLLAPSING", Color.DarkRed);
                    Main.NewText("GOODBYE.", Color.DarkRed);
                }

                WorldGen.SaveAndQuit();
            }
        }

        public static void ForceReset()
        {
            BossActive = false;
            BossPhase = 0;
            ScreenShakeIntensity = 0f;
            KickTimer = -1;
            DeathChaosActive = false;
            DeathChaosTimer = 0;
            DeathChaosProgress = 0f;
        }

        public override void OnWorldLoad()
        {
            ForceReset();
        }

        public override void OnWorldUnload()
        {
            ForceReset();
        }

        public override void PostUpdateNPCs()
        {
            BossActive = false;
            BossPhase = 0;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<GodEyeBoss>())
                {
                    BossActive = true;
                    BossPhase = (int)Main.npc[i].ai[0];
                    break;
                }
            }

            if (!BossActive && !DeathChaosActive)
            {
                ScreenShakeIntensity = 0;
            }
        }

        public override void ModifyScreenPosition()
        {
            if (!BossActive && !DeathChaosActive) return;

            if (ModContent.GetInstance<GodEyeConfig>().DisableScreenShake)
            {
                ScreenShakeIntensity = 0f;
                return;
            }

            float intensity = 0;

            if (DeathChaosActive)
            {
                intensity = 30f;
            }
            else
            {
                switch (BossPhase)
                {
                    case 1: intensity = 2f; break;
                    case 2: intensity = 4f; break;
                    case 3: intensity = 8f; break;
                }
            }

            intensity += ScreenShakeIntensity;
            ScreenShakeIntensity *= 0.95f;

            if (intensity > 0)
            {
                Main.screenPosition += new Vector2(
                    Main.rand.NextFloat(-intensity, intensity),
                    Main.rand.NextFloat(-intensity, intensity));
            }

            if (BossPhase >= 2 && Main.rand.NextBool(100))
            {
                Main.screenPosition += new Vector2(Main.rand.Next(-20, 20), Main.rand.Next(-20, 20));
            }
            else if (BossPhase >= 3 && Main.rand.NextBool(40))
            {
                Main.screenPosition += new Vector2(Main.rand.Next(-40, 40), Main.rand.Next(-40, 40));
            }
        }
    }

    public static class GodEyeGlitchSysteme
    {
        public static void StartDeathChaos(Vector2 bossCenter)
        {
            GodEyeGlitchSystem.StartDeathChaos(bossCenter);
        }
    }
}