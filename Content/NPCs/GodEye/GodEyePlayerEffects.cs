using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content.NPCs.GodEye
{
    public class GodEyePlayerEffects : ModPlayer
    {
        public bool invertedControls = false;
        private int invertTimer = 0;
        private int darknessTimer = 0;

        public override void ResetEffects()
        {
            if (invertTimer > 0)
            {
                invertTimer--;
                invertedControls = true;
            }
            else
            {
                invertedControls = false;
            }
        }

        public override void PostUpdate()
        {
            if (!GodEyeGlitchSystem.BossActive) return;

            int phase = GodEyeGlitchSystem.BossPhase;

            if (phase >= 2 && Main.rand.NextBool(3000))
            {
                invertTimer = 30 + Main.rand.Next(20);
                if (Main.netMode != NetmodeID.Server)
                    Main.NewText("CONTROLS_INVERTED", Color.OrangeRed);
            }

            if (phase >= 2 && Main.rand.NextBool(2400))
            {
                darknessTimer = 40;
            }

            if (darknessTimer > 0)
            {
                darknessTimer--;
                Lighting.GlobalBrightness = MathHelper.Lerp(Lighting.GlobalBrightness, 0.1f, 0.1f);
            }

            if (phase >= 3 && Main.rand.NextBool(600))
            {
                Player.Hurt(PlayerDeathReason.ByCustomReason(Player.name + " was corrupted by the God Eye"),
                    Main.rand.Next(10, 25), 0);
                if (Main.netMode != NetmodeID.Server)
                    Main.NewText("COLLISION_ERROR: damage applied", Color.Red);
            }
        }

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            GodEyeRitualSystem.OnPlayerDied();
            GodEyeQuestSystem.ResetAll();
            ResetBossQuestEffects();

            if (GodEyeGlitchSystem.BossActive)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    Main.NewText("YOUR SOUL HAS BEEN CONSUMED.", Color.DarkRed);
                    Main.NewText("GOODBYE.", Color.Red);

                    WorldGen.SaveAndQuit();
                }
            }
        }

        public override void OnRespawn()
        {
            invertTimer = 0;
            darknessTimer = 0;
            invertedControls = false;
        }

        public override void OnEnterWorld()
        {
            invertTimer = 0;
            darknessTimer = 0;
            invertedControls = false;
            GodEyeGlitchSystem.ForceReset();
            GodEyeQuestSystem.ResetAll();
        }

        private void ResetBossQuestEffects()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.type == ModContent.NPCType<GodEyeBoss>())
                {
                    (npc.ModNPC as GodEyeBoss)?.ResetQuestEffects();
                    break;
                }
            }
        }

        public override void ProcessTriggers(Terraria.GameInput.TriggersSet triggersSet)
        {
            if (!invertedControls) return;

            if (Player.controlLeft && !Player.controlRight)
            {
                Player.controlLeft = false;
                Player.controlRight = true;
            }
            else if (Player.controlRight && !Player.controlLeft)
            {
                Player.controlRight = false;
                Player.controlLeft = true;
            }
        }
    }
}