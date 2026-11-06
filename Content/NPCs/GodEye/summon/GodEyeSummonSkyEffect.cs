using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace GodEyeMod.Content.NPCs.GodEye.summon
{
    public class GodEyeSummonSky : CustomSky
    {
        private bool _isActive;
        private float _opacity;

        public override void Activate(Vector2 position, params object[] args)
        {
            _isActive = true;
        }

        public override void Deactivate(params object[] args)
        {
            _isActive = false;
        }

        public override bool IsActive() => _isActive || _opacity > 0.01f;

        public override void Reset()
        {
            _isActive = false;
            _opacity = 0f;
        }

        public override void Update(GameTime gameTime)
        {
            if (_isActive && GodEyeSummonSystem.SummonSequenceActive)
            {
                _opacity = MathHelper.Lerp(_opacity, 1f, 0.05f);
            }
            else
            {
                _opacity = MathHelper.Lerp(_opacity, 0f, 0.1f);
                if (_opacity < 0.01f) _isActive = false;
            }
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (_opacity <= 0.01f) return;
            if (!(minDepth < 1f && maxDepth > 0f)) return;

            spriteBatch.Draw(
                Terraria.GameContent.TextureAssets.MagicPixel.Value,
                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                Color.Black * _opacity * 0.8f
            );

            if (GodEyeSummonSystem.SequencePhase >= 3)
            {
                float time = (float)Main.gameTimeCache.TotalGameTime.TotalSeconds;
                Random seededRand = new Random(12345);

                int eyeCount = 30;
                for (int i = 0; i < eyeCount; i++)
                {
                    float x = seededRand.Next(Main.screenWidth);
                    float y = seededRand.Next(Main.screenHeight / 2);
                    float twinkle = (float)Math.Sin(time * 2 + i * 0.7f) * 0.5f + 0.5f;

                    float fadeIn = Math.Min(1f,
                        (GodEyeSummonSystem.SequenceTimer - GodEyeSummonSystem.PHASE_RIFT_START)
                        / 60f);

                    Color eyeColor = new Color(200, 0, 0) * twinkle * _opacity * fadeIn * 0.5f;
                    int size = (int)(2 + twinkle * 3);

                    spriteBatch.Draw(
                        Terraria.GameContent.TextureAssets.MagicPixel.Value,
                        new Rectangle((int)x, (int)y, size, size),
                        eyeColor
                    );
                }
            }

            if (GodEyeSummonSystem.RiftScale > 0)
            {
                float time = GodEyeSummonSystem.SequenceTimer * 0.03f;
                Vector2 riftPos = GodEyeSummonSystem.SummonWorldPosition - Main.screenPosition;

                for (int ring = 0; ring < 5; ring++)
                {
                    float ringTime = time - ring * 0.5f;
                    if (ringTime < 0) continue;

                    float expand = (ringTime % 3f) / 3f;
                    float ringAlpha = (1f - expand) * 0.3f * _opacity;
                    float radius = expand * 500;

                    int points = 40;
                    for (int p = 0; p < points; p++)
                    {
                        float angle = (float)p / points * MathHelper.TwoPi;
                        Vector2 pos = riftPos + new Vector2(
                            (float)Math.Cos(angle) * radius,
                            (float)Math.Sin(angle) * radius * 0.5f
                        );

                        spriteBatch.Draw(
                            Terraria.GameContent.TextureAssets.MagicPixel.Value,
                            new Rectangle((int)pos.X, (int)pos.Y, 3, 3),
                            new Color(100, 0, 180) * ringAlpha
                        );
                    }
                }
            }
        }

        public override Color OnTileColor(Color inColor)
        {
            if (_opacity <= 0.01f) return inColor;

            Color tint = Color.Lerp(inColor,
                new Color(80, 0, 120),
                _opacity * 0.4f);

            return tint;
        }

        public override float GetCloudAlpha() => 1f - _opacity;
    }

    public class GodEyeSummonSkyLoader : ModSystem
    {
        public override void Load()
        {
            if (Main.dedServ) return;
            SkyManager.Instance["GodEyeMod:SummonSky"] = new GodEyeSummonSky();
        }
    }
}