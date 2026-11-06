using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content.NPCs.GodEye
{
    public class GodEyeWallProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 420;
            Projectile.alpha = 0;
            Projectile.light = 0.8f;
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();

            int aliveTime = 420 - Projectile.timeLeft;

            if (aliveTime < 30)
            {
                Projectile.velocity = Vector2.Zero;
                float appear = aliveTime / 30f;
                Projectile.scale = appear;
                Projectile.Opacity = 0.3f + appear * 0.7f;

                if (Main.GameUpdateCount % 6 < 3)
                    Lighting.AddLight(Projectile.Center, 1f, 0.3f, 0.3f);
                else
                    Lighting.AddLight(Projectile.Center, 0.3f, 0.1f, 0.1f);

                if (Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustDirect(Projectile.Center - new Vector2(5, 5), 10, 10,
                        DustID.RedTorch, 0, 0, 100, default, 0.8f);
                    d.noGravity = true;
                    d.velocity *= 0.2f;
                }

                return;
            }

            if (aliveTime == 30)
            {
                Projectile.velocity = new Vector2(Projectile.ai[0], Projectile.ai[1]);
                Projectile.scale = 1f;
                Projectile.Opacity = 1f;

                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.5f }, Projectile.Center);
            }

            if (Projectile.timeLeft < 60)
            {
                Projectile.velocity *= 0.98f;
                Projectile.Opacity = Projectile.timeLeft / 60f;
            }

            Lighting.AddLight(Projectile.Center, 0.8f, 0.2f, 0.6f);

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                    DustID.PurpleTorch, 0, 0, 100, default, 0.7f);
                dust.noGravity = true;
                dust.velocity = -Projectile.velocity * 0.1f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() / 2f;
            Vector2 drawCenter = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;

                float progress = (float)i / Projectile.oldPos.Length;
                float opacity = (1f - progress) * 0.4f * Projectile.Opacity;
                float scale = Projectile.scale * (1f - progress * 0.4f);

                Color trailColor = Color.Lerp(Color.Magenta, Color.DarkRed, progress) * opacity;
                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;

                Main.EntitySpriteDraw(texture, trailPos, null, trailColor,
                    Projectile.oldRot[i], origin, scale, SpriteEffects.None, 0);
            }

            float glowPulse = 1f + (float)Math.Sin(Main.GameUpdateCount * 0.1f + Projectile.whoAmI) * 0.12f;

            Main.EntitySpriteDraw(texture, drawCenter, null,
                Color.Purple * 0.2f * Projectile.Opacity, Projectile.rotation, origin,
                Projectile.scale * 2.2f * glowPulse, SpriteEffects.None, 0);

            Main.EntitySpriteDraw(texture, drawCenter, null,
                Color.Magenta * 0.35f * Projectile.Opacity, Projectile.rotation, origin,
                Projectile.scale * 1.5f * glowPulse, SpriteEffects.None, 0);

            Main.EntitySpriteDraw(texture, drawCenter, null,
                Color.White * 0.9f * Projectile.Opacity, Projectile.rotation, origin,
                Projectile.scale * 1.1f, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.White * Projectile.Opacity;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 6; i++)
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                    DustID.PurpleTorch, 0, 0, 100, default, 1f);
                dust.noGravity = true;
                dust.velocity = Main.rand.NextVector2Circular(3, 3);
            }
        }
    }
}