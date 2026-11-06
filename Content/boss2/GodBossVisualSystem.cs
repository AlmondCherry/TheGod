using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.GameContent;

namespace GodEyeMod.Content.boss2
{
    public class GodBossVisualSystem : ModSystem
    {
        private static Color flashColor = Color.Transparent;
        private static float flashIntensity = 0f;
        private static float flashDecay = 0.04f;

        private static float shakeIntensity = 0f;
        private static int shakeDuration = 0;
        private static int shakeTimer = 0;

        private static float darkenOverlay = 0f;
        private static float whiteOverlay = 0f;
        private static string endText = "";
        private static float endTextAlpha = 0f;
        private static float timerDisplay = 0f;
        private static bool showTimer = false;
        private static float glitchAmount = 0f;

        private static float vignetteIntensity = 0f;
        private static float timerPulse = 0f;

        private static float questionMarkAlpha = 0f;
        private static float questionMarkScale = 0f;
        private static float questionMarkTimer = 0f;
        private static bool showQuestionMark = false;
        private static float endTextScale = 0f;
        private static float endAppearTimer = 0f;

        private static bool musicStopped = false;

        public static void TriggerFlash(Color color, float intensity)
        {
            flashColor = color;
            flashIntensity = intensity;
        }

        public static void TriggerShake(float intensity, int duration)
        {
            shakeIntensity = intensity;
            shakeDuration = duration;
            shakeTimer = 0;
        }

        public static void SetDarken(float amount) => darkenOverlay = amount;
        public static void SetWhiteOut(float amount) => whiteOverlay = amount;
        public static void SetTimer(float time, bool show) { timerDisplay = time; showTimer = show; }
        public static void SetGlitch(float amount) => glitchAmount = amount;

        public static void SetEndText(string text, float alpha)
        {
            endText = text;
            endTextAlpha = alpha;
        }

        public static void SetEndAnimation(bool showQuestion, float qTimer)
        {
            showQuestionMark = showQuestion;
            questionMarkTimer = qTimer;
        }

        public static void StopMusic()
        {
            if (!musicStopped)
            {
                musicStopped = true;
                Main.musicVolume = 0f;
            }
        }

        public static void RestoreMusic()
        {
            musicStopped = false;
        }

        public override void ModifyScreenPosition()
        {
            if (shakeTimer < shakeDuration && shakeIntensity > 0)
            {
                float progress = 1f - (float)shakeTimer / shakeDuration;
                float currentIntensity = shakeIntensity * progress;
                Main.screenPosition += Main.rand.NextVector2Circular(currentIntensity, currentIntensity);
                shakeTimer++;
            }
        }

        public override void PostUpdateEverything()
        {
            flashIntensity -= flashDecay;
            if (flashIntensity < 0) flashIntensity = 0;

            timerPulse += 1f / 60f;

            int bossIdx = FindGodBoss();
            if (bossIdx >= 0)
            {
                vignetteIntensity = MathHelper.Lerp(vignetteIntensity, 0.4f, 0.02f);
            }
            else
            {
                darkenOverlay = 0f;
                whiteOverlay = 0f;
                endTextAlpha = 0f;
                showTimer = false;
                glitchAmount = 0f;
                vignetteIntensity = MathHelper.Lerp(vignetteIntensity, 0f, 0.05f);
                showQuestionMark = false;
                questionMarkAlpha = 0f;
                endTextScale = 0f;
                endAppearTimer = 0f;
                if (musicStopped) RestoreMusic();
            }
        }

        private int FindGodBoss()
        {
            int type = ModContent.NPCType<GodBossPhase2>();
            for (int i = 0; i < Main.maxNPCs; i++)
                if (Main.npc[i].active && Main.npc[i].type == type) return i;
            return -1;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseIdx = layers.FindIndex(l => l.Name.Equals("Vanilla: Mouse Text"));
            if (mouseIdx >= 0)
            {
                layers.Insert(mouseIdx, new LegacyGameInterfaceLayer("GodEyeMod: Boss Overlay",
                    delegate { DrawOverlays(Main.spriteBatch); return true; },
                    InterfaceScaleType.UI));
            }
        }

        private void DrawOverlays(SpriteBatch spriteBatch)
        {
            var config = ModContent.GetInstance<GodEyeConfig>();
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle fullScreen = new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);

            if (vignetteIntensity > 0.01f && (config == null || !config.DisableAmbientVisuals))
                DrawVignette(spriteBatch, pixel, vignetteIntensity);

            if (flashIntensity > 0.01f && (config == null || !config.DisableFlashing))
                spriteBatch.Draw(pixel, fullScreen, flashColor * flashIntensity);

            if (darkenOverlay > 0.01f && (config == null || !config.DisableAmbientVisuals))
                spriteBatch.Draw(pixel, fullScreen, Color.Black * darkenOverlay);

            if (whiteOverlay > 0.01f && (config == null || !config.DisableFlashing))
                spriteBatch.Draw(pixel, fullScreen, Color.White * whiteOverlay);

