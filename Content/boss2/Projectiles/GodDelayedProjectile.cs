using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content.boss2.Projectiles
{
    public class GodDelayedProjectile : ModProjectile
    {
        private float delay;
        private Vector2 storedVelocity;
        private bool launched = false;
        private float waitTimer = 0f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900;
            Projectile.alpha = 0;
            Projectile.light = 0.4f;
        }

        public override void AI()
        {
            if (!launched)
            {
                if (waitTimer == 0f)
                {
                    delay = Projectile.ai[0];
                    storedVelocity = Projectile.velocity;
                    Projectile.velocity = Vector2.Zero;
                }

                waitTimer++;
                Projectile.Opacity = 0.3f + (float)Math.Sin(waitTimer * 0.3f) * 0.2f;

                if (waitTimer % 5 == 0)
                {
                    float ang = Projectile.ai[1];
                    Vector2 dustPos = Projectile.Center + ang.ToRotationVector2() * 5f;
                    Dust d = Dust.NewDustDirect(dustPos, 1, 1, DustID.RedTorch, 0, 0, 200, default, 0.5f);
                    d.noGravity = true;
                    d.velocity *= 0.1f;
                }

                if (waitTimer >= delay)
                {
                    launched = true;
                    Projectile.velocity = storedVelocity;
                    Projectile.Opacity = 1f;
                    Projectile.timeLeft = 600;

                    for (int i = 0; i < 5; i++)
                    {
                        Dust d = Dust.NewDustDirect(Projectile.Center, 1, 1, DustID.RedTorch, 0, 0, 100, default, 1f);
                        d.noGravity = true;
                        d.velocity = Main.rand.NextVector2Circular(3f, 3f);
                    }
                }
                return;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Projectile.timeLeft < 30)
                Projectile.Opacity = Projectile.timeLeft / 30f;

            Dust trail = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                DustID.RedTorch, 0, 0, 100, default, 0.7f);
            trail.noGravity = true;
            trail.velocity *= 0.15f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch spriteBatch = Main.spriteBatch;
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() / 2f;

            if (launched)
            {
                for (int i = ProjectileID.Sets.TrailCacheLength[Projectile.type] - 1; i >= 0; i--)
                {
                    float progress = (float)i / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                    float alpha = (1f - progress) * 0.4f * Projectile.Opacity;
                    Color trailColor = Color.Lerp(Color.Red, Color.DarkRed, progress) * alpha;
                    Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                    spriteBatch.Draw(texture, trailPos, null, trailColor, Projectile.oldRot[i],
                        origin, MathHelper.Lerp(1f, 0.3f, progress), SpriteEffects.None, 0f);
                }
            }

            Color mainColor;
            if (!launched)
            {
                float pulse = 0.5f + (float)Math.Sin(waitTimer * 0.3f) * 0.3f;
                mainColor = Color.OrangeRed * pulse * Projectile.Opacity;
            }
            else
            {
                mainColor = Color.Lerp(Color.Red, Color.OrangeRed,
                    (float)Math.Sin(Main.GameUpdateCount * 0.15f) * 0.5f + 0.5f) * Projectile.Opacity;
            }

            Color glowColor = Color.Red * 0.3f * Projectile.Opacity;
            for (int g = 0; g < 4; g++)
            {
                Vector2 offset = (MathHelper.PiOver2 * g + Main.GameUpdateCount * 0.1f).ToRotationVector2() * 2f;
                spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + offset, null, glowColor,
                    Projectile.rotation, origin, 1.15f, SpriteEffects.None, 0f);
            }

            spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, mainColor,
                Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);

            return false;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool? CanHitNPC(NPC target) => false;
    }
}