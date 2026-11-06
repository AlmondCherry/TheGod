using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content.boss2.Projectiles
{
    public class GodBarrierProjectile : ModProjectile
    {
        private float maxRadius;
        private float currentRadius;
        private float minRadius = 400f;
        private float spawnTime;
        private float runeRotation;
        private float pulsePhase;

        private const int OuterSegments = 80;
        private const int InnerSegments = 48;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 99999;
        }

        public override void AI()
        {
            int bossType = ModContent.NPCType<GodBossPhase2>();
            bool bossAlive = false;
            Vector2 bossPos = Vector2.Zero;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == bossType)
                {
                    bossAlive = true;
                    bossPos = Main.npc[i].Center;
                    break;
                }
            }

            if (!bossAlive) { Projectile.Kill(); return; }

            Projectile.Center = bossPos;

            if (Projectile.localAI[0] == 0f)
            {
                maxRadius = Projectile.ai[0];
                if (maxRadius <= 0) maxRadius = 2000f;
                currentRadius = maxRadius;
                spawnTime = 0f;
                Projectile.localAI[0] = 1f;
            }

            spawnTime += 1f / 60f;
            runeRotation += 0.006f;
            pulsePhase += 0.035f;

            float totalDuration = 30f;
            float progress = Math.Clamp(spawnTime / totalDuration, 0f, 1f);
            currentRadius = MathHelper.Lerp(maxRadius, minRadius, progress * progress);

            foreach (Player player in Main.player)
            {
                if (!player.active || player.dead) continue;

                float dist = Vector2.Distance(player.Center, Projectile.Center);
                if (dist > currentRadius - 50f)
                {
                    Vector2 pushDir = (Projectile.Center - player.Center).SafeNormalize(Vector2.Zero);
                    player.velocity += pushDir * 3f;
                    float targetDist = currentRadius - 60f;
                    Vector2 targetPos = Projectile.Center + (player.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * targetDist;
                    player.Center = Vector2.Lerp(player.Center, targetPos, 0.4f);

                    if (dist > currentRadius - 20f && spawnTime % 0.5f < 0.02f)
                    {
                        player.Hurt(Terraria.DataStructures.PlayerDeathReason.ByCustomReason(
                            player.name + " touched the barrier."), 50, 0);
                    }
                }
            }

            if (Main.rand.NextBool(2))
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 dustPos = Projectile.Center + angle.ToRotationVector2() * currentRadius;
                Dust d = Dust.NewDustDirect(dustPos, 1, 1, DustID.RedTorch, 0, 0, 120, default, 1.2f);
                d.noGravity = true;
                d.velocity = (angle + MathHelper.PiOver2).ToRotationVector2() * 3f;
                d.fadeIn = 1.2f;
            }

            if (Main.rand.NextBool(4))
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float dist2 = currentRadius * 0.92f + Main.rand.NextFloat(currentRadius * 0.08f);
                Vector2 dustPos = Projectile.Center + angle.ToRotationVector2() * dist2;
                Dust d = Dust.NewDustDirect(dustPos, 1, 1, DustID.Shadowflame, 0, 0, 150, default, 0.8f);
                d.noGravity = true;
                d.velocity = (Projectile.Center - dustPos).SafeNormalize(Vector2.Zero) * 1f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch sb = Main.spriteBatch;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 center = Projectile.Center - Main.screenPosition;

            float basePulse = 0.7f + (float)Math.Sin(pulsePhase) * 0.15f;
            float shrinkPulse = 0.8f + (float)Math.Sin(pulsePhase * 3f) * 0.1f;

            DrawRing(sb, pixel, center, currentRadius + 30f, OuterSegments, 35f,
                Color.DarkRed * 0.1f * basePulse, 0f);
            DrawRing(sb, pixel, center, currentRadius + 15f, OuterSegments, 22f,
                Color.Red * 0.15f * basePulse, 0f);
            DrawRing(sb, pixel, center, currentRadius, OuterSegments, 10f,
                Color.Red * 0.65f * basePulse, 0f);
            DrawRing(sb, pixel, center, currentRadius - 18f, InnerSegments, 5f,
                Color.OrangeRed * 0.35f * shrinkPulse, runeRotation);

            int runeCount = 32;
            for (int i = 0; i < runeCount; i++)
            {
                float angle = MathHelper.TwoPi / runeCount * i + runeRotation * 2f;
                Vector2 runePos = center + angle.ToRotationVector2() * (currentRadius - 6f);
                float runeAlpha = 0.5f + (float)Math.Sin(angle * 3f + pulsePhase * 2f) * 0.3f;
                Color runeColor = Color.Lerp(Color.Red, Color.OrangeRed,
                    (float)Math.Sin(angle * 2f + pulsePhase) * 0.5f + 0.5f) * runeAlpha * basePulse;
                float runeSize = 7f + (float)Math.Sin(angle * 5f + pulsePhase * 3f) * 3f;
                sb.Draw(pixel, runePos, new Rectangle(0, 0, 1, 1), runeColor,
                    angle + pulsePhase, new Vector2(0.5f, 0.5f),
                    new Vector2(runeSize, runeSize), SpriteEffects.None, 0f);
            }

            int arcCount = 8;
            for (int i = 0; i < arcCount; i++)
            {
                float arcAngle = MathHelper.TwoPi / arcCount * i - runeRotation * 1.5f;
                float arcLen = MathHelper.ToRadians(18 + (float)Math.Sin(pulsePhase + i * 2f) * 12f);
                int arcSegs = 10;
                for (int j = 0; j < arcSegs; j++)
                {
                    float segAngle = arcAngle + arcLen / arcSegs * j;
                    Vector2 segPos = center + segAngle.ToRotationVector2() * (currentRadius + 4f);
                    float segLen = currentRadius * arcLen / arcSegs;
                    float arcAlpha = (float)Math.Sin((float)j / arcSegs * MathHelper.Pi) * 0.8f;
                    Color arcColor = Color.White * arcAlpha * basePulse * 0.5f;
                    sb.Draw(pixel, segPos, new Rectangle(0, 0, 1, 1), arcColor,
                        segAngle + MathHelper.PiOver2, new Vector2(0.5f, 0.5f),
                        new Vector2(4f, segLen * 1.1f), SpriteEffects.None, 0f);
                }
            }

            int sparkCount = 20;
            for (int i = 0; i < sparkCount; i++)
            {
                float sparkAngle = MathHelper.TwoPi / sparkCount * i + runeRotation * 3f;
                float sparkFlicker = (float)Math.Sin(sparkAngle * 7f + pulsePhase * 5f);
                if (sparkFlicker < 0.3f) continue;
                Vector2 sparkPos = center + sparkAngle.ToRotationVector2() * currentRadius;
                Color sparkColor = Color.White * sparkFlicker * 0.6f;
                float sparkSize = 4f + sparkFlicker * 4f;
                sb.Draw(pixel, sparkPos, new Rectangle(0, 0, 1, 1), sparkColor,
                    0f, new Vector2(0.5f, 0.5f),
                    new Vector2(sparkSize, sparkSize), SpriteEffects.None, 0f);
            }

            return false;
        }

        private void DrawRing(SpriteBatch sb, Texture2D pixel, Vector2 center, float radius,
            int segments, float thickness, Color color, float rotOffset)
        {
            float angleStep = MathHelper.TwoPi / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = angleStep * i + rotOffset;
                Vector2 pos = center + angle.ToRotationVector2() * radius;
                float segLen = radius * angleStep;
                float segAlpha = 1f + (float)Math.Sin(angle * 4f + pulsePhase * 2f) * 0.15f;
                sb.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), color * segAlpha,
                    angle + MathHelper.PiOver2, new Vector2(0.5f, 0.5f),
                    new Vector2(thickness, segLen * 1.05f), SpriteEffects.None, 0f);
            }
        }

        public override bool ShouldUpdatePosition() => false;
    }
}