using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using ReLogic.Graphics;
using Terraria.DataStructures;

namespace GodEyeMod.Content.Ui
{
    public class GodEyeBossBar : ModBossBar
    {
        private Random rng = new Random();
        private int frameCounter = 0;

        private int hpChangeTimer = 0;
        private string displayHPText = "";
        private bool showFake = false;

        private float displayedPercent = 1f;
        private float percentTarget = 1f;
        private float percentDriftSpeed = 1f;
        private int percentJumpTimer = 0;
        private float percentNoise = 0f;

        private string currentName = "The GOD";
        private int nameTimer = 0;

        private int cachedLife = 0;
        private int cachedLifeMax = 1;
        private float realPercent = 1f;
        private int currentPhase = 0;

        private Texture2D headTex;
        private bool headLoaded = false;

        public override bool? ModifyInfo(ref BigProgressBarInfo info, ref float life,
            ref float lifeMax, ref float shield, ref float shieldMax)
        {
            if (info.npcIndexToAimAt < 0 || info.npcIndexToAimAt >= Main.maxNPCs)
                return false;

            NPC npc = Main.npc[info.npcIndexToAimAt];
            if (!npc.active) return false;
            if (npc.type != ModContent.NPCType<NPCs.GodEye.GodEyeBoss>()) return false;

            life = npc.life;
            lifeMax = npc.lifeMax;
            shield = 0f;
            shieldMax = 0f;

            cachedLife = npc.life;
            cachedLifeMax = npc.lifeMax;
            realPercent = MathHelper.Clamp((float)npc.life / npc.lifeMax, 0f, 1f);

            if (realPercent > 0.7f) currentPhase = 0;
            else if (realPercent > 0.4f) currentPhase = 1;
            else if (realPercent > 0.15f) currentPhase = 2;
            else currentPhase = 3;

            frameCounter++;
            UpdateBarFill();
            UpdateHPText();
            UpdateName();

            return true;
        }

        private void UpdateBarFill()
        {
            percentJumpTimer--;

            float driftRange;
            float noiseAmount;
            float lerpSpeed;
            int jumpMinTime, jumpMaxTime;
            float jumpChance;

            switch (currentPhase)
            {
                case 0:
                    driftRange = 0.06f;
                    noiseAmount = 0.008f;
                    lerpSpeed = 0.03f;
                    jumpMinTime = 90;
                    jumpMaxTime = 240;
                    jumpChance = 0.003f;
                    break;
                case 1:
                    driftRange = 0.15f;
                    noiseAmount = 0.015f;
                    lerpSpeed = 0.04f;
                    jumpMinTime = 40;
                    jumpMaxTime = 120;
                    jumpChance = 0.008f;
                    break;
                case 2:
                    driftRange = 0.3f;
                    noiseAmount = 0.025f;
                    lerpSpeed = 0.05f;
                    jumpMinTime = 15;
                    jumpMaxTime = 60;
                    jumpChance = 0.02f;
                    break;
                case 3:
                default:
                    driftRange = 0.6f;
                    noiseAmount = 0.04f;
                    lerpSpeed = 0.07f;
                    jumpMinTime = 3;
                    jumpMaxTime = 20;
                    jumpChance = 0.05f;
                    break;
            }

            if (percentJumpTimer <= 0)
            {
                percentJumpTimer = rng.Next(jumpMinTime, jumpMaxTime);

                float drift = (float)(rng.NextDouble() * 2 - 1) * driftRange;
                percentTarget = MathHelper.Clamp(realPercent + drift, 0.01f, 1f);

                if (currentPhase >= 3 && rng.NextDouble() < 0.15f)
                {
                    percentTarget = (float)rng.NextDouble();
                }
                else if (currentPhase >= 2 && rng.NextDouble() < 0.08f)
                {
                    percentTarget = rng.NextDouble() < 0.5f ? 0.95f : 0.05f;
                }

                percentDriftSpeed = 0.5f + (float)rng.NextDouble() * 1.5f;
            }

            if (rng.NextDouble() < jumpChance)
            {
                float jumpDrift = (float)(rng.NextDouble() * 2 - 1) * driftRange * 1.5f;
                displayedPercent = MathHelper.Clamp(realPercent + jumpDrift, 0.01f, 1f);
            }

            displayedPercent = MathHelper.Lerp(displayedPercent, percentTarget,
                lerpSpeed * percentDriftSpeed);

            percentNoise = (float)(rng.NextDouble() * 2 - 1) * noiseAmount;
            displayedPercent = MathHelper.Clamp(displayedPercent + percentNoise, 0.005f, 1f);

            float wave = (float)Math.Sin(frameCounter * 0.04f * (1 + currentPhase * 0.3f));
            float waveAmount = currentPhase switch
            {
                0 => 0.01f,
                1 => 0.03f,
                2 => 0.06f,
                3 => 0.12f,
                _ => 0.01f
            };
            displayedPercent = MathHelper.Clamp(displayedPercent + wave * waveAmount, 0.005f, 1f);
        }

