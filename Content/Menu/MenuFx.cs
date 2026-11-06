using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace GodEyeMod.Content.Menu
{
    internal static class MenuFx
    {
        public static readonly Random Rand = new Random();

        private const string Junk = "#$%&@!?/\\|_-=+*<>01";

        private static Texture2D softDot;

        public static GodEyeConfig Config => ModContent.GetInstance<GodEyeConfig>();
        public static bool NoFlashing => Config?.DisableFlashing ?? false;
        public static bool NoShake => Config?.DisableScreenShake ?? false;
        public static bool NoEyeEvents => Config?.DisableEyeEvents ?? false;
        public static bool NoGlitchText => Config?.DisableGlitchText ?? false;
        public static bool NoSystemWhispers => Config?.DisableSystemInfoWhispers ?? false;

        public static DynamicSpriteFont Font
        {
            get
            {
                var asset = FontAssets.MouseText;
                return asset != null && asset.IsLoaded ? asset.Value : null;
            }
        }

        public static Texture2D Pixel => TextureAssets.MagicPixel.Value;

        // мягкая круглая точка, рисуется один раз при первом обращении (из потока отрисовки)
        public static Texture2D SoftDot
        {
            get
            {
                if (softDot == null || softDot.IsDisposed)
                {
                    const int size = 32;
                    var data = new Color[size * size];
                    float r = size / 2f;
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float dx = x + 0.5f - r;
                            float dy = y + 0.5f - r;
                            float d = MathF.Sqrt(dx * dx + dy * dy) / r;
                            float a = MathHelper.Clamp(1f - d, 0f, 1f);
                            data[y * size + x] = Color.White * (a * a);
                        }
                    }
                    softDot = new Texture2D(Main.instance.GraphicsDevice, size, size);
                    softDot.SetData(data);
                }
                return softDot;
            }
        }

        private static Texture2D fadeDown;
        private static Texture2D fadeRight;
        private static Texture2D glowUp;

        // сверху непрозрачно, книзу в ноль
        public static Texture2D FadeDown => fadeDown == null || fadeDown.IsDisposed
            ? fadeDown = MakeGradient(1, 256, t => 1f - t)
            : fadeDown;

        // слева непрозрачно, вправо в ноль
        public static Texture2D FadeRight => fadeRight == null || fadeRight.IsDisposed
            ? fadeRight = MakeGradient(256, 1, t => 1f - t)
            : fadeRight;

        // свечение снизу экрана
        public static Texture2D GlowUp => glowUp == null || glowUp.IsDisposed
            ? glowUp = MakeGradient(1, 256, t => t * t * t)
            : glowUp;

        private static Texture2D MakeGradient(int width, int height, Func<float, float> curve)
        {
            int len = Math.Max(width, height);
            var data = new Color[width * height];
            for (int i = 0; i < len; i++)
                data[i] = Color.White * MathHelper.Clamp(curve(i / (float)(len - 1)), 0f, 1f);

            var tex = new Texture2D(Main.instance.GraphicsDevice, width, height);
            tex.SetData(data);
            return tex;
        }

        public static void DisposeTextures()
        {
            var list = new[] { softDot, fadeDown, fadeRight, glowUp };
            softDot = null;
            fadeDown = null;
            fadeRight = null;
            glowUp = null;
            Main.QueueMainThreadAction(() =>
            {
                foreach (var tex in list)
                    tex?.Dispose();
            });
        }

        public static bool NoScreamer => Config?.DisableScreamer ?? false;

        public static float NextFloat() => (float)Rand.NextDouble();
        public static float NextFloat(float min, float max) => min + (float)Rand.NextDouble() * (max - min);
        public static bool Chance(double p) => Rand.NextDouble() < p;
        public static T Pick<T>(T[] items) => items[Rand.Next(items.Length)];

        public static char JunkChar() => Junk[Rand.Next(Junk.Length)];

        public static string Scramble(string text, float amount)
        {
            if (string.IsNullOrEmpty(text) || amount <= 0f)
                return text;

            var sb = new StringBuilder(text.Length);
            foreach (char c in text)
                sb.Append(c != ' ' && Rand.NextDouble() < amount ? JunkChar() : c);
            return sb.ToString();
        }

        // текст "проявляется" слева направо из мусора
        public static string Decode(string text, float progress)
        {
            if (string.IsNullOrEmpty(text) || progress >= 1f)
                return text;

            int shown = (int)(text.Length * MathHelper.Clamp(progress, 0f, 1f));
            var sb = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                if (i < shown || text[i] == ' ')
                    sb.Append(text[i]);
                else
                    sb.Append(JunkChar());
            }
            return sb.ToString();
        }

        // плавный переход одной надписи в другую через мусор
        public static string Morph(string from, string to, float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            if (t < 0.5f)
                return Scramble(from, t * 2f);
            return Decode(to, (t - 0.5f) * 2f);
        }

        public static void DrawText(SpriteBatch sb, string text, Vector2 pos, Color color, float scale, float shadow = 0.5f)
        {
            var font = Font;
            if (font == null || string.IsNullOrEmpty(text))
                return;

            if (shadow > 0f)
                sb.DrawString(font, text, pos + Vector2.One, Color.Black * (shadow * color.A / 255f), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            sb.DrawString(font, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        // текст с RGB-расслоением, glitch от 0 до 1
        public static void DrawGlitchText(SpriteBatch sb, string text, Vector2 pos, Color color, float scale, float glitch)
        {
            var font = Font;
            if (font == null || string.IsNullOrEmpty(text))
                return;

            if (glitch > 0.01f)
            {
                float alpha = color.A / 255f;
                float off = 1f + glitch * 3.5f;
                Vector2 jitter = new Vector2(NextFloat(-1f, 1f), 0f) * glitch * 2f;
                sb.DrawString(font, text, pos + new Vector2(-off, 0f) + jitter, new Color(255, 40, 60, 0) * (0.55f * alpha * glitch), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                sb.DrawString(font, text, pos + new Vector2(off, 0f) - jitter, new Color(40, 220, 255, 0) * (0.55f * alpha * glitch), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
            DrawText(sb, text, pos, color, scale);
        }

        public static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color, int t = 1)
        {
            var px = Pixel;
            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, t), color);
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - t, rect.Width, t), color);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, t, rect.Height), color);
            sb.Draw(px, new Rectangle(rect.Right - t, rect.Y, t, rect.Height), color);
        }

        public static void DrawScanlines(SpriteBatch sb, Rectangle rect, float alpha, float scroll)
        {
            if (alpha <= 0f)
                return;

            var px = Pixel;
            int offset = (int)scroll % 3;
            for (int y = rect.Y + offset; y < rect.Bottom; y += 3)
                sb.Draw(px, new Rectangle(rect.X, y, rect.Width, 1), Color.Black * alpha);
        }

        public static float EaseInOutCubic(float t) => t < 0.5f ? 4f * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 3) / 2f;
        public static float EaseOutCubic(float t) => 1f - MathF.Pow(1f - t, 3);

        public static string FormatTime(float seconds)
        {
            int s = Math.Max(0, (int)seconds);
            return (s / 60) + ":" + (s % 60).ToString("00");
        }
    }
}