            if (glitchAmount > 0.01f && (config == null || !config.DisableAmbientVisuals))
                DrawGlitchEffect(spriteBatch, pixel, glitchAmount);

            if (showTimer) DrawTimer(spriteBatch);

            if (endTextAlpha > 0.01f) DrawEndScreen(spriteBatch);
        }

        private void DrawVignette(SpriteBatch spriteBatch, Texture2D pixel, float intensity)
        {
            int edgeSize = (int)(Math.Max(Main.screenWidth, Main.screenHeight) * 0.25f);
            for (int i = 0; i < edgeSize; i++)
            {
                float alpha = (1f - (float)i / edgeSize) * intensity * 0.6f;
                Color c = Color.Black * alpha;
                spriteBatch.Draw(pixel, new Rectangle(0, i, Main.screenWidth, 1), c);
                spriteBatch.Draw(pixel, new Rectangle(0, Main.screenHeight - i, Main.screenWidth, 1), c);
            }
            for (int i = 0; i < edgeSize; i++)
            {
                float alpha = (1f - (float)i / edgeSize) * intensity * 0.4f;
                Color c = Color.Black * alpha;
                spriteBatch.Draw(pixel, new Rectangle(i, 0, 1, Main.screenHeight), c);
                spriteBatch.Draw(pixel, new Rectangle(Main.screenWidth - i, 0, 1, Main.screenHeight), c);
            }
            float redPulse = (float)Math.Sin(timerPulse * 2f) * 0.5f + 0.5f;
            int redEdge = edgeSize / 3;
            for (int i = 0; i < redEdge; i++)
            {
                float alpha = (1f - (float)i / redEdge) * intensity * 0.15f * redPulse;
                Color c = Color.DarkRed * alpha;
                spriteBatch.Draw(pixel, new Rectangle(0, i, Main.screenWidth, 1), c);
                spriteBatch.Draw(pixel, new Rectangle(0, Main.screenHeight - i, Main.screenWidth, 1), c);
            }
        }

        private void DrawGlitchEffect(SpriteBatch spriteBatch, Texture2D pixel, float intensity)
        {
            int lineCount = (int)(intensity * 15);
            for (int i = 0; i < lineCount; i++)
            {
                int y = Main.rand.Next(Main.screenHeight);
                int height = Main.rand.Next(1, 4);
                int xOffset = Main.rand.Next(-(int)(intensity * 20), (int)(intensity * 20));
                Color glitchColor = Main.rand.NextBool() ? Color.Red * 0.25f : Color.Cyan * 0.15f;
                spriteBatch.Draw(pixel, new Rectangle(xOffset, y, Main.screenWidth, height), glitchColor * intensity);
            }
            if (intensity > 0.3f && Main.rand.NextBool(5))
            {
                int blockY = Main.rand.Next(Main.screenHeight);
                int blockH = Main.rand.Next(5, 20);
                spriteBatch.Draw(pixel, new Rectangle(0, blockY, Main.screenWidth, blockH), Color.Black * intensity * 0.5f);
            }
        }

        private void DrawTimer(SpriteBatch spriteBatch)
        {
            int minutes = (int)(timerDisplay / 60f);
            int seconds = (int)(timerDisplay % 60f);
            int centiseconds = (int)((timerDisplay % 1f) * 100f);
            string timeStr = $"{minutes:D1}:{seconds:D2}.{centiseconds:D2}";

            Vector2 pos = new Vector2(Main.screenWidth / 2f, 50f);
            float pulse = 1f + (float)Math.Sin(timerPulse * 3f) * 0.03f;

            Color urgencyColor;
            if (timerDisplay < 30f)
                urgencyColor = Color.Lerp(Color.Red, Color.OrangeRed, (float)Math.Sin(timerPulse * 8f) * 0.5f + 0.5f);
            else if (timerDisplay < 120f)
                urgencyColor = Color.Lerp(Color.OrangeRed, Color.Gold, (float)Math.Sin(timerPulse * 4f) * 0.5f + 0.5f);
            else
                urgencyColor = Color.Lerp(Color.White, Color.LightGray, (float)Math.Sin(timerPulse * 2f) * 0.5f + 0.5f);

            float mainScale = 0.8f * pulse;
            var font = FontAssets.DeathText.Value;
            Vector2 size = font.MeasureString(timeStr);
            Vector2 origin = size / 2f;

            for (int dx = -3; dx <= 3; dx++)
                for (int dy = -3; dy <= 3; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);
                    float shadowAlpha = (1f - dist / 4.5f) * 0.5f;
                    Utils.DrawBorderStringFourWay(spriteBatch, font, timeStr,
                        pos.X + dx, pos.Y + dy, Color.Black * shadowAlpha, Color.Transparent, origin, mainScale);
                }

            Color glowColor = urgencyColor * 0.3f;
            for (int g = 0; g < 4; g++)
            {
                float glowAngle = MathHelper.PiOver2 * g + timerPulse * 2f;
                Utils.DrawBorderStringFourWay(spriteBatch, font, timeStr,
                    pos.X + MathF.Cos(glowAngle) * 2f, pos.Y + MathF.Sin(glowAngle) * 2f,
                    glowColor, Color.Transparent, origin, mainScale * 1.02f);
            }

            Utils.DrawBorderStringFourWay(spriteBatch, font, timeStr,
                pos.X, pos.Y, urgencyColor, Color.Black * 0.8f, origin, mainScale);
        }

        private void DrawEndScreen(SpriteBatch spriteBatch)
        {
            var font = FontAssets.DeathText.Value;
            Vector2 center = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);

            endAppearTimer += 1f / 60f;
            float endAlpha = Math.Clamp(endAppearTimer / 3f, 0f, 1f) * endTextAlpha;
            endTextScale = MathHelper.Lerp(endTextScale, 1.5f, 0.02f);

            float breathe = 1f + (float)Math.Sin(timerPulse * 1f) * 0.015f;
            float finalScale = endTextScale * breathe;

            string mainText = "END";
            Vector2 endSize = font.MeasureString(mainText);
            Vector2 endOrigin = endSize / 2f;

            Color glowCol = Color.White * endAlpha * 0.12f;
            for (int g = 0; g < 8; g++)
            {
                float angle = MathHelper.TwoPi / 8f * g + timerPulse * 0.5f;
                float dist = 5f;
                Utils.DrawBorderStringFourWay(spriteBatch, font, mainText,
                    center.X + MathF.Cos(angle) * dist, center.Y + MathF.Sin(angle) * dist,
                    glowCol, Color.Transparent, endOrigin, finalScale);
            }

            for (int dx = -4; dx <= 4; dx++)
                for (int dy = -4; dy <= 4; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    float d = MathF.Sqrt(dx * dx + dy * dy);
                    if (d > 4f) continue;
                    Utils.DrawBorderStringFourWay(spriteBatch, font, mainText,
                        center.X + dx, center.Y + dy,
                        Color.Black * endAlpha * (1f - d / 5f) * 0.4f,
                        Color.Transparent, endOrigin, finalScale);
                }

            Utils.DrawBorderStringFourWay(spriteBatch, font, mainText,
                center.X, center.Y, Color.White * endAlpha, Color.Black * endAlpha * 0.5f,
                endOrigin, finalScale);

            if (showQuestionMark)
            {
                float qTime = questionMarkTimer;

                float qAppear = Math.Clamp(qTime / 2f, 0f, 1f);
                questionMarkAlpha = MathHelper.Lerp(questionMarkAlpha, qAppear, 0.03f);
                questionMarkScale = MathHelper.Lerp(questionMarkScale, 1f, 0.025f);

                if (questionMarkAlpha < 0.01f)
                {
                    questionMarkScale = 3f;
                }

                float qFinalScale = finalScale * questionMarkScale;
                float qAlpha = questionMarkAlpha * endAlpha;

                string qMark = " ?";
                Vector2 fullText = font.MeasureString(mainText + qMark);
                Vector2 qPos = center + new Vector2(endSize.X * finalScale / 2f + 10f, 0);
                Vector2 qOrigin = font.MeasureString(qMark) / 2f;

                float qPulse = 1f + (float)Math.Sin(timerPulse * 2f + qTime) * 0.05f;
                float qFlicker = qTime < 1f ? (0.5f + (float)Math.Sin(qTime * 30f) * 0.5f) : 1f;

                Color qGlow = Color.Red * qAlpha * 0.2f * qFlicker;
                for (int g = 0; g < 6; g++)
                {
                    float angle = MathHelper.TwoPi / 6f * g + timerPulse;
                    Utils.DrawBorderStringFourWay(spriteBatch, font, qMark,
                        qPos.X + MathF.Cos(angle) * 4f, qPos.Y + MathF.Sin(angle) * 4f,
                        qGlow, Color.Transparent, qOrigin, qFinalScale * qPulse);
                }

                for (int dx = -3; dx <= 3; dx++)
                    for (int dy = -3; dy <= 3; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        float d = MathF.Sqrt(dx * dx + dy * dy);
                        if (d > 3f) continue;
                        Utils.DrawBorderStringFourWay(spriteBatch, font, qMark,
                            qPos.X + dx, qPos.Y + dy,
                            Color.Black * qAlpha * (1f - d / 4f) * 0.5f,
                            Color.Transparent, qOrigin, qFinalScale * qPulse);
                    }

                Color qColor = Color.Lerp(Color.White, Color.Red, (float)Math.Sin(timerPulse * 1.5f) * 0.3f + 0.3f);
                Utils.DrawBorderStringFourWay(spriteBatch, font, qMark,
                    qPos.X, qPos.Y, qColor * qAlpha * qFlicker,
                    Color.Black * qAlpha * 0.4f, qOrigin, qFinalScale * qPulse);
            }
        }
    }
}