using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using System;

namespace GodEyeMod.Content.test
{
    public class UnmakeBeamProjectile : ModProjectile
    {
        private float beamWidth = 0f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 30;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 4;
            Projectile.timeLeft = 80;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 8;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 3;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();

            float lifeRatio = (float)Projectile.timeLeft / 80f;
            beamWidth = lifeRatio;

            float zig = (float)Math.Sin(Main.GameUpdateCount * 0.9 + Projectile.whoAmI * 5) * 0.015f;
            Projectile.velocity = Projectile.velocity.RotatedBy(zig);

            if (Main.GameUpdateCount % 3 == 0)
            {
                Dust white = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(2, 2),
                    DustID.WhiteTorch,
                    Main.rand.NextVector2Circular(0.5f, 0.5f) - Projectile.velocity * 0.01f,
                    60, default, Main.rand.NextFloat(0.3f, 0.5f));
                white.noGravity = true;
                white.fadeIn = 0.5f;

                if (Main.rand.NextBool(2))
                {
                    Dust red = Dust.NewDustPerfect(Projectile.Center,
                        DustID.RedTorch,
                        Main.rand.NextVector2Circular(1f, 1f),
                        80, default, 0.3f);
                    red.noGravity = true;
                }

                if (Main.rand.NextBool(4))
                {
                    Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(3, 3),
                        DustID.Smoke, Vector2.Zero, 230, Color.Black, 0.3f).noGravity = true;
                }
            }

            Lighting.AddLight(Projectile.Center, 0.4f * lifeRatio, 0.35f * lifeRatio, 0.35f * lifeRatio);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Ichor, 600);
            target.AddBuff(BuffID.BetsysCurse, 600);
            target.AddBuff(BuffID.ShadowFlame, 300);

            for (int i = 0; i < 8; i++)
            {
                Dust.NewDustPerfect(
                    target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f),
                    DustID.WhiteTorch,
                    Main.rand.NextVector2Circular(4f, 4f), 60, default, Main.rand.NextFloat(0.6f, 1.2f)).noGravity = true;
            }

            for (int i = 0; i < 4; i++)
            {
                Dust.NewDustPerfect(target.Center, DustID.RedTorch,
                    Main.rand.NextVector2Circular(5f, 5f), 80, default, 0.8f).noGravity = true;
            }

            if (target.life <= 0)
            {
                Player owner = Main.player[Projectile.owner];
                if (owner.HeldItem.ModItem is VoidGaze vg)
                    vg.RegisterKill();

                for (int i = 0; i < 35; i++)
                {
                    float angle = MathHelper.TwoPi / 35f * i;
                    Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * Main.rand.NextFloat(3f, 8f);
                    Dust d = Dust.NewDustPerfect(target.Center, DustID.WhiteTorch, vel, 20, default, 1.5f);
                    d.noGravity = true;
                    d.fadeIn = 1.8f;
                }

                for (int i = 0; i < 12; i++)
                {
                    Dust.NewDustPerfect(target.Center, DustID.Smoke,
                        Main.rand.NextVector2Circular(6f, 6f), 200, Color.Black, 1.5f).noGravity = true;
                }

                if (Projectile.owner == Main.myPlayer)
                    Main.LocalPlayer.GetModPlayer<VoidGazePlayer>().TriggerShake(8, 6f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch sb = Main.spriteBatch;
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Texture2D glowTex = TextureAssets.Extra[89].Value;
            Vector2 origin = tex.Size() / 2f;
            Vector2 glowOrigin = glowTex.Size() / 2f;

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;

                float progress = 1f - (float)i / Projectile.oldPos.Length;
                Vector2 dp = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;

                Color c;
                if (progress > 0.75f)
                    c = Color.Lerp(new Color(255, 180, 180), Color.White, (progress - 0.75f) / 0.25f);
                else if (progress > 0.35f)
                    c = Color.Lerp(new Color(220, 20, 20), new Color(255, 180, 180), (progress - 0.35f) / 0.4f);
                else
                    c = Color.Lerp(new Color(40, 0, 0), new Color(220, 20, 20), progress / 0.35f);

                c *= progress * beamWidth;
                float scale = progress * beamWidth;

                sb.Draw(glowTex, dp, null, c * 0.15f,
                    Projectile.oldRot[i], glowOrigin, scale * 4f, SpriteEffects.None, 0f);

                sb.Draw(glowTex, dp, null, c * 0.35f,
                    Projectile.oldRot[i], glowOrigin, scale * 2f, SpriteEffects.None, 0f);

                sb.Draw(tex, dp, null, c * 0.9f,
                    Projectile.oldRot[i], origin, scale * 1.2f, SpriteEffects.None, 0f);
            }

            Vector2 headPos = Projectile.Center - Main.screenPosition;

            sb.Draw(glowTex, headPos, null, new Color(255, 200, 200, 0) * beamWidth * 0.5f,
                Projectile.rotation, glowOrigin, beamWidth * 3f, SpriteEffects.None, 0f);

            sb.Draw(glowTex, headPos, null, Color.White * beamWidth * 0.7f,
                Projectile.rotation, glowOrigin, beamWidth * 1f, SpriteEffects.None, 0f);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            sb.Draw(tex, headPos, null, Color.White,
                Projectile.rotation, origin, beamWidth * 1.5f, SpriteEffects.None, 0f);

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 12; i++)
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.WhiteTorch,
                    Main.rand.NextVector2Circular(3f, 3f), 60, default, 0.8f).noGravity = true;
            }
        }
    }
}