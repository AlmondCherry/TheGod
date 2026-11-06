using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content.boss2.Projectiles
{
    public class GodCoreProjectile : ModProjectile
    {
        private float trailAlpha = 1f;
        private float pulseTimer;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 12;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            Projectile.alpha = 0;
            Projectile.light = 0.4f;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            pulseTimer += 0.1f;

            if (Projectile.timeLeft < 30)
            {
                trailAlpha = Projectile.timeLeft / 30f;
                Projectile.Opacity = trailAlpha;
            }

            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustDirect(Projectile.Center - Projectile.velocity * 0.3f,
                    1, 1, DustID.RedTorch, 0, 0, 150, default, 0.5f);
                d.noGravity = true;
                d.velocity = -Projectile.velocity * 0.05f;
                d.fadeIn = 0.6f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch sb = Main.spriteBatch;
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            for (int i = ProjectileID.Sets.TrailCacheLength[Projectile.type] - 1; i >= 0; i--)
            {
                float progress = (float)i / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                float alpha = (1f - progress) * 0.35f * trailAlpha;
                Color trailColor = Color.Lerp(Color.OrangeRed, Color.DarkRed, progress) * alpha;
                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                float trailScale = MathHelper.Lerp(0.9f, 0.2f, progress);

                sb.Draw(texture, trailPos, null, trailColor, Projectile.oldRot[i],
                    origin, trailScale, SpriteEffects.None, 0f);
            }

            float glowPulse = 1f + (float)Math.Sin(pulseTimer * 2f) * 0.15f;
            Color outerGlow = Color.Red * 0.2f * Projectile.Opacity;
            sb.Draw(texture, drawPos, null, outerGlow, Projectile.rotation,
                origin, 1.8f * glowPulse, SpriteEffects.None, 0f);

            Color midGlow = Color.OrangeRed * 0.35f * Projectile.Opacity;
            sb.Draw(texture, drawPos, null, midGlow, Projectile.rotation,
                origin, 1.3f * glowPulse, SpriteEffects.None, 0f);

            Color mainColor = Color.Lerp(Color.Red, Color.OrangeRed,
                (float)Math.Sin(pulseTimer) * 0.5f + 0.5f) * Projectile.Opacity;
            sb.Draw(texture, drawPos, null, mainColor, Projectile.rotation,
                origin, 1f, SpriteEffects.None, 0f);

            Color coreColor = Color.White * 0.6f * Projectile.Opacity * glowPulse;
            sb.Draw(texture, drawPos, null, coreColor, Projectile.rotation,
                origin, 0.4f, SpriteEffects.None, 0f);

            return false;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;
    }
}