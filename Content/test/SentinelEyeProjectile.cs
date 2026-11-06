using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using System;

namespace GodEyeMod.Content.test
{
    public class SentinelEyeProjectile : ModProjectile
    {
        private float orbitAngle;
        private int attackCd = 0;
        private float pulsePhase = 0f;

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 720;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            pulsePhase += 0.08f;

            if (!owner.active || owner.dead || !(owner.HeldItem.ModItem is VoidGaze))
            {
                Projectile.alpha += 12;
                if (Projectile.alpha >= 255) Projectile.Kill();
                return;
            }

            Projectile.timeLeft = Math.Max(Projectile.timeLeft, 60);
            if (Projectile.alpha > 0) Projectile.alpha = Math.Max(0, Projectile.alpha - 10);

            orbitAngle = Projectile.ai[0];
            orbitAngle += 0.03f;
            Projectile.ai[0] = orbitAngle;

            float breathe = (float)Math.Sin(Main.GameUpdateCount * 0.02 + Projectile.whoAmI * 2.5);
            float radius = 120f + 25f * breathe;

            Vector2 target = owner.Center + new Vector2(
                (float)Math.Cos(orbitAngle) * radius,
                (float)Math.Sin(orbitAngle) * radius * 0.5f);

            Vector2 diff = target - Projectile.Center;
            Projectile.Center += diff * 0.1f;

            int targetNpc = FindTarget(600f);
            if (targetNpc >= 0)
            {
                Vector2 toEnemy = (Main.npc[targetNpc].Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                float desired = toEnemy.ToRotation();
                Projectile.rotation = MathHelper.Lerp(Projectile.rotation, desired, 0.12f);

                attackCd--;
                if (attackCd <= 0 && Projectile.owner == Main.myPlayer)
                {
                    attackCd = 30;
                    Vector2 shootVel = toEnemy * 18f;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                        Projectile.Center, shootVel,
                        ModContent.ProjectileType<VoidEyeProjectile>(),
                        Math.Max(Projectile.damage, 100), 1f, Projectile.owner, ai0: -1);

                    for (int i = 0; i < 6; i++)
                    {
                        Dust.NewDustPerfect(Projectile.Center, DustID.RedTorch,
                            toEnemy * Main.rand.NextFloat(2f, 6f) + Main.rand.NextVector2Circular(1f, 1f),
                            80, default, 0.7f).noGravity = true;
                    }
                }
            }
            else
            {
                Vector2 toCursor = (Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX);
                Projectile.rotation = MathHelper.Lerp(Projectile.rotation, toCursor.ToRotation(), 0.04f);
            }

            if (Main.rand.NextBool(8) && Projectile.alpha < 100)
            {
                float pAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                float pRad = Main.rand.NextFloat(8f, 18f);
                Vector2 pPos = Projectile.Center + new Vector2((float)Math.Cos(pAngle), (float)Math.Sin(pAngle)) * pRad;
                Vector2 toC = (Projectile.Center - pPos).SafeNormalize(Vector2.Zero);
                Dust d = Dust.NewDustPerfect(pPos, DustID.RedTorch, toC * 0.5f, 180, default, 0.2f);
                d.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, 0.25f, 0.02f, 0.02f);
        }

        private int FindTarget(float range)
        {
            int best = -1;
            float bestDist = range;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;
                float d = Vector2.Distance(Projectile.Center, npc.Center);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            return best;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch sb = Main.spriteBatch;
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Texture2D glowTex = TextureAssets.Extra[89].Value;
            Vector2 origin = tex.Size() / 2f;
            Vector2 glowOrigin = glowTex.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            float alpha = (255f - Projectile.alpha) / 255f;
            float pulse = 0.7f + 0.3f * (float)Math.Sin(pulsePhase);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            sb.Draw(glowTex, drawPos, null, new Color(200, 0, 0, 0) * alpha * 0.2f * pulse,
                0f, glowOrigin, 1.8f, SpriteEffects.None, 0f);

            sb.Draw(glowTex, drawPos, null, new Color(255, 50, 50, 0) * alpha * 0.3f * pulse,
                0f, glowOrigin, 0.8f, SpriteEffects.None, 0f);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            sb.Draw(tex, drawPos, null, Color.White * 0.9f * alpha,
                Projectile.rotation, origin, 0.75f, SpriteEffects.None, 0f);

            return false;
        }
    }
}