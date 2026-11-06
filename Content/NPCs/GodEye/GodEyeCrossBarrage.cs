using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content.NPCs.GodEye
{
    public class GodEyeCrossBarrage : ModProjectile
    {
        private int Timer
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private float FadeProgress
        {
            get => Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }

        private List<Vector2> trailPositions = new List<Vector2>();
        private List<float> trailRotations = new List<float>();
        private List<float> trailScales = new List<float>();
        private const int MaxTrailLength = 10;

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.scale = 0.1f;
            Projectile.light = 0.3f;
            Projectile.Opacity = 0f;
        }

        public override void AI()
        {
            Timer++;

            if (Timer < 10)
            {
                float progress = Timer / 10f;
                Projectile.scale = MathHelper.Lerp(0.1f, 2.2f, EaseOutBack(progress));
                Projectile.Opacity = MathHelper.Lerp(0f, 1f, progress);
                Projectile.rotation += MathHelper.ToRadians(25f) * (1f - progress);

                for (int i = 0; i < 3; i++)
                {
                    Dust d = Dust.NewDustDirect(Projectile.Center + Main.rand.NextVector2Circular(30, 30),
                        1, 1, DustID.WhiteTorch, 0, 0, 0, Color.White, 2f);
                    d.noGravity = true;
                    d.velocity = Main.rand.NextVector2Circular(5, 5);
                    d.fadeIn = 1.2f;
                }
            }
            else if (Timer < 120)
            {
                float pulse = (float)Math.Sin(Timer * 0.2f) * 0.15f;
                Projectile.scale = 2.0f + pulse;
                Projectile.Opacity = 0.85f + (float)Math.Sin(Timer * 0.3f) * 0.15f;

                Projectile.rotation += MathHelper.ToRadians(1.5f);

                if (Main.rand.NextBool(4))
                {
                    Dust d = Dust.NewDustDirect(Projectile.Center + Main.rand.NextVector2Circular(40, 40),
                        1, 1, DustID.WhiteTorch, 0, 0, 80, Color.White, 1f);
                    d.noGravity = true;
                    d.velocity = (Projectile.Center - d.position).SafeNormalize(Vector2.Zero) * 1f;
                }

                if (Main.rand.NextBool(20))
                {
                    Projectile.position += Main.rand.NextVector2Circular(4, 4);
                    Dust d = Dust.NewDustDirect(Projectile.Center, 1, 1,
                        DustID.Vortex, 0, 0, 150, default, 1.5f);
                    d.noGravity = true;
                }
            }
            else
            {
                float fadeTime = (Timer - 120f) / 30f;
                Projectile.scale = MathHelper.Lerp(2.0f, 0.3f, fadeTime);
                Projectile.Opacity = MathHelper.Lerp(0.85f, 0f, fadeTime);
                Projectile.rotation += MathHelper.ToRadians(8f) * fadeTime;

                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustDirect(Projectile.Center + Main.rand.NextVector2Circular(20, 20),
                        1, 1, DustID.PurpleTorch, 0, 0, 100, default, 1.2f);
                    d.noGravity = true;
                    d.velocity = Main.rand.NextVector2Circular(3, 3);
                }
            }

            if (Timer % 2 == 0)
            {
                trailPositions.Insert(0, Projectile.Center);
                trailRotations.Insert(0, Projectile.rotation);
                trailScales.Insert(0, Projectile.scale);
                if (trailPositions.Count > MaxTrailLength)
                {
                    trailPositions.RemoveAt(trailPositions.Count - 1);
                    trailRotations.RemoveAt(trailRotations.Count - 1);
                    trailScales.RemoveAt(trailScales.Count - 1);
                }
            }

            Projectile.light = 0.2f + Projectile.Opacity * 0.5f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = tex.Size() / 2f;
            SpriteBatch sb = Main.spriteBatch;

            for (int i = trailPositions.Count - 1; i >= 0; i--)
            {
                float trailOp = (1f - (float)i / trailPositions.Count) * 0.3f * Projectile.Opacity;
                float tScale = trailScales.Count > i ? trailScales[i] : Projectile.scale;
                float tRot = trailRotations.Count > i ? trailRotations[i] : Projectile.rotation;

                sb.Draw(tex, trailPositions[i] - Main.screenPosition,
                    null, Color.White * trailOp, tRot, origin, tScale * 0.9f, SpriteEffects.None, 0f);
            }

            if (Projectile.Opacity > 0.3f)
            {
                float g = 3f * Projectile.Opacity;
                sb.Draw(tex, Projectile.Center - Main.screenPosition + new Vector2(g, 0),
                    null, Color.Red * 0.2f * Projectile.Opacity,
                    Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
                sb.Draw(tex, Projectile.Center - Main.screenPosition + new Vector2(-g, 0),
                    null, Color.Blue * 0.2f * Projectile.Opacity,
                    Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }

            float glowPulse = (float)Math.Sin(Main.GameUpdateCount * 0.1f) * 0.1f + 0.9f;
            sb.Draw(tex, Projectile.Center - Main.screenPosition,
                null, Color.White * 0.2f * Projectile.Opacity * glowPulse,
                Projectile.rotation, origin, Projectile.scale * 1.3f, SpriteEffects.None, 0f);

            sb.Draw(tex, Projectile.Center - Main.screenPosition,
                null, Color.MediumPurple * 0.15f * Projectile.Opacity,
                Projectile.rotation, origin, Projectile.scale * 1.4f, SpriteEffects.None, 0f);

            sb.Draw(tex, Projectile.Center - Main.screenPosition,
                null, Color.White * Projectile.Opacity, Projectile.rotation, origin,
                Projectile.scale, SpriteEffects.None, 0f);

            return false;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Main.rand.NextBool(4))
                target.AddBuff(BuffID.Confused, 45);
            if (Main.rand.NextBool(3))
                target.AddBuff(BuffID.Slow, 60);

            for (int i = 0; i < 8; i++)
            {
                Dust d = Dust.NewDustDirect(target.Center, 1, 1,
                    DustID.WhiteTorch, 0, 0, 0, Color.White, 2f);
                d.noGravity = true;
                d.velocity = Main.rand.NextVector2Circular(8, 8);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float scaledW = Projectile.width * Projectile.scale;
            float scaledH = Projectile.height * Projectile.scale;
            Rectangle scaled = new Rectangle(
                (int)(Projectile.Center.X - scaledW / 2f),
                (int)(Projectile.Center.Y - scaledH / 2f),
                (int)scaledW, (int)scaledH);
            return scaled.Intersects(targetHitbox);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.Center, 1, 1,
                    DustID.WhiteTorch, 0, 0, 80, Color.White, 1.2f);
                d.noGravity = true;
                d.velocity = Main.rand.NextVector2Circular(5, 5);
            }
        }

        private float EaseOutBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * (float)Math.Pow(t - 1f, 3) + c1 * (float)Math.Pow(t - 1f, 2);
        }
    }
}