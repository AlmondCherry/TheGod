using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace GodEyeMod.Content.NPCs.GodEye.summon
{
    public class GodEyeSummonRenderer : ModSystem
    {
        private static Asset<Texture2D> _eyeTexture;

        private static float _eyeScale;
        private static float _eyeOpacity;
        private static float _eyeRotation;

        private static List<EyeAfterimage> _afterimages = new List<EyeAfterimage>();

        public override void Load()
        {
            if (Main.dedServ) return;

            _eyeTexture = ModContent.Request<Texture2D>(
                "GodEyeMod/Content/NPCs/GodEye/summon/GodEyeTexture",
                AssetRequestMode.ImmediateLoad);

            On_Main.DrawInterface += DrawSummonOverlay;
        }

        public override void Unload()
        {
            if (Main.dedServ) return;
            On_Main.DrawInterface -= DrawSummonOverlay;
            _eyeTexture = null;
            _afterimages?.Clear();
        }

        private void DrawSummonOverlay(On_Main.orig_DrawInterface orig, Main self, GameTime gameTime)
        {
            orig(self, gameTime);

            if (!GodEyeSummonSystem.SummonSequenceActive) return;

            SpriteBatch sb = Main.spriteBatch;

            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.UIScaleMatrix);

            try
            {
                DrawDarknessOverlay(sb);
                DrawPersistentGlitches(sb);
                DrawGlitchBlocks(sb);
                DrawScreenTear(sb);
                DrawRift(sb);
                DrawTheEye(sb);
                DrawFlash(sb);
                DrawUIGlitches(sb);
                DrawScanlines(sb);
                DrawInvertOverlay(sb);
                DrawCorruptedHealthBar(sb);
            }
            finally
            {
                sb.End();
            }
        }

        private void DrawPersistentGlitches(SpriteBatch sb)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            foreach (PersistentGlitch pg in GodEyeSummonSystem.PersistentGlitches)
            {
                if (!pg.Visible) continue;

                Color drawColor = pg.Color * pg.Opacity;
                Vector2 pos = pg.ScreenPosition;

                pos += new Vector2(
                    Main.rand.NextFloat(-GodEyeSummonSystem.UIShakeIntensity, GodEyeSummonSystem.UIShakeIntensity),
                    Main.rand.NextFloat(-GodEyeSummonSystem.UIShakeIntensity, GodEyeSummonSystem.UIShakeIntensity)
                ) * 0.3f;

                switch (pg.Type)
                {
                    case 0:
                        sb.Draw(pixel,
                            new Rectangle((int)pos.X, (int)pos.Y, (int)pg.Size.X, (int)pg.Size.Y),
                            null, drawColor, pg.Angle, Vector2.Zero, SpriteEffects.None, 0);
                        break;

                    case 1:
                        sb.Draw(pixel,
                            new Rectangle(0, (int)pos.Y, Main.screenWidth, Math.Max(1, (int)pg.Size.Y)),
                            drawColor * 0.4f);
                        break;

                    case 2:
                        sb.Draw(pixel,
                            new Rectangle((int)pos.X, 0, Math.Max(1, (int)pg.Size.X), Main.screenHeight),
                            drawColor * 0.3f);
                        break;

                    case 3:
                        for (int p = 0; p < 8; p++)
                        {
                            Vector2 offset = new Vector2(
                                Main.rand.Next(-(int)pg.Size.X, (int)pg.Size.X),
                                Main.rand.Next(-(int)pg.Size.Y, (int)pg.Size.Y));
                            sb.Draw(pixel,
                                new Rectangle(
                                    (int)(pos.X + offset.X),
                                    (int)(pos.Y + offset.Y),
                                    Main.rand.Next(1, 6), Main.rand.Next(1, 6)),
                                drawColor);
                        }
                        break;

                    case 4:
                        int cellSize = Math.Max(1, (int)(pg.Size.X / 4));
                        for (int cx = 0; cx < 4; cx++)
                        {
                            for (int cy = 0; cy < 4; cy++)
                            {
                                if ((cx + cy) % 2 == 0)
                                {
                                    sb.Draw(pixel,
                                        new Rectangle(
                                            (int)pos.X + cx * cellSize,
                                            (int)pos.Y + cy * cellSize,
                                            cellSize, cellSize),
                                        drawColor);
                                }
                            }
                        }
                        break;

                    case 5:
                        if (pg.CorruptedText != null)
                        {
                            Vector2 textPos = pos;
                            ChatManager.DrawColorCodedStringWithShadow(
                                sb, Terraria.GameContent.FontAssets.DeathText.Value,
                                pg.CorruptedText, textPos,
                                drawColor, 0f, Vector2.Zero,
                                new Vector2(pg.Size.X / 30f));
                        }
                        else
                        {
                            sb.Draw(pixel,
                                new Rectangle((int)pos.X, (int)pos.Y, (int)pg.Size.X, (int)pg.Size.Y),
                                drawColor);
                        }
                        break;
                }
            }
        }

        private void DrawScreenTear(SpriteBatch sb)
        {
            if (!GodEyeSummonSystem.ScreenTearActive) return;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int tearY = GodEyeSummonSystem.ScreenTearY;
            float offset = GodEyeSummonSystem.ScreenTearOffset;

            sb.Draw(pixel,
                new Rectangle(0, tearY - 1, Main.screenWidth, 3),
                Color.Black);

            for (int i = 0; i < 20; i++)
            {
                int x = Main.rand.Next(Main.screenWidth);
                int w = Main.rand.Next(5, 40);

                Color tearColor = Main.rand.Next(3) switch
                {
                    0 => new Color(255, 0, 0, 150),
                    1 => new Color(0, 255, 0, 150),
                    _ => new Color(0, 0, 255, 150),
                };

                sb.Draw(pixel,
                    new Rectangle(x + (int)offset, tearY - 2, w, 5),
                    tearColor * 0.5f);
            }
        }

        private void DrawInvertOverlay(SpriteBatch sb)
        {
            if (!GodEyeSummonSystem.InvertColors) return;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            sb.Draw(pixel,
                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                Color.White * 0.6f);
        }

        private void DrawCorruptedHealthBar(SpriteBatch sb)
        {
            if (!GodEyeSummonSystem.HealthBarCorrupted) return;

            Texture2D pixel = TextureAssets.MagicPixel.Value;

            int barCount = 3 + (int)(GodEyeSummonSystem.GlitchIntensity * 5);
            for (int i = 0; i < barCount; i++)
            {
                if (!Main.rand.NextBool(3)) continue;

                float x = Main.rand.NextFloat(0, Main.screenWidth - 200);
                float y = Main.rand.NextFloat(0, Main.screenHeight - 30);
                float w = Main.rand.NextFloat(60, 250);
                float fill = Main.rand.NextFloat(0f, 1f);

                sb.Draw(pixel, new Rectangle((int)x - 1, (int)y - 1, (int)w + 2, 14), Color.Black * 0.7f);
                sb.Draw(pixel, new Rectangle((int)x, (int)y, (int)w, 12), new Color(40, 0, 0) * 0.8f);

                Color barColor = Main.rand.NextBool()
                    ? new Color(255, 0, 0)
                    : new Color(0, 255, 0);
                sb.Draw(pixel, new Rectangle((int)x, (int)y, (int)(w * fill), 12), barColor * 0.7f);

                if (Main.rand.NextBool(2))
                {
                    string hpText = Main.rand.Next(4) switch
                    {
                        0 => $"{Main.rand.Next(-999, 0)}/{Main.rand.Next(100, 999)}",
                        1 => "NaN/NaN",
                        2 => "∞/0",
                        _ => $"???/{Main.rand.Next(1, 99999)}",
                    };

                    ChatManager.DrawColorCodedStringWithShadow(
                        sb, Terraria.GameContent.FontAssets.MouseText.Value,
                        hpText, new Vector2(x + w + 5, y - 2),
                        barColor * 0.8f, 0f, Vector2.Zero, Vector2.One * 0.8f);
                }
            }
        }

        private void DrawTheEye(SpriteBatch sb)
        {
            if (GodEyeSummonSystem.EyeOpenAmount <= 0f) return;
            if (_eyeTexture == null || !_eyeTexture.IsLoaded) return;

            Texture2D eyeTex = _eyeTexture.Value;

            float openAmount = GodEyeSummonSystem.EyeOpenAmount;
            float time = GodEyeSummonSystem.SequenceTimer * 0.02f;

            Vector2 eyeScreenCenter = GodEyeSummonSystem.SummonWorldPosition - Main.screenPosition;

            float baseScale;
            if (openAmount < 0.2f)
            {
                baseScale = MathHelper.Lerp(0.01f, 2.5f, EaseOutBack(openAmount / 0.2f));
            }
            else if (openAmount < 0.5f)
            {
                float t = (openAmount - 0.2f) / 0.3f;
                baseScale = MathHelper.Lerp(2.5f, 3.5f, t);
            }
            else if (openAmount < 0.8f)
            {
                float t = (openAmount - 0.5f) / 0.3f;
                baseScale = MathHelper.Lerp(3.5f, 4f, t);
            }
            else
            {
                float pulse = (float)Math.Sin(time * 2f) * 0.15f;
                float heavyPulse = (float)Math.Sin(time * 0.5f) * 0.3f;
                baseScale = 4f + pulse + heavyPulse;
            }

            _eyeScale = baseScale;

            if (openAmount < 0.1f)
            {
                _eyeOpacity = MathHelper.Lerp(0f, 0.2f, openAmount / 0.1f);
            }
            else if (openAmount < 0.3f)
            {
                float t = (openAmount - 0.1f) / 0.2f;
                _eyeOpacity = MathHelper.Lerp(0.2f, 0.7f, t);
            }
            else if (openAmount < 0.6f)
            {
                float t = (openAmount - 0.3f) / 0.3f;
                _eyeOpacity = MathHelper.Lerp(0.7f, 0.95f, t);
            }
            else
            {
                _eyeOpacity = MathHelper.Lerp(0.95f, 1f, (openAmount - 0.6f) / 0.4f);
            }

            _eyeRotation = (float)Math.Sin(time * 0.5f) * 0.03f * (1f - openAmount * 0.7f);

            if (openAmount < 0.15f)
            {
                float chaosAmount = 1f - (openAmount / 0.15f);
                _eyeRotation += chaosAmount * (float)Math.Sin(time * 25f) * 0.8f;
            }

            Vector2 eyeOrigin = new Vector2(eyeTex.Width * 0.5f, eyeTex.Height * 0.5f);

            DrawEyeGlow(sb, eyeScreenCenter, openAmount, time);

            DrawAfterimages(sb, eyeTex, eyeScreenCenter, eyeOrigin, openAmount, time);

            float scaleX = _eyeScale;
            float scaleY = _eyeScale;

            if (openAmount < 0.25f)
            {
                scaleY *= MathHelper.Lerp(0.02f, 1f, EaseOutQuad(openAmount / 0.25f));
            }

            Color eyeColor;
            if (openAmount < 0.4f)
            {
                float tintLerp = openAmount / 0.4f;
                Color tinted = new Color(
                    (int)MathHelper.Lerp(80, 255, tintLerp),
                    (int)MathHelper.Lerp(0, 255, tintLerp),
                    (int)MathHelper.Lerp(180, 255, tintLerp)
                );
                eyeColor = tinted * _eyeOpacity;
            }
            else
            {
                eyeColor = Color.White * _eyeOpacity;
            }

            if (openAmount < 0.3f && Main.rand.NextBool(4))
            {
                eyeColor *= Main.rand.NextFloat(0.3f, 1f);
            }

            sb.Draw(
                eyeTex,
                eyeScreenCenter,
                null,
                eyeColor,
                _eyeRotation,
                eyeOrigin,
                new Vector2(scaleX, scaleY),
                SpriteEffects.None,
                0f
            );

            if (openAmount > 0.3f && openAmount < 0.8f)
            {
                float echoAlpha = (float)Math.Sin(time * 8) * 0.15f;
                float echoScale = _eyeScale * 1.1f;
                Color echoColor = new Color(150, 0, 255) * Math.Abs(echoAlpha);

                sb.Draw(eyeTex, eyeScreenCenter, null, echoColor,
                    _eyeRotation + 0.02f, eyeOrigin,
                    new Vector2(echoScale, echoScale * (openAmount < 0.25f ? MathHelper.Lerp(0.02f, 1f, openAmount / 0.25f) : 1f)),
                    SpriteEffects.None, 0f);
            }

            if (openAmount > 0.35f)
            {
                DrawEyeTendrils(sb, eyeScreenCenter, _eyeScale, openAmount, time);
            }

            if (openAmount > 0.15f)
            {
                DrawEyeParticles(sb, eyeScreenCenter, _eyeScale, openAmount, time);
            }

            if (openAmount > 0.5f)
            {
                DrawEyeEnergyRing(sb, eyeScreenCenter, openAmount, time);
            }

            UpdateAfterimages(eyeScreenCenter, openAmount);
        }

        private void DrawEyeGlow(SpriteBatch sb, Vector2 center, float openAmount, float time)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            float pulse = (float)Math.Sin(time * 1.5f) * 0.3f + 0.7f;
            float glowSize = 200f * _eyeScale * openAmount;

            int layers = 12;
            for (int i = layers; i >= 0; i--)
            {
                float t = (float)i / layers;
                float layerSize = glowSize * (0.2f + t * 0.8f);
                float alpha = (1f - t) * 0.06f * openAmount * pulse;

                float sinVal = (float)Math.Sin(time + t * 3) * 0.2f;

                int r = (int)MathHelper.Clamp(MathHelper.Lerp(150, 255, t + sinVal), 0, 255);
                int b = (int)MathHelper.Clamp(MathHelper.Lerp(200, 50, t + sinVal), 0, 255);

                Color glowColor = new Color(r, 0, b) * alpha;

                Rectangle glowRect = new Rectangle(
                    (int)(center.X - layerSize),
                    (int)(center.Y - layerSize * 0.6f),
                    (int)(layerSize * 2),
                    (int)(layerSize * 1.2f)
                );

                sb.Draw(pixel, glowRect, glowColor);
            }

            if (openAmount > 0.5f)
            {
                int rayCount = 24;
                for (int i = 0; i < rayCount; i++)
                {
                    float angle = (float)i / rayCount * MathHelper.TwoPi + time * 0.2f;
                    float rayLength = (80 + (float)Math.Sin(time * 1.5f + i * 1.2f) * 50) * _eyeScale * openAmount;
                    float rayWidth = 2.5f * _eyeScale;

                    Vector2 rayStart = center + new Vector2(
                        (float)Math.Cos(angle) * 50 * _eyeScale,
                        (float)Math.Sin(angle) * 30 * _eyeScale
                    );

                    Vector2 rayEnd = center + new Vector2(
                        (float)Math.Cos(angle) * rayLength,
                        (float)Math.Sin(angle) * rayLength * 0.6f
                    );

                    Color rayColor = new Color(120, 0, 180) * (openAmount * 0.25f * pulse);
                    DrawLine(sb, pixel, rayStart, rayEnd, rayWidth, rayColor);
                }
            }
        }

        private void DrawEyeEnergyRing(SpriteBatch sb, Vector2 center, float openAmount, float time)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            int ringCount = 3;
            for (int ring = 0; ring < ringCount; ring++)
            {
                float ringTime = time * (1.5f + ring * 0.3f);
                float radius = (70 + ring * 30) * _eyeScale;
                float pulse = (float)Math.Sin(ringTime) * 10 * _eyeScale;

                int segments = 50;
                for (int s = 0; s < segments; s++)
                {
                    float angle = (float)s / segments * MathHelper.TwoPi + ringTime;

                    float gapCheck = (float)Math.Sin(angle * 3 + ring) * 0.5f + 0.5f;
                    if (gapCheck < 0.3f) continue;

                    Vector2 pos = center + new Vector2(
                        (float)Math.Cos(angle) * (radius + pulse),
                        (float)Math.Sin(angle) * (radius + pulse) * 0.5f
                    );

                    float segAlpha = gapCheck * openAmount * 0.4f;
                    int size = Math.Max(1, (int)(3 * _eyeScale * gapCheck));

                    Color ringColor = Color.Lerp(
                        new Color(100, 0, 200),
                        new Color(255, 0, 100),
                        (float)Math.Sin(angle + time) * 0.5f + 0.5f
                    ) * segAlpha;

                    sb.Draw(pixel,
                        new Rectangle((int)pos.X, (int)pos.Y, size, size),
                        ringColor);
                }
            }
        }

        private void DrawAfterimages(SpriteBatch sb, Texture2D eyeTex, Vector2 center,
            Vector2 origin, float openAmount, float time)
        {
            if (openAmount < 0.15f) return;

            foreach (EyeAfterimage afterimage in _afterimages)
            {
                float lifeRatio = (float)afterimage.Life / afterimage.MaxLife;
                float alpha = lifeRatio * 0.25f * openAmount;

                Color afterColor = new Color(100, 0, 150) * alpha;

                float afterScale = _eyeScale * (1f + (1f - lifeRatio) * 0.6f);
                float afterScaleY = afterScale;
                if (openAmount < 0.25f)
                {
                    afterScaleY *= MathHelper.Lerp(0.02f, 1f, openAmount / 0.25f);
                }

                sb.Draw(eyeTex, afterimage.Position, null, afterColor,
                    _eyeRotation + afterimage.RotationOffset, origin,
                    new Vector2(afterScale, afterScaleY),
                    SpriteEffects.None, 0f);
            }
        }

        private void UpdateAfterimages(Vector2 center, float openAmount)
        {
            if (openAmount > 0.15f && GodEyeSummonSystem.SequenceTimer % 4 == 0)
            {
                _afterimages.Add(new EyeAfterimage
                {
                    Position = center + Main.rand.NextVector2Circular(15, 15),
                    RotationOffset = Main.rand.NextFloat(-0.15f, 0.15f),
                    Life = 25,
                    MaxLife = 25
                });
            }

            for (int i = _afterimages.Count - 1; i >= 0; i--)
            {
                _afterimages[i].Life--;
                _afterimages[i].Position += Main.rand.NextVector2Circular(3, 3);

                if (_afterimages[i].Life <= 0)
                {
                    _afterimages.RemoveAt(i);
                }
            }
        }

        private void DrawEyeTendrils(SpriteBatch sb, Vector2 center,
            float scale, float openAmount, float time)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            int tendrilCount = 24;
            for (int i = 0; i < tendrilCount; i++)
            {
                float baseAngle = (float)i / tendrilCount * MathHelper.TwoPi;

                Vector2 start = center + new Vector2(
                    (float)Math.Cos(baseAngle) * 48 * scale,
                    (float)Math.Sin(baseAngle) * 30 * scale
                );

                Vector2 prev = start;
                int segments = 8 + (int)(openAmount * 8);

                for (int s = 0; s < segments; s++)
                {
                    float st = (float)s / segments;
                    float segAngle = baseAngle +
                        (float)Math.Sin(time * 2.5f + s * 0.7f + i * 0.4f) * 0.7f * st;

                    float segLength = (10 + st * 15) * scale * openAmount;

                    Vector2 next = prev + new Vector2(
                        (float)Math.Cos(segAngle) * segLength,
                        (float)Math.Sin(segAngle) * segLength
                    );

                    float thickness = (1f - st) * 3f * scale;
                    float tendrilAlpha = Math.Max(0f, ((openAmount - 0.35f) / 0.65f) * (1f - st * 0.6f) * 0.6f);

                    int tr = (int)MathHelper.Lerp(100, 200, st);
                    int tb = (int)MathHelper.Lerp(180, 50, st);

                    Color tendrilColor = new Color(tr, 0, tb) * tendrilAlpha;
                    DrawLine(sb, pixel, prev, next, thickness, tendrilColor);
                    prev = next;
                }

                if (openAmount > 0.6f)
                {
                    float dotPulse = (float)Math.Sin(time * 4 + i) * 0.5f + 0.5f;
                    float dotAlpha = dotPulse * Math.Max(0f, (openAmount - 0.6f) / 0.4f) * 0.7f;

                    sb.Draw(pixel,
                        new Rectangle((int)(prev.X - 2), (int)(prev.Y - 2), 5, 5),
                        new Color(255, 0, 100) * dotAlpha);
                }
            }
        }

        private void DrawEyeParticles(SpriteBatch sb, Vector2 center,
            float scale, float openAmount, float time)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            int particleCount = (int)(openAmount * 40);

            for (int i = 0; i < particleCount; i++)
            {
                float angle = (float)i / particleCount * MathHelper.TwoPi + time * (1f + i * 0.008f);
                float dist = (70 + (float)Math.Sin(time * 1.5f + i * 0.25f) * 25) * scale;

                Vector2 particlePos = center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist * 0.5f
                );

                float particleAlpha = ((float)Math.Sin(time * 2.5f + i) * 0.5f + 0.5f) * openAmount * 0.45f;
                int size = 2 + (int)(Main.rand.NextFloat() * 4);

                float colorLerp = (float)Math.Sin(i * 0.4f) * 0.5f + 0.5f;
                int pr = (int)MathHelper.Lerp(150, 255, colorLerp);
                int pg = (int)MathHelper.Lerp(0, 50, colorLerp);
                int pb = (int)MathHelper.Lerp(255, 50, colorLerp);

                Color particleColor = new Color(pr, pg, pb) * particleAlpha;

                sb.Draw(pixel,
                    new Rectangle((int)particlePos.X, (int)particlePos.Y, size, size),
                    particleColor);
            }
        }

        private void DrawDarknessOverlay(SpriteBatch sb)
        {
            if (GodEyeSummonSystem.DarknessOverlay <= 0) return;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            sb.Draw(pixel,
                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                Color.Black * GodEyeSummonSystem.DarknessOverlay);

            DrawVignette(sb, GodEyeSummonSystem.DarknessOverlay * 0.4f);
        }

        private void DrawVignette(SpriteBatch sb, float strength)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int bands = 20;

            for (int i = 0; i < bands; i++)
            {
                float t = (float)i / bands;
                float alpha = t * t * strength;
                int maxDim = Math.Max(Main.screenWidth, Main.screenHeight);
                int border = (int)((1f - t) * maxDim * 0.3f);
                int thickness = Math.Max(1, border / bands);
                Color bandColor = Color.Black * alpha;

                sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, thickness), bandColor);
                sb.Draw(pixel, new Rectangle(0, Main.screenHeight - thickness, Main.screenWidth, thickness), bandColor);
                sb.Draw(pixel, new Rectangle(0, 0, thickness, Main.screenHeight), bandColor);
                sb.Draw(pixel, new Rectangle(Main.screenWidth - thickness, 0, thickness, Main.screenHeight), bandColor);
            }
        }

        private void DrawGlitchBlocks(SpriteBatch sb)
        {
            if (GodEyeSummonSystem.GlitchIntensity <= 0) return;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float glitchInt = GodEyeSummonSystem.GlitchIntensity;

            foreach (GlitchBlock glitch in GodEyeSummonSystem.ActiveGlitches)
            {
                float lifeRatio = (float)glitch.Lifetime / glitch.MaxLifetime;
                Vector2 screenPos = glitch.Position - Main.screenPosition;

                if (screenPos.X < -100 || screenPos.X > Main.screenWidth + 100) continue;
                if (screenPos.Y < -100 || screenPos.Y > Main.screenHeight + 100) continue;

                Color drawColor = glitch.Color * (lifeRatio * glitchInt);

                switch (glitch.GlitchType)
                {
                    case 0:
                        sb.Draw(pixel,
                            new Rectangle((int)screenPos.X, (int)screenPos.Y,
                                (int)glitch.Size, (int)(glitch.Size * 0.3f)),
                            drawColor);
                        break;
                    case 1:
                        sb.Draw(pixel,
                            new Rectangle(0, (int)screenPos.Y,
                                Main.screenWidth, Math.Max(1, (int)(glitch.Size * 0.15f))),
                            drawColor * 0.5f);
                        break;
                    case 2:
                        for (int p = 0; p < 6; p++)
                        {
                            Vector2 offset = new Vector2(
                                Main.rand.Next(-(int)glitch.Size, (int)glitch.Size),
                                Main.rand.Next(-(int)glitch.Size, (int)glitch.Size));
                            sb.Draw(pixel,
                                new Rectangle(
                                    (int)(screenPos.X + offset.X),
                                    (int)(screenPos.Y + offset.Y),
                                    Main.rand.Next(2, 10), Main.rand.Next(2, 10)),
                                drawColor);
                        }
                        break;
                    case 3:
                        sb.Draw(pixel,
                            new Rectangle((int)screenPos.X, (int)screenPos.Y,
                                (int)glitch.Size, (int)glitch.Size),
                            Color.White * (lifeRatio * 0.3f * glitchInt));
                        break;
                    case 4:
                        for (int d = 0; d < (int)glitch.Size; d++)
                        {
                            sb.Draw(pixel,
                                new Rectangle(
                                    (int)screenPos.X + d,
                                    (int)screenPos.Y + d,
                                    3, 3),
                                drawColor * 0.7f);
                        }
                        break;
                }
            }

            if (glitchInt > 0.4f && Main.rand.NextBool(2))
            {
                Color stripeColor = new Color(Main.rand.Next(255), Main.rand.Next(255), Main.rand.Next(255)) * 0.2f;
                sb.Draw(pixel,
                    new Rectangle(Main.rand.Next(-40, 40), Main.rand.Next(Main.screenHeight),
                        Main.screenWidth, Main.rand.Next(1, 25)),
                    stripeColor);
            }
        }

        private void DrawRift(SpriteBatch sb)
        {
            if (GodEyeSummonSystem.RiftScale <= 0) return;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 riftScreenPos = GodEyeSummonSystem.SummonWorldPosition - Main.screenPosition;
            float scale = GodEyeSummonSystem.RiftScale;
            float time = GodEyeSummonSystem.SequenceTimer * 0.02f;

            int ringCount = 12;
            for (int ring = ringCount; ring >= 0; ring--)
            {
                float ringScale = (float)ring / ringCount;
                float ringSize = 250 * scale * (0.3f + ringScale * 0.7f);
                float pulse = (float)Math.Sin(time * 3 + ring * 0.7f) * 0.2f + 0.8f;

                Color ringColor;
                if (ring == 0)
                {
                    ringColor = Color.Black;
                }
                else
                {
                    float colorAlpha = (1f - ringScale) * pulse * scale;
                    int rr = (int)MathHelper.Lerp(100, 200, ringScale);
                    int rb = (int)MathHelper.Lerp(200, 50, ringScale);
                    ringColor = new Color(rr, 0, rb) * colorAlpha;
                }

                int segments = 40;
                for (int s = 0; s < segments; s++)
                {
                    float angle = (float)s / segments * MathHelper.TwoPi;
                    Vector2 pos = riftScreenPos + new Vector2(
                        (float)Math.Cos(angle) * ringSize * pulse,
                        (float)Math.Sin(angle) * ringSize * pulse * 0.5f
                    );

                    int dotSize = Math.Max(1, (int)(4 * scale));
                    sb.Draw(pixel,
                        new Rectangle((int)pos.X, (int)pos.Y, dotSize, dotSize),
                        ringColor);
                }
            }

            int blackSize = (int)(50 * scale);
            sb.Draw(pixel,
                new Rectangle(
                    (int)(riftScreenPos.X - blackSize),
                    (int)(riftScreenPos.Y - blackSize / 2),
                    blackSize * 2, blackSize),
                Color.Black * scale);
        }

        private void DrawFlash(SpriteBatch sb)
        {
            if (GodEyeSummonSystem.FlashIntensity <= 0) return;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            sb.Draw(pixel,
                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                Color.White * GodEyeSummonSystem.FlashIntensity);
        }

        private void DrawUIGlitches(SpriteBatch sb)
        {
            if (GodEyeSummonSystem.GlitchIntensity <= 0.2f) return;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float intensity = GodEyeSummonSystem.GlitchIntensity;

            if (Main.rand.NextBool(8))
            {
                Vector2 pos = new Vector2(Main.rand.Next(Main.screenWidth), Main.rand.Next(Main.screenHeight));
                sb.Draw(pixel,
                    new Rectangle((int)pos.X, (int)pos.Y, Main.rand.Next(50, 250), Main.rand.Next(4, 12)),
                    new Color(255, 0, 0) * (intensity * 0.5f));
            }

            if (Main.rand.NextBool(4))
            {
                int side = Main.rand.Next(4);
                int thickness = Main.rand.Next(2, 8);
                Rectangle rect = side switch
                {
                    0 => new Rectangle(0, 0, Main.screenWidth, thickness),
                    1 => new Rectangle(0, Main.screenHeight - thickness, Main.screenWidth, thickness),
                    2 => new Rectangle(0, 0, thickness, Main.screenHeight),
                    _ => new Rectangle(Main.screenWidth - thickness, 0, thickness, Main.screenHeight),
                };
                Color edgeColor = Main.rand.NextBool()
                    ? new Color(255, 0, 0)
                    : new Color(255, 0, 255);
                sb.Draw(pixel, rect, edgeColor * (intensity * 0.8f));
            }
        }

        private void DrawScanlines(SpriteBatch sb)
        {
            if (GodEyeSummonSystem.SequencePhase < 2) return;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float intensity = GodEyeSummonSystem.GlitchIntensity * 0.15f;

            int spacing = Math.Max(2, 4 - (int)(GodEyeSummonSystem.GlitchIntensity * 2));
            for (int y = 0; y < Main.screenHeight; y += spacing)
            {
                sb.Draw(pixel,
                    new Rectangle(0, y, Main.screenWidth, 1),
                    Color.Black * intensity);
            }
        }

        private void DrawLine(SpriteBatch sb, Texture2D pixel,
            Vector2 start, Vector2 end, float thickness, Color color)
        {
            Vector2 diff = end - start;
            float length = diff.Length();
            if (length < 1f) return;

            float angle = (float)Math.Atan2(diff.Y, diff.X);
            int drawThickness = Math.Max(1, (int)thickness);

            sb.Draw(pixel,
                new Rectangle((int)start.X, (int)start.Y, (int)length, drawThickness),
                null, color, angle,
                new Vector2(0, 0.5f),
                SpriteEffects.None, 0);
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * (float)Math.Pow(t - 1, 3) + c1 * (float)Math.Pow(t - 1, 2);
        }

        private float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    }

    public class EyeAfterimage
    {
        public Vector2 Position;
        public float RotationOffset;
        public int Life;
        public int MaxLife;
    }
}