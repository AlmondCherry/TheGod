using System;
using System.Collections.Generic;
using GodEyeMod.Content.Menu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content
{
    public partial class GodEyeMenu
    {
        private class MusicTrack
        {
            public string DisplayName;
            public string Origin;
            public string FilePath;
            public int MusicSlot = -1;
            public float Duration;

            public float Glitch;
            public string FakeName;
            public float FakeTime;

            public MusicTrack(string displayName, string origin, string filePath, float duration)
            {
                DisplayName = displayName;
                Origin = origin;
                FilePath = filePath;
                Duration = duration;
            }
        }

        private static readonly string[] CreepyTrackNames =
        {
            "he is listening",
            "it was never music",
            "don't turn it off",
            "play me again",
            "your heartbeat",
            "static.ogg",
            "the last thing you hear"
        };

        private static readonly string[] CreepyLabels = { "Listen", "It hears", "Silence", "Music?" };

        private const float NowPlayingDuration = 5f;
        private const float NowPlayingFadeIn = 1f;
        private const float NowPlayingFadeOut = 1.5f;

        private List<MusicTrack> tracks;
        private int currentTrackIndex;
        private float trackTimer;

        private bool musicPanelOpen;
        private float panelOpenProgress;
        private int hoveredTrackIndex = -1;
        private bool hoverMusicButton;
        private bool mouseWasDown;

        private float nowPlayingAlpha;
        private float nowPlayingTimer;
        private float nowPlayingAge;
        private string nowPlayingText = "";
        private string nowPlayingOrigin = "";
        private string nowPlayingFake;

        private float labelGlitch;
        private string labelFake;
        private float labelFakeTime;
        private float panelTear;
        private int panelTearOffset;
        private float clockGlitch;

        private float ghostTimer;
        private float ghostTime;
        private bool ghostHovered;

        private int creditsMusicSlot = -2;

        private int CreditsMusicSlot()
        {
            if (creditsMusicSlot == -2)
            {
                try
                {
                    creditsMusicSlot = MusicLoader.GetMusicSlot(Mod, "Content/Music/GodCredits");
                }
                catch
                {
                    creditsMusicSlot = -1;
                }
            }
            return creditsMusicSlot;
        }

        public override int Music
        {
            get
            {
                // при долгом бездействии музыка затихает
                if (DreadActive)
                    return 0;

                // в титрах — своя музыка
                if (creditsActive && CreditsMusicSlot() > 0)
                    return creditsMusicSlot;

                if (tracks != null && currentTrackIndex >= 0 && currentTrackIndex < tracks.Count && tracks[currentTrackIndex].MusicSlot >= 0)
                    return tracks[currentTrackIndex].MusicSlot;
                return MusicLoader.GetMusicSlot(Mod, "Content/Music/Disgrace");
            }
        }

        private void SetupMusic()
        {
            tracks = new List<MusicTrack>
            {
                new MusicTrack("Disgrace. Humiliation.", "ULTRAKILL", "Content/Music/Disgrace", 110f),
                new MusicTrack("The Fire Is Gone", "ULTRAKILL", "Content/Music/TheFireIsGone", 170f),
                new MusicTrack("Clair de Lune", "Debussy", "Content/Music/ClairDeLune", 301f),
                new MusicTrack("Gentle Wind", "Ambient", "Content/Music/GentleWind", 49f)
            };

            foreach (var track in tracks)
                track.MusicSlot = MusicLoader.GetMusicSlot(Mod, track.FilePath);

            currentTrackIndex = 0;
            trackTimer = 0f;
            musicPanelOpen = false;
            panelOpenProgress = 0f;
            hoveredTrackIndex = -1;
            mouseWasDown = Main.mouseLeft;

            labelGlitch = 0f;
            labelFake = null;
            panelTear = 0f;
            clockGlitch = 0f;
            ghostTimer = MenuFx.NextFloat(20f, 40f);
            ghostTime = 0f;

            TriggerNowPlaying(tracks[0], false);
        }

        private void TriggerNowPlaying(MusicTrack track, bool allowFake)
        {
            nowPlayingText = track.DisplayName;
            nowPlayingOrigin = track.Origin;
            nowPlayingTimer = NowPlayingFadeIn + NowPlayingDuration + NowPlayingFadeOut;
            nowPlayingAlpha = 0f;
            nowPlayingAge = 0f;
            // иногда сначала показывается "не та" песня
            nowPlayingFake = allowFake && !MenuFx.NoGlitchText && MenuFx.Chance(0.15) ? MenuFx.Pick(CreepyTrackNames) : null;
        }

        private void SelectTrack(int index)
        {
            currentTrackIndex = index;
            trackTimer = 0f;
            TriggerNowPlaying(tracks[index], true);
        }

        private void UpdateMusic(float dt)
        {
            if (tracks == null)
                return;

            float target = musicPanelOpen ? 1f : 0f;
            float step = dt * 3f;
            panelOpenProgress = panelOpenProgress < target
                ? Math.Min(panelOpenProgress + step, target)
                : Math.Max(panelOpenProgress - step, target);

            if (nowPlayingTimer > 0f)
            {
                nowPlayingTimer -= dt;
                nowPlayingAge += dt;
                float remaining = nowPlayingTimer;
                if (remaining > NowPlayingDuration + NowPlayingFadeOut)
                    nowPlayingAlpha = MathHelper.Clamp(1f - (remaining - NowPlayingDuration - NowPlayingFadeOut) / NowPlayingFadeIn, 0f, 1f);
                else if (remaining > NowPlayingFadeOut)
                    nowPlayingAlpha = 1f;
                else
                    nowPlayingAlpha = MathHelper.Clamp(remaining / NowPlayingFadeOut, 0f, 1f);
            }
            else
            {
                nowPlayingAlpha = 0f;
            }

            trackTimer += dt;
            if (trackTimer >= tracks[currentTrackIndex].Duration)
                SelectTrack((currentTrackIndex + 1) % tracks.Count);

            UpdatePanelGlitches(dt);
            HandleMusicInput();
        }

        private void UpdatePanelGlitches(float dt)
        {
            bool textGlitches = !MenuFx.NoGlitchText;

            labelGlitch -= dt;
            if (labelGlitch <= 0f && MenuFx.Chance(dt * 0.6f))
                labelGlitch = MenuFx.NextFloat(0.08f, 0.2f);

            if (labelFake != null)
            {
                labelFakeTime -= dt;
                if (labelFakeTime <= 0f)
                    labelFake = null;
            }
            else if (textGlitches && MenuFx.Chance(dt * 0.05f))
            {
                labelFake = MenuFx.Pick(CreepyLabels);
                labelFakeTime = MenuFx.NextFloat(0.5f, 0.9f);
                labelGlitch = 0.15f;
            }

            panelTear -= dt;
            if (panelOpenProgress > 0.9f && panelTear <= 0f && MenuFx.Chance(dt * 0.4f))
            {
                panelTear = MenuFx.NextFloat(0.08f, 0.2f);
                panelTearOffset = MenuFx.NoShake ? 0 : MenuFx.Rand.Next(-6, 7);
            }

            clockGlitch -= dt;
            if (clockGlitch <= 0f && textGlitches && MenuFx.Chance(dt * 0.08f))
                clockGlitch = MenuFx.NextFloat(0.2f, 0.4f);

            foreach (var t in tracks)
            {
                t.Glitch -= dt;
                if (t.Glitch <= 0f && MenuFx.Chance(dt * 0.15f))
                    t.Glitch = MenuFx.NextFloat(0.1f, 0.25f);

                if (t.FakeName != null)
                {
                    t.FakeTime -= dt;
                    if (t.FakeTime <= 0f)
                        t.FakeName = null;
                }
                else if (textGlitches && panelOpenProgress > 0.9f && MenuFx.Chance(dt * 0.03f))
                {
                    t.FakeName = MenuFx.Pick(CreepyTrackNames);
                    t.FakeTime = MenuFx.NextFloat(0.5f, 1f);
                    t.Glitch = 0.15f;
                }
            }

            if (ghostTime > 0f)
            {
                ghostTime -= dt;
            }
            else if (panelOpenProgress > 0.9f && !MenuFx.NoEyeEvents)
            {
                ghostTimer -= dt;
                if (ghostTimer <= 0f)
                {
                    ghostTime = 1.8f;
                    ghostTimer = MenuFx.NextFloat(25f, 50f);
                }
            }
        }

        private void HandleMusicInput()
        {
            bool down = Main.mouseLeft;
            bool clicked = down && !mouseWasDown;
            mouseWasDown = down;

            Point mouse = Main.MouseScreen.ToPoint();

            hoverMusicButton = GetMusicButtonRect().Contains(mouse);
            if (clicked && hoverMusicButton)
            {
                musicPanelOpen = !musicPanelOpen;
                SoundEngine.PlaySound(musicPanelOpen ? SoundID.MenuOpen : SoundID.MenuClose);
                return;
            }

            hoveredTrackIndex = -1;
            ghostHovered = false;
            if (panelOpenProgress < 0.9f)
                return;

            for (int i = 0; i < tracks.Count; i++)
            {
                if (!GetTrackRect(i).Contains(mouse))
                    continue;

                hoveredTrackIndex = i;
                if (clicked && currentTrackIndex != i)
                {
                    SelectTrack(i);
                    SoundEngine.PlaySound(SoundID.MenuTick);
                }
                break;
            }

            if (ghostTime > 0f && GetGhostRect().Contains(mouse))
                ghostHovered = true;
        }

        private static Rectangle GetMusicButtonRect() => new Rectangle(16, 16, 220, 44);

        private static Rectangle GetTrackRect(int index) => new Rectangle(16, 66 + index * 61, 320, 56);

        private Rectangle GetGhostRect()
        {
            Rectangle rect = GetTrackRect(tracks.Count);
            rect.Y += 14;
            return rect;
        }

        private void DrawMusicPanel(SpriteBatch sb)
        {
            if (tracks == null || MenuFx.Font == null)
                return;

            Texture2D pixel = MenuFx.Pixel;
            float eased = MenuFx.EaseInOutCubic(panelOpenProgress);
            bool glitching = labelGlitch > 0f;

            Rectangle btn = GetMusicButtonRect();
            if (glitching && !MenuFx.NoShake)
                btn.X += MenuFx.Rand.Next(-3, 4);

            sb.Draw(pixel, btn, hoverMusicButton ? new Color(30, 25, 50, 210) : new Color(14, 12, 28, 190));
            MenuFx.DrawScanlines(sb, btn, 0.18f, time * 20f);
            MenuFx.DrawBorder(sb, btn, glitching ? new Color(150, 40, 60, 160) : new Color(70, 60, 110, 140));

            if (eyeSpriteSheet != null && eyeSpriteSheet.IsLoaded)
            {
                int frame = (int)(time * 3f) % TotalFrames;
                Rectangle src = new Rectangle(0, frame * FrameHeight, FrameWidth, FrameHeight);
                Vector2 iconPos = new Vector2(btn.X + 22, btn.Y + btn.Height / 2f);
                Color iconColor = hoverMusicButton ? new Color(220, 200, 255) : new Color(160, 140, 200);
                if (glitching)
                    iconColor = Color.Lerp(iconColor, new Color(255, 80, 90), 0.5f);
                sb.Draw(eyeSpriteSheet.Value, iconPos, src, iconColor, 0f, new Vector2(FrameWidth / 2f, FrameHeight / 2f), 0.06f, SpriteEffects.None, 0f);
            }

            string label = labelFake ?? "Music";
            if (glitching)
                label = MenuFx.Scramble(label, 0.3f);
            label += musicPanelOpen ? "   ^" : "   v";

            Color labelColor = hoverMusicButton ? new Color(220, 210, 255) : new Color(160, 150, 200);
            if (labelFake != null)
                labelColor = new Color(200, 90, 100);
            MenuFx.DrawGlitchText(sb, label, new Vector2(btn.X + 44, btn.Y + 10), labelColor, 0.9f, glitching ? 1f : 0.15f);

            if (eased <= 0.01f)
                return;

            int panelHeight = (int)((tracks.Count * 61 + 10) * eased);
            int tearX = panelTear > 0f ? panelTearOffset : 0;
            Rectangle panel = new Rectangle(16 + tearX, 66, 320, panelHeight);

            sb.Draw(pixel, panel, new Color(8, 6, 18, (int)(230 * eased)));
            MenuFx.DrawBorder(sb, panel, new Color(50, 40, 90, (int)(120 * eased)));

            if (eased > 0.3f)
            {
                float alpha = MathHelper.Clamp((eased - 0.3f) / 0.7f, 0f, 1f);
                for (int i = 0; i < tracks.Count; i++)
                {
                    Rectangle rect = GetTrackRect(i);
                    rect.X += tearX;
                    if (rect.Bottom > panel.Bottom)
                        continue;
                    DrawTrack(sb, tracks[i], rect, i == currentTrackIndex, i == hoveredTrackIndex, alpha);
                }
            }

            MenuFx.DrawScanlines(sb, panel, 0.22f * eased, time * 25f);

            if (panelTear > 0f)
            {
                for (int i = 0; i < 4; i++)
                {
                    int y = panel.Y + MenuFx.Rand.Next(Math.Max(1, panel.Height - 6));
                    int hgt = MenuFx.Rand.Next(2, 7);
                    int shift = MenuFx.Rand.Next(-14, 15);
                    sb.Draw(pixel, new Rectangle(panel.X + shift, y, panel.Width, hgt), new Color(120, 90, 200) * 0.18f);
                }
            }

            if (ghostTime > 0f && eased > 0.9f)
                DrawGhostTrack(sb);
        }

        private void DrawTrack(SpriteBatch sb, MusicTrack track, Rectangle rect, bool active, bool hovered, float alpha)
        {
            Texture2D pixel = MenuFx.Pixel;

            Color bg = active ? new Color(40, 30, 70, (int)(190 * alpha))
                : hovered ? new Color(28, 22, 50, (int)(170 * alpha))
                : new Color(12, 10, 25, (int)(150 * alpha));
            sb.Draw(pixel, rect, bg);

            bool glitching = track.Glitch > 0f;
            string name = track.FakeName ?? track.DisplayName;
            if (glitching)
                name = MenuFx.Scramble(name, 0.25f);

            Vector2 namePos = new Vector2(rect.X + 12, rect.Y + 6);
            if (hovered && MenuFx.Chance(0.08))
                namePos.X += MenuFx.Rand.Next(-1, 2);

            Color nameColor = active ? Color.Lerp(new Color(200, 180, 255), Color.White, 0.4f)
                : hovered ? new Color(190, 175, 220)
                : new Color(150, 140, 190);
            if (track.FakeName != null)
                nameColor = new Color(210, 90, 100);

            MenuFx.DrawGlitchText(sb, (active ? "> " : "  ") + name, namePos, nameColor * alpha, 0.85f, glitching ? 1f : 0f);

            Color originColor = new Color(90, 80, 130) * alpha;
            MenuFx.DrawText(sb, "  " + (glitching && MenuFx.Chance(0.3) ? "GOD" : track.Origin), new Vector2(rect.X + 12, rect.Y + 30), originColor, 0.7f, 0.3f);

            if (!active)
                return;

            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, 3, rect.Height), new Color(130, 90, 220) * (0.9f * alpha));
            float pulse = MathF.Sin(time * 3f) * 0.2f + 0.8f;
            MenuFx.DrawBorder(sb, rect, new Color(90, 70, 170) * (0.3f * alpha * pulse));

            // прогресс трека
            float progress = MathHelper.Clamp(trackTimer / track.Duration, 0f, 1f);
            sb.Draw(pixel, new Rectangle(rect.X + 3, rect.Bottom - 3, rect.Width - 3, 2), new Color(40, 30, 70) * alpha);
            sb.Draw(pixel, new Rectangle(rect.X + 3, rect.Bottom - 3, (int)((rect.Width - 3) * progress), 2), new Color(150, 110, 240) * alpha);

            string clockText = clockGlitch > 0f
                ? "6:66 / 6:66"
                : MenuFx.FormatTime(trackTimer) + " / " + MenuFx.FormatTime(track.Duration);
            var font = MenuFx.Font;
            float clockScale = 0.62f;
            Vector2 clockSize = font.MeasureString(clockText) * clockScale;
            Color clockColor = clockGlitch > 0f ? new Color(200, 70, 80) : new Color(110, 95, 160);
            MenuFx.DrawText(sb, clockText, new Vector2(rect.Right - 10 - clockSize.X, rect.Y + 32), clockColor * alpha, clockScale, 0.3f);

            // эквалайзер
            for (int j = 0; j < 5; j++)
            {
                float n = MathF.Abs(MathF.Sin(time * (5.5f + j * 1.7f) + j * 2.1f) * MathF.Sin(time * 2.3f + j * 0.9f));
                if (glitching)
                    n = MenuFx.NextFloat();
                int barH = 3 + (int)(n * 16f);
                int x = rect.Right - 46 + j * 7;
                sb.Draw(pixel, new Rectangle(x, rect.Y + 24 - barH, 4, barH), new Color(150, 110, 240) * (0.8f * alpha));
            }
        }

        private void DrawGhostTrack(SpriteBatch sb)
        {
            Rectangle rect = GetGhostRect();
            float flicker = MenuFx.Chance(0.15) ? 0.2f : 1f;
            float fade = MathHelper.Clamp(Math.Min(ghostTime, 1.8f - ghostTime) / 0.3f, 0f, 1f);
            float alpha = flicker * fade;

            sb.Draw(MenuFx.Pixel, rect, new Color(20, 4, 8) * (0.85f * alpha));
            MenuFx.DrawBorder(sb, rect, new Color(140, 30, 40) * (0.6f * alpha));

            string name = ghostHovered ? "not for you" : MenuFx.Scramble("??????????????", 0.4f);
            MenuFx.DrawGlitchText(sb, "  " + name, new Vector2(rect.X + 12, rect.Y + 6), new Color(220, 80, 90) * alpha, 0.85f, 0.6f);
            MenuFx.DrawText(sb, "  unknown", new Vector2(rect.X + 12, rect.Y + 30), new Color(120, 40, 50) * alpha, 0.7f, 0.3f);
        }

        private void DrawNowPlaying(SpriteBatch sb)
        {
            var font = MenuFx.Font;
            if (nowPlayingAlpha <= 0.01f || font == null)
                return;

            Texture2D pixel = MenuFx.Pixel;
            float a = nowPlayingAlpha;

            string name;
            bool fakeShowing = false;
            if (nowPlayingFake != null && nowPlayingAge < 1.6f)
            {
                if (nowPlayingAge < 1.2f)
                {
                    name = MenuFx.Decode(nowPlayingFake, nowPlayingAge / 0.6f);
                    fakeShowing = true;
                }
                else
                {
                    name = MenuFx.Morph(nowPlayingFake, nowPlayingText, (nowPlayingAge - 1.2f) / 0.4f);
                }
            }
            else
            {
                float start = nowPlayingFake != null ? 1.6f : 0f;
                name = MenuFx.Decode(nowPlayingText, (nowPlayingAge - start) / 0.8f);
            }

            bool glitching = labelGlitch > 0f && MenuFx.Chance(0.5);

            const string label = "Now Playing";
            float widest = Math.Max(font.MeasureString(label).X * 0.7f,
                Math.Max(font.MeasureString(nowPlayingText).X * 0.95f, font.MeasureString(nowPlayingOrigin).X * 0.65f));
            if (nowPlayingFake != null)
                widest = Math.Max(widest, font.MeasureString(nowPlayingFake).X * 0.95f);

            float boxWidth = widest + 36f;
            float boxX = Main.screenWidth - boxWidth - 20f + (1f - MenuFx.EaseOutCubic(a)) * 50f;
            float boxY = 16f;
            if (glitching && !MenuFx.NoShake)
                boxX += MenuFx.Rand.Next(-3, 4);

            Rectangle box = new Rectangle((int)boxX, (int)boxY, (int)boxWidth, 78);
            sb.Draw(pixel, box, new Color(10, 8, 22) * (0.82f * a));
            MenuFx.DrawScanlines(sb, box, 0.2f * a, time * 20f);
            MenuFx.DrawBorder(sb, box, (fakeShowing ? new Color(150, 40, 50) : new Color(70, 50, 130)) * (0.4f * a));
            sb.Draw(pixel, new Rectangle(box.X, box.Y, 2, box.Height), (fakeShowing ? new Color(200, 50, 60) : new Color(120, 80, 200)) * (0.7f * a));

            MenuFx.DrawText(sb, label, new Vector2(boxX + 14, boxY + 8), new Color(100, 80, 160) * a, 0.7f, 0.3f);
            sb.Draw(pixel, new Rectangle((int)(boxX + 10), (int)(boxY + 28), (int)(boxWidth * 0.8f), 1), new Color(60, 40, 110) * (0.3f * a));

            Color nameColor = fakeShowing ? new Color(230, 90, 100) : Color.Lerp(new Color(210, 190, 255), Color.White, 0.3f);
            MenuFx.DrawGlitchText(sb, name, new Vector2(boxX + 14, boxY + 33), nameColor * a, 0.95f, glitching || fakeShowing ? 0.8f : 0f);

            string origin = fakeShowing ? "???" : nowPlayingOrigin;
            MenuFx.DrawText(sb, origin, new Vector2(boxX + 14, boxY + 56), new Color(80, 65, 120) * a, 0.65f, 0.2f);
        }
    }
}
