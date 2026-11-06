using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace GodEyeMod.Content.NPCs.GodEye
{
    public class GodEyeWorldEffects : ModSystem
    {
        private static int timeGlitchCooldown = 0;
        private static int weatherGlitchCooldown = 0;

        public override void PostUpdateTime()
        {
            if (!GodEyeGlitchSystem.BossActive) return;

            int phase = GodEyeGlitchSystem.BossPhase;

            if (timeGlitchCooldown > 0) timeGlitchCooldown--;
            if (weatherGlitchCooldown > 0) weatherGlitchCooldown--;

            if (phase >= 1 && timeGlitchCooldown <= 0 && Main.rand.NextBool(180))
            {
                timeGlitchCooldown = 120;
                double timeShift = Main.rand.Next(-2000, 2000);
                Main.time += timeShift;
                if (Main.time < 0) Main.time = 0;
                if (Main.time > 54000) Main.time = 54000;
            }

            if (phase >= 2 && weatherGlitchCooldown <= 0 && Main.rand.NextBool(300))
            {
                weatherGlitchCooldown = 180;

                if (Main.rand.NextBool(2))
                {
                    Main.StartRain();
                    Main.maxRaining = Main.rand.NextFloat(0.5f, 1f);
                }
                else
                {
                    Main.StopRain();
                }

                if (Main.rand.NextBool(3))
                {
                    Main.windSpeedTarget = Main.rand.NextFloat(-1f, 1f);
                }
            }

            if (phase >= 2)
            {
                Main.dayTime = false;
            }

            if (phase >= 3 && Main.rand.NextBool(60))
            {
                Main.moonPhase = Main.rand.Next(8);
            }
        }

        public override void PostUpdateEverything()
        {
            if (!GodEyeGlitchSystem.BossActive) return;

            int phase = GodEyeGlitchSystem.BossPhase;

            if (phase >= 1)
            {
                float intensity = phase * 0.1f;
            }
        }
    }
}