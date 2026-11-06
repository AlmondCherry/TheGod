using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using System;
using System.Collections.Generic;

namespace GodEyeMod.Content.test
{
    public class VoidEyeProjectile : ModProjectile
    {
        private int targetIndex = -1;
        private bool isChaos = false;
        private float glowPulse = 0f;
        private float trailWidth = 1f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 4;
            Projectile.timeLeft = 400;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.alpha = 0;
        }

        public override void AI()
        {
            isChaos = Projectile.ai[0] < 0;
            glowPulse += 0.15f;

            if (isChaos)
            {
                ChaosAI();
            }
            else
            {
                HomingAI();
            }

            float lifeRatio = Projectile.timeLeft / 400f;
            trailWidth = MathHelper.Lerp(0.3f, 1f, lifeRatio);

            SpawnCoreParticles();

            float lightPulse = 0.4f + 0.2f * (float)Math.Sin(glowPulse);
            Lighting.AddLight(Projectile.Center, lightPulse, 0.02f, 0.02f);
        }

        private void HomingAI()
        {
            if (targetIndex < 0 || !IsValidTarget(targetIndex))
                targetIndex = FindPreferredTarget();

            if (targetIndex >= 0)
            {
                NPC target = Main.npc[targetIndex];
                Vector2 toTarget = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                float dist = Vector2.Distance(Projectile.Center, target.Center);

                float speed = Projectile.velocity.Length();
                float homing = dist < 200f ? 0.25f : 0.12f;

                Projectile.velocity = Vector2.Lerp(Projectile.velocity, toTarget * speed, homing);
                Projectile.rotation = toTarget.ToRotation();

                if (dist < 300f && Main.rand.NextBool(4))
                {
                    Vector2 mid = Vector2.Lerp(Projectile.Center, target.Center, Main.rand.NextFloat(0.2f, 0.8f));
                    mid += Main.rand.NextVector2Circular(8f, 8f);
                    Dust link = Dust.NewDustPerfect(mid, DustID.RedTorch, Vector2.Zero, 200, default, 0.2f);
                    link.noGravity = true;
                    link.fadeIn = 0.5f;
                }
            }
            else
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
        }

        private void ChaosAI()
        {
            Projectile.scale = 0.5f + 0.2f * (float)Math.Sin(glowPulse * 2f);

            float wobble = (float)Math.Sin(Main.GameUpdateCount * 0.8 + Projectile.whoAmI * 7) * 0.12f;
            Projectile.velocity = Projectile.velocity.RotatedBy(wobble);

            int t = FindClosestTarget(400f);
            if (t >= 0)
            {
                Vector2 dir = (Main.npc[t].Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, dir * Projectile.velocity.Length(), 0.05f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Main.rand.NextBool(2))
            {
                Vector2 sparkVel = Main.rand.NextVector2Circular(3f, 3f);
                Dust spark = Dust.NewDustPerfect(Projectile.Center, DustID.RedTorch, sparkVel, 100, default, 0.3f);
                spark.noGravity = true;
                spark.noLight = true;
            }
        }

        private void SpawnCoreParticles()
        {
            if (Main.rand.NextBool(2))
            {
                Vector2 offset = Main.rand.NextVector2Circular(4, 4);
                Dust fire = Dust.NewDustPerfect(Projectile.Center + offset - Projectile.velocity * 0.3f,
                    DustID.RedTorch, -Projectile.velocity * 0.06f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    120, default, Main.rand.NextFloat(0.3f, 0.6f));
                fire.noGravity = true;
                fire.fadeIn = 0.8f;
            }

            if (Main.rand.NextBool(4))
            {
                Dust dark = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(6, 6),
                    DustID.Smoke, -Projectile.velocity * 0.02f, 230, Color.Black, 0.5f);
                dark.noGravity = true;
            }

            if (Main.rand.NextBool(8))
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 orbPos = Projectile.Center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 10f;
                Dust white = Dust.NewDustPerfect(orbPos, DustID.WhiteTorch, Vector2.Zero, 100, default, 0.2f);
                white.noGravity = true;
                white.fadeIn = 0.3f;
            }
        }

        private int FindPreferredTarget()
        {
            List<(int idx, float dist)> candidates = new List<(int, float)>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (!IsValidTarget(i)) continue;
                float d = Vector2.Distance(Projectile.Center, Main.npc[i].Center);
                if (d < 900f) candidates.Add((i, d));
            }
            if (candidates.Count == 0) return -1;
            candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

            int preferred = (int)Math.Abs(Projectile.ai[0]) % Math.Max(candidates.Count, 1);
            return candidates[preferred].idx;
        }

        private int FindClosestTarget(float range)
        {
            int best = -1;
            float bestD = range;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (!IsValidTarget(i)) continue;
                float d = Vector2.Distance(Projectile.Center, Main.npc[i].Center);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        private bool IsValidTarget(int i)
        {
            return i >= 0 && i < Main.maxNPCs && Main.npc[i].active && !Main.npc[i].friendly && !Main.npc[i].dontTakeDamage;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.ShadowFlame, 240);

            for (int i = 0; i < 12; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(6f, 6f);
                Dust blood = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(10, 10),
                    DustID.RedTorch, vel, 60, default, Main.rand.NextFloat(0.8f, 1.4f));
                blood.noGravity = true;
            }

            for (int i = 0; i < 5; i++)
            {
                Dust.NewDustPerfect(target.Center, DustID.Smoke,
                    Main.rand.NextVector2Circular(4f, 4f), 200, Color.Black, 1f).noGravity = true;
            }

            if (target.life <= 0)
            {
                NotifyKill();

                if (!isChaos && Projectile.penetrate > 1)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float a = MathHelper.TwoPi / 3f * i + Main.rand.NextFloat(-0.2f, 0.2f);
                        Vector2 newVel = new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * Projectile.velocity.Length() * 0.7f;
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                            Projectile.Center, newVel, Type,
                            Projectile.damage * 2 / 3, Projectile.knockBack * 0.3f,
                            Projectile.owner, ai0: -1);
                    }

                    for (int i = 0; i < 20; i++)
                    {
                        Dust.NewDustPerfect(Projectile.Center, DustID.RedTorch,
                            Main.rand.NextVector2Circular(8f, 8f), 50, default, 1.2f).noGravity = true;
                    }
                }
            }
        }

        private void NotifyKill()
        {
            Player owner = Main.player[Projectile.owner];
            if (owner.HeldItem.ModItem is VoidGaze vg)
                vg.RegisterKill();
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 15; i++)
            {
                float angle = MathHelper.TwoPi / 15f * i;
                Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 3f;
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.RedTorch, vel, 80, default, 0.8f);
                d.noGravity = true;
            }

            for (int i = 0; i < 5; i++)
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.Smoke,
                    Main.rand.NextVector2Circular(2f, 2f), 220, Color.Black, 0.8f).noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch sb = Main.spriteBatch;
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            Vector2 pos = Projectile.Center - Main.screenPosition;

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;
                float progress = 1f - (float)i / Projectile.oldPos.Length;

                Color trailCol;
                if (progress > 0.8f)
                    trailCol = Color.Lerp(new Color(255, 100, 100), Color.White, (progress - 0.8f) / 0.2f);
                else if (progress > 0.3f)
                    trailCol = Color.Lerp(new Color(180, 0, 0), new Color(255, 100, 100), (progress - 0.3f) / 0.5f);
                else
                    trailCol = Color.Lerp(new Color(40, 0, 0), new Color(180, 0, 0), progress / 0.3f);

                trailCol *= progress * 0.6f;

                float trailScale = Projectile.scale * progress * trailWidth;
                Vector2 tp = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;

                sb.Draw(tex, tp, null, trailCol * 0.3f, Projectile.oldRot[i], origin,
                    trailScale * 2.5f, SpriteEffects.None, 0f);

                sb.Draw(tex, tp, null, trailCol, Projectile.oldRot[i], origin,
                    trailScale, SpriteEffects.None, 0f);
            }

            float pulse1 = 1f + 0.25f * (float)Math.Sin(glowPulse);
            float pulse2 = 1f + 0.15f * (float)Math.Sin(glowPulse * 1.7f + 1f);

            sb.Draw(tex, pos, null, new Color(255, 0, 0, 0) * 0.15f,
                Projectile.rotation, origin, Projectile.scale * 3f * pulse1, SpriteEffects.None, 0f);

            sb.Draw(tex, pos, null, new Color(255, 50, 50, 0) * 0.25f,
                Projectile.rotation, origin, Projectile.scale * 2f * pulse2, SpriteEffects.None, 0f);

            sb.Draw(tex, pos, null, new Color(255, 150, 150, 0) * 0.4f,
                Projectile.rotation, origin, Projectile.scale * 1.3f, SpriteEffects.None, 0f);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            float mainAlpha = (255f - Projectile.alpha) / 255f;
            sb.Draw(tex, pos, null, Color.White * mainAlpha,
                Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);

            return false;
        }
    }
}