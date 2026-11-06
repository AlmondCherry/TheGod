using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content.NPCs.GodEye.summon
{
    public class GodEyeSummonSystem : ModSystem
    {
        public static bool SummonSequenceActive { get; private set; }
        public static int SequenceTimer { get; private set; }
        public static int SequencePhase { get; private set; }
        public static Vector2 SummonWorldPosition { get; private set; }
        public static Player TargetPlayer { get; private set; }

        public const int PHASE_SILENCE_START = 0;
        public const int PHASE_SILENCE_END = 240;
        public const int PHASE_GLITCH_START = 240;
        public const int PHASE_GLITCH_END = 540;
        public const int PHASE_RIFT_START = 540;
        public const int PHASE_RIFT_END = 840;
        public const int PHASE_EYE_START = 840;
        public const int PHASE_EYE_END = 1200;
        public const int PHASE_AWAKENING_START = 1200;
        public const int PHASE_AWAKENING_END = 1500;
        public const int SEQUENCE_TOTAL = 1500;

        public static List<GlitchBlock> ActiveGlitches = new List<GlitchBlock>();
        public static List<PersistentGlitch> PersistentGlitches = new List<PersistentGlitch>();
        public static float ScreenShakeIntensity;
        public static float DarknessOverlay;
        public static float GlitchIntensity;
        public static float RiftScale;
        public static float EyeOpenAmount;
        public static float FlashIntensity;
        public static bool EyeSpawned;

        public static bool ScreenTearActive;
        public static float ScreenTearOffset;
        public static int ScreenTearY;

        public static bool HealthBarCorrupted;
        public static float UIShakeIntensity;
        public static bool InvertColors;
        public static int InvertTimer;

        private static int _soundGlitchCooldown;
        private static float _savedMusicVolume;

        private static readonly string[] GlitchMessages = new string[]
        {
            "[System] Memory access violation at 0x00000000",
            "[World] Chunk corruption detected...",
            "[Entity] Player reference NULL",
            "[Core] Reality anchor destabilized",
            "[???] I SEE YOU",
            "[System] Save file compromised",
            "[World] Tile ID overflow — impossible value",
            "[Core] Fourth wall integrity: 0%",
            "[???] You were never the player.",
            "[System] He was always here.",
            "[Error] Cannot kill what wrote you.",
            "[???] Every block you placed — I watched.",
            "[World] Generating observer... done.",
            "[Core] Who is playing whom?",
            "[FATAL] Reality.dll has stopped responding",
            "[???] Close the game. It won't help.",
            "[System] Your save file is being read by another entity",
            "[Error] Player.soul not found",
            "[???] I am the game.",
            "[Core] Unhandled exception in Universe.Run()",
            "[???] Did you think the Moon Lord was the end?",
            "[System] Terraria.exe is not responding...",
            "[???] Look behind you.",
            "[FATAL] Stack overflow in Reality.cs line ∞",
        };

        private static int lastMessageIndex = -1;
        private static int messageTimer = 0;

        public static void StartSummonSequence(Player player)
        {
            if (SummonSequenceActive) return;

            SummonSequenceActive = true;
            SequenceTimer = 0;
            SequencePhase = 0;
            TargetPlayer = player;
            SummonWorldPosition = player.Center - new Vector2(0, 400);
            ScreenShakeIntensity = 0f;
            DarknessOverlay = 0f;
            GlitchIntensity = 0f;
            RiftScale = 0f;
            EyeOpenAmount = 0f;
            FlashIntensity = 0f;
            EyeSpawned = false;
            ScreenTearActive = false;
            HealthBarCorrupted = false;
            UIShakeIntensity = 0f;
            InvertColors = false;
            InvertTimer = 0;
            ActiveGlitches.Clear();
            PersistentGlitches.Clear();
            lastMessageIndex = -1;
            messageTimer = 0;
            _soundGlitchCooldown = 0;

            if (Main.netMode != NetmodeID.Server)
            {
                _savedMusicVolume = Main.musicVolume;
            }
        }

        public override void PostUpdateEverything()
        {
            if (!SummonSequenceActive) return;

            SequenceTimer++;

            if (Main.netMode != NetmodeID.Server && !EyeSpawned)
            {
                Main.musicVolume = 0f;
                for (int i = 0; i < Main.musicFade.Length; i++)
                {
                    Main.musicFade[i] = 0f;
                }
            }

            if (TargetPlayer != null && TargetPlayer.active)
            {
                SummonWorldPosition = TargetPlayer.Center - new Vector2(0, 500);
            }

            UpdatePhase();

            if (InvertTimer > 0)
            {
                InvertTimer--;
                if (InvertTimer <= 0) InvertColors = false;
            }

            if (_soundGlitchCooldown > 0) _soundGlitchCooldown--;

            if (SequenceTimer >= SEQUENCE_TOTAL)
            {
                FinishSequence();
            }
        }

        private void UpdatePhase()
        {
            if (SequenceTimer >= PHASE_SILENCE_START && SequenceTimer < PHASE_SILENCE_END)
            {
                SequencePhase = 1;
                float progress = (float)(SequenceTimer - PHASE_SILENCE_START) / (PHASE_SILENCE_END - PHASE_SILENCE_START);

                DarknessOverlay = MathHelper.Lerp(0f, 0.75f, EaseInCubic(progress));

                if (progress > 0.5f)
                {
                    SlowDownEntities(progress);
                }

                if (progress > 0.7f)
                {
                    ScreenShakeIntensity = MathHelper.Lerp(0f, 2f, (progress - 0.7f) / 0.3f);

                    if (Main.rand.NextBool(10))
                    {
                        SpawnPersistentGlitch(0.3f);
                    }
                }

                if (SequenceTimer == PHASE_SILENCE_END - 30)
                {
                    PlayGlitchSound(0.5f);
                }
            }

            if (SequenceTimer >= PHASE_GLITCH_START && SequenceTimer < PHASE_GLITCH_END)
            {
                SequencePhase = 2;
                float progress = (float)(SequenceTimer - PHASE_GLITCH_START) / (PHASE_GLITCH_END - PHASE_GLITCH_START);

                DarknessOverlay = MathHelper.Lerp(0.75f, 0.88f, progress);
                GlitchIntensity = MathHelper.Lerp(0f, 1f, EaseInOutQuad(progress));
                ScreenShakeIntensity = MathHelper.Lerp(2f, 6f, progress);
                UIShakeIntensity = MathHelper.Lerp(0f, 5f, progress);

                if (SequenceTimer % Math.Max(1, 4 - (int)(progress * 3)) == 0)
                {
                    SpawnGlitchBlocks(progress);
                }

                if (SequenceTimer % Math.Max(1, 15 - (int)(progress * 12)) == 0)
                {
                    SpawnPersistentGlitch(progress);
                }

                messageTimer++;
                int messageRate = Math.Max(1, 35 - (int)(progress * 30));
                if (messageTimer > messageRate)
                {
                    messageTimer = 0;
                    SendGlitchMessage();
                }

                if (progress > 0.5f && Main.rand.NextBool(20))
                {
                    ScreenTearActive = true;
                    ScreenTearOffset = Main.rand.NextFloat(-20, 20) * progress;
                    ScreenTearY = Main.rand.Next(Main.screenHeight);
                }
                else if (Main.rand.NextBool(5))
                {
                    ScreenTearActive = false;
                }

                if (progress > 0.6f && Main.rand.NextBool(60))
                {
                    InvertColors = true;
                    InvertTimer = Main.rand.Next(5, 20);
                }

                if (SequenceTimer % 15 == 0)
                {
                    PlayGlitchSound(progress);
                }

                if (Main.rand.NextBool(Math.Max(1, 5 - (int)(progress * 4))))
                {
                    Lighting.Clear();
                }

                HealthBarCorrupted = progress > 0.4f;
            }

            if (SequenceTimer >= PHASE_RIFT_START && SequenceTimer < PHASE_RIFT_END)
            {
                SequencePhase = 3;
                float progress = (float)(SequenceTimer - PHASE_RIFT_START) / (PHASE_RIFT_END - PHASE_RIFT_START);

                DarknessOverlay = MathHelper.Lerp(0.88f, 0.95f, progress);
                GlitchIntensity = MathHelper.Lerp(1f, 0.6f, progress);
                RiftScale = MathHelper.Lerp(0f, 1f, EaseOutBack(progress));
                ScreenShakeIntensity = MathHelper.Lerp(6f, 10f, progress);
                UIShakeIntensity = MathHelper.Lerp(5f, 10f, progress);

                if (SequenceTimer % 8 == 0)
                {
                    SpawnPersistentGlitch(progress);
                }

                if (SequenceTimer % 2 == 0)
                {
                    SpawnRiftParticles(progress);
                }

                if (SequenceTimer % 8 == 0)
                {
                    ShakeNearbyTiles(progress);
                }

                if (Main.rand.NextBool(8))
                {
                    ScreenTearActive = true;
                    ScreenTearOffset = Main.rand.NextFloat(-40, 40) * progress;
                    ScreenTearY = Main.rand.Next(Main.screenHeight);
                }

                if (SequenceTimer == PHASE_RIFT_START)
                {
                    SoundEngine.PlaySound(SoundID.DD2_EtherianPortalOpen with
                    {
                        Volume = 2f,
                        Pitch = -0.8f
                    });
                }

                if (SequenceTimer % 25 == 0)
                {
                    PlayGlitchSound(0.5f + progress * 0.5f);
                }

                if (SequenceTimer == PHASE_RIFT_END - 20)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath58 with
                    {
                        Volume = 2f,
                        Pitch = -1f
                    });
                }
            }

            if (SequenceTimer >= PHASE_EYE_START && SequenceTimer < PHASE_EYE_END)
            {
                SequencePhase = 4;
                float progress = (float)(SequenceTimer - PHASE_EYE_START) / (PHASE_EYE_END - PHASE_EYE_START);

                DarknessOverlay = 0.95f;
                RiftScale = 1f;
                EyeOpenAmount = MathHelper.Lerp(0f, 1f, EaseInOutQuad(progress));
                GlitchIntensity = MathHelper.Lerp(0.6f, 0.3f, progress);

                ScreenShakeIntensity = MathHelper.Lerp(10f, 3f, progress);
                UIShakeIntensity = MathHelper.Lerp(10f, 2f, progress);

                if (SequenceTimer % 20 == 0)
                {
                    SpawnPersistentGlitch(0.3f);
                }

                if (SequenceTimer == PHASE_EYE_START)
                {
                    SoundEngine.PlaySound(SoundID.Zombie105 with
                    {
                        Volume = 2.5f,
                        Pitch = -1f
                    });
                }

                if (SequenceTimer == PHASE_EYE_START + 90)
                {
                    Main.NewText("I have always been here.", new Color(180, 0, 0));
                }

                if (SequenceTimer == PHASE_EYE_START + 180)
                {
                    Main.NewText("Every block. Every death. Every save.", new Color(200, 0, 0));
                }

                if (SequenceTimer == PHASE_EYE_START + 270)
                {
                    Main.NewText("You freed me.", new Color(255, 0, 0));
                    PlayGlitchSound(1f);
                }

                ScreenTearActive = false;
            }

            if (SequenceTimer >= PHASE_AWAKENING_START && SequenceTimer < PHASE_AWAKENING_END)
            {
                SequencePhase = 5;
                float progress = (float)(SequenceTimer - PHASE_AWAKENING_START) / (PHASE_AWAKENING_END - PHASE_AWAKENING_START);

                if (progress < 0.15f)
                {
                    float shrinkT = progress / 0.15f;
                    EyeOpenAmount = MathHelper.Lerp(1f, 0.4f, EaseInCubic(shrinkT));
                    RiftScale = MathHelper.Lerp(1f, 0.2f, EaseInCubic(shrinkT));
                    DarknessOverlay = 0.95f;
                    ScreenShakeIntensity = MathHelper.Lerp(3f, 15f, shrinkT);
                    GlitchIntensity = MathHelper.Lerp(0.3f, 1f, shrinkT);
                    FlashIntensity = 0f;

                    if (SequenceTimer == PHASE_AWAKENING_START)
                    {
                        SoundEngine.PlaySound(SoundID.DD2_EtherianPortalIdleLoop with
                        {
                            Volume = 2f,
                            Pitch = -1f
                        });
                    }
                }
                else if (progress < 0.35f)
                {
                    float flashUpT = (progress - 0.15f) / 0.2f;
                    EyeOpenAmount = MathHelper.Lerp(0.4f, 0f, EaseInCubic(flashUpT));
                    RiftScale = MathHelper.Lerp(0.2f, 0f, EaseInCubic(flashUpT));
                    DarknessOverlay = MathHelper.Lerp(0.95f, 0f, flashUpT);
                    FlashIntensity = MathHelper.Lerp(0f, 1f, EaseInQuad(flashUpT));
                    ScreenShakeIntensity = MathHelper.Lerp(15f, 25f, flashUpT);
                    GlitchIntensity = MathHelper.Lerp(1f, 0f, flashUpT);

                    if (flashUpT > 0.5f)
                    {
                        PersistentGlitches.Clear();
                    }

                    if (SequenceTimer == PHASE_AWAKENING_START + (int)(0.15f * (PHASE_AWAKENING_END - PHASE_AWAKENING_START)))
                    {
                        SoundEngine.PlaySound(SoundID.DD2_BetsyFireballImpact with
                        {
                            Volume = 3f,
                            Pitch = -1f
                        });
                    }
                }
                else if (progress < 0.45f)
                {
                    EyeOpenAmount = 0f;
                    RiftScale = 0f;
                    DarknessOverlay = 0f;
                    FlashIntensity = 1f;
                    ScreenShakeIntensity = 25f;
                    GlitchIntensity = 0f;
                    PersistentGlitches.Clear();
                    ActiveGlitches.Clear();
                    ScreenTearActive = false;
                    InvertColors = false;
                    HealthBarCorrupted = false;
                }
                else if (progress < 0.55f)
                {
                    float fadeStartT = (progress - 0.45f) / 0.1f;
                    FlashIntensity = MathHelper.Lerp(1f, 0.6f, fadeStartT);
                    ScreenShakeIntensity = MathHelper.Lerp(25f, 12f, fadeStartT);

                    if (!EyeSpawned)
                    {
                        SpawnTheGod();
                        SpawnAwakeningBurst();
                        EyeSpawned = true;

                        if (Main.netMode != NetmodeID.Server)
                        {
                            Main.musicVolume = _savedMusicVolume;
                        }
                    }
                }
                else
                {
                    float fadeOutT = (progress - 0.55f) / 0.45f;
                    FlashIntensity = MathHelper.Lerp(0.6f, 0f, EaseOutQuad(fadeOutT));
                    ScreenShakeIntensity = MathHelper.Lerp(12f, 0f, EaseOutQuad(fadeOutT));
                }
            }

            UpdateGlitchBlocks();
            UpdatePersistentGlitches();
        }

        private void PlayGlitchSound(float intensity)
        {
            if (_soundGlitchCooldown > 0 || Main.netMode == NetmodeID.Server) return;
            _soundGlitchCooldown = 10;

            int soundChoice = Main.rand.Next(5);
            switch (soundChoice)
            {
                case 0:
                    SoundEngine.PlaySound(SoundID.NPCDeath55 with
                    {
                        Volume = 0.3f + intensity * 0.5f,
                        Pitch = Main.rand.NextFloat(-1f, 1f),
                        PitchVariance = 0.5f
                    });
                    break;
                case 1:
                    SoundEngine.PlaySound(SoundID.NPCDeath58 with
                    {
                        Volume = 0.2f + intensity * 0.3f,
                        Pitch = Main.rand.NextFloat(-1f, 0f)
                    });
                    break;
                case 2:
                    SoundEngine.PlaySound(SoundID.Zombie105 with
                    {
                        Volume = 0.3f + intensity * 0.4f,
                        Pitch = Main.rand.NextFloat(-1f, -0.5f)
                    });
                    break;
                case 3:
                    SoundEngine.PlaySound(SoundID.Item122 with
                    {
                        Volume = 0.2f + intensity * 0.3f,
                        Pitch = -1f
                    });
                    break;
                case 4:
                    SoundEngine.PlaySound(SoundID.NPCDeath62 with
                    {
                        Volume = intensity * 0.5f,
                        Pitch = Main.rand.NextFloat(-1f, 1f)
                    });
                    break;
            }
        }

        private void SlowDownEntities(float intensity)
        {
            float slowFactor = 1f - intensity * 0.4f;

            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.boss)
                {
                    npc.velocity *= slowFactor;
                }
            }

            foreach (Projectile proj in Main.projectile)
            {
                if (proj.active)
                {
                    proj.velocity *= slowFactor;
                }
            }
        }

        private void SpawnGlitchBlocks(float intensity)
        {
            if (TargetPlayer == null) return;

            int count = (int)(intensity * 8) + 1;
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = TargetPlayer.Center + Main.rand.NextVector2Circular(900, 700);
                ActiveGlitches.Add(new GlitchBlock
                {
                    Position = pos,
                    Size = Main.rand.NextFloat(8, 50) * intensity,
                    Lifetime = Main.rand.Next(3, 15),
                    MaxLifetime = 15,
                    Color = Main.rand.Next(5) switch
                    {
                        0 => new Color(255, 0, 0, 200),
                        1 => new Color(0, 255, 255, 150),
                        2 => new Color(255, 255, 255, 100),
                        3 => new Color(255, 0, 255, 180),
                        _ => new Color(0, 255, 0, 120),
                    },
                    Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
                    GlitchType = Main.rand.Next(5)
                });
            }
        }

        public void SpawnPersistentGlitch(float intensity)
        {
            if (PersistentGlitches.Count > 150) return;

            int count = Main.rand.Next(1, 4);
            for (int i = 0; i < count; i++)
            {
                PersistentGlitches.Add(new PersistentGlitch
                {
                    ScreenPosition = new Vector2(
                        Main.rand.Next(-50, Main.screenWidth + 50),
                        Main.rand.Next(-50, Main.screenHeight + 50)),
                    Size = new Vector2(
                        Main.rand.NextFloat(5, 80) * intensity,
                        Main.rand.NextFloat(2, 40) * intensity),
                    Color = Main.rand.Next(6) switch
                    {
                        0 => new Color(255, 0, 0, Main.rand.Next(50, 200)),
                        1 => new Color(0, 255, 255, Main.rand.Next(30, 150)),
                        2 => new Color(255, 0, 255, Main.rand.Next(50, 180)),
                        3 => new Color(0, 255, 0, Main.rand.Next(30, 120)),
                        4 => new Color(255, 255, 0, Main.rand.Next(30, 100)),
                        _ => new Color(255, 255, 255, Main.rand.Next(20, 100)),
                    },
                    Opacity = Main.rand.NextFloat(0.2f, 0.8f) * intensity,
                    Type = Main.rand.Next(6),
                    FlickerRate = Main.rand.Next(3, 30),
                    FlickerTimer = 0,
                    Visible = true,
                    Permanent = Main.rand.NextBool(3),
                    Angle = Main.rand.NextFloat(-0.3f, 0.3f),
                    CorruptedText = Main.rand.NextBool(5) ? GetRandomCorruptedText() : null
                });
            }
        }

        private string GetRandomCorruptedText()
        {
            string[] texts = new string[]
            {
                "NaN", "NULL", "ERR", "0x00", "???", "VOID",
                "undefined", "nil", "∞", "404", "FATAL",
                "HELP", "HE SEES", "RUN", "NO EXIT"
            };
            return texts[Main.rand.Next(texts.Length)];
        }

        private void SpawnRiftParticles(float progress)
        {
            if (TargetPlayer == null) return;

            int count = (int)(progress * 20) + 3;
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(600 - progress * 400, 600 - progress * 400);
                Vector2 pos = SummonWorldPosition + offset;
                Vector2 vel = (SummonWorldPosition - pos).SafeNormalize(Vector2.Zero) * (3f + progress * 8f);

                int dustType = Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch;
                Dust dust = Dust.NewDustDirect(pos, 1, 1, dustType);
                dust.velocity = vel;
                dust.noGravity = true;
                dust.scale = 1.5f + progress * 3f;
                dust.color = new Color(100, 0, 200, 0);
            }
        }

        private void ShakeNearbyTiles(float progress)
        {
            if (TargetPlayer == null || Main.netMode == NetmodeID.Server) return;

            int range = (int)(10 + progress * 25);
            Point center = TargetPlayer.Center.ToTileCoordinates();

            for (int attempt = 0; attempt < 8; attempt++)
            {
                int x = center.X + Main.rand.Next(-range, range);
                int y = center.Y + Main.rand.Next(-range, range);

                if (WorldGen.InWorld(x, y) && Main.tile[x, y].HasTile)
                {
                    WorldGen.KillTile_MakeTileDust(x, y, Main.tile[x, y]);
                }
            }
        }

        private void SendGlitchMessage()
        {
            int index;
            do
            {
                index = Main.rand.Next(GlitchMessages.Length);
            } while (index == lastMessageIndex && GlitchMessages.Length > 1);

            lastMessageIndex = index;

            Color messageColor = Main.rand.Next(4) switch
            {
                0 => new Color(255, 0, 0),
                1 => new Color(150, 150, 150),
                2 => new Color(255, 0, 255),
                _ => new Color(0, 255, 0),
            };

            string message = GlitchMessages[index];
            if (Main.rand.NextBool(3))
            {
                message = CorruptString(message);
            }

            Main.NewText(message, messageColor);
        }

        private string CorruptString(string input)
        {
            char[] chars = input.ToCharArray();
            int corruptions = Main.rand.Next(3, 10);

            for (int i = 0; i < corruptions; i++)
            {
                int pos = Main.rand.Next(chars.Length);
                chars[pos] = (char)Main.rand.Next(33, 126);
            }

            return new string(chars);
        }

        private void SpawnAwakeningBurst()
        {
            if (Main.netMode == NetmodeID.Server) return;

            for (int i = 0; i < 300; i++)
            {
                Vector2 vel = Main.rand.NextVector2CircularEdge(20, 20) * Main.rand.NextFloat(0.3f, 1f);
                Dust dust = Dust.NewDustDirect(SummonWorldPosition, 1, 1, DustID.PurpleTorch);
                dust.velocity = vel;
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(2f, 5f);
                dust.color = new Color(200, 0, 255, 0);
            }

            for (int i = 0; i < 150; i++)
            {
                Vector2 vel = Main.rand.NextVector2CircularEdge(25, 25) * Main.rand.NextFloat(0.2f, 1f);
                Dust dust = Dust.NewDustDirect(SummonWorldPosition, 1, 1, DustID.Shadowflame);
                dust.velocity = vel;
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(2.5f, 6f);
            }

            for (int i = 0; i < 40; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(12, 12);
                Gore gore = Gore.NewGoreDirect(
                    new EntitySource_Misc("GodEyeAwakening"),
                    SummonWorldPosition,
                    vel,
                    Main.rand.Next(61, 64)
                );
                gore.scale = Main.rand.NextFloat(0.5f, 2f);
            }
        }

        private void SpawnTheGod()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (TargetPlayer == null) return;

            int bossType = ModContent.NPCType<GodEyeBoss>();
            int npcIndex = NPC.NewNPC(
                new EntitySource_Misc("GodEyeAwakening"),
                (int)SummonWorldPosition.X,
                (int)SummonWorldPosition.Y,
                bossType,
                Target: TargetPlayer.whoAmI
            );

            if (npcIndex < Main.maxNPCs && Main.npc[npcIndex].active)
            {
                Main.npc[npcIndex].netUpdate = true;
            }

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.NewText("The All-Seeing has awakened.", new Color(180, 0, 220));
            }
            else
            {
                Terraria.Chat.ChatHelper.BroadcastChatMessage(
                    Terraria.Localization.NetworkText.FromLiteral("The All-Seeing has awakened."),
                    new Color(180, 0, 220)
                );
            }
        }

        private void UpdateGlitchBlocks()
        {
            for (int i = ActiveGlitches.Count - 1; i >= 0; i--)
            {
                ActiveGlitches[i].Lifetime--;
                if (ActiveGlitches[i].Lifetime <= 0)
                {
                    ActiveGlitches.RemoveAt(i);
                }
            }
        }

        private void UpdatePersistentGlitches()
        {
            for (int i = PersistentGlitches.Count - 1; i >= 0; i--)
            {
                PersistentGlitch pg = PersistentGlitches[i];

                if (!pg.Permanent)
                {
                    pg.FlickerTimer++;
                    if (pg.FlickerTimer >= pg.FlickerRate)
                    {
                        pg.FlickerTimer = 0;
                        pg.Visible = !pg.Visible;
                    }
                }

                pg.ScreenPosition += new Vector2(
                    Main.rand.NextFloat(-0.5f, 0.5f),
                    Main.rand.NextFloat(-0.3f, 0.3f)
                );
            }
        }

        private void FinishSequence()
        {
            SummonSequenceActive = false;
            SequenceTimer = 0;
            SequencePhase = 0;
            ScreenShakeIntensity = 0f;
            DarknessOverlay = 0f;
            GlitchIntensity = 0f;
            RiftScale = 0f;
            EyeOpenAmount = 0f;
            FlashIntensity = 0f;
            ScreenTearActive = false;
            HealthBarCorrupted = false;
            UIShakeIntensity = 0f;
            InvertColors = false;
            ActiveGlitches.Clear();
            PersistentGlitches.Clear();

            if (Main.netMode != NetmodeID.Server)
            {
                Main.musicVolume = _savedMusicVolume;
            }
        }

        private float EaseInCubic(float t) => t * t * t;
        private float EaseInQuad(float t) => t * t;
        private float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
        private float EaseInOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - (float)Math.Pow(-2f * t + 2f, 2) / 2f;
        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * (float)Math.Pow(t - 1, 3) + c1 * (float)Math.Pow(t - 1, 2);
        }
    }

    public class GlitchBlock
    {
        public Vector2 Position;
        public float Size;
        public int Lifetime;
        public int MaxLifetime;
        public Color Color;
        public float Rotation;
        public int GlitchType;
    }

    public class PersistentGlitch
    {
        public Vector2 ScreenPosition;
        public Vector2 Size;
        public Color Color;
        public float Opacity;
        public int Type;
        public int FlickerRate;
        public int FlickerTimer;
        public bool Visible;
        public bool Permanent;
        public float Angle;
        public string CorruptedText;
    }
}