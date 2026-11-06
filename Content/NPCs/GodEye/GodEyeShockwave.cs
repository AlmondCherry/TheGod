using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace GodEyeMod.Content.NPCs.GodEye
{
    public class GodEyeShockwave : ModProjectile
    {
        private float radius = 0;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            radius += 25f;

            int dustCount = (int)(radius * 0.5f);
            for (int i = 0; i < dustCount; i++)
            {
                float angle = MathHelper.TwoPi / dustCount * i;
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * radius;

                Dust dust = Dust.NewDustDirect(pos, 1, 1, Terraria.ID.DustID.Vortex, 0, 0, 100, default, 2f);
                dust.noGravity = true;
                dust.velocity = angle.ToRotationVector2() * 2f;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float dist = Vector2.Distance(Projectile.Center, targetHitbox.Center.ToVector2());
            return dist >= radius - 30 && dist <= radius + 30;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}