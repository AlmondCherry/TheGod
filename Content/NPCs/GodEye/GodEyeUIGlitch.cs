using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.ID;

namespace GodEyeMod.Content.NPCs.GodEye
{
    public class GodEyeUIGlitch : ModSystem
    {
        private static float uiOffsetX = 0f;
        private static float uiOffsetY = 0f;
        private static float uiTargetOffsetX = 0f;
        private static float uiTargetOffsetY = 0f;
        private static int uiChangeTimer = 0;

        private struct ScreenEye
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Scale;
            public float Rotation;
            public float RotSpeed;
            public float Opacity;
            public float PulseSpeed;
            public float PulseOffset;
        }

        private static List<ScreenEye> screenEyes = new List<ScreenEye>();
        private static int eyeSpawnTimer = 0;

        private static int screamerTimer = 0;
        private static float screamerOpacity = 0f;

        private static bool cursorHijacked = false;
        private static int cursorHijackTimer = 0;
        private static Vector2 cursorHijackTarget = Vector2.Zero;
        private static Vector2 fakeCursorPos = Vector2.Zero;

        private static int worldControlTimer = 0;
        private static int rainControlTimer = 0;
        private static int windControlTimer = 0;
        private static int moonPhaseTimer = 0;

        private static bool wasBossActive = false;

        private static GodEyeConfig GetConfig()
        {
            try { return ModContent.GetInstance<GodEyeConfig>(); }
            catch { return null; }
        }

        public override void PostUpdateEverything()
        {
            bool deathChaos = GodEyeGlitchSystem.DeathChaosActive;
            bool currentlyActive = GodEyeGlitchSystem.BossActive || deathChaos;

            if (!currentlyActive)
            {
                if (wasBossActive)
                {
                    Main.GameZoomTarget = 1f;
                    wasBossActive = false;
                }

                uiOffsetX = MathHelper.Lerp(uiOffsetX, 0, 0.1f);
                uiOffsetY = MathHelper.Lerp(uiOffsetY, 0, 0.1f);
                screenEyes.Clear();
                screamerTimer = 0;
                screamerOpacity = 0f;
                cursorHijacked = false;
                cursorHijackTimer = 0;

                return;
            }

            wasBossActive = true;
            GodEyeConfig config = GetConfig();
            int phase = deathChaos ? 4 : GodEyeGlitchSystem.BossPhase;

            bool disableVisuals = config != null && config.DisableAmbientVisuals;

            if (!disableVisuals)
            {
                uiChangeTimer++;
                int changeRate = phase >= 3 ? 15 : (phase >= 2 ? 50 : 100);
                if (uiChangeTimer >= changeRate)
                {
                    uiChangeTimer = 0;
                    float maxOffset = phase >= 3 ? 80f : (phase >= 2 ? 30f : 8f);
                    uiTargetOffsetX = Main.rand.NextFloat(-maxOffset, maxOffset);
                    uiTargetOffsetY = Main.rand.NextFloat(-maxOffset, maxOffset);
                }

                uiOffsetX = MathHelper.Lerp(uiOffsetX, uiTargetOffsetX, 0.03f);
                uiOffsetY = MathHelper.Lerp(uiOffsetY, uiTargetOffsetY, 0.03f);

                if (phase >= 2 && Main.rand.NextBool(phase >= 3 ? 15 : 50))
                {
                    float jerk = phase >= 3 ? 50f : 20f;
                    uiOffsetX += Main.rand.NextFloat(-jerk, jerk);
                    uiOffsetY += Main.rand.NextFloat(-jerk, jerk);
                }
            }
            else
            {
                uiOffsetX = 0f;
                uiOffsetY = 0f;
            }

            if (!disableVisuals)
                UpdateScreenEyes(phase);
            else
                screenEyes.Clear();

            bool disableScreamer = config != null && config.DisableScreamer;
            if (!disableScreamer)
                UpdateScreamer(phase);
            else
            {
                screamerTimer = 0;
                screamerOpacity = 0f;
            }

            bool disableBossWeather = config != null && config.DisableBossWeatherControl;
            if (!disableBossWeather)
            {
                UpdateTimeSpeed(phase);
                UpdateWorldControl(phase, config);
            }
        }

        #region Time & World Control (Boss Fight Only)

        private void UpdateTimeSpeed(int phase)
        {
            double timeSpeed = phase switch
            {
                0 => 2, 1 => 8, 2 => 20, 3 => 50, _ => 100
            };

            if (GodEyeGlitchSystem.DeathChaosActive)
                timeSpeed = 100;

            Main.time += timeSpeed;

            if (Main.dayTime && Main.time >= 54000)
            {
                Main.dayTime = false;
                Main.time = 0;
            }
            else if (!Main.dayTime && Main.time >= 32400)
            {
                Main.dayTime = true;
                Main.time = 0;
            }
        }

        private void UpdateWorldControl(int phase, GodEyeConfig config)
        {
            worldControlTimer++;
            bool disableChat = config != null && config.DisableChatMessages;

            if (rainControlTimer > 0) rainControlTimer--;
            if (phase >= 1 && rainControlTimer <= 0 && Main.rand.NextBool(phase >= 3 ? 300 : 600))
            {
                rainControlTimer = 180;
                if (Main.raining)
                {
                    Main.StopRain();
                    if (Main.netMode != NetmodeID.Server && !disableChat)
                        Main.NewText("WEATHER.RAIN: disabled", Color.Gray);
                }
                else
                {
                    Main.StartRain();
                    Main.maxRaining = Main.rand.NextFloat(0.5f, 1f);
                    if (Main.netMode != NetmodeID.Server && !disableChat)
                        Main.NewText("WEATHER.RAIN: enabled (intensity=" + Main.maxRaining.ToString("F2") + ")", Color.Cyan);
                }
            }

            if (windControlTimer > 0) windControlTimer--;
            if (phase >= 1 && windControlTimer <= 0 && Main.rand.NextBool(phase >= 3 ? 180 : 480))
            {
                windControlTimer = 120;
                Main.windSpeedTarget = Main.rand.NextFloat(-1f, 1f);
                if (phase >= 3) Main.windSpeedTarget = Main.rand.NextFloat(-2f, 2f);
            }

            if (moonPhaseTimer > 0) moonPhaseTimer--;
            if (phase >= 2 && moonPhaseTimer <= 0 && Main.rand.NextBool(phase >= 3 ? 120 : 360))
            {
                moonPhaseTimer = 60;
                Main.moonPhase = Main.rand.Next(8);
            }

            if (phase >= 2 && Main.rand.NextBool(phase >= 3 ? 60 : 180))
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int starX = Main.rand.Next(100, Main.maxTilesX - 100) * 16;
                    int starY = Main.rand.Next(100, 300) * 16;
                    Projectile.NewProjectile(null, new Vector2(starX, starY),
                        new Vector2(Main.rand.NextFloat(-5, 5), 10f),
                        ProjectileID.FallingStar, 1000, 10f, Main.myPlayer);
                }
            }

            if (phase >= 3 && Main.rand.NextBool(600) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Player target = Main.player[Main.myPlayer];
                if (target.active && !target.dead)
                {
                    int[] glitchNPCs = { NPCID.DemonEye, NPCID.WanderingEye, NPCID.DemonEyeOwl, NPCID.ServantofCthulhu, NPCID.EyeofCthulhu };
                    int npcType = glitchNPCs[Main.rand.Next(glitchNPCs.Length)];
                    Vector2 spawnPos = target.Center + Main.rand.NextVector2CircularEdge(800, 800);
                    NPC.NewNPC(null, (int)spawnPos.X, (int)spawnPos.Y, npcType);
                    if (Main.netMode != NetmodeID.Server && !disableChat)
                        Main.NewText("NPC_SPAWN_OVERRIDE: type=" + npcType, Color.OrangeRed);
                }
            }

            if (phase >= 3 && Main.rand.NextBool(900))
            {
                if (Main.netMode != NetmodeID.Server && !disableChat)
                    Main.NewText("PHYSICS.GRAVITY: recalculating...", Color.Cyan);
            }

            if (phase >= 2 && Main.rand.NextBool(phase >= 3 ? 300 : 720))
            {
                Player target = Main.player[Main.myPlayer];
                if (target.active && !target.dead)
                {
                    int[] buffs = { BuffID.Swiftness, BuffID.Rage, BuffID.Wrath, BuffID.Endurance };
                    int buff = buffs[Main.rand.Next(buffs.Length)];
                    target.AddBuff(buff, 300);
                    if (Main.netMode != NetmodeID.Server && !disableChat)
                        Main.NewText("BUFF_INJECTION: " + Lang.GetBuffName(buff), Color.Lime);
                }
            }

            if (phase >= 2 && Main.rand.NextBool(phase >= 3 ? 120 : 360))
                Lighting.GlobalBrightness = Main.rand.NextFloat(0.3f, 1.2f);

            if (Math.Abs(Lighting.GlobalBrightness - 1f) > 0.05f && Main.rand.NextBool(10))
                Lighting.GlobalBrightness = MathHelper.Lerp(Lighting.GlobalBrightness, 1f, 0.02f);
        }

        #endregion

        #region Screamer

        private void UpdateScreamer(int phase)
        {
            if (screamerTimer > 0)
            {
                screamerTimer--;
                screamerOpacity = screamerTimer > 10 ? 1f : (float)screamerTimer / 10f;
                return;
            }

            screamerOpacity = 0f;
            if (phase < 3 && !GodEyeGlitchSystem.DeathChaosActive) return;

            int chance = GodEyeGlitchSystem.DeathChaosActive ? 600 : 8000;
            if (Main.rand.NextBool(chance))
            {
                screamerTimer = 25;
                screamerOpacity = 0f;
            }
        }

        #endregion

        #region Screen Eyes

        private void UpdateScreenEyes(int phase)
        {
            int maxEyes = phase switch { 0 => 2, 1 => 5, 2 => 12, 3 => 20, _ => GodEyeGlitchSystem.DeathChaosActive ? 35 : 20 };

            eyeSpawnTimer++;
            int spawnRate = phase >= 3 ? 5 : (phase >= 2 ? 15 : (phase >= 1 ? 40 : 80));
            if (GodEyeGlitchSystem.DeathChaosActive) spawnRate = 2;

            if (eyeSpawnTimer >= spawnRate && screenEyes.Count < maxEyes)
            {
                eyeSpawnTimer = 0;
                SpawnScreenEye(phase);
            }

            for (int i = screenEyes.Count - 1; i >= 0; i--)
            {
                ScreenEye eye = screenEyes[i];
                eye.Position += eye.Velocity;
                eye.Rotation += eye.RotSpeed;
                eye.Opacity = (float)(Math.Sin(Main.GameUpdateCount * eye.PulseSpeed + eye.PulseOffset) * 0.25 + 0.45);

                if (phase >= 3 || GodEyeGlitchSystem.DeathChaosActive)
                    eye.Opacity = Math.Min(eye.Opacity + 0.2f, 0.8f);

                bool oob = eye.Position.X < -200 || eye.Position.X > Main.screenWidth + 200 ||
                           eye.Position.Y < -200 || eye.Position.Y > Main.screenHeight + 200;
                if (oob) { screenEyes.RemoveAt(i); continue; }

                if ((phase >= 3 || GodEyeGlitchSystem.DeathChaosActive) && Main.rand.NextBool(90))
                    eye.Velocity = Main.rand.NextVector2Circular(2f, 2f);

                screenEyes[i] = eye;
            }
        }

        private void SpawnScreenEye(int phase)
        {
            ScreenEye eye = new ScreenEye();

            if (Main.rand.NextBool(3))
            {
                int side = Main.rand.Next(4);
                switch (side)
                {
                    case 0: eye.Position = new Vector2(Main.rand.Next(Main.screenWidth), -80); break;
                    case 1: eye.Position = new Vector2(Main.rand.Next(Main.screenWidth), Main.screenHeight + 80); break;
                    case 2: eye.Position = new Vector2(-80, Main.rand.Next(Main.screenHeight)); break;
                    case 3: eye.Position = new Vector2(Main.screenWidth + 80, Main.rand.Next(Main.screenHeight)); break;
                }
                Vector2 center = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
                eye.Velocity = (center - eye.Position).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.2f, 1.5f);
            }
            else
            {
                eye.Position = new Vector2(Main.rand.Next(Main.screenWidth), Main.rand.Next(Main.screenHeight));
                eye.Velocity = Main.rand.NextVector2Circular(0.8f, 0.8f);
            }

            float baseScale = phase >= 3 ? 1.2f : (phase >= 2 ? 0.8f : 0.5f);
            if (GodEyeGlitchSystem.DeathChaosActive) baseScale = 1.8f;

            eye.Scale = Main.rand.NextFloat(baseScale * 0.3f, baseScale * 1.3f);
            eye.Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            eye.RotSpeed = Main.rand.NextFloat(-0.02f, 0.02f);
            eye.Opacity = Main.rand.NextFloat(0.15f, 0.6f);
            eye.PulseSpeed = Main.rand.NextFloat(0.02f, 0.08f);
            eye.PulseOffset = Main.rand.NextFloat(MathHelper.TwoPi);

            screenEyes.Add(eye);
        }

        #endregion

        #region Interface Layers

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            bool active = GodEyeGlitchSystem.BossActive || GodEyeGlitchSystem.DeathChaosActive;
            if (!active) return;

            GodEyeConfig config = GetConfig();
            bool disableVisuals = config != null && config.DisableAmbientVisuals;
            bool disableScreamer = config != null && config.DisableScreamer;

            if (!disableScreamer)
            {
                int idx = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Cursor"));
                if (idx != -1)
                    layers.Insert(idx, new LegacyGameInterfaceLayer("GodEyeMod: Screamer",
                        delegate { DrawScreamer(Main.spriteBatch); return true; }, InterfaceScaleType.None));
            }

            if (!disableVisuals)
            {
                int idx2 = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Cursor"));
                if (idx2 != -1)
                    layers.Insert(idx2, new LegacyGameInterfaceLayer("GodEyeMod: Screen Eyes",
                        delegate { DrawScreenEyes(Main.spriteBatch); return true; }, InterfaceScaleType.None));

                int idx3 = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Resource Bars"));
                if (idx3 != -1)
                    layers.Insert(idx3 + 1, new LegacyGameInterfaceLayer("GodEyeMod: Glitch Overlay",
                        delegate { DrawGlitchOverlay(Main.spriteBatch); return true; }, InterfaceScaleType.UI));

                int idx4 = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
                if (idx4 != -1)
                    layers.Insert(idx4 + 1, new LegacyGameInterfaceLayer("GodEyeMod: Screen Effects",
                        delegate { DrawScreenEffects(Main.spriteBatch); return true; }, InterfaceScaleType.None));
            }

            if (cursorHijacked)
            {
                int idx5 = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Cursor"));
                if (idx5 != -1)
                    layers.Insert(idx5 + 1, new LegacyGameInterfaceLayer("GodEyeMod: Fake Cursor",
                        delegate { DrawHijackEffect(Main.spriteBatch); return true; }, InterfaceScaleType.None));
            }
        }

        #endregion

        #region Draw Methods

        private void DrawHijackEffect(SpriteBatch spriteBatch)
        {
            if (!cursorHijacked) return;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 realPos = new Vector2(Main.mouseX, Main.mouseY);
            if (Main.rand.NextBool(3))
            {
                Color lineColor = new Color(255, Main.rand.Next(50, 150), 0, Main.rand.Next(50, 150));
                Vector2 diff = cursorHijackTarget - realPos;
                spriteBatch.Draw(pixel, new Rectangle((int)realPos.X, (int)realPos.Y, (int)diff.Length(), 2),
                    null, lineColor, diff.ToRotation(), Vector2.Zero, SpriteEffects.None, 0f);
            }
        }

        private void DrawScreamer(SpriteBatch spriteBatch)
        {
            if (screamerOpacity <= 0.01f) return;
            Texture2D screamerTexture;
            try { screamerTexture = ModContent.Request<Texture2D>("GodEyeMod/Content/NPCs/GodEye/Screamer", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value; }
            catch { screamerTexture = TextureAssets.Npc[ModContent.NPCType<GodEyeBoss>()].Value; }
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            spriteBatch.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * screamerOpacity);
            Rectangle fs = new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);
            spriteBatch.Draw(screamerTexture, fs, null, Color.White * screamerOpacity);
            spriteBatch.Draw(screamerTexture, fs, null, Color.Red * screamerOpacity * 0.3f);
        }

        private void DrawScreenEyes(SpriteBatch spriteBatch)
        {
            if (screenEyes.Count == 0) return;
            Texture2D eyeTexture;
            try { eyeTexture = ModContent.Request<Texture2D>("GodEyeMod/Content/NPCs/GodEye/ScreenEye", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value; }
            catch { eyeTexture = TextureAssets.Npc[NPCID.DemonEye].Value; }
            Vector2 origin = new Vector2(eyeTexture.Width / 2f, eyeTexture.Height / 2f);
            int phase = GodEyeGlitchSystem.DeathChaosActive ? 4 : GodEyeGlitchSystem.BossPhase;
            GodEyeConfig config = GetConfig();
            bool disableFlash = config != null && config.DisableFlashing;

            foreach (ScreenEye eye in screenEyes)
            {
                Color eyeColor;
                if (phase >= 3 || GodEyeGlitchSystem.DeathChaosActive)
                    eyeColor = Color.Lerp(new Color(255, 30, 30), new Color(200, 0, 255), (float)Math.Sin(Main.GameUpdateCount * 0.03f + eye.PulseOffset) * 0.5f + 0.5f) * eye.Opacity;
                else if (phase >= 2)
                    eyeColor = new Color(150, 50, 200) * eye.Opacity;
                else
                    eyeColor = new Color(120, 120, 180) * eye.Opacity * 0.5f;

                if (!disableFlash && (phase >= 2 || GodEyeGlitchSystem.DeathChaosActive) && Main.rand.NextBool(5))
                {
                    Vector2 rOff = new Vector2(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2));
                    Vector2 bOff = new Vector2(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2));
                    spriteBatch.Draw(eyeTexture, eye.Position + rOff, null, Color.Red * eye.Opacity * 0.25f, eye.Rotation, origin, eye.Scale, SpriteEffects.None, 0f);
                    spriteBatch.Draw(eyeTexture, eye.Position + bOff, null, Color.Blue * eye.Opacity * 0.25f, eye.Rotation, origin, eye.Scale, SpriteEffects.None, 0f);
                }
                spriteBatch.Draw(eyeTexture, eye.Position, null, eyeColor, eye.Rotation, origin, eye.Scale, SpriteEffects.None, 0f);
            }
        }

        private void DrawGlitchOverlay(SpriteBatch spriteBatch)
        {
            int phase = GodEyeGlitchSystem.DeathChaosActive ? 4 : GodEyeGlitchSystem.BossPhase;
            if (phase < 1 && !GodEyeGlitchSystem.DeathChaosActive) return;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            GodEyeConfig config = GetConfig();
            bool disableFlash = config != null && config.DisableFlashing;
            bool disableText = config != null && config.DisableGlitchText;

            int glitchChance = GodEyeGlitchSystem.DeathChaosActive ? 1 : (phase >= 3 ? 5 : (phase >= 2 ? 10 : 25));
            if (Main.rand.NextBool(glitchChance))
            {
                int numBars = Main.rand.Next(1, GodEyeGlitchSystem.DeathChaosActive ? 12 : (phase >= 3 ? 4 : 2));
                for (int i = 0; i < numBars; i++)
                {
                    int y = Main.rand.Next(0, Main.screenHeight);
                    int height = Main.rand.Next(1, GodEyeGlitchSystem.DeathChaosActive ? 30 : (phase >= 3 ? 10 : 5));
                    int xOffset = Main.rand.Next(-30, 30);
                    Color barColor = new Color(Main.rand.Next(100, 255), Main.rand.Next(0, 80), Main.rand.Next(100, 255),
                        Main.rand.Next(10, GodEyeGlitchSystem.DeathChaosActive ? 120 : (phase >= 3 ? 40 : 25)));
                    spriteBatch.Draw(pixel, new Rectangle(xOffset, y, Main.screenWidth + 60, height), barColor);
                }
            }

            if (!disableFlash)
            {
                if (GodEyeGlitchSystem.DeathChaosActive && Main.rand.NextBool(5))
                    spriteBatch.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                        new Color(Main.rand.Next(256), Main.rand.Next(40), Main.rand.Next(256), Main.rand.Next(8, 40)));
                else if (phase >= 3 && Main.rand.NextBool(30))
                    spriteBatch.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                        new Color(Main.rand.Next(256), Main.rand.Next(30), Main.rand.Next(256), Main.rand.Next(5, 15)));
            }

            if ((phase >= 2 || GodEyeGlitchSystem.DeathChaosActive) && Main.rand.NextBool(GodEyeGlitchSystem.DeathChaosActive ? 3 : (phase >= 3 ? 8 : 15)))
            {
                int lineCount = Main.rand.Next(1, GodEyeGlitchSystem.DeathChaosActive ? 6 : 3);
                for (int i = 0; i < lineCount; i++)
                {
                    int y = Main.rand.Next(0, Main.screenHeight);
                    int h = Main.rand.Next(2, GodEyeGlitchSystem.DeathChaosActive ? 30 : 12);
                    int shift = Main.rand.Next(-50, 50);
                    spriteBatch.Draw(pixel, new Rectangle(shift, y, Main.screenWidth, h),
                        new Color(Main.rand.Next(150, 255), Main.rand.Next(0, 60), Main.rand.Next(150, 255), Main.rand.Next(10, 40)));
                }
            }

            if (!disableText) DrawGlitchText(spriteBatch, phase);
            if (GodEyeGlitchSystem.DeathChaosActive) DrawDeathOverlay(spriteBatch);
        }

        private void DrawGlitchText(SpriteBatch spriteBatch, int phase)
        {
            int textChance = GodEyeGlitchSystem.DeathChaosActive ? 3 : (phase >= 3 ? 10 : (phase >= 2 ? 18 : 35));
            if (!Main.rand.NextBool(textChance)) return;

            string[] glitchTexts = {
                "I SEE YOU", "ERROR", "NULL", "VOID", "FATAL", "CORRUPT",
                "GOD_EYE", "NO ESCAPE", "CRASH", "OVERFLOW", "WATCH", "OBEY", "SUBMIT",
                "E̸̛R̸̛R̸̛Ơ̸R̸̛", "TERRARIA.EXE HAS STOPPED", "PLAYER NOT FOUND", "SAVE DELETED",
                "I AM THE GAME", "CURSOR_OVERRIDE", "GRAVITY.FLIP()", "WEATHER.RANDOM()",
                "WORLD.CORRUPT()", "PLAYER.TELEPORT()", "ZOOM.MALFUNCTION()", "INPUT.HIJACK()",
                "BUFF.INJECT()", "TIME.ACCELERATE()",
                "MOON.PHASE = " + Main.rand.Next(99),
                "WIND.SPEED = " + Main.rand.NextFloat(-5f, 5f).ToString("F1"),
                "RENDER.GLITCH()"
            };

            int textCount = GodEyeGlitchSystem.DeathChaosActive ? Main.rand.Next(2, 5) : 1;
            for (int i = 0; i < textCount; i++)
            {
                string text = glitchTexts[Main.rand.Next(glitchTexts.Length)];
                Vector2 pos = new Vector2(Main.rand.Next(10, Main.screenWidth - 300) + uiOffsetX, Main.rand.Next(10, Main.screenHeight - 40) + uiOffsetY);
                float scale = Main.rand.NextFloat(0.6f, GodEyeGlitchSystem.DeathChaosActive ? 2f : 1.2f);
                Color textColor = Main.rand.Next(4) switch
                {
                    0 => new Color(255, Main.rand.Next(0, 60), 0),
                    1 => new Color(Main.rand.Next(150, 255), 0, Main.rand.Next(150, 255)),
                    2 => new Color(0, Main.rand.Next(180, 255), 0),
                    _ => Color.White
                };
                textColor *= Main.rand.NextFloat(0.4f, 0.8f);
                Utils.DrawBorderString(spriteBatch, text, pos, textColor, scale);
            }
        }

        private void DrawDeathOverlay(SpriteBatch spriteBatch)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float progress = GodEyeGlitchSystem.DeathChaosProgress;
            GodEyeConfig config = GetConfig();
            bool disableFlash = config != null && config.DisableFlashing;
            bool disableText = config != null && config.DisableGlitchText;

            int blockCount = (int)(progress * 30);
            for (int i = 0; i < blockCount; i++)
            {
                int x = Main.rand.Next(-50, Main.screenWidth);
                int y = Main.rand.Next(-50, Main.screenHeight);
                int w = Main.rand.Next(20, (int)(150 * progress) + 20);
                int h = Main.rand.Next(15, (int)(100 * progress) + 15);
                spriteBatch.Draw(pixel, new Rectangle(x, y, w, h), Color.Black * (progress * 0.7f));
            }

            if (!disableFlash && Main.rand.NextBool(4))
                spriteBatch.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.DarkRed * (progress * 0.2f));

            if (!disableText && Main.rand.NextBool(6))
            {
                string[] deathTexts = { "YOUR SOUL BELONGS TO ME", "THERE IS NO RESPAWN", "G̸̛A̸̛M̸̛E̸̛ ̸̛Ơ̸V̸̛E̸̛R̸̛", "DELETING SAVE...", "GOODBYE FOREVER", "THE VOID CONSUMES ALL" };
                string text = deathTexts[Main.rand.Next(deathTexts.Length)];
                Vector2 pos = new Vector2(Main.rand.Next(20, Main.screenWidth - 300), Main.rand.Next(20, Main.screenHeight - 50));
                Utils.DrawBorderString(spriteBatch, text, pos, Color.Lerp(Color.Red, Color.White, Main.rand.NextFloat()) * progress, Main.rand.NextFloat(1.2f, 2.5f));
            }
        }

        private void DrawScreenEffects(SpriteBatch spriteBatch)
        {
            int phase = GodEyeGlitchSystem.DeathChaosActive ? 4 : GodEyeGlitchSystem.BossPhase;
            if (phase < 1 && !GodEyeGlitchSystem.DeathChaosActive) return;

            float vignetteStrength = GodEyeGlitchSystem.DeathChaosActive ? 0.7f : (phase >= 3 ? 0.4f : (phase >= 2 ? 0.25f : 0.1f));
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int borderSize = (int)(100 * vignetteStrength);
            Color borderColor = Color.Black * vignetteStrength;

            spriteBatch.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, borderSize), borderColor);
            spriteBatch.Draw(pixel, new Rectangle(0, Main.screenHeight - borderSize, Main.screenWidth, borderSize), borderColor);
            spriteBatch.Draw(pixel, new Rectangle(0, 0, borderSize, Main.screenHeight), borderColor);
            spriteBatch.Draw(pixel, new Rectangle(Main.screenWidth - borderSize, 0, borderSize, Main.screenHeight), borderColor);
        }

        #endregion
    }
}