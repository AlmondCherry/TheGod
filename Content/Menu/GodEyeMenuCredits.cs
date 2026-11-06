using System;
using System.Collections.Generic;
using GodEyeMod.Content.Menu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace GodEyeMod.Content
{
    // свои титры: огромный глаз и везде "GOD IS HERE"
    public partial class GodEyeMenu
    {
        private const int CreditsEntry = 5;

        private static readonly string[] CreditsCategories =
        {
            "CREATED BY", "DIRECTED BY", "PROGRAMMING", "ART", "MUSIC", "SOUND",
            "WRITING", "TESTING", "WATCHING", "SPECIAL THANKS", "IN MEMORY OF", "AND"
        };

        private struct CreditsLine
        {
            public string Text;
            public bool Heading;
            public float Y;
        }

        private bool creditsActive;
        private float creditsTime;
        private int prevMenuMode;
        private int lastMainHover = -1;

        private List<CreditsLine> creditsLines;
        private float creditsHeight;

        // трек титров, если они сейчас открыты; иначе -1
        internal int CreditsMusic => creditsActive ? CreditsMusicSlot() : -1;

        private void UpdateCredits(float dt)
        {
            // в титры попадаем с главного меню, когда последней под мышью была кнопка Credits
            if (Main.menuMode == 0)
            {
                int hover = MenuHoverText.HoveredEntry;
                if (hover >= 0)
                    lastMainHover = hover;
                creditsActive = false;
            }
            else if (prevMenuMode == 0 && lastMainHover == CreditsEntry)
            {
                creditsActive = true;
                creditsTime = 0f;
                BuildCredits();
            }

            var state = Main.MenuUI?.CurrentState;
            if (!creditsActive && Main.menuMode != 0 && state != null && state.GetType().Name.Contains("Credit"))
            {
                creditsActive = true;
                creditsTime = 0f;
                BuildCredits();
            }

            prevMenuMode = Main.menuMode;
            if (creditsActive)
                creditsTime += dt;
        }

        private void BuildCredits()
        {
            creditsLines = new List<CreditsLine>();
            float y = 0f;

            void Add(string text, bool heading, float gapAfter)
            {
                creditsLines.Add(new CreditsLine { Text = text, Heading = heading, Y = y });
                y += gapAfter;
            }

            foreach (string category in CreditsCategories)
            {
                Add(category, true, 34f);
                int names = MenuFx.Rand.Next(2, 5);
                for (int i = 0; i < names; i++)
                    Add("GOD IS HERE", false, 36f);
                y += 60f;
            }
            Add("THANK YOU FOR PLAYING", true, 44f);
            Add("GOD IS HERE", false, 140f);

            creditsHeight = y;

        }

        private void DrawCredits(SpriteBatch sb)
        {
            if (creditsLines == null)
                return;

            int w = Main.screenWidth;
            int h = Main.screenHeight;
            float appear = MathHelper.Clamp(creditsTime / 0.6f, 0f, 1f);

            // кнопку Back внизу не закрываем
            int coverH = (int)(h * 0.86f);
            sb.Draw(MenuFx.Pixel, new Rectangle(0, 0, w, coverH), Color.Black * (0.97f * appear));
            sb.Draw(MenuFx.FadeDown, new Rectangle(0, coverH, w, (int)(h * 0.08f)), Color.Black * (0.97f * appear));

            DrawCreditsEye(sb, appear);
            DrawCreditsRoll(sb, appear, coverH);
        }

        private void DrawCreditsEye(SpriteBatch sb, float appear)
        {
            if (eyeSpriteSheet == null || !eyeSpriteSheet.IsLoaded)
                return;

            int w = Main.screenWidth;
            int h = Main.screenHeight;
            float t = creditsTime;

            float breathe = 1f + 0.03f * MathF.Sin(t * 1.3f);
            float scale = Math.Min(w, h) * 1.25f / FrameWidth * breathe;
            Vector2 center = new Vector2(w / 2f, h * 0.43f);

            // иногда дёргается
            bool twitch = MenuFx.Chance(0.02);
            if (twitch && !MenuFx.NoShake)
                center += new Vector2(MenuFx.NextFloat(-14f, 14f), MenuFx.NextFloat(-6f, 6f));

            // почти всегда смотрит прямо, изредка моргает
            float blinkPhase = t % 7f;
            int frame = blinkPhase > 6.75f ? 3 : twitch ? 2 : 0;
            Rectangle src = new Rectangle(0, frame * FrameHeight, FrameWidth, FrameHeight);
            Vector2 origin = new Vector2(FrameWidth / 2f, FrameHeight / 2f);

            sb.Draw(eyeSpriteSheet.Value, center, src, Color.White * appear, 0f, origin, scale, SpriteEffects.None, 0f);
        }

        private void DrawCreditsRoll(SpriteBatch sb, float appear, int bottom)
        {
            var font = MenuFx.Font;
            if (font == null)
                return;

            int w = Main.screenWidth;
            float speed = 45f;
            float offset = creditsTime * speed % creditsHeight;
            float startY = bottom - offset;

            // рисуем две копии, чтобы титры шли по кругу без дырки
            for (int copy = 0; copy < 2; copy++)
            {
                float baseY = startY + copy * creditsHeight;
                foreach (var line in creditsLines)
                {
                    float y = baseY + line.Y;
                    if (y < -40f || y > bottom)
                        continue;

                    // у краёв титры гаснут
                    float edge = MathHelper.Clamp(Math.Min(y / 120f, (bottom - y) / 120f), 0f, 1f);
                    float scale = line.Heading ? 0.75f : 1.15f;
                    string text = line.Text;
                    float glitch = 0f;
                    if (!line.Heading && MenuFx.Chance(0.015))
                    {
                        text = MenuFx.Scramble(text, 0.3f);
                        glitch = 1f;
                    }

                    Vector2 size = font.MeasureString(line.Text) * scale;
                    Vector2 pos = new Vector2(w / 2f - size.X / 2f, y);
                    Color color = line.Heading ? new Color(150, 140, 150) : new Color(235, 60, 60);
                    MenuFx.DrawGlitchText(sb, text, pos, color * (edge * appear), scale, glitch);
                }
            }
        }
    }
}
