using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content.boss2.Projectiles
{
    public class GodWallProjectile : ModProjectile
    {
        private const float WallHeight = 800f;
        private const float WallThickness = 30f;
        private float pulseTimer;

        public override void SetDefaults()
        {
            Projectile.width = (int)WallThickness;
            Projectile.height = (int)WallHeight;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 480;
            Projectile.alpha = 0;
            Projectile.light = 0.3f;
        }

        public override void AI()
        {
            pulseTimer += 0.05f;

            if (Projectile.timeLeft < 30)
                Projectile.Opacity = Projectile.timeLeft / 30f;
            if (Projectile.timeLeft > 450)
                Projectile.Opacity = (480f - Projectile.timeLeft) / 30f;

            for (int i = 0; i < 2; i++)
            {
                float yOffset = Main.rand.NextFloat(-WallHeight / 2f, WallHeight / 2f);
                Vector2 dustPos = Projectile.Center + new Vector2(0, yOffset);
                Dust d = Dust.NewDustDirect(dustPos, 1, 1, DustID.Shadowflame, 0, 0, 200, default, 0.8f);
                d.noGravity = true;
                d.velocity = new Vector2(Projectile.velocity.X * 0.3f, Main.rand.NextFloat(-0.5f, 0.5f));
                d.color = Color.Black;
            }

            if (Main.rand.NextBool(4))
            {
                float yOffset = Main.rand.NextFloat(-WallHeight / 2f, WallHeight / 2f);
                Vector2 dustPos = Projectile.Center + new Vector2(Main.rand.NextFloat(-10, 10), yOffset);
                Dust d = Dust.NewDustDirect(dustPos, 1, 1, DustID.Smoke, 0, 0, 220, Color.Black, 1.5f);
                d.noGravity = true;
                d.velocity *= 0.3f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch sb = Main.spriteBatch;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            float pulse = 0.8f + (float)Math.Sin(pulseTimer) * 0.1f;

            Color outerGlow = new Color(30, 0, 0) * 0.3f * Projectile.Opacity;
            sb.Draw(pixel, drawPos, new Rectangle(0, 0, 1, 1), outerGlow, 0f,
                new Vector2(0.5f, 0.5f), new Vector2(WallThickness * 3f, WallHeight * 1.05f),
                SpriteEffects.None, 0f);

            Color edgeColor = new Color(60, 0, 0) * 0.5f * Projectile.Opacity * pulse;
            sb.Draw(pixel, drawPos, new Rectangle(0, 0, 1, 1), edgeColor, 0f,
                new Vector2(0.5f, 0.5f), new Vector2(WallThickness * 1.8f, WallHeight * 1.02f),
                SpriteEffects.None, 0f);

            Color mainColor = new Color(10, 0, 5) * 0.95f * Projectile.Opacity;
            sb.Draw(pixel, drawPos, new Rectangle(0, 0, 1, 1), mainColor, 0f,
                new Vector2(0.5f, 0.5f), new Vector2(WallThickness, WallHeight),
                SpriteEffects.None, 0f);

            Color lineColor = Color.DarkRed * 0.6f * Projectile.Opacity * pulse;
            sb.Draw(pixel, drawPos, new Rectangle(0, 0, 1, 1), lineColor, 0f,
                new Vector2(0.5f, 0.5f), new Vector2(2f, WallHeight * 0.95f),
                SpriteEffects.None, 0f);

            int dotCount = 12;
            for (int i = 0; i < dotCount; i++)
            {
                float yPos = -WallHeight / 2f + WallHeight / dotCount * i;
                float flicker = (float)Math.Sin(pulseTimer * 3f + i * 1.5f);
                if (flicker < 0.2f) continue;

                Vector2 dotPos = drawPos + new Vector2(0, yPos);
                Color dotColor = Color.Red * flicker * 0.5f * Projectile.Opacity;
                sb.Draw(pixel, dotPos, new Rectangle(0, 0, 1, 1), dotColor, 0f,
                    new Vector2(0.5f, 0.5f), new Vector2(4f, 4f), SpriteEffects.None, 0f);
            }

            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Rectangle wallRect = new Rectangle(
                (int)(Projectile.Center.X - WallThickness / 2f),
                (int)(Projectile.Center.Y - WallHeight / 2f),
                (int)WallThickness, (int)WallHeight);
            return wallRect.Intersects(targetHitbox);
        }
    }
}