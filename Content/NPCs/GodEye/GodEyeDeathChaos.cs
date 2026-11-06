using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace GodEyeMod.Content.NPCs.GodEye
{
    public class GodEyeGlitchSystems : ModSystem
    {
        public static bool DeathChaosActive = false;
        public static int ChaosTimer = 0;
        public static int MaxChaosTime = 1080;
        public static int KickTimer = -1;
        public static Vector2 DeathPosition = Vector2.Zero;

        private static float blackHoleRadius = 0f;
        private static float screenShake = 0f;
        private static float redFlash = 0f;
        private static float whiteFlash = 0f;
        private static float staticNoise = 0f;
        private static float chromaticAberration = 0f;
        private static float blackOverlay = 0f;
        private static float worldCrumble = 0f;

        private static bool screamerActive = false;
        private static int screamerTimer = 0;
        private static int screamerMaxTime = 180;
        private static float screamerShake = 0f;
        private static float screamerAlpha = 0f;
        private static float screamerScale = 1f;
        private static float screamerFlicker = 0f;
        private static bool screamerSoundPlayed = false;

        private static bool crashScreenActive = false;
        private static int crashTimer = 0;
        private static int currentCrashLine = 0;
        private static float crashTextAlpha = 1f;

        private static List<EyeCrack> eyeCracks = new List<EyeCrack>();
        private static List<FloatingEye> floatingEyes = new List<FloatingEye>();
        private static List<FallingChunk> fallingChunks = new List<FallingChunk>();
        private static List<GlitchText> glitchTexts = new List<GlitchText>();
        private static List<BloodDrip> bloodDrips = new List<BloodDrip>();
        private static List<VoidTendril> voidTendrils = new List<VoidTendril>();

        private static int lastSoundTick = 0;

        private class EyeCrack
        {
            public Vector2 Position;
            public float Scale;
            public float Rotation;
            public float RotSpeed;
            public float Life;
            public float MaxLife;
            public float Opacity;
            public Color Tint;
            public Vector2 Velocity;
            public float PulseOffset;
            public bool Bleeding;
        }

        private class FloatingEye
        {
            public Vector2 Position;
            public float Scale;
            public float Rotation;
            public float Life;
            public float Opacity;
            public float PulseSpeed;
            public float PulseOffset;
            public Vector2 Drift;
            public bool LookingAtPlayer;
        }

        private class FallingChunk
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Rotation;
            public float RotSpeed;
            public float Size;
            public Color ChunkColor;
            public float Life;
        }

        private class GlitchText
        {
            public Vector2 Position;
            public string Text;
            public Color TextColor;
            public float Scale;
            public float Life;
            public float MaxLife;
            public float XVelocity;
        }

        private class BloodDrip
        {
            public Vector2 Position;
            public float Length;
            public float Speed;
            public float Width;
            public float Life;
            public Color DripColor;
        }

        private class VoidTendril
        {
            public Vector2 Origin;
            public float Angle;
            public float Length;
            public float TargetLength;
            public float Width;
            public float WaveOffset;
            public float Life;
        }

        private static string[] deathMessages = new string[]
        {
            "WHAT HAVE YOU DONE", "THE VOID SPREADS", "REALITY.DAT CORRUPTED",
            "WORLD INTEGRITY: 0%", "FATAL: EXISTENCE NOT FOUND",
            "MEMORY: 0xDEADDEAD", "STACK OVERFLOW IN: universe.cs",
            "Y̸̛Ơ̵Ư̷ ̶̛K̴̛I̸̛L̵̛L̶̛E̷̛D̸̛ ̵̛G̶̛Ơ̷D̸̛",
            "THE GAME IS DYING", "AUTOSAVE FAILED",
            "RENDERER: CRITICAL FAILURE", "TILES ARE FALLING",
            "GRAVITY: UNDEFINED", "BIOME: NULL",
            "NPC.AI() THREW: DeathException",
            "PLAYER.SANITY = -∞", "WORLD.DELETE()",
            "AUDIO: SCREAMING", "INPUT: LOST",
            "SKY.RENDER(): VOID", "TIME: STOPPED",
            "THERE IS NOTHING LEFT",
            "CAN YOU HEAR IT BREATHING?",
            "IT'S BEHIND YOUR SCREEN",
            "CHECK YOUR WEBCAM",
            "I'M STILL HERE",
            "CLOSING IN 3... 2... 1...",
            "JUST KIDDING. YOU CAN'T LEAVE.",
        };

        private static string[] crashMessages = new string[]
        {
            "Terraria has encountered a fatal error.",
            "",
            "Exception: GodEyeDeathException",
            "Message: You killed a god. Gods don't die quietly.",
            "",
            "Stack Trace:",
            "  at GodEye.OnDeath() in universe.cs:line 666",
            "  at Reality.Collapse() in existence.cs:line 0",
            "  at World.Destroy() in terraria.cs:line NULL",
            "  at Void.Consume(Everything everything)",
            "  at Screen.Shatter()",
            "  at Game.End()",
            "",
            "  World File: CORRUPTED",
            "  Player File: CORRUPTED",
            "  Recovery: IMPOSSIBLE",
            "",
            "  \"I̶̛ ̸̛W̵̛I̶̛L̷̛L̸̛ ̵̛R̶̛E̷̛T̸̛Ư̵R̶̛N̷̛\"",
            "",
            "Press any key to close...",
            "",
            "...",
            "...did you think this was fake?",
        };

        public static void StartDeathChaos(Vector2 bossDeathPos)
        {
            if (DeathChaosActive) return;

            DeathChaosActive = true;
            ChaosTimer = 0;
            KickTimer = -1;
            DeathPosition = bossDeathPos;

            blackHoleRadius = 0f;
            screenShake = 0f;
            redFlash = 0f;
            whiteFlash = 1f;
            staticNoise = 0f;
            chromaticAberration = 0f;
            blackOverlay = 0f;
            worldCrumble = 0f;

            screamerActive = false;
            screamerTimer = 0;
            screamerSoundPlayed = false;

            crashScreenActive = false;
            crashTimer = 0;
            currentCrashLine = 0;

            eyeCracks.Clear();
            floatingEyes.Clear();
            fallingChunks.Clear();
            glitchTexts.Clear();
            bloodDrips.Clear();
            voidTendrils.Clear();

            SoundEngine.PlaySound(SoundID.Roar, bossDeathPos);

            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText("", Color.White);
                Main.NewText("W̸̛H̵̛A̷̛T̶̛ ̸̛H̵̛A̶̛V̷̛E̸̛ ̵̛Y̶̛Ơ̷Ư̸ ̵̛D̶̛Ơ̷N̸̛E̵̛", Color.Red);
            }

            GodEyeIconReplacer.TriggerDeathIcon();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.townNPC)
                {
                    npc.life = 0;
                    npc.checkDead();
                    npc.active = false;
                }
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.townNPC && !npc.boss)
                {
                    npc.life = 0;
                    npc.active = false;
                }
            }

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active)
                    Main.projectile[i].Kill();
            }

            Music = -1;
        }

        private static int Music
        {
            set
            {
                if (Main.netMode != NetmodeID.Server)
                    Main.musicVolume = value == -1 ? 0f : 1f;
            }
        }

        public override void PostUpdateEverything()
        {
            if (!DeathChaosActive) return;

            ChaosTimer++;
            float progress = Math.Min(1f, (float)ChaosTimer / MaxChaosTime);

            if (ChaosTimer == 1)
            {
                KillBossCompletely();
            }

            if (ChaosTimer % 10 == 0)
            {
                StopAllGameProcesses();
            }

            if (screamerActive)
            {
                UpdateScreamer();
                return;
            }

            if (crashScreenActive)
            {
                UpdateCrashScreen();
                return;
            }

            if (ChaosTimer <= 180)
            {
                UpdatePhase1(progress);
            }
            else if (ChaosTimer <= 420)
            {
                UpdatePhase2(progress);
            }
            else if (ChaosTimer <= 720)
            {
                UpdatePhase3(progress);
            }
            else if (ChaosTimer <= 900)
            {
                UpdatePhase4(progress);
            }
            else if (ChaosTimer <= MaxChaosTime)
            {
                UpdatePhase5(progress);
            }

            UpdateAllEffects();
            UpdateSounds();
            ApplyPlayerDebuffs(progress);

            whiteFlash = MathHelper.Lerp(whiteFlash, 0f, 0.04f);
            redFlash = MathHelper.Lerp(redFlash, 0f, 0.03f);
        }

        private void KillBossCompletely()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && (npc.type == ModContent.NPCType<GodEyeBoss>()
                    || npc.type == ModContent.NPCType<GodEyeMinion>()))
                {
                    npc.life = 0;
                    npc.active = false;
                    npc.dontTakeDamage = true;

                    for (int d = 0; d < 30; d++)
                    {
                        Dust dust = Dust.NewDustDirect(npc.Center, 1, 1, DustID.Shadowflame, 0, 0, 200, default, 3f);
                        dust.noGravity = true;
                        dust.velocity = Main.rand.NextVector2Circular(15, 15);
                    }
                }
            }
        }

        private void StopAllGameProcesses()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && npc.type != NPCID.None)
                {
                    npc.active = false;
                }
            }

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.hostile)
                {
                    p.Kill();
                }
            }
        }

        private void UpdatePhase1(float progress)
        {
            float p = ChaosTimer / 180f;

            blackHoleRadius = MathHelper.Lerp(blackHoleRadius, p * 150f, 0.02f);
            screenShake = p * 3f;
            staticNoise = p * 0.08f;
            chromaticAberration = p * 2f;

            if (ChaosTimer % 25 == 0)
                SpawnEyeCrack(false);

            if (ChaosTimer % 50 == 0)
                SpawnGlitchText(false);

            if (ChaosTimer % 5 == 0)
                SpawnWorldDust(3, blackHoleRadius * 2f);

            if (ChaosTimer == 60)
                redFlash = 0.5f;

            if (ChaosTimer % 40 == 0 && ChaosTimer > 60)
                SpawnBloodDrip();
        }

        private void UpdatePhase2(float progress)
        {
            float p = (ChaosTimer - 180f) / 240f;

            blackHoleRadius = MathHelper.Lerp(blackHoleRadius, 150f + p * 500f, 0.03f);
            screenShake = 3f + p * 10f;
            staticNoise = 0.08f + p * 0.15f;
            chromaticAberration = 2f + p * 6f;
            blackOverlay = p * 0.25f;

            if (ChaosTimer % 10 == 0)
                SpawnEyeCrack(true);

            if (ChaosTimer % 30 == 0)
                SpawnFloatingEye();

            if (ChaosTimer % 8 == 0)
                SpawnBloodDrip();

            if (ChaosTimer % 12 == 0)
                SpawnFallingChunk();

            if (ChaosTimer % 15 == 0)
                SpawnGlitchText(true);

            if (ChaosTimer % 45 == 0)
                SpawnVoidTendril();

            if (ChaosTimer % 40 == 0)
                redFlash = 0.3f + p * 0.2f;

            if (ChaosTimer % 3 == 0)
                SpawnWorldDust(6, blackHoleRadius * 1.5f);

            if (ChaosTimer % 45 == 0 && Main.netMode != NetmodeID.Server)
            {
                Main.NewText(deathMessages[Main.rand.Next(deathMessages.Length)],
                    new Color(Main.rand.Next(180, 255), 0, Main.rand.Next(0, 40)));
            }
        }

        private void UpdatePhase3(float progress)
        {
            float p = (ChaosTimer - 420f) / 300f;

            blackHoleRadius = MathHelper.Lerp(blackHoleRadius, 650f + p * 1500f, 0.04f);
            screenShake = 13f + p * 18f;
            staticNoise = 0.23f + p * 0.35f;
            chromaticAberration = 8f + p * 18f;
            blackOverlay = 0.25f + p * 0.35f;
            worldCrumble = p;

            if (ChaosTimer % 4 == 0)
            {
                SpawnEyeCrack(true);
                SpawnEyeCrack(true);
            }

            if (ChaosTimer % 12 == 0)
                SpawnFloatingEye();

            if (ChaosTimer % 3 == 0)
            {
                SpawnBloodDrip();
                SpawnBloodDrip();
            }

            if (ChaosTimer % 4 == 0)
            {
                SpawnFallingChunk();
                SpawnFallingChunk();
            }

            if (ChaosTimer % 6 == 0)
                SpawnGlitchText(true);

            if (ChaosTimer % 20 == 0)
            {
                SpawnVoidTendril();
                SpawnVoidTendril();
            }

            if (ChaosTimer % 20 == 0)
                redFlash = Main.rand.NextFloat(0.3f, 0.7f);

            SpawnWorldDust(15, blackHoleRadius);

            if (ChaosTimer % 15 == 0 && Main.netMode != NetmodeID.Server)
            {
                string msg = deathMessages[Main.rand.Next(deathMessages.Length)];
                char[] c = msg.ToCharArray();
                for (int i = 0; i < (int)(c.Length * p * 0.5f); i++)
                    c[Main.rand.Next(c.Length)] = (char)Main.rand.Next(33, 126);
                Main.NewText(new string(c),
                    new Color(Main.rand.Next(256), Main.rand.Next(0, 50), Main.rand.Next(0, 50)));
            }
        }

        private void UpdatePhase4(float progress)
        {
            float p = (ChaosTimer - 720f) / 180f;

            blackHoleRadius = MathHelper.Lerp(blackHoleRadius, 2150f + p * 4000f, 0.05f);
            screenShake = 31f + p * 20f;
            staticNoise = 0.58f + p * 0.4f;
            chromaticAberration = 26f + p * 20f;
            blackOverlay = 0.6f + p * 0.35f;
            worldCrumble = 1f;

            for (int i = 0; i < 5; i++)
            {
                SpawnEyeCrack(true);
                SpawnFallingChunk();
                SpawnBloodDrip();
            }
            if (ChaosTimer % 3 == 0) SpawnFloatingEye();
            if (ChaosTimer % 5 == 0) SpawnGlitchText(true);
            if (ChaosTimer % 8 == 0) SpawnVoidTendril();

            SpawnWorldDust(25, blackHoleRadius);

            if (ChaosTimer % 8 == 0)
                redFlash = Main.rand.NextFloat(0.4f, 0.9f);

            if (ChaosTimer % 5 == 0 && Main.netMode != NetmodeID.Server)
            {
                string garbage = "";
                for (int i = 0; i < Main.rand.Next(10, 50); i++)
                    garbage += (char)Main.rand.Next(33, 126);
                Main.NewText(garbage,
                    new Color(Main.rand.Next(256), Main.rand.Next(256), Main.rand.Next(256)));
            }
        }

        private void UpdatePhase5(float progress)
        {
            float p = (ChaosTimer - 900f) / (MaxChaosTime - 900f);

            whiteFlash = MathHelper.Lerp(whiteFlash, p, 0.05f);
            screenShake = 50f * (1f - p);
            blackOverlay = MathHelper.Lerp(blackOverlay, 0f, 0.1f);
            staticNoise = MathHelper.Lerp(staticNoise, 1f, 0.05f);

            if (p > 0.5f)
            {
                eyeCracks.Clear();
                floatingEyes.Clear();
                fallingChunks.Clear();
                glitchTexts.Clear();
                bloodDrips.Clear();
                voidTendrils.Clear();
            }

            if (ChaosTimer >= MaxChaosTime)
            {
                screamerActive = true;
                screamerTimer = 0;
                screamerAlpha = 0f;
                screamerScale = 3f;
                screamerShake = 40f;
                screamerSoundPlayed = false;
                screamerFlicker = 0f;
                whiteFlash = 0f;
                blackOverlay = 0f;
                staticNoise = 0f;
            }
        }

        private void UpdateScreamer()
        {
            screamerTimer++;

            if (screamerTimer <= 30)
            {
                float p = screamerTimer / 30f;
                screamerAlpha = MathHelper.Lerp(0f, 1f, p * p);
                screamerScale = MathHelper.Lerp(3f, 1.1f, p);
                screamerShake = 40f * (1f - p * 0.5f);

                if (!screamerSoundPlayed)
                {
                    screamerSoundPlayed = true;
                }
            }
            else if (screamerTimer <= 120)
            {
                screamerAlpha = 1f;
                screamerScale = 1.1f + (float)Math.Sin(screamerTimer * 0.3f) * 0.08f;
                screamerShake = 15f + (float)Math.Sin(screamerTimer * 0.5f) * 10f;

                screamerFlicker = Main.rand.NextBool(8) ? Main.rand.NextFloat(0.3f, 0.7f) : 0f;

                if (screamerTimer % 30 == 0)
                    SoundEngine.PlaySound(SoundID.Zombie104, Main.LocalPlayer.Center);

                if (screamerTimer % 15 == 0)
                    redFlash = 0.4f;
                redFlash = MathHelper.Lerp(redFlash, 0f, 0.05f);
            }
            else if (screamerTimer <= 160)
            {
                float p = (screamerTimer - 120f) / 40f;
                screamerAlpha = 1f - p;
                screamerScale = 1.1f + p * 2f;
                screamerShake = 15f * (1f - p);

                if (Main.rand.NextBool(3))
                    screamerAlpha = Main.rand.NextFloat(0f, 1f - p);
            }
            else if (screamerTimer <= screamerMaxTime)
            {
                screamerAlpha = 0f;
                blackOverlay = 1f;
                screamerShake = 0f;
            }
            else
            {
                screamerActive = false;
                crashScreenActive = true;
                crashTimer = 0;
                currentCrashLine = 0;
                blackOverlay = 1f;
            }
        }

        private void UpdateCrashScreen()
        {
            crashTimer++;

            if (crashTimer < 60) return;

            if (crashTimer >= 60 && (crashTimer - 60) % 5 == 0 && currentCrashLine < crashMessages.Length)
            {
                currentCrashLine++;
            }

            if (Main.rand.NextBool(40))
                crashTextAlpha = Main.rand.NextFloat(0.6f, 1f);
            else
                crashTextAlpha = MathHelper.Lerp(crashTextAlpha, 1f, 0.1f);

            if (currentCrashLine >= crashMessages.Length && crashTimer > 60 + crashMessages.Length * 5 + 240)
            {
                if (KickTimer == -1)
                {
                    SpawnLoot();
                    KickTimer = 180;
                }

                KickTimer--;

                if (KickTimer <= 0)
                {
                    ResetAll();

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        WorldGen.SaveAndQuit();
                    else
                    {
                        Main.gameMenu = true;
                        Main.menuMode = 0;
                    }
                }
            }
        }

        private void SpawnEyeCrack(bool large)
        {
            if (eyeCracks.Count > 80) return;

            Vector2 origin;
            if (Main.rand.NextBool(3))
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                origin = (DeathPosition - Main.screenPosition) + angle.ToRotationVector2() * Main.rand.NextFloat(50, blackHoleRadius * 0.8f);
            }
            else
            {
                origin = new Vector2(Main.rand.Next(-50, Main.screenWidth + 50),
                                    Main.rand.Next(-50, Main.screenHeight + 50));
            }

            float scale = Main.rand.NextFloat(0.2f, large ? 1.2f : 0.6f);
            bool bleeding = Main.rand.NextBool(3);

            eyeCracks.Add(new EyeCrack
            {
                Position = origin,
                Scale = scale,
                Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
                RotSpeed = Main.rand.NextFloat(-0.02f, 0.02f),
                Life = Main.rand.NextFloat(120f, 360f),
                MaxLife = 360f,
                Opacity = 0f,
                Tint = bleeding ? Color.DarkRed : Color.Red,
                Velocity = Main.rand.NextVector2Circular(0.5f, 0.5f),
                PulseOffset = Main.rand.NextFloat(MathHelper.TwoPi),
                Bleeding = bleeding
            });

            if (bleeding)
            {
                for (int i = 0; i < 3; i++)
                    SpawnBloodDripAt(origin);
            }
        }

        private void SpawnFloatingEye()
        {
            if (floatingEyes.Count > 25) return;

            floatingEyes.Add(new FloatingEye
            {
                Position = new Vector2(Main.rand.Next(50, Main.screenWidth - 50),
                                      Main.rand.Next(50, Main.screenHeight - 50)),
                Scale = Main.rand.NextFloat(0.3f, 1.5f),
                Rotation = Main.rand.NextFloat(-0.3f, 0.3f),
                Life = Main.rand.NextFloat(60f, 240f),
                Opacity = 0f,
                PulseSpeed = Main.rand.NextFloat(0.03f, 0.1f),
                PulseOffset = Main.rand.NextFloat(MathHelper.TwoPi),
                Drift = Main.rand.NextVector2Circular(0.5f, 0.3f),
                LookingAtPlayer = Main.rand.NextBool(3)
            });
        }

        private void SpawnFallingChunk()
        {
            if (fallingChunks.Count > 60) return;

            fallingChunks.Add(new FallingChunk
            {
                Position = new Vector2(Main.rand.Next(-50, Main.screenWidth + 50),
                                      Main.rand.Next(-150, -10)),
                Velocity = new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(2f, 10f)),
                Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
                RotSpeed = Main.rand.NextFloat(-0.15f, 0.15f),
                Size = Main.rand.NextFloat(6f, 35f),
                ChunkColor = new Color(Main.rand.Next(20, 80), Main.rand.Next(10, 40), Main.rand.Next(5, 25)),
                Life = 400f
            });
        }

        private void SpawnGlitchText(bool intense)
        {
            if (glitchTexts.Count > 50) return;

            string text = deathMessages[Main.rand.Next(deathMessages.Length)];
            if (intense && Main.rand.NextBool(2))
            {
                text = "";
                for (int i = 0; i < Main.rand.Next(5, 25); i++)
                    text += (char)Main.rand.Next(33, 126);
            }

            glitchTexts.Add(new GlitchText
            {
                Position = new Vector2(Main.rand.Next(10, Main.screenWidth - 10),
                                      Main.rand.Next(10, Main.screenHeight - 10)),
                Text = text,
                TextColor = Main.rand.NextBool(4)
                    ? Color.Red : new Color(Main.rand.Next(100, 200), 0, 0),
                Scale = Main.rand.NextFloat(0.3f, intense ? 2.5f : 1f),
                Life = Main.rand.NextFloat(20f, 120f),
                MaxLife = 120f,
                XVelocity = Main.rand.NextFloat(-4f, 4f)
            });
        }

        private void SpawnBloodDrip()
        {
            SpawnBloodDripAt(new Vector2(Main.rand.Next(0, Main.screenWidth), Main.rand.Next(-20, 0)));
        }

        private void SpawnBloodDripAt(Vector2 pos)
        {
            if (bloodDrips.Count > 100) return;

            bloodDrips.Add(new BloodDrip
            {
                Position = pos,
                Length = 0f,
                Speed = Main.rand.NextFloat(1f, 5f),
                Width = Main.rand.NextFloat(2f, 6f),
                Life = Main.rand.NextFloat(120f, 360f),
                DripColor = new Color(
                    Main.rand.Next(120, 200),
                    0,
                    Main.rand.Next(0, 20)) * Main.rand.NextFloat(0.5f, 1f)
            });
        }

        private void SpawnVoidTendril()
        {
            if (voidTendrils.Count > 15) return;

            Vector2 screenCenter = DeathPosition - Main.screenPosition;
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);

            voidTendrils.Add(new VoidTendril
            {
                Origin = screenCenter,
                Angle = angle,
                Length = 0f,
                TargetLength = Main.rand.NextFloat(200f, 800f),
                Width = Main.rand.NextFloat(5f, 25f),
                WaveOffset = Main.rand.NextFloat(MathHelper.TwoPi),
                Life = Main.rand.NextFloat(120f, 300f)
            });
        }

        private void SpawnWorldDust(int count, float radius)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 dustPos = DeathPosition + Main.rand.NextVector2Circular(radius, radius);
                int dustType = Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.Vortex;
                Dust d = Dust.NewDustDirect(dustPos, 1, 1, dustType, 0, 0, 200, default, Main.rand.NextFloat(1f, 4f));
                d.noGravity = true;
                d.velocity = (DeathPosition - d.position).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2f, 12f);
            }
        }

        private void UpdateAllEffects()
        {
            for (int i = eyeCracks.Count - 1; i >= 0; i--)
            {
                var c = eyeCracks[i];
                c.Life--;
                c.Position += c.Velocity;
                c.Rotation += c.RotSpeed;
                c.Opacity = Math.Min(1f, c.Opacity + 0.05f);
                if (c.Life < 30) c.Opacity = c.Life / 30f;
                if (c.Life <= 0) eyeCracks.RemoveAt(i);
            }

            for (int i = floatingEyes.Count - 1; i >= 0; i--)
            {
                var e = floatingEyes[i];
                e.Life--;
                e.Position += e.Drift;
                e.Opacity = Math.Min(1f, e.Opacity + 0.03f);
                if (e.Life < 20) e.Opacity = e.Life / 20f;

                if (e.LookingAtPlayer)
                {
                    Vector2 mouseScreen = new Vector2(Main.mouseX, Main.mouseY);
                    float targetRot = (mouseScreen - e.Position).ToRotation();
                    e.Rotation = MathHelper.Lerp(e.Rotation, targetRot, 0.03f);
                }

                if (e.Life <= 0) floatingEyes.RemoveAt(i);
            }

            for (int i = fallingChunks.Count - 1; i >= 0; i--)
            {
                var c = fallingChunks[i];
                c.Position += c.Velocity;
                c.Velocity.Y += 0.2f;
                c.Rotation += c.RotSpeed;
                c.Life--;
                if (c.Life <= 0 || c.Position.Y > Main.screenHeight + 200) fallingChunks.RemoveAt(i);
            }

            for (int i = glitchTexts.Count - 1; i >= 0; i--)
            {
                var t = glitchTexts[i];
                t.Life--;
                t.Position.X += t.XVelocity;
                if (Main.rand.NextBool(15)) t.Position.Y += Main.rand.NextFloat(-4, 4);
                if (t.Life <= 0) glitchTexts.RemoveAt(i);
            }

            for (int i = bloodDrips.Count - 1; i >= 0; i--)
            {
                var b = bloodDrips[i];
                b.Length += b.Speed;
                b.Life--;
                if (b.Position.Y + b.Length > Main.screenHeight + 50) b.Speed *= 0.9f;
                if (b.Life <= 0) bloodDrips.RemoveAt(i);
            }

            for (int i = voidTendrils.Count - 1; i >= 0; i--)
            {
                var t = voidTendrils[i];
                t.Length = MathHelper.Lerp(t.Length, t.TargetLength, 0.04f);
                t.Life--;
                if (t.Life <= 0) voidTendrils.RemoveAt(i);
            }
        }

        private void UpdateSounds()
        {
            if (Main.netMode == NetmodeID.Server || screamerActive || crashScreenActive) return;

            float progress = Math.Min(1f, (float)ChaosTimer / MaxChaosTime);
            int interval = (int)MathHelper.Lerp(50, 8, progress);

            if (ChaosTimer - lastSoundTick >= interval)
            {
                lastSoundTick = ChaosTimer;
                int sound = Main.rand.Next(4);
                switch (sound)
                {
                    case 0: SoundEngine.PlaySound(SoundID.Zombie105, Main.LocalPlayer.Center); break;
                    case 1: SoundEngine.PlaySound(SoundID.NPCDeath10, Main.LocalPlayer.Center); break;
                    case 2: SoundEngine.PlaySound(SoundID.Zombie104, Main.LocalPlayer.Center); break;
                    case 3: SoundEngine.PlaySound(SoundID.Item119, Main.LocalPlayer.Center); break;
                }
            }

            if (ChaosTimer % 90 == 0)
                SoundEngine.PlaySound(SoundID.Roar, DeathPosition);
        }

        private void ApplyPlayerDebuffs(float progress)
        {
            Player p = Main.LocalPlayer;
            if (!p.active || p.dead) return;

            if (ChaosTimer % 40 == 0)
            {
                p.AddBuff(BuffID.Darkness, 120);
                if (progress > 0.2f) p.AddBuff(BuffID.Slow, 120);
                if (progress > 0.4f) p.AddBuff(BuffID.Confused, 60);
                if (progress > 0.5f) p.AddBuff(BuffID.Blackout, 120);
                if (progress > 0.7f) p.AddBuff(BuffID.Obstructed, 60);
            }

            if (progress > 0.4f && ChaosTimer % 60 == 0)
            {
                int dmg = (int)(5 + 20 * progress);
                p.Hurt(PlayerDeathReason.ByCustomReason(p.name + " was consumed by the void"), dmg, 0);
            }

            if (progress > 0.5f && Main.rand.NextBool(300))
                p.gravDir *= -1f;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (!DeathChaosActive) return;

            layers.Add(new LegacyGameInterfaceLayer(
                "GodEyeMod: Death Chaos",
                delegate
                {
                    DrawDeathChaos(Main.spriteBatch);
                    return true;
                },
                InterfaceScaleType.None
            ));
        }

        private void DrawDeathChaos(SpriteBatch sb)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            if (crashScreenActive)
            {
                DrawCrashScreen(sb, pixel);
                return;
            }

            if (screamerActive)
            {
                DrawScreamer(sb, pixel);
                return;
            }

            if (blackOverlay > 0.01f)
                sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * blackOverlay);

            DrawBlackHole(sb, pixel);
            DrawVoidTendrils(sb, pixel);
            DrawBloodDrips(sb, pixel);
            DrawFallingChunks(sb, pixel);
            DrawEyeCracks(sb);
            DrawFloatingEyes(sb);
            DrawGlitchTexts(sb);

            if (staticNoise > 0.01f)
                DrawStaticNoise(sb, pixel);

            if (chromaticAberration > 0.5f)
                DrawChromaticEdges(sb, pixel);

            DrawGlitchBands(sb, pixel);
            DrawScanLines(sb, pixel);

            if (redFlash > 0.01f)
                sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Red * redFlash);

            if (whiteFlash > 0.01f)
                sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * whiteFlash);

            float progress = Math.Min(1f, (float)ChaosTimer / MaxChaosTime);
            DrawVignette(sb, pixel, 0.3f + progress * 0.6f);
        }

        private void DrawScreamer(SpriteBatch sb, Texture2D pixel)
        {
            sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black);

            if (screamerAlpha < 0.01f && screamerTimer > 160) return;

            Texture2D screamerTex;
            try
            {
                screamerTex = ModContent.Request<Texture2D>("GodEyeMod/Content/NPCs/GodEye/Screamer",
                    ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            catch
            {
                screamerTex = TextureAssets.Npc[NPCID.EyeofCthulhu].Value;
            }

            Vector2 center = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
            Vector2 origin = new Vector2(screamerTex.Width / 2f, screamerTex.Height / 2f);

            float baseScale = Math.Max(
                (float)Main.screenWidth / screamerTex.Width,
                (float)Main.screenHeight / screamerTex.Height) * 1.2f;
            float finalScale = baseScale * screamerScale;

            Vector2 glitchOffset = Vector2.Zero;
            if (screamerTimer > 30 && screamerTimer <= 120)
                glitchOffset = Main.rand.NextVector2Circular(8, 8);

            Vector2 shakeOffset = Vector2.Zero;
            if (screamerShake > 0.1f)
                shakeOffset = Main.rand.NextVector2Circular(screamerShake, screamerShake);

            Vector2 drawPos = center + glitchOffset + shakeOffset;

            float drawAlpha = screamerAlpha;

            if (screamerFlicker > 0)
                drawAlpha *= (1f - screamerFlicker);

            sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                Color.DarkRed * drawAlpha * 0.3f);

            float rgbOff = 6f + (float)Math.Sin(screamerTimer * 0.2f) * 4f;
            sb.Draw(screamerTex, drawPos + new Vector2(-rgbOff, -rgbOff * 0.5f), null,
                Color.Red * drawAlpha * 0.5f, 0f, origin, finalScale, SpriteEffects.None, 0f);
            sb.Draw(screamerTex, drawPos + new Vector2(rgbOff, rgbOff * 0.3f), null,
                Color.Blue * drawAlpha * 0.4f, 0f, origin, finalScale, SpriteEffects.None, 0f);
            sb.Draw(screamerTex, drawPos + new Vector2(0, rgbOff), null,
                Color.Green * drawAlpha * 0.3f, 0f, origin, finalScale, SpriteEffects.None, 0f);

            Color mainColor = Color.White * drawAlpha;
            if (screamerTimer > 30 && Main.rand.NextBool(10))
                mainColor = Color.Red * drawAlpha;

            sb.Draw(screamerTex, drawPos, null, mainColor, 0f, origin, finalScale, SpriteEffects.None, 0f);

            if (screamerTimer > 30 && screamerTimer <= 120)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (!Main.rand.NextBool(3)) continue;
                    int y = Main.rand.Next(Main.screenHeight);
                    int h = Main.rand.Next(3, 20);
                    int xOff = Main.rand.Next(-40, 40);
                    sb.Draw(pixel, new Rectangle(xOff, y, Main.screenWidth + 80, h),
                        Color.Black * Main.rand.NextFloat(0.2f, 0.6f));
                }
            }

            if (redFlash > 0.01f)
                sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Red * redFlash);

            if (screamerTimer > 30)
            {
                for (int i = 0; i < 100; i++)
                {
                    int x = Main.rand.Next(Main.screenWidth);
                    int y = Main.rand.Next(Main.screenHeight);
                    sb.Draw(pixel, new Rectangle(x, y, Main.rand.Next(1, 8), Main.rand.Next(1, 3)),
                        Color.White * 0.04f);
                }
            }
        }

        private void DrawBlackHole(SpriteBatch sb, Texture2D pixel)
        {
            if (blackHoleRadius < 1f) return;

            Vector2 screenCenter = DeathPosition - Main.screenPosition;
            int rings = 25;

            for (int i = rings; i >= 0; i--)
            {
                float ratio = (float)i / rings;
                float radius = blackHoleRadius * ratio;
                float alpha = (1f - ratio) * 0.9f;

                Color ringColor;
                if (i > rings * 0.85f)
                {
                    float edge = (ratio - 0.85f) / 0.15f;
                    ringColor = Color.Lerp(Color.Black, new Color(80, 0, 0), edge) * alpha * 0.6f;
                }
                else
                    ringColor = Color.Black * alpha;

                int size = (int)(radius * 2);
                sb.Draw(pixel, new Rectangle(
                    (int)(screenCenter.X - radius), (int)(screenCenter.Y - radius),
                    size, size), ringColor);
            }

            float pulseRadius = blackHoleRadius + (float)Math.Sin(ChaosTimer * 0.06f) * 20f;
            Color edgeCol = Color.DarkRed * (0.3f + (float)Math.Sin(ChaosTimer * 0.1f) * 0.2f);
            int thick = 3;
            sb.Draw(pixel, new Rectangle((int)(screenCenter.X - pulseRadius), (int)(screenCenter.Y - pulseRadius), (int)(pulseRadius * 2), thick), edgeCol);
            sb.Draw(pixel, new Rectangle((int)(screenCenter.X - pulseRadius), (int)(screenCenter.Y + pulseRadius), (int)(pulseRadius * 2), thick), edgeCol);
            sb.Draw(pixel, new Rectangle((int)(screenCenter.X - pulseRadius), (int)(screenCenter.Y - pulseRadius), thick, (int)(pulseRadius * 2)), edgeCol);
            sb.Draw(pixel, new Rectangle((int)(screenCenter.X + pulseRadius), (int)(screenCenter.Y - pulseRadius), thick, (int)(pulseRadius * 2)), edgeCol);
        }

        private void DrawEyeCracks(SpriteBatch sb)
        {
            Texture2D eyeTex;
            try
            {
                eyeTex = ModContent.Request<Texture2D>("GodEyeMod/Content/NPCs/GodEye/ScreenEye",
                    ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            catch
            {
                eyeTex = TextureAssets.Npc[NPCID.DemonEye].Value;
            }

            Vector2 eyeOrigin = new Vector2(eyeTex.Width / 2f, eyeTex.Height / 2f);

            foreach (var crack in eyeCracks)
            {
                float pulse = 1f + (float)Math.Sin(ChaosTimer * 0.08f + crack.PulseOffset) * 0.15f;
                float scale = crack.Scale * pulse;
                Vector2 pos = crack.Position;
                float alpha = crack.Opacity;

                if (Main.rand.NextBool(40))
                    pos += Main.rand.NextVector2Circular(5, 5);

                Color tint = crack.Tint * alpha;

                sb.Draw(eyeTex, pos, null, Color.Black * alpha * 0.6f,
                    crack.Rotation, eyeOrigin, scale * 1.4f, SpriteEffects.None, 0f);

                sb.Draw(eyeTex, pos, null, (crack.Bleeding ? Color.DarkRed : Color.Red) * alpha * 0.35f,
                    crack.Rotation, eyeOrigin, scale * 1.2f, SpriteEffects.None, 0f);

                if (chromaticAberration > 3f)
                {
                    float ca = Math.Min(chromaticAberration * 0.3f, 6f);
                    sb.Draw(eyeTex, pos + new Vector2(-ca, 0), null, Color.Red * alpha * 0.3f,
                        crack.Rotation, eyeOrigin, scale, SpriteEffects.None, 0f);
                    sb.Draw(eyeTex, pos + new Vector2(ca, 0), null, Color.Blue * alpha * 0.25f,
                        crack.Rotation, eyeOrigin, scale, SpriteEffects.None, 0f);
                }

                sb.Draw(eyeTex, pos, null, tint, crack.Rotation, eyeOrigin, scale, SpriteEffects.None, 0f);

                sb.Draw(eyeTex, pos, null, Color.White * alpha * 0.12f,
                    crack.Rotation, eyeOrigin, scale * 0.5f, SpriteEffects.None, 0f);
            }
        }

        private void DrawFloatingEyes(SpriteBatch sb)
        {
            Texture2D eyeTex;
            try
            {
                eyeTex = ModContent.Request<Texture2D>("GodEyeMod/Content/NPCs/GodEye/ScreenEye",
                    ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            catch
            {
                eyeTex = TextureAssets.Npc[NPCID.DemonEye].Value;
            }

            Vector2 eyeOrigin = new Vector2(eyeTex.Width / 2f, eyeTex.Height / 2f);

            foreach (var eye in floatingEyes)
            {
                float pulse = 1f + (float)Math.Sin(ChaosTimer * eye.PulseSpeed + eye.PulseOffset) * 0.1f;
                float scale = eye.Scale * pulse;
                float alpha = eye.Opacity;

                Vector2 pos = eye.Position;

                if (Main.rand.NextBool(50))
                    alpha *= Main.rand.NextFloat(0.3f, 1f);

                sb.Draw(eyeTex, pos, null, Color.Black * alpha * 0.5f,
                    eye.Rotation, eyeOrigin, scale * 1.5f, SpriteEffects.None, 0f);

                sb.Draw(eyeTex, pos, null, Color.Red * alpha * 0.3f,
                    eye.Rotation, eyeOrigin, scale * 1.15f, SpriteEffects.None, 0f);

                Color eyeColor = eye.LookingAtPlayer ? Color.Yellow : Color.Red;
                sb.Draw(eyeTex, pos, null, eyeColor * alpha,
                    eye.Rotation, eyeOrigin, scale, SpriteEffects.None, 0f);
            }
        }

        private void DrawBloodDrips(SpriteBatch sb, Texture2D pixel)
        {
            foreach (var drip in bloodDrips)
            {
                float alpha = Math.Min(1f, drip.Life / 60f);
                sb.Draw(pixel, new Rectangle(
                    (int)drip.Position.X, (int)drip.Position.Y,
                    (int)drip.Width, (int)drip.Length),
                    drip.DripColor * alpha);
            }
        }

        private void DrawVoidTendrils(SpriteBatch sb, Texture2D pixel)
        {
            foreach (var tendril in voidTendrils)
            {
                float alpha = Math.Min(1f, tendril.Life / 30f);
                int segments = 20;

                for (int s = 0; s < segments; s++)
                {
                    float t = (float)s / segments;
                    float segLen = tendril.Length * t;

                    float wave = (float)Math.Sin(t * 5f + ChaosTimer * 0.05f + tendril.WaveOffset) * 20f * t;

                    Vector2 dir = tendril.Angle.ToRotationVector2();
                    Vector2 perp = new Vector2(-dir.Y, dir.X);
                    Vector2 pos = tendril.Origin + dir * segLen + perp * wave;

                    float segWidth = tendril.Width * (1f - t * 0.7f);
                    Color col = Color.Lerp(Color.Black, Color.DarkRed, t * 0.3f) * alpha;

                    sb.Draw(pixel, new Rectangle(
                        (int)(pos.X - segWidth / 2), (int)(pos.Y - segWidth / 2),
                        (int)segWidth, (int)segWidth), col);
                }
            }
        }

        private void DrawFallingChunks(SpriteBatch sb, Texture2D pixel)
        {
            foreach (var chunk in fallingChunks)
            {
                float alpha = Math.Min(1f, chunk.Life / 40f);
                sb.Draw(pixel, chunk.Position, new Rectangle(0, 0, 1, 1),
                    chunk.ChunkColor * alpha, chunk.Rotation,
                    new Vector2(0.5f, 0.5f), new Vector2(chunk.Size, chunk.Size),
                    SpriteEffects.None, 0f);

                sb.Draw(pixel, chunk.Position, new Rectangle(0, 0, 1, 1),
                    Color.Black * alpha * 0.4f, chunk.Rotation,
                    new Vector2(0.5f, 0.5f), new Vector2(chunk.Size + 3, chunk.Size + 3),
                    SpriteEffects.None, 0f);
            }
        }

        private void DrawGlitchTexts(SpriteBatch sb)
        {
            foreach (var t in glitchTexts)
            {
                float alpha = Math.Min(1f, t.Life / 20f);
                Color col = t.TextColor * alpha;

                Vector2 pos = t.Position;
                if (Main.rand.NextBool(15))
                    pos += Main.rand.NextVector2Circular(8, 4);

                if (chromaticAberration > 5f && Main.rand.NextBool(3))
                {
                    Utils.DrawBorderString(sb, t.Text, pos + new Vector2(-2, 0), Color.Red * alpha * 0.3f, t.Scale);
                    Utils.DrawBorderString(sb, t.Text, pos + new Vector2(2, 0), Color.Blue * alpha * 0.3f, t.Scale);
                }

                Utils.DrawBorderString(sb, t.Text, pos, col, t.Scale);
            }
        }

        private void DrawStaticNoise(SpriteBatch sb, Texture2D pixel)
        {
            int count = (int)(staticNoise * 1000);
            for (int i = 0; i < count; i++)
            {
                int x = Main.rand.Next(Main.screenWidth);
                int y = Main.rand.Next(Main.screenHeight);
                int w = Main.rand.Next(1, (int)(12 * staticNoise) + 2);
                int h = Main.rand.Next(1, 3);
                float a = Main.rand.NextFloat(0.03f, 0.2f) * staticNoise;
                Color c = Main.rand.NextBool(8) ? Color.Red : Color.White;
                sb.Draw(pixel, new Rectangle(x, y, w, h), c * a);
            }
        }

        private void DrawChromaticEdges(SpriteBatch sb, Texture2D pixel)
        {
            float size = Math.Min(chromaticAberration * 4f, 80f);
            float intensity = Math.Min(1f, chromaticAberration / 15f) * 0.2f;
            sb.Draw(pixel, new Rectangle(0, 0, (int)size, Main.screenHeight), Color.Red * intensity);
            sb.Draw(pixel, new Rectangle(Main.screenWidth - (int)size, 0, (int)size, Main.screenHeight), Color.Blue * intensity);
            sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, (int)size), Color.Green * intensity * 0.7f);
            sb.Draw(pixel, new Rectangle(0, Main.screenHeight - (int)size, Main.screenWidth, (int)size), Color.Magenta * intensity * 0.7f);
        }

        private void DrawGlitchBands(SpriteBatch sb, Texture2D pixel)
        {
            float progress = Math.Min(1f, (float)ChaosTimer / MaxChaosTime);
            if (progress < 0.15f) return;

            int count = (int)(progress * 10);
            for (int i = 0; i < count; i++)
            {
                if (!Main.rand.NextBool(4)) continue;
                int y = Main.rand.Next(Main.screenHeight);
                int h = Main.rand.Next(2, (int)(35 * progress) + 3);
                int xOff = Main.rand.Next(-50, 50);
                Color c = Main.rand.NextBool(5) ? Color.Red * 0.15f : Color.Black * Main.rand.NextFloat(0.05f, 0.25f);
                sb.Draw(pixel, new Rectangle(xOff, y, Main.screenWidth + 100, h), c);
            }
        }

        private void DrawScanLines(SpriteBatch sb, Texture2D pixel)
        {
            float progress = Math.Min(1f, (float)ChaosTimer / MaxChaosTime);
            if (progress < 0.1f) return;

            int offset = (int)(Main.GameUpdateCount * 2) % 4;
            for (int y = offset; y < Main.screenHeight; y += 3)
            {
                sb.Draw(pixel, new Rectangle(0, y, Main.screenWidth, 1), Color.Black * 0.04f * progress);
            }
        }

        private void DrawVignette(SpriteBatch sb, Texture2D pixel, float strength)
        {
            if (strength < 0.01f) return;
            int layers = 8;
            for (int i = 0; i < layers; i++)
            {
                float ratio = (float)i / layers;
                int margin = (int)(Math.Min(Main.screenWidth, Main.screenHeight) * 0.5f * (1f - ratio));
                float alpha = strength * ratio * 0.15f;
                sb.Draw(pixel, new Rectangle(0, 0, margin, Main.screenHeight), Color.Black * alpha);
                sb.Draw(pixel, new Rectangle(Main.screenWidth - margin, 0, margin, Main.screenHeight), Color.Black * alpha);
                sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, margin), Color.Black * alpha);
                sb.Draw(pixel, new Rectangle(0, Main.screenHeight - margin, Main.screenWidth, margin), Color.Black * alpha);
            }
        }

        private void DrawCrashScreen(SpriteBatch sb, Texture2D pixel)
        {
            sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), new Color(0, 0, 50));

            if (crashTimer < 60) return;

            float y = 30;
            for (int i = 0; i < Math.Min(currentCrashLine, crashMessages.Length); i++)
            {
                string line = crashMessages[i];
                Color col = Color.White;

                if (line.Contains("Exception") || line.Contains("FATAL") || line.Contains("Error"))
                    col = Color.Red;
                else if (line.Contains("CORRUPTED") || line.Contains("IMPOSSIBLE"))
                    col = Color.OrangeRed;
                else if (line.Contains("I̶") || line.Contains("RETURN"))
                    col = Color.DarkRed;
                else if (line.Contains("did you"))
                    col = Color.Yellow;
                else if (line.Contains("Stack") || line.Contains("Additional"))
                    col = Color.LightBlue;

                Vector2 pos = new Vector2(35, y);
                if (Main.rand.NextBool(80))
                    pos.X += Main.rand.Next(-3, 3);

                Utils.DrawBorderString(sb, line, pos, col * crashTextAlpha, 0.8f);
                y += 20;
            }

            if (currentCrashLine < crashMessages.Length && Main.GameUpdateCount % 30 < 15)
            {
                sb.Draw(pixel, new Rectangle(35, (int)y, 8, 16), Color.White);
            }

            if (KickTimer > 0 && KickTimer < 90)
            {
                float fade = 1f - KickTimer / 90f;
                sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * fade);
            }

            for (int i = 0; i < 30; i++)
            {
                int nx = Main.rand.Next(Main.screenWidth);
                int ny = Main.rand.Next(Main.screenHeight);
                sb.Draw(pixel, new Rectangle(nx, ny, Main.rand.Next(1, 4), 1), Color.White * 0.02f);
            }
        }

        public override void ModifyScreenPosition()
        {
            if (!DeathChaosActive) return;

            if (screamerActive && screamerShake > 0.1f)
            {
                Main.screenPosition += Main.rand.NextVector2Circular(screamerShake, screamerShake);
                return;
            }

            if (crashScreenActive) return;

            if (screenShake > 0.1f)
                Main.screenPosition += Main.rand.NextVector2Circular(screenShake, screenShake);

            float progress = Math.Min(1f, (float)ChaosTimer / MaxChaosTime);
            if (progress > 0.2f)
            {
                Vector2 pull = (DeathPosition - Main.LocalPlayer.Center).SafeNormalize(Vector2.Zero);
                float str = (progress - 0.2f) / 0.8f * 10f;
                Main.screenPosition += pull * str * (float)Math.Sin(ChaosTimer * 0.08f);
            }
        }

        private void SpawnLoot()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            Player player = Main.LocalPlayer;
            Vector2 lootPos = (player != null && player.active) ? player.Center : DeathPosition;
            var source = new EntitySource_WorldEvent();

            if (Main.expertMode)
            {
                int bagType = ModContent.ItemType<GodEyeBag>();
                if (bagType > 0)
                {
                    bool given = TryGiveItem(player, bagType, 1);

                    if (!given)
                    {
                        int itemIndex = Item.NewItem(source, lootPos, Vector2.Zero, bagType);

                        if (itemIndex >= 0 && itemIndex < Main.maxItems && Main.item[itemIndex].active)
                        {
                            given = true;
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, itemIndex);
                        }
                    }

                    if (!given)
                    {
                        GodEyeLootSaveSystem.PendingBagDrop = true;
                    }
                }
            }
            else
            {
                GodEyeLootSaveSystem.PendingNormalLoot = true;
            }

            GodEyeLootSaveSystem.PendingCoins = true;
        }

        private bool TryGiveItem(Player player, int type, int stack = 1)
        {
            if (player == null || !player.active) return false;
            if (type <= 0) return false;

            for (int i = 0; i < 50; i++)
            {
                if (player.inventory[i].type == type &&
                    player.inventory[i].stack < player.inventory[i].maxStack)
                {
                    int canAdd = player.inventory[i].maxStack - player.inventory[i].stack;
                    int toAdd = Math.Min(canAdd, stack);
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

        private void SpawnItem(IEntitySource source, Vector2 pos, int type, int stack = 1)
        {
            if (type <= 0) return;
            int itemIndex = Item.NewItem(source, pos, Vector2.Zero, type, stack);
            if (Main.netMode == NetmodeID.Server && itemIndex >= 0)
                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, itemIndex);
        }

        private void ResetAll()
        {
            DeathChaosActive = false;
            crashScreenActive = false;
            screamerActive = false;
            ChaosTimer = 0;
            KickTimer = -1;

            eyeCracks.Clear();
            floatingEyes.Clear();
            fallingChunks.Clear();
            glitchTexts.Clear();
            bloodDrips.Clear();
            voidTendrils.Clear();

            if (Main.netMode != NetmodeID.Server)
            {
                Main.GameZoomTarget = 1f;
                Main.musicVolume = 1f;
            }

            Player p = Main.LocalPlayer;
            if (p.active && p.gravDir < 0)
                p.gravDir = 1f;
        }
    }
}