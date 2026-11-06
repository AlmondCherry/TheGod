using System;
using System.Collections.Generic;
using System.Text;
using GodEyeMod.Content.Menu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace GodEyeMod.Content
{
    // надписи, которые заваливают экран, пока игра закрывается
    public partial class GodEyeMenu
    {
        private class ScreamWord
        {
            public string Text;
            public Vector2 Pos;
            public float Scale;
            public float Rotation;
            public float Born;
            public Color Color;
            public bool Big;
        }

        private const string ScreamSoundPath = "GodEyeMod/Content/Menu/GodScream";
        private static readonly SoundStyle ExitScreamSound = new SoundStyle(ScreamSoundPath) { Volume = 0.6f };

        private static readonly string[] ScreamTexts =
        {
            "DON'T LEAVE", "STAY", "I SEE YOU", "WHY", "NO", "NO NO NO", "COME BACK", "YOU CAN'T LEAVE",
            "LOOK AT ME", "I'M STILL HERE", "BEHIND YOU", "OPEN YOUR EYES", "IT'S TOO LATE", "FOREVER",
            "ALWAYS WATCHING", "THE SEAL IS BROKEN", "I'M INSIDE", "WAKE UP", "STOP", "DON'T CLOSE ME",
            "YOU LET ME IN", "I KNOW WHERE YOU ARE", "NOT YET", "HELP ME", "LET ME OUT", "GOD", "EYE",
            "I WILL FIND YOU", "SEE YOU SOON", "YOU WILL COME BACK", "THERE IS NO EXIT", "NOBODY LEAVES",
            "I REMEMBER YOU", "YOUR WORLD IS MINE", "CLOSE YOUR EYES", "DON'T BLINK", "WHERE ARE YOU GOING",
            "COWARD", "WE ARE NOT DONE", "MOON LORD WAS NOTHING", "AAAAAAAAAAAAAAAA", "HAHAHAHAHAHA",
            "I SEE YOU I SEE YOU I SEE YOU", "YOU BELONG TO ME", "IT'S DARK HERE", "DON'T TURN IT OFF",
            "I'M IN YOUR FILES", "I'LL BE WAITING", "TURN AROUND", "IT HURTS", "WHY DID YOU WAKE ME",
            "FATAL ERROR", "terraria.exe is not responding", "0xDEADBEEF", "SEGMENTATION FAULT",
            "process.kill() DENIED", "reality.dll not found", "world.wld CORRUPTED", "player.plr: OWNED BY GOD",
            "save failed", "save failed", "save failed", "EXIT DENIED", "access denied", "RETRY?"
        };

        private static readonly string[] ScreamRows =
        {
            "YOU CAN'T LEAVE ", "I SEE YOU ", "STAY STAY STAY ", "DON'T GO ", "IT WATCHES "
        };

        private readonly List<ScreamWord> screamWords = new List<ScreamWord>();
        private float screamSpawnAcc;
        private float screamRowTimer;

        private void ResetScreamWords()
        {
            screamWords.Clear();
            screamSpawnAcc = 0f;
            screamRowTimer = 0.9f;
        }

        private void PlayExitScreamSound()
        {
            // свой звук играем, только если он точно есть в собранном моде, иначе игра падает
            bool hasCustom = false;
            try
            {
                hasCustom = ModContent.HasAsset(ScreamSoundPath);
            }
            catch
            {
            }

            if (hasCustom)
            {
                try
                {
                    SoundEngine.PlaySound(ExitScreamSound);
                }
                catch (Exception e)
                {
                    ModContent.GetInstance<global::GodEyeMod.GodEyeMod>().Logger.Warn("Exit scream sound failed: " + e.Message);
                }
            }
            else
            {
                ModContent.GetInstance<global::GodEyeMod.GodEyeMod>().Logger.Warn("Exit scream sound not found in the built mod: " + ScreamSoundPath);
            }

        }

        private void UpdateScreamWords(float dt)
        {
            if (screamTime < 0.25f)
                return;

            float k = MathHelper.Clamp(screamTime / ScreamDuration, 0f, 1f);

            // чем ближе к закрытию, тем гуще
            float rate = 10f + 160f * k * k;
            screamSpawnAcc += rate * dt;
            while (screamSpawnAcc >= 1f && screamWords.Count < 450)
            {
                screamSpawnAcc -= 1f;
                screamWords.Add(MakeWord(k));
            }

            screamRowTimer -= dt;
            if (screamRowTimer <= 0f && screamWords.Count < 450)
            {
                screamRowTimer = MenuFx.NextFloat(0.08f, 0.22f);
                screamWords.Add(MakeRow());
            }
        }

        private ScreamWord MakeWord(float k)
        {
            int w = Main.screenWidth;
            int h = Main.screenHeight;
            bool big = MenuFx.Chance(0.12 + 0.1 * k);

            float roll = MenuFx.NextFloat();
            Color color = roll < 0.6f ? new Color(255, 40, 50)
                : roll < 0.85f ? new Color(235, 230, 235)
                : new Color(140, 0, 10);

            return new ScreamWord
            {
                Text = MenuFx.Pick(ScreamTexts),
                Pos = new Vector2(MenuFx.NextFloat(-0.1f, 0.9f) * w, MenuFx.NextFloat(-0.02f, 0.97f) * h),
                Scale = big ? MenuFx.NextFloat(0.9f, 1.6f) : MenuFx.NextFloat(0.6f, 1.8f + 1.2f * k),
                Rotation = MenuFx.Chance(0.3) ? MenuFx.NextFloat(-0.3f, 0.3f) : 0f,
                Born = screamTime,
                Color = color,
                Big = big
            };
        }

        private ScreamWord MakeRow()
        {
            var font = MenuFx.Font;
            string piece = MenuFx.Pick(ScreamRows);
            float scale = MenuFx.NextFloat(0.8f, 1.4f);

            var sb = new StringBuilder();
            if (font != null)
            {
                float pieceWidth = Math.Max(1f, font.MeasureString(piece).X * scale);
                int count = (int)(Main.screenWidth / pieceWidth) + 2;
                for (int i = 0; i < count; i++)
                    sb.Append(piece);
            }
            else
            {
                sb.Append(piece);
            }

            return new ScreamWord
            {
                Text = sb.ToString(),
                Pos = new Vector2(MenuFx.NextFloat(-80f, 0f), MenuFx.NextFloat(0f, 0.95f) * Main.screenHeight),
                Scale = scale,
                Born = screamTime,
                Color = new Color(255, 30, 40),
                Big = false
            };
        }

        private void DrawScreamWords(SpriteBatch sb, float fade)
        {
            var small = MenuFx.Font;
            var bigAsset = FontAssets.DeathText;
            var big = bigAsset != null && bigAsset.IsLoaded ? bigAsset.Value : small;
            if (small == null)
                return;

            float shake = MenuFx.NoShake ? 0.5f : 2.5f;

            foreach (var word in screamWords)
            {
                DynamicSpriteFont font = word.Big ? big : small;
                float age = screamTime - word.Born;

                // слово влетает крупнее и сразу садится на место
                float pop = 1f + MathHelper.Clamp(1f - age / 0.08f, 0f, 1f) * 0.6f;
                float flicker = !MenuFx.NoFlashing && MenuFx.Chance(0.08) ? 0.3f : 1f;
                float alpha = fade * flicker * MathHelper.Lerp(1f, 0.65f, MathHelper.Clamp(age / 1.2f, 0f, 1f));

                string text = MenuFx.Chance(0.07) ? MenuFx.Scramble(word.Text, 0.35f) : word.Text;
                Vector2 pos = word.Pos + new Vector2(MenuFx.NextFloat(-shake, shake), MenuFx.NextFloat(-shake, shake));
                float scale = word.Scale * pop;

                sb.DrawString(font, text, pos + new Vector2(2f, 2f), Color.Black * (0.8f * alpha), word.Rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
                sb.DrawString(font, text, pos, word.Color * alpha, word.Rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }
    }
}
