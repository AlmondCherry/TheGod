using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content.boss2.Projectiles
{
    public class GodMinionProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.alpha = 50;
            Projectile.light = 0.3f;
        }

        public override void AI()
        {
            Projectile.rotation += 0.2f;

            if (Projectile.timeLeft < 20)
                Projectile.Opacity = Projectile.timeLeft / 20f;

            Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                DustID.Shadowflame, 0, 0, 150, default, 0.6f);
            d.noGravity = true;
            d.velocity *= 0.1f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch spriteBatch = Main.spriteBatch;
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() / 2f;

            for (int i = ProjectileID.Sets.TrailCacheLength[Projectile.type] - 1; i >= 0; i--)
            {
                float progress = (float)i / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                float alpha = (1f - progress) * 0.4f * Projectile.Opacity;
                Color trailColor = Color.Purple * alpha;
                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;

                spriteBatch.Draw(texture, trailPos, null, trailColor, Projectile.oldRot[i],
                    origin, MathHelper.Lerp(1f, 0.4f, progress), SpriteEffects.None, 0f);
            }

            Color mainColor = Color.Lerp(Color.Purple, Color.MediumPurple,
                (float)Math.Sin(Main.GameUpdateCount * 0.2f) * 0.5f + 0.5f) * Projectile.Opacity;

            spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, mainColor,
                Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);

            return false;
        }
    }
}