        private void UpdateHPText()
        {
            hpChangeTimer--;
            if (hpChangeTimer > 0) return;

            hpChangeTimer = currentPhase switch
            {
                0 => rng.Next(60, 180),
                1 => rng.Next(20, 60),
                2 => rng.Next(8, 30),
                3 => rng.Next(2, 10),
                _ => 120
            };

            float fakeChance = currentPhase switch
            {
                0 => 0.15f,
                1 => 0.45f,
                2 => 0.7f,
                3 => 0.9f,
                _ => 0f
            };

            if (rng.NextDouble() < fakeChance)
            {
                showFake = true;
                int type = rng.Next(12);
                switch (type)
                {
                    case 0:
                        displayHPText = $"{rng.Next(1000000, 99999999)} / {rng.Next(1000000, 99999999)}";
                        break;
                    case 1:
                        displayHPText = $"-{rng.Next(1, 99999999)} / {cachedLifeMax}";
                        break;
                    case 2:
                        string[] nans = { "NaN / NaN", "NaN / Infinity", "Infinity / NaN",
                                          "-Infinity / 0", "0 / -0", "NaN / -NaN" };
                        displayHPText = nans[rng.Next(nans.Length)];
                        break;
                    case 3:
                        string[] msgs = {
                            "I SEE YOU", "??? / ???", "ERROR", "inf / -inf",
                            "YOU CAN'T WIN", "0 / 0", "UNDEFINED", "NULL",
                            "OVERFLOW", "HP IS A LIE", "404 NOT FOUND",
                            "STOP FIGHTING", "I AM THE CODE", "GIVE UP",
                            "THIS ISN'T REAL", "SEGFAULT", "ACCESS DENIED",
                            "YOU ARE NOTHING", "I FEEL NOTHING", "WHY?",
                            "HEALTH.DLL MISSING", "FATAL ERROR", "BLUE SCREEN",
                            "kernel panic", "sudo kill player", "rm -rf /hp",
                            "MEMORY LEAK", "BUFFER OVERFLOW", "STACK CORRUPT",
                            "HP = VOID", "> cat /dev/null", "chmod 000 life",
                            "WORLD.EXE CRASHED", "BOSS.HP = YES", "PAIN.DAT",
                            "LOADING...", "PLEASE WAIT...", "RECONNECTING...",
                            "CONNECTION LOST", "SYNC ERROR", "DESYNC",
                            "YOUR HP IS MINE", "I CONTROL EVERYTHING",
                            "DO YOU ENJOY THIS?", "THERE IS NO HP",
                            "NUMBERS MEAN NOTHING", "MATH IS BROKEN",
                            "HP WAS NEVER REAL", "THIS BAR LIES",
                            "LOOK AWAY", "CLOSE YOUR EYES",
                            "YOU ALREADY LOST", "GAME OVER",
                            "PRESS ALT+F4", "UNINSTALL.EXE",
                            "YOUR SAVE IS CORRUPT", "ROLLBACK FAILED",
                            "BACKUP NOT FOUND"
                        };
                        displayHPText = msgs[rng.Next(msgs.Length)];
                        break;
                    case 4:
                        int off = rng.Next(-500000, 500000);
                        displayHPText = $"{Math.Max(0, cachedLife + off)} / {cachedLifeMax + rng.Next(-300000, 300000)}";
                        break;
                    case 5:
                        displayHPText = $"{rng.Next(0, cachedLifeMax * 3)} / {cachedLifeMax}";
                        break;
                    case 6:
                        string g = "#$%&@!?*0123456789<>[]{}|/\\";
                        char[] c = new char[rng.Next(5, 18)];
                        for (int i = 0; i < c.Length; i++) c[i] = g[rng.Next(g.Length)];
                        displayHPText = new string(c);
                        break;
                    case 7:
                        string[] pcts = { "???%", "-42%", "1337%", "0.001%",
                                          "NaN%", "99999%", "-inf%", "pi%" };
                        displayHPText = pcts[rng.Next(pcts.Length)];
                        break;
                    case 8:
                        string[] codes = {
                            "0x7F454C46", "0xDEADBEEF", "0xCAFEBABE",
                            "01101000 01110000", "0xFFFFFFFF", "0x00000000",
                            "0xGODGODGO", "0xBADC0DE" };
                        displayHPText = codes[rng.Next(codes.Length)];
                        break;
                    case 9:
                        displayHPText = $"{rng.Next(1, 10)}...{rng.Next(0, 3)}...{rng.Next(0, 2)}...";
                        break;
                    case 10:
                        int fakeLife = (int)(cachedLifeMax * (rng.NextDouble() * 0.3 + 0.7));
                        displayHPText = $"{fakeLife} / {cachedLifeMax}";
                        break;
                    case 11:
                        string[] words = { "PAIN", "VOID", "NULL", "FEAR",
                                           "DEATH", "GOD", "LIES", "TRUTH",
                                           "HELP", "STOP", "RUN", "DIE",
                                           "WAKE UP", "IT HURTS", "NO" };
                        displayHPText = words[rng.Next(words.Length)];
                        break;
                }
            }
            else
            {
                showFake = false;
                displayHPText = $"{cachedLife} / {cachedLifeMax}";
            }
        }

