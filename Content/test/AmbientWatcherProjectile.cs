using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using System;

namespace GodEyeMod.Content.test
{
    public class AmbientWatcherProjectile : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 200;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            Vector2 toPlayer = (owner.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
            Projectile.rotation = toPlayer.ToRotation();

            float life01 = (float)Projectile.timeLeft / 200f;
            if (life01 > 0.85f)
                Projectile.alpha = (int)(255 * ((life01 - 0.85f) / 0.15f));
            else if (life01 < 0.15f)
                Projectile.alpha = (int)(255 * (1f - life01 / 0.15f));
            else
                Projectile.alpha = 0;

            Projectile.position.Y += (float)Math.Sin(Main.GameUpdateCount * 0.035 + Projectile.whoAmI) * 0.25f;
            Projectile.position.X += (float)Math.Cos(Main.GameUpdateCount * 0.02 + Projectile.whoAmI * 1.3) * 0.1f;

            float glow = (255f - Projectile.alpha) / 255f;
            Lighting.AddLight(Projectile.Center, 0.2f * glow, 0f, 0f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            float alpha = (255f - Projectile.alpha) / 255f;

            Main.EntitySpriteDraw(tex, drawPos, null,
                new Color(80, 0, 0, 0) * alpha * 0.25f,
                Projectile.rotation, origin, 1.3f, SpriteEffects.None, 0);

            Main.EntitySpriteDraw(tex, drawPos, null,
                Color.White * alpha * 0.7f,
                Projectile.rotation, origin, 0.7f, SpriteEffects.None, 0);

            return false;
        }
    }
}