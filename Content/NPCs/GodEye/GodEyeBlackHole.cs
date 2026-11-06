using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content.NPCs.GodEye
{
    public class GodEyeBlackHole : ModProjectile
    {
        private float pullRadius = 400f;
        private float damageRadius = 60f;
        private float currentScale = 0f;
        private bool exploded = false;

        public override void SetDefaults()
        {
            Projectile.width = 100;
            Projectile.height = 100;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            Projectile.alpha = 0;
            Projectile.light = 0.8f;
        }

        public override void AI()
        {
            if (currentScale < 1f)
                currentScale += 0.015f;
            Projectile.scale = currentScale;

            Projectile.rotation += 0.05f;

            bool playerInside = false;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead) continue;

                float dist = Vector2.Distance(Projectile.Center, player.Center);

                if (dist < damageRadius * currentScale)
                {
                    playerInside = true;
                }
                else if (dist < pullRadius)
                {
                    Vector2 pullDir = (Projectile.Center - player.Center).SafeNormalize(Vector2.Zero);
                    float pullStrength = (1f - dist / pullRadius) * 2f;
                    player.velocity += pullDir * pullStrength * 0.05f;
                }
            }

            if (playerInside && !exploded)
            {
                Explode();
            }

            for (int i = 0; i < 3; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float dist2 = Main.rand.NextFloat(pullRadius * 0.3f, pullRadius * 0.8f);
                Vector2 dustPos = Projectile.Center + angle.ToRotationVector2() * dist2;

                Dust dust = Dust.NewDustDirect(dustPos, 1, 1, DustID.Vortex, 0, 0, 100, default, 1.2f);
                dust.noGravity = true;
                dust.velocity = (Projectile.Center - dustPos).SafeNormalize(Vector2.Zero) * 3f;
            }

            if (Projectile.timeLeft < 30 && !exploded)
            {
                currentScale = (float)Projectile.timeLeft / 30f;
            }
        }

        private void Explode()
        {
            exploded = true;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 16; i++)
                {
                    float angle = MathHelper.TwoPi / 16f * i;
                    Vector2 vel = angle.ToRotationVector2() * 12f;
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, vel,
                        ModContent.ProjectileType<GodEyeLaser>(), Projectile.damage, 0, Main.myPlayer);
                }
            }

            for (int i = 0; i < 40; i++)
            {
                Dust dust = Dust.NewDustDirect(Projectile.Center, 1, 1, DustID.Vortex, 0, 0, 100, default, 2.5f);
                dust.noGravity = true;
                dust.velocity = Main.rand.NextVector2Circular(15, 15);
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            Projectile.Kill();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float dist = Vector2.Distance(Projectile.Center, targetHitbox.Center.ToVector2());
            return dist < damageRadius * currentScale;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() / 2f;

            for (int i = 0; i < 6; i++)
            {
                float rot = Projectile.rotation + MathHelper.TwoPi / 6f * i;
                Vector2 offset = rot.ToRotationVector2() * 5f * currentScale;
                Color glowColor = Color.Lerp(Color.Purple, Color.Black, 0.5f) * 0.3f;

                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + offset,
                    null, glowColor, Projectile.rotation, origin, currentScale * 1.3f, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition,
                null, Color.Black * 0.9f, Projectile.rotation, origin, currentScale, SpriteEffects.None, 0);

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition,
                null, Color.Purple * 0.5f, -Projectile.rotation * 2f, origin, currentScale * 0.6f, SpriteEffects.None, 0);

            return false;
        }
    }
}