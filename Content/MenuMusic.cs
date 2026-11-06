using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using GodEyeMod.Content.Menu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content
{
    public partial class GodEyeMenu : ModMenu
    {
        private enum EyeType { Static, Floating }

        private class FloatingEye
        {
            public Vector2 Position;
            public Vector2 GatherOffset;
            public float Scale;
            public float Speed;
            public float Opacity;
            public float MaxOpacity;
            public float OpacitySpeed;
            public int Frame;
            public float FrameTimer;
            public float FrameSpeed;
            public float SinOffset;
            public float SinAmplitude;
            public EyeType Type;
            public float Rotation;
            public float RotationSpeed;
        }

        private const int TotalFrames = 6;
        private const int FrameWidth = 400;
        private const int FrameHeight = 400;
        private const int FloatingCount = 14;
        private const int StaticCount = 10;

        private const float GatherAfter = 20f;

        // долгое бездействие: всё темнеет и остаётся один огромный неподвижный глаз
        private const float DreadAfter = 60f;
        private const float DreadFade = 5f;
        private const float DreadEyeDelay = 3f;
        private const float DreadEyeFade = 4f;
        private const float CursorEyeAfter = 8f;

        internal static GodEyeMenu Instance;
        internal static int DrawFrame;

        private List<FloatingEye> eyes;
        private Asset<Texture2D> eyeSpriteSheet;
        private MenuWind wind;

        private readonly Stopwatch clock = new Stopwatch();
        private float time;
        private float smoothDt = 1f / 60f;

        private float screamTime = -1f;
        private bool quitRequested;
        private const float ScreamDuration = 3f;

        // перед скримером — обманка: музыка обрывается, экран гаснет, как будто игра правда закрывается
        private float fakeOut = -1f;
        private const float FakeOutDuration = 0.9f;

        private bool volumesSaved;
        private float savedMusicVolume;
        private float savedSoundVolume;

        private float gradientBreathTimer;

        private Vector2 lastMouse;
        private float idleTime;
        // бездействие меряем по настоящим часам: когда окно не в фокусе, игра рисует реже, а время всё равно идёт
        private readonly Stopwatch idleClock = new Stopwatch();
        private Vector2 idleAnchor;

        private float bigEyeTimer;
        private float bigEyeTime = -1f;
        private const float BigEyeDuration = 4.5f;

        private float logoGlitchTimer;
        private float logoGlitch;
        private Vector2 logoGlitchOffset;

        // в настройках из-за края экрана выглядывает глаз
        private float peekTimer;
        private float peekTime = -1f;
        private int peekSide;
        private const float PeekDuration = 2.6f;

        private float cursorEyeAlpha;
        private int cursorEyeFrame;


        public override string DisplayName => "God Eye Theme";
        public override Asset<Texture2D> Logo => ModContent.Request<Texture2D>("GodEyeMod/Content/Logo");
        public override Asset<Texture2D> SunTexture => ModContent.Request<Texture2D>("GodEyeMod/Content/EyeSun");
        public override Asset<Texture2D> MoonTexture => ModContent.Request<Texture2D>("GodEyeMod/Content/EyeMoon");

        public override void OnSelected()
        {
            Main.dayTime = true;
            Main.time = 27000;

            Instance = this;
            eyes = new List<FloatingEye>();
            eyeSpriteSheet = ModContent.Request<Texture2D>("GodEyeMod/Content/EyeAnim");

            wind = new MenuWind();
            wind.Reset();

            SetupMusic();

            for (int i = 0; i < StaticCount; i++)
                eyes.Add(CreateStaticEye());
            for (int i = 0; i < FloatingCount; i++)
                eyes.Add(CreateFloatingEye(true));

            time = 0f;
            smoothDt = 1f / 60f;
            screamTime = -1f;
            fakeOut = -1f;
            quitRequested = false;
            idleTime = 0f;
            lastMouse = Main.MouseScreen;
            idleAnchor = Main.MouseScreen;
            idleClock.Restart();
            clock.Restart();

            bigEyeTimer = MenuFx.NextFloat(35f, 70f);
            bigEyeTime = -1f;
            cursorEyeAlpha = 0f;
            peekTimer = MenuFx.NextFloat(4f, 8f);
            peekTime = -1f;
            logoGlitch = 0f;
            logoGlitchTimer = MenuFx.NextFloat(20f, 40f);


            MenuHoverText.Reset();
        }

        public override void OnDeselected()
        {
            RestoreVolumes();
            MenuHoverText.Restore();
            if (Instance == this)
                Instance = null;

            clock.Stop();
            eyes = null;
            wind = null;
            tracks = null;
        }

        private FloatingEye CreateStaticEye()
        {
            return new FloatingEye
            {
                Position = RandomStaticPosition(),
                GatherOffset = RandomGatherOffset(),
                Scale = MenuFx.NextFloat(0.08f, 0.26f),
                Opacity = 0f,
                MaxOpacity = MenuFx.NextFloat(0.35f, 0.85f),
                OpacitySpeed = MenuFx.NextFloat(0.12f, 0.36f),
                Frame = MenuFx.Rand.Next(TotalFrames),
                FrameSpeed = MenuFx.NextFloat(1.2f, 3.6f),
                SinOffset = MenuFx.NextFloat(0f, MathHelper.TwoPi),
                SinAmplitude = MenuFx.NextFloat(0.3f, 0.8f),
                Type = EyeType.Static,
                RotationSpeed = MenuFx.NextFloat(-0.06f, 0.06f)
            };
        }

        private FloatingEye CreateFloatingEye(bool randomY)
        {
            return new FloatingEye
            {
                Position = new Vector2(
                    MenuFx.NextFloat(0f, Main.screenWidth),
                    randomY ? MenuFx.NextFloat(0f, Main.screenHeight) : Main.screenHeight + MenuFx.NextFloat(50f, 300f)),
                GatherOffset = RandomGatherOffset(),
                Scale = MenuFx.NextFloat(0.06f, 0.2f),
                Speed = MenuFx.NextFloat(9f, 63f),
                Opacity = 0f,
                MaxOpacity = MenuFx.NextFloat(0.3f, 0.85f),
                OpacitySpeed = MenuFx.NextFloat(0.18f, 0.54f),
                Frame = MenuFx.Rand.Next(TotalFrames),
                FrameSpeed = MenuFx.NextFloat(1.8f, 5.4f),
                SinOffset = MenuFx.NextFloat(0f, MathHelper.TwoPi),
                SinAmplitude = MenuFx.NextFloat(0.8f, 3.3f),
                Type = EyeType.Floating,
                Rotation = MenuFx.NextFloat(-0.12f, 0.12f),
                RotationSpeed = MenuFx.NextFloat(-0.12f, 0.12f)
            };
        }

        private static Vector2 RandomStaticPosition()
        {
            int w = Math.Max(Main.screenWidth, 300);
            int h = Math.Max(Main.screenHeight, 300);
            return new Vector2(MenuFx.NextFloat(100f, w - 100f), MenuFx.NextFloat(60f, h * 0.72f));
        }

        private static Vector2 RandomGatherOffset()
        {
            float angle = MenuFx.NextFloat(0f, MathHelper.TwoPi);
            return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * MenuFx.NextFloat(110f, 320f);
        }

        private void UpdateAll()
        {
            // кадры в меню приходят неровно; без сглаживания всё движется рывками
            float raw = clock.IsRunning ? (float)clock.Elapsed.TotalSeconds : 1f / 60f;
            clock.Restart();
            raw = MathHelper.Clamp(raw, 1f / 240f, 0.05f);
            smoothDt += (raw - smoothDt) * 0.1f;
            float dt = smoothDt;
            time += dt;

            if (fakeOut >= 0f)
            {
                fakeOut += dt;
                if (fakeOut >= FakeOutDuration)
                {
                    fakeOut = -1f;
                    BeginBlast();
                }
            }
            else if (screamTime >= 0f)
            {
                screamTime += dt;
                UpdateScreamWords(dt);
                if (screamTime >= ScreamDuration && !quitRequested)
                {
                    quitRequested = true;
                    RestoreVolumes();
                    GodEyeMenuSystem.QuitForReal();
                }
                else if (quitRequested && screamTime >= ScreamDuration + 3f)
                {
                    // игра почему-то не закрылась — не держим человека в чёрном экране
                    screamTime = -1f;
                    quitRequested = false;
                    RestoreVolumes();
                }
            }

            gradientBreathTimer += dt * 0.12f;

            Vector2 mouse = Main.MouseScreen;
            lastMouse = mouse;

            // движение считаем только в активном окне и только если мышь реально сдвинулась от места остановки
            bool moved = Main.instance.IsActive && Vector2.DistanceSquared(mouse, idleAnchor) > 36f;
            if (moved || !idleClock.IsRunning)
            {
                if (idleTime > GatherAfter && !MenuFx.NoEyeEvents)
                    Startle();
                idleAnchor = mouse;
                idleClock.Restart();
            }
            idleTime = (float)idleClock.Elapsed.TotalSeconds;

            UpdateMusic(dt);
            wind.Update(dt);
            UpdateEyes(dt, mouse);
            UpdateEvents(dt);
            // пока висит глаз от бездействия — никаких щелчков и подмен
            if (DreadActive)
                MenuHoverText.Restore();
            else
                MenuHoverText.Update(dt);
            MenuExtras.UpdateSettings(dt);
            UpdateCredits(dt);
        }

        // глаза собрались вокруг курсора, а ты дёрнул мышку — разбегаются
        private void Startle()
        {
            foreach (var eye in eyes)
                eye.OpacitySpeed = -Math.Max(Math.Abs(eye.OpacitySpeed), 2.5f);
            wind.Gust(180f);
        }

        private void UpdateEyes(float dt, Vector2 mouse)
        {
            bool gathering = idleTime > GatherAfter && !MenuFx.NoEyeEvents;

            for (int i = 0; i < eyes.Count; i++)
            {
                FloatingEye eye = eyes[i];

                eye.FrameTimer += eye.FrameSpeed * dt;
                if (eye.FrameTimer >= 1f)
                {
                    eye.FrameTimer -= 1f;
                    eye.Frame = (eye.Frame + 1) % TotalFrames;
                }

                eye.Rotation += eye.RotationSpeed * dt;

                // глаза не любят, когда на них смотрят
                bool shy = !gathering && Vector2.Distance(eye.Position, mouse) < 60f + eye.Scale * 300f;

                if (eye.Type == EyeType.Static)
                {
                    if (gathering)
                    {
                        Vector2 target = mouse + eye.GatherOffset;
                        eye.Position += (target - eye.Position) * (1f - MathF.Exp(-dt * 0.35f));
                        eye.Opacity = Math.Min(eye.MaxOpacity, eye.Opacity + dt * 0.3f);
                        continue;
                    }

                    eye.Position.Y += MathF.Sin(time + eye.SinOffset) * 3.6f * dt;
                    eye.Position.X += MathF.Sin(time * 0.6f + eye.SinOffset * 1.3f) * 1.8f * dt;

                    if (shy)
                        eye.OpacitySpeed = -Math.Max(Math.Abs(eye.OpacitySpeed), 1.2f);

                    eye.Opacity += eye.OpacitySpeed * dt;
                    if (eye.Opacity >= eye.MaxOpacity)
                    {
                        eye.Opacity = eye.MaxOpacity;
                        eye.OpacitySpeed = -Math.Abs(eye.OpacitySpeed);
                    }
                    if (eye.Opacity <= 0f)
                    {
                        eyes[i] = CreateStaticEye();
                        if (MenuFx.Chance(0.4))
                            eyes[i].Position = eye.Position;
                    }
                }
                else
                {
                    eye.Position.Y -= eye.Speed * dt;
                    eye.Position.X += MathF.Sin(time * 1.5f + eye.SinOffset) * eye.SinAmplitude * 9f * dt
                                    + wind.Wind * 0.05f * eye.Scale * 10f * dt;

                    if (shy)
                        eye.OpacitySpeed = -Math.Max(Math.Abs(eye.OpacitySpeed), 1.5f);

                    eye.Opacity += eye.OpacitySpeed * dt;
                    if (eye.Opacity >= eye.MaxOpacity)
                    {
                        eye.Opacity = eye.MaxOpacity;
                        eye.OpacitySpeed = -Math.Abs(eye.OpacitySpeed) * 0.2f;
                    }

                    if (eye.Position.Y < -300f || (eye.Opacity <= 0f && eye.OpacitySpeed < 0f))
                        eyes[i] = CreateFloatingEye(false);
                }
            }
        }

        private void UpdateEvents(float dt)
        {
            if (!MenuFx.NoEyeEvents)
            {
                if (bigEyeTime >= 0f)
                {
                    bigEyeTime += dt;
                    if (bigEyeTime >= BigEyeDuration)
                        bigEyeTime = -1f;
                }
                else
                {
                    bigEyeTimer -= dt;
                    if (bigEyeTimer <= 0f)
                    {
                        bigEyeTime = 0f;
                        bigEyeTimer = MenuFx.NextFloat(45f, 95f);
                    }
                }
            }
            else
            {
                bigEyeTime = -1f;
            }

            // логотип изредка дёргается
            logoGlitchTimer -= dt;
            if (logoGlitchTimer <= 0f)
            {
                logoGlitch = MenuFx.NextFloat(0.15f, 0.3f);
                logoGlitchTimer = MenuFx.NextFloat(25f, 50f);
            }
            if (logoGlitch > 0f)
            {
                logoGlitch -= dt;
                if (MenuFx.Chance(0.5))
                    logoGlitchOffset = new Vector2(MenuFx.NextFloat(-6f, 6f), MenuFx.NextFloat(-2f, 2f));
            }

            bool inSubMenu = Main.menuMode != 0 && Main.MenuUI?.CurrentState == null && !MenuFx.NoEyeEvents && !creditsActive;
            if (peekTime >= 0f)
            {
                peekTime += dt;
                if (peekTime >= PeekDuration || !inSubMenu)
                    peekTime = -1f;
            }
            else if (inSubMenu)
            {
                peekTimer -= dt;
                if (peekTimer <= 0f)
                {
                    peekTime = 0f;
                    peekSide = MenuFx.Rand.Next(3);
                    peekTimer = MenuFx.NextFloat(8f, 16f);
                }
            }

            bool wantCursorEye = idleTime > CursorEyeAfter && idleTime < GatherAfter + 2f && !MenuFx.NoEyeEvents;
            if (wantCursorEye)
                cursorEyeAlpha = Math.Min(1f, cursorEyeAlpha + dt * 0.5f);
            else
                cursorEyeAlpha = Math.Max(0f, cursorEyeAlpha - dt * 6f);
            cursorEyeFrame = (int)(time * 4f) % TotalFrames;
        }

        public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter,
            ref float logoRotation, ref float logoScale, ref Color drawColor)
        {
            if (eyes == null || wind == null)
                return true;

            DrawFrame++;
            MenuHoverText.ProbeFocus();
            BlockInputWhileScreaming();
            UpdateAll();
            ModListEyes.Apply();

            DrawBackground(spriteBatch);
            DrawBigEye(spriteBatch);
            wind.Draw(spriteBatch, 0f, 0.55f);
            DrawEyes(spriteBatch);
            wind.Draw(spriteBatch, 0.55f, 1.01f);
            DrawVignette(spriteBatch);
            DrawMusicPanel(spriteBatch);
            DrawNowPlaying(spriteBatch);

            drawColor *= 1f - 0.4f * BigEyeStrength();

            if (logoGlitch > 0f)
            {
                if (!MenuFx.NoShake)
                    logoDrawCenter += logoGlitchOffset;
                drawColor = Color.Lerp(drawColor, new Color(255, 200, 210), 0.25f);
            }

            return true;
        }

        public override void PostDrawLogo(SpriteBatch spriteBatch, Vector2 logoDrawCenter, float logoRotation, float logoScale, Color drawColor)
        {
            if (logoGlitch <= 0f)
                return;

            var logo = Logo;
            if (logo == null || !logo.IsLoaded)
                return;

            Texture2D tex = logo.Value;
            Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
            float off = MenuFx.NextFloat(3f, 8f);
            float a = drawColor.A / 255f;

            spriteBatch.Draw(tex, logoDrawCenter + new Vector2(-off, 0f), null, new Color(255, 30, 50, 0) * (0.45f * a), logoRotation, origin, logoScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(tex, logoDrawCenter + new Vector2(off, 0f), null, new Color(30, 220, 255, 0) * (0.45f * a), logoRotation, origin, logoScale, SpriteEffects.None, 0f);
        }

        private float BigEyeStrength()
        {
            if (bigEyeTime < 0f)
                return 0f;
            return MathF.Sin(MathHelper.Pi * MathHelper.Clamp(bigEyeTime / BigEyeDuration, 0f, 1f));
        }

        private void DrawBackground(SpriteBatch sb)
        {
            sb.Draw(MenuFx.Pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black);

            float breathe = MathF.Sin(gradientBreathTimer) * 0.5f + 0.5f;
            float gradientPercent = 0.10f + 0.15f * breathe;

            int glowHeight = Math.Max(1, (int)(Main.screenHeight * gradientPercent));
            float intensity = (120f + breathe * 135f) / 255f;
            byte shade = (byte)(MathHelper.Clamp(intensity, 0f, 1f) * 255f);

            sb.Draw(MenuFx.GlowUp, new Rectangle(0, Main.screenHeight - glowHeight, Main.screenWidth, glowHeight), new Color(shade, shade, shade));
        }

        private void DrawBigEye(SpriteBatch sb)
        {
            float k = BigEyeStrength();
            if (k <= 0.001f || eyeSpriteSheet == null || !eyeSpriteSheet.IsLoaded)
                return;

            Texture2D tex = eyeSpriteSheet.Value;
            int frame = (int)(bigEyeTime * 5f) % TotalFrames;
            Rectangle src = new Rectangle(0, frame * FrameHeight, FrameWidth, FrameHeight);
            float scale = Math.Min(Main.screenWidth, Main.screenHeight) * 1.1f / FrameWidth;
            Vector2 pos = new Vector2(Main.screenWidth / 2f, Main.screenHeight * 0.45f);

            sb.Draw(tex, pos, src, new Color(200, 190, 220) * (0.2f * k), 0f, new Vector2(FrameWidth / 2f, FrameHeight / 2f), scale, SpriteEffects.None, 0f);
        }

        private void DrawVignette(SpriteBatch sb)
        {
            DrawEdges(sb, 180, Color.Black * 0.6f, Color.Black * 0.4f, Color.Transparent);
        }

        private void DrawEyes(SpriteBatch sb)
        {
            if (eyeSpriteSheet == null || !eyeSpriteSheet.IsLoaded)
                return;

            Texture2D tex = eyeSpriteSheet.Value;
            Texture2D dot = MenuFx.SoftDot;
            Vector2 dotOrigin = new Vector2(dot.Width / 2f, dot.Height / 2f);
            Vector2 origin = new Vector2(FrameWidth / 2f, FrameHeight / 2f);

            foreach (FloatingEye eye in eyes)
            {
                if (eye.Opacity <= 0.01f)
                    continue;

                float glowScale = FrameWidth * eye.Scale * 1.1f / dot.Width;
                sb.Draw(dot, eye.Position, null, Color.White * (eye.Opacity * 0.06f), 0f, dotOrigin, glowScale, SpriteEffects.None, 0f);

                Rectangle src = new Rectangle(0, eye.Frame * FrameHeight, FrameWidth, FrameHeight);
                Color color = eye.Type == EyeType.Static
                    ? Color.Lerp(Color.White, new Color(220, 200, 255), 0.15f)
                    : Color.Lerp(Color.White, new Color(255, 160, 120), 0.2f);

                sb.Draw(tex, eye.Position, src, color * eye.Opacity, eye.Rotation, origin, eye.Scale, SpriteEffects.None, 0f);
            }
        }

        // верх, бока и низ экрана затемняются градиентом
        private static void DrawEdges(SpriteBatch sb, int edge, Color top, Color sides, Color bottom)
        {
            int w = Main.screenWidth;
            int h = Main.screenHeight;

            if (top.A > 0)
                sb.Draw(MenuFx.FadeDown, new Rectangle(0, 0, w, edge), top);
            if (bottom.A > 0)
                sb.Draw(MenuFx.FadeDown, new Rectangle(0, h - edge, w, edge), null, bottom, 0f, Vector2.Zero, SpriteEffects.FlipVertically, 0f);
            if (sides.A > 0)
            {
                sb.Draw(MenuFx.FadeRight, new Rectangle(0, 0, edge, h), sides);
                sb.Draw(MenuFx.FadeRight, new Rectangle(w - edge, 0, edge, h), null, sides, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0f);
            }
        }

        // клик по Exit: вместо выхода сначала глаз на весь экран
        internal void BeforeMenuDraw() => BlockInputWhileScreaming();

        internal bool Screaming => screamTime >= 0f || fakeOut >= 0f;

        internal bool DreadActive => idleTime > DreadAfter && !MenuFx.NoEyeEvents && !Screaming;

        private void DrawIdleDread(SpriteBatch sb)
        {
            if (!DreadActive)
                return;

            float over = idleTime - DreadAfter;
            int w = Main.screenWidth;
            int h = Main.screenHeight;

            float dark = MathHelper.Clamp(over / DreadFade, 0f, 1f);
            sb.Draw(MenuFx.Pixel, new Rectangle(-w, -h, w * 3, h * 3), Color.Black * dark);

            float eyeAlpha = MathHelper.Clamp((over - DreadEyeDelay) / DreadEyeFade, 0f, 1f);
            if (eyeAlpha <= 0f || eyeSpriteSheet == null || !eyeSpriteSheet.IsLoaded)
                return;

            // просто картинка: один кадр, без движения
            float scale = w * 1.3f / FrameWidth;
            sb.Draw(eyeSpriteSheet.Value, new Vector2(w / 2f, h / 2f), new Rectangle(0, 0, FrameWidth, FrameHeight), Color.White * eyeAlpha,
                0f, new Vector2(FrameWidth / 2f, FrameHeight / 2f), scale, SpriteEffects.None, 0f);
        }

        // пока глаз на экране, меню не должно реагировать на мышь
        private void BlockInputWhileScreaming()
        {
            if (!Screaming)
                return;

            Main.mouseLeft = false;
            Main.mouseLeftRelease = false;
            Main.mouseRight = false;
            Main.mouseRightRelease = false;
        }

        internal void StartExitScream()
        {
            if (Screaming)
                return;

            fakeOut = 0f;
            screamTime = -1f;
            quitRequested = false;
            MenuHoverText.Restore();
            ResetScreamWords();

            if (!volumesSaved)
            {
                savedMusicVolume = Main.musicVolume;
                savedSoundVolume = Main.soundVolume;
                volumesSaved = true;
            }
            Main.musicVolume = 0f;
        }

        private void BeginBlast()
        {
            screamTime = 0f;
            PlayExitScreamSound();
        }

        // настройки громкости сохраняются при выходе, поэтому возвращаем их до него
        internal void RestoreVolumes()
        {
            if (!volumesSaved)
                return;
            Main.musicVolume = savedMusicVolume;
            Main.soundVolume = savedSoundVolume;
            volumesSaved = false;
        }

        private void DrawExitScream(SpriteBatch sb)
        {
            int w = Main.screenWidth;
            int h = Main.screenHeight;
            float t = screamTime;
            float k = MathHelper.Clamp(t / ScreamDuration, 0f, 1f);

            sb.Draw(MenuFx.Pixel, new Rectangle(-w, -h, w * 3, h * 3), Color.Black);

            if (!MenuFx.NoFlashing && t < 0.06f)
            {
                sb.Draw(MenuFx.Pixel, new Rectangle(-w, -h, w * 3, h * 3), Color.White * 0.7f);
                return;
            }

            float fade = MathHelper.Clamp((ScreamDuration - t) / 0.2f, 0f, 1f);

            if (eyeSpriteSheet != null && eyeSpriteSheet.IsLoaded)
            {
                Texture2D tex = eyeSpriteSheet.Value;

                // сначала прищур, потом глаз распахивается и дёргается
                int frame = t < 0.18f ? 3 : ((int)(t * 16f) % 3 == 0 ? 2 : 0);
                Rectangle src = new Rectangle(0, frame * FrameHeight, FrameWidth, FrameHeight);
                Vector2 origin = new Vector2(FrameWidth / 2f, FrameHeight / 2f);

                float scale = Math.Max(w, h) * 1.4f / FrameWidth * (1f + k * 0.55f);
                float shakePower = MenuFx.NoShake ? 4f : 30f * (1f - k * 0.5f);
                Vector2 center = new Vector2(w / 2f, h / 2f) + new Vector2(MenuFx.NextFloat(-1f, 1f), MenuFx.NextFloat(-1f, 1f)) * shakePower;

                bool redFlash = !MenuFx.NoFlashing && (int)(t * 18f) % 3 == 0;
                Color color = redFlash ? new Color(255, 80, 80) : Color.White;
                float split = 8f + 26f * k;

                sb.Draw(tex, center + new Vector2(-split, 0f), src, new Color(255, 0, 30, 0) * (0.55f * fade), 0f, origin, scale, SpriteEffects.None, 0f);
                sb.Draw(tex, center + new Vector2(split, 0f), src, new Color(0, 230, 255, 0) * (0.55f * fade), 0f, origin, scale, SpriteEffects.None, 0f);
                DrawTornEye(sb, tex, src, center, scale, color * fade, MenuFx.NoShake ? 10f : 70f);
            }

            DrawScreamTears(sb, fade);


            DrawScreamWords(sb, fade);

            var font = MenuFx.Font;
            if (font != null && t > 0.4f)
            {
                string text = t > 1.6f ? "YOU CAN'T LEAVE" : "DON'T LEAVE ME";
                if (MenuFx.Chance(0.25))
                    text = MenuFx.Scramble(text, 0.3f);
                const float textScale = 2.4f;
                Vector2 size = font.MeasureString(text) * textScale;
                Vector2 pos = new Vector2(w / 2f - size.X / 2f, h * 0.78f) + new Vector2(MenuFx.NextFloat(-3f, 3f), 0f);
                MenuFx.DrawGlitchText(sb, text, pos, new Color(255, 40, 50) * fade, textScale, 1f);
            }

            // тьма в последние мгновения
            if (k > 0.93f)
                sb.Draw(MenuFx.Pixel, new Rectangle(-w, -h, w * 3, h * 3), Color.Black * ((k - 0.93f) / 0.07f));
        }

        private void DrawFakeOut(SpriteBatch sb)
        {
            int w = Main.screenWidth;
            int h = Main.screenHeight;
            float a = MathHelper.Clamp(fakeOut / 0.3f, 0f, 1f);
            sb.Draw(MenuFx.Pixel, new Rectangle(-w, -h, w * 3, h * 3), Color.Black * a);

            if (fakeOut > 0.35f)
                MenuFx.DrawText(sb, "Saving settings...", new Vector2(w - 190f, h - 40f), new Color(90, 90, 90) * a, 0.7f, 0f);
        }

        // глаз режется на горизонтальные полосы, часть из них съезжает в сторону
        private static void DrawTornEye(SpriteBatch sb, Texture2D tex, Rectangle src, Vector2 center, float scale, Color color, float tearPower)
        {
            const int strips = 14;
            int stripH = src.Height / strips;
            for (int i = 0; i < strips; i++)
            {
                Rectangle part = new Rectangle(src.X, src.Y + i * stripH, src.Width, stripH);
                Vector2 origin = new Vector2(src.Width / 2f, src.Height / 2f - i * stripH);
                float shift = MenuFx.Chance(0.35) ? MenuFx.NextFloat(-1f, 1f) * tearPower : 0f;
                sb.Draw(tex, center + new Vector2(shift, 0f), part, color, 0f, origin, scale, SpriteEffects.None, 0f);
            }
        }

        // рваные полосы по всему экрану во время скримера
        private void DrawScreamTears(SpriteBatch sb, float fade)
        {
            int w = Main.screenWidth;
            int h = Main.screenHeight;
            int count = MenuFx.Rand.Next(4, 12);
            for (int i = 0; i < count; i++)
            {
                int bandH = MenuFx.Rand.Next(2, 26);
                int y = MenuFx.Rand.Next(0, Math.Max(1, h - bandH));
                Color c;
                float r = MenuFx.NextFloat();
                if (MenuFx.NoFlashing)
                    c = Color.Black * 0.8f;
                else if (r < 0.4f)
                    c = Color.Black * 0.85f;
                else if (r < 0.75f)
                    c = new Color(255, 30, 40) * 0.35f;
                else
                    c = Color.White * 0.25f;
                sb.Draw(MenuFx.Pixel, new Rectangle(0, y, w, bandH), c * fade);
            }
        }

        // глаз медленно выползает из-за края, смотрит и резко прячется
        private void DrawPeekingEye(SpriteBatch sb)
        {
            if (eyeSpriteSheet == null || !eyeSpriteSheet.IsLoaded)
                return;

            float t = peekTime;
            float show;
            if (t < 1f)
                show = MenuFx.EaseOutCubic(t / 1f);
            else if (t < PeekDuration - 0.25f)
                show = 1f;
            else
                show = 1f - (t - (PeekDuration - 0.25f)) / 0.25f;
            show = MathHelper.Clamp(show, 0f, 1f);

            float scale = 0.6f;
            float size = FrameWidth * scale;
            int w = Main.screenWidth;
            int h = Main.screenHeight;

            // наполовину спрятан за краем
            float hidden = size * 0.5f + 20f;
            float shown = size * 0.12f;
            float inset = MathHelper.Lerp(-hidden, shown, show);

            Vector2 pos;
            float rotation;
            switch (peekSide)
            {
                case 0:
                    pos = new Vector2(inset, h * 0.55f);
                    rotation = 0f;
                    break;
                case 1:
                    pos = new Vector2(w - inset, h * 0.4f);
                    rotation = 0f;
                    break;
                default:
                    pos = new Vector2(w * 0.72f, h - inset);
                    rotation = 0f;
                    break;
            }

            // пока смотрит — не двигается, моргает в конце
            int frame = t > 1f && t < PeekDuration - 0.6f ? 0 : 3;
            Rectangle src = new Rectangle(0, frame * FrameHeight, FrameWidth, FrameHeight);
            sb.Draw(eyeSpriteSheet.Value, pos, src, Color.White * 0.9f, rotation,
                new Vector2(FrameWidth / 2f, FrameHeight / 2f), scale, SpriteEffects.None, 0f);
        }

        internal void DrawOverlay(SpriteBatch sb)
        {
            if (fakeOut >= 0f)
            {
                DrawFakeOut(sb);
                return;
            }
            if (screamTime >= 0f)
            {
                DrawExitScream(sb);
                return;
            }
            if (creditsActive)
            {
                DrawCredits(sb);
                DrawIdleDread(sb);
                return;
            }

            // на "Exit" края экрана наливаются красным
            if (MenuHoverText.HoveredEntry == MenuHoverText.ExitIndex)
            {
                float pulse = 0.22f + 0.1f * MathF.Sin(time * 4f);
                Color c = new Color(120, 0, 10) * pulse;
                DrawEdges(sb, 140, c, c, c);
            }

            if (peekTime >= 0f)
                DrawPeekingEye(sb);

            if (cursorEyeAlpha > 0.01f && eyeSpriteSheet != null && eyeSpriteSheet.IsLoaded)
            {
                Rectangle src = new Rectangle(0, cursorEyeFrame * FrameHeight, FrameWidth, FrameHeight);
                Vector2 pos = Main.MouseScreen + new Vector2(34f, -26f);
                sb.Draw(eyeSpriteSheet.Value, pos, src, Color.White * (cursorEyeAlpha * 0.8f), 0f,
                    new Vector2(FrameWidth / 2f, FrameHeight / 2f), 0.075f, SpriteEffects.None, 0f);
            }

            DrawIdleDread(sb);
        }
    }
}
