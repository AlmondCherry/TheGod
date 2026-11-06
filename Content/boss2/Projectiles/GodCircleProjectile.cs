using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content.boss2.Projectiles
{
    public class GodCircleProjectile : ModProjectile
    {
        private float closeTimer;
        private float closeDuration;
        private float gapAngle;
        private float gapSize = MathHelper.ToRadians(45);
        private float radius = 0f;
        private float maxRadius = 350f;
        private bool closing = false;
        private const int Segments = 48;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            Projectile.alpha = 0;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                closeDuration = Projectile.ai[0] * 60f;
                if (closeDuration <= 0) closeDuration = 30f;
                gapAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                Projectile.localAI[0] = 1f;
                radius = maxRadius;
            }

            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            Projectile.Center = target.Center;

            closeTimer++;
            float progress = Math.Clamp(closeTimer / closeDuration, 0f, 1f);
            radius = MathHelper.Lerp(maxRadius, 0f, progress);

            if (radius <= 10f || closeTimer >= closeDuration)
            {
                Projectile.Kill();
                return;
            }

            gapSize = MathHelper.Lerp(MathHelper.ToRadians(45), MathHelper.ToRadians(25), progress);

            float angleStep = MathHelper.TwoPi / Segments;
            for (int i = 0; i < Segments; i++)
            {
                float angle = angleStep * i;
                float angleDiff = MathHelper.WrapAngle(angle - gapAngle);
                if (Math.Abs(angleDiff) < gapSize / 2f)
                    continue;

                Vector2 segmentPos = Projectile.Center + angle.ToRotationVector2() * radius;

                foreach (Player player in Main.player)
                {
                    if (!player.active || player.dead) continue;
                    float dist = Vector2.Distance(player.Center, segmentPos);
                    if (dist < 25f)
                    {
                        player.Hurt(Terraria.DataStructures.PlayerDeathReason.ByProjectile(player.whoAmI, Projectile.whoAmI),
                            Projectile.damage, 0);
                    }
                }
            }

            gapAngle += 0.01f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => false;

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch spriteBatch = Main.spriteBatch;
            Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;

            float angleStep = MathHelper.TwoPi / Segments;

            for (int i = 0; i < Segments; i++)
            {
                float angle = angleStep * i;
                float angleDiff = MathHelper.WrapAngle(angle - gapAngle);
                if (Math.Abs(angleDiff) < gapSize / 2f)
                    continue;

                Vector2 segmentPos = Projectile.Center + angle.ToRotationVector2() * radius - Main.screenPosition;
                float segLength = radius * angleStep;

                Color color = Color.Lerp(Color.Red, Color.DarkRed,
                    (float)Math.Sin(angle * 3f + Main.GameUpdateCount * 0.1f) * 0.5f + 0.5f);
                color *= 0.8f;

                float rot = angle + MathHelper.PiOver2;
                spriteBatch.Draw(pixel, segmentPos, new Rectangle(0, 0, 1, 1), color, rot,
                    new Vector2(0.5f, 0.5f), new Vector2(8f, segLength * 0.8f), SpriteEffects.None, 0f);

                Color glowCol = Color.Red * 0.3f;
                spriteBatch.Draw(pixel, segmentPos, new Rectangle(0, 0, 1, 1), glowCol, rot,
                    new Vector2(0.5f, 0.5f), new Vector2(16f, segLength * 0.9f), SpriteEffects.None, 0f);
            }

            Vector2 gapPos = Projectile.Center + gapAngle.ToRotationVector2() * radius - Main.screenPosition;
            Color gapColor = Color.Green * (0.5f + (float)Math.Sin(Main.GameUpdateCount * 0.3f) * 0.3f);
            spriteBatch.Draw(pixel, gapPos, new Rectangle(0, 0, 1, 1), gapColor, 0f,
                new Vector2(0.5f, 0.5f), new Vector2(20f, 20f), SpriteEffects.None, 0f);

            return false;
        }
    }
}