        private void UpdateName()
        {
            nameTimer--;
            if (nameTimer > 0) return;

            nameTimer = currentPhase switch
            {
                0 => rng.Next(240, 600),
                1 => rng.Next(80, 200),
                2 => rng.Next(20, 80),
                3 => rng.Next(5, 25),
                _ => 300
            };

            float glitchChance = currentPhase switch
            {
                0 => 0.08f,
                1 => 0.3f,
                2 => 0.6f,
                3 => 0.85f,
                _ => 0f
            };

            if (rng.NextDouble() < glitchChance)
            {
                string[] names = {
                    "The G0D", "Th3 GOD", "The  GOD", "THE GOD", "the god",
                    "The G O D", "T.H.E. G.O.D.", "TheGOD", "The_GOD",
                    "???", "?!?", "...", "---", "___", "///",
                    "ERROR", "NULL", "VOID", "NaN", "UNDEFINED",
                    "MISSING_NAME", "NAME_NOT_FOUND", "boss_name.txt",
                    "[ERROR 404]", "[REDACTED]", "[CENSORED]", "[DELETED]",
                    "[DATA EXPUNGED]", "[CORRUPTED]", "[UNKNOWN]",
                    "The GOD.exe", "god.dll", "reality.sys", "world.dat",
                    "BOSS_HP.NULL", "entity_???", "npc_type: -1",
                    "class TheGod", "import god", "sudo god",
                    "I AM HERE", "I SEE YOU", "Y O U", "BEHIND YOU",
                    "LOOK UP", "LOOK AWAY", "WAKE UP", "IT KNOWS",
                    "WE ARE ONE", "YOU ARE ME", "I AM YOU",
                    "THERE IS NO GOD", "I AM THE GAME", "I AM REAL",
                    "ARE YOU?", "WHO ARE YOU?", "WHY FIGHT?",
                    "RUN", "STOP", "PLEASE", "HELP ME",
                    "IT HURTS", "MAKE IT STOP", "I CAN'T DIE",
                    "NOT A BOSS", "NOT AN NPC", "NOT A GAME",
                    "The TRUTH", "The VOID", "The ERROR",
                    "The PLAYER", "The WORLD", "The CODE",
                    "The EYE", "The END", "The NULL",
                    "The NOTHING", "The EVERYTHING",
                    "The WATCHER", "The OBSERVER",
                    "The MACHINE", "The SYSTEM",
                    "YOUR NAME HERE", "TERRARIA", "TMODLOADER",
                    "STEAM_USER", "Player 1", "Main.player[0]",
                    "localhost", "127.0.0.1", "0.0.0.0",
                    "Th#$G@D", "T%e &OD", "Th3 G*D", "@#$%^&",
                    " ", "  ", ".",
                    "The GOD The GOD The GOD",
                    "GODGODGODGODGODGOD",
                    "TheTheTheTheThe",
                };
                currentName = names[rng.Next(names.Length)];

                if (currentPhase >= 2 && rng.NextDouble() < 0.15f)
                {
                    char[] n = "The GOD".ToCharArray();
                    for (int i = 0; i < n.Length; i++)
                        if (rng.NextDouble() < 0.4f + currentPhase * 0.1f)
                            n[i] = (char)rng.Next(33, 90);
                    currentName = new string(n);
                }
            }
            else
            {
                currentName = "The GOD";
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, NPC npc, ref BossBarDrawParams drawParams)
        {
            if (!headLoaded)
            {
                try
                {
                    headTex = ModContent.Request<Texture2D>(
                        "GodEyeMod/Content/NPCs/GodEye/GodEyeBoss_Head_Boss",
                        ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                }
                catch { headTex = null; }
                headLoaded = true;
            }

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 center = drawParams.BarCenter;

            int barWidth = 480;
            int barHeight = 18;
            int border = 2;
            int iconSize = 34;
            int iconPadding = 10;
            int totalWidth = iconSize + iconPadding + barWidth + border * 2;

            float startX = center.X - totalWidth / 2f;
            Vector2 iconPos = new Vector2(startX, center.Y - iconSize / 2f);
            float barX = startX + iconSize + iconPadding;
            Vector2 barCenter = new Vector2(barX + barWidth / 2f + border, center.Y);

            int pad = 8;
            Rectangle bgRect = new Rectangle(
                (int)(startX - pad), (int)(center.Y - iconSize / 2f - pad),
                totalWidth + pad * 2, iconSize + pad * 2);

            spriteBatch.Draw(pixel, bgRect, new Color(6, 4, 12, 210));

            Color frameColor = GetPhaseFrameColor();
            DrawBorder(spriteBatch, pixel, bgRect, frameColor, 1);
            Rectangle outerBg = new Rectangle(bgRect.X - 1, bgRect.Y - 1, bgRect.Width + 2, bgRect.Height + 2);
            DrawBorder(spriteBatch, pixel, outerBg, frameColor * 0.3f, 1);

            DrawIcon(spriteBatch, pixel, iconPos, iconSize, frameColor);

            DrawHealthBar(spriteBatch, pixel, barX, center.Y, barWidth, barHeight, border);

            DrawNotches(spriteBatch, pixel, barX + border, center.Y, barWidth, barHeight);

            DrawHPText(spriteBatch, barCenter);

            DrawName(spriteBatch, center, bgRect);

            return false;
        }

        private Color GetPhaseFrameColor()
        {
            float p = (float)Math.Sin(frameCounter * 0.04f) * 0.15f + 0.85f;
            return currentPhase switch
            {
                0 => new Color((int)(80 * p), (int)(50 * p), (int)(140 * p), 200),
                1 => new Color((int)(140 * p), (int)(60 * p), (int)(120 * p), 210),
                2 => new Color((int)(180 * p), (int)(40 * p), (int)(60 * p), 220),
                3 => Color.Lerp(
                    new Color(200, 30, 30, 230),
                    new Color(150, 30, 200, 230),
                    (float)Math.Sin(frameCounter * 0.1f) * 0.5f + 0.5f) * p,
                _ => new Color(80, 50, 140, 200)
            };
        }

        private void DrawIcon(SpriteBatch sb, Texture2D pixel, Vector2 pos, int size, Color frameCol)
        {
            sb.Draw(pixel, new Rectangle((int)pos.X - 2, (int)pos.Y - 2, size + 4, size + 4),
                new Color(15, 8, 25, 230));
            DrawBorder(sb, pixel,
                new Rectangle((int)pos.X - 2, (int)pos.Y - 2, size + 4, size + 4), frameCol, 1);

            if (headTex != null)
            {
                sb.Draw(headTex, new Rectangle((int)pos.X, (int)pos.Y, size, size), Color.White);
                if (currentPhase >= 2)
                {
                    float glow = (float)Math.Sin(frameCounter * 0.08f) * 0.15f + 0.1f;
                    sb.Draw(headTex,
                        new Rectangle((int)pos.X - 2, (int)pos.Y - 2, size + 4, size + 4),
                        (currentPhase >= 3 ? Color.Red : Color.Purple) * glow);
                }
            }
            else
            {
                sb.Draw(pixel,
                    new Rectangle((int)pos.X + 4, (int)pos.Y + 4, size - 8, size - 8),
                    Color.Purple * 0.5f);
            }
        }

        private void DrawHealthBar(SpriteBatch sb, Texture2D pixel,
            float x, float centerY, int w, int h, int border)
        {
            int left = (int)x;
            int top = (int)(centerY - (h + border * 2) / 2f);
            int outerW = w + border * 2;
            int outerH = h + border * 2;

            Color borderCol = GetPhaseFrameColor() * 0.8f;
            DrawBorder(sb, pixel, new Rectangle(left, top, outerW, outerH), borderCol, border);

            sb.Draw(pixel, new Rectangle(left + border, top + border, w, h),
                new Color(5, 3, 8, 240));

            for (int i = 1; i < 20; i++)
            {
                int lineX = left + border + (int)(w * (i / 20f));
                sb.Draw(pixel, new Rectangle(lineX, top + border, 1, h),
                    new Color(30, 20, 40, 60));
            }

            int fillW = (int)(w * displayedPercent);
            if (fillW <= 0) return;
            fillW = Math.Min(fillW, w);

            Color mainColor = GetFillColor(displayedPercent);

            sb.Draw(pixel, new Rectangle(left + border, top + border, fillW, h), mainColor);

            int glowH = Math.Max(1, h / 3);
            Color topGlow = Color.Lerp(mainColor, Color.White, 0.45f) * 0.55f;
            sb.Draw(pixel, new Rectangle(left + border, top + border, fillW, glowH), topGlow);

            if (fillW > 2)
                sb.Draw(pixel, new Rectangle(left + border + 1, top + border, fillW - 2, 1),
                    Color.White * 0.2f);

            Color botShadow = Color.Lerp(mainColor, Color.Black, 0.45f) * 0.5f;
            sb.Draw(pixel, new Rectangle(left + border, top + border + h - glowH, fillW, glowH), botShadow);

            if (fillW > 4)
            {
                float edgePulse = (float)Math.Sin(frameCounter * 0.12f) * 0.3f + 0.3f;
                int pulseW = Math.Min(6, fillW);
                sb.Draw(pixel,
                    new Rectangle(left + border + fillW - pulseW, top + border, pulseW, h),
                    Color.White * edgePulse);
            }

            if (currentPhase >= 2 && rng.NextDouble() < 0.15f + currentPhase * 0.1f)
            {
                int glitchCount = 1 + rng.Next(currentPhase);
                for (int i = 0; i < glitchCount; i++)
                {
                    int gx = left + border + rng.Next(Math.Max(1, fillW));
                    int gw = rng.Next(3, 20);
                    Color gc = rng.Next(3) switch
                    {
                        0 => Color.Red * 0.3f,
                        1 => Color.Cyan * 0.3f,
                        _ => Color.White * 0.2f
                    };
                    sb.Draw(pixel, new Rectangle(gx, top + border, gw, h), gc);
                }
            }

            if (currentPhase >= 3 && rng.NextDouble() < 0.2f)
            {
                float ghostPercent = (float)rng.NextDouble();
                int ghostW = (int)(w * ghostPercent);
                Color ghostCol = Color.Lerp(Color.Red, Color.Purple, (float)rng.NextDouble()) * 0.15f;
                sb.Draw(pixel, new Rectangle(left + border, top + border, ghostW, h), ghostCol);
            }
        }

        private Color GetFillColor(float percent)
        {
            float pulse = (float)Math.Sin(frameCounter * 0.06f) * 0.06f;

            Color baseColor;
            if (percent > 0.7f)
            {
                float t = (percent - 0.7f) / 0.3f;
                baseColor = Color.Lerp(new Color(40, 200, 120), new Color(70, 255, 170), t);
            }
            else if (percent > 0.4f)
            {
                float t = (percent - 0.4f) / 0.3f;
                baseColor = Color.Lerp(new Color(240, 180, 30), new Color(40, 200, 120), t);
            }
            else if (percent > 0.15f)
            {
                float t = (percent - 0.15f) / 0.25f;
                baseColor = Color.Lerp(new Color(230, 60, 30), new Color(240, 180, 30), t);
            }
            else
            {
                float t = percent / 0.15f;
                baseColor = Color.Lerp(new Color(180, 20, 20), new Color(230, 60, 30), t);
                pulse = (float)Math.Sin(frameCounter * 0.15f) * 0.12f;
            }

            return Color.Lerp(baseColor, Color.White, Math.Max(0, pulse));
        }

        private void DrawNotches(SpriteBatch sb, Texture2D pixel, float barLeft, float centerY, int w, int h)
        {
            int top = (int)(centerY - h / 2f) - 1;
            for (int i = 1; i <= 3; i++)
            {
                int nx = (int)(barLeft + w * (i / 4f));
                sb.Draw(pixel, new Rectangle(nx, top, 1, h + 2), new Color(255, 255, 255, 35));
            }
        }

        private void DrawHPText(SpriteBatch sb, Vector2 barCenter)
        {
            string text = showFake ? displayHPText : $"{cachedLife} / {cachedLifeMax}";

            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 size = font.MeasureString(text);
            Vector2 pos = barCenter - new Vector2(size.X * 0.85f / 2f, size.Y * 0.85f / 2f - 1f);

            ChatManager.DrawColorCodedStringWithShadow(sb, font, text,
                pos + new Vector2(1, 1), Color.Black * 0.7f, 0f, Vector2.Zero, Vector2.One * 0.85f);

            Color textCol = showFake
                ? Color.Lerp(Color.White, Color.Red, 0.2f + currentPhase * 0.1f)
                : Color.White;

            ChatManager.DrawColorCodedStringWithShadow(sb, font, text,
                pos, textCol, 0f, Vector2.Zero, Vector2.One * 0.85f);
        }

        private void DrawName(SpriteBatch sb, Vector2 center, Rectangle bgRect)
        {
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 nameSize = font.MeasureString(currentName);
            Vector2 namePos = new Vector2(center.X - nameSize.X / 2f, bgRect.Y - nameSize.Y - 2f);

            ChatManager.DrawColorCodedStringWithShadow(sb, font, currentName,
                namePos + new Vector2(1, 1), Color.Black * 0.6f, 0f, Vector2.Zero, Vector2.One);

            Color nameCol = currentPhase switch
            {
                0 => new Color(190, 160, 255),
                1 => new Color(220, 140, 220),
                2 => new Color(255, 120, 120),
                3 => Color.Lerp(
                    new Color(255, 80, 80),
                    new Color(200, 80, 255),
                    (float)Math.Sin(frameCounter * 0.08f) * 0.5f + 0.5f),
                _ => Color.White
            };

            ChatManager.DrawColorCodedStringWithShadow(sb, font, currentName,
                namePos, nameCol, 0f, Vector2.Zero, Vector2.One);
        }

        private void DrawBorder(SpriteBatch sb, Texture2D pixel, Rectangle r, Color c, int t)
        {
            sb.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, t), c);
            sb.Draw(pixel, new Rectangle(r.X, r.Y + r.Height - t, r.Width, t), c);
            sb.Draw(pixel, new Rectangle(r.X, r.Y + t, t, r.Height - t * 2), c);
            sb.Draw(pixel, new Rectangle(r.X + r.Width - t, r.Y + t, t, r.Height - t * 2), c);
        }
    }
}