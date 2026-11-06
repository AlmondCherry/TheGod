using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content.NPCs.GodEye
{
    public class GodEyeCross : ModProjectile
    {
        private float Mode => Projectile.ai[1];
        private bool IsBarrageMode => Mode >= 1f;

        private int Timer
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private float SwingAngle
        {
            get => Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }

        private int CurrentPhase = 0;

        private List<Vector2> trailPositions = new List<Vector2>();
        private List<float> trailRotations = new List<float>();
        private List<float> trailScales = new List<float>();
        private const int MaxTrailLength = 16;

        private float currentScale = 0.3f;
        private float targetScale = 0.3f;
        private float glowIntensity = 0f;

        public static readonly SoundStyle CrossSwingSound = new SoundStyle("GodEyeMod/Content/NPCs/GodEye/sfx/GodEyeCrossVZMAH")
        {
            Volume = 4f,
            PitchVariance = 0.2f,
            MaxInstances = 6,
            SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest
        };

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 720;
            Projectile.scale = 0.3f;
            Projectile.light = 0.4f;
            Projectile.Opacity = 0f;
        }

        public override void AI()
        {
            int bossIndex = (int)Projectile.ai[0];
            NPC boss = null;
            if (bossIndex >= 0 && bossIndex < Main.maxNPCs && Main.npc[bossIndex].active)
                boss = Main.npc[bossIndex];

            Player target = FindClosestPlayer();

            Timer++;

            currentScale = MathHelper.Lerp(currentScale, targetScale, 0.12f);
            Projectile.scale = currentScale;

            glowIntensity = MathHelper.Lerp(glowIntensity,
                CurrentPhase == 1 ? 1.2f : (CurrentPhase == 2 ? 0.8f : 0.5f), 0.08f);

            if (Projectile.Opacity < 1f)
                Projectile.Opacity = MathHelper.Lerp(Projectile.Opacity, 1f, 0.08f);

            Projectile.light = 0.3f + glowIntensity * 0.5f;

            if (CurrentPhase >= 1)
            {
                trailPositions.Insert(0, Projectile.Center);
                trailRotations.Insert(0, Projectile.rotation);
                trailScales.Insert(0, currentScale);
                if (trailPositions.Count > MaxTrailLength)
                {
                    trailPositions.RemoveAt(trailPositions.Count - 1);
                    trailRotations.RemoveAt(trailRotations.Count - 1);
                    trailScales.RemoveAt(trailScales.Count - 1);
                }
            }

            SpawnPhaseDust();

            switch (CurrentPhase)
            {
                case 0: WindupPhase(boss, target); break;
                case 1: SwingPhase(boss, target); break;
                case 2: FlyToPlayerPhase(target); break;
                case 3: ReturnToBossPhase(boss); break;
                case 4: FadeOutPhase(); break;
            }
        }

        private void SpawnPhaseDust()
        {
            switch (CurrentPhase)
            {
                case 0:
                    if (Main.rand.NextBool(Math.Max(1, 8 - Timer / (IsBarrageMode ? 3 : 5))))
                    {
                        Dust d = Dust.NewDustDirect(Projectile.Center + Main.rand.NextVector2Circular(20 * currentScale, 20 * currentScale),
                            1, 1, DustID.WhiteTorch, 0, 0, 100, Color.White, 0.8f + currentScale * 0.5f);
                        d.noGravity = true;
                        d.velocity = (Projectile.Center - d.position).SafeNormalize(Vector2.Zero) * 1.5f;
                    }
                    if (Timer > (IsBarrageMode ? 5 : 15) && Main.rand.NextBool(4))
                    {
                        Dust d2 = Dust.NewDustDirect(Projectile.Center + Main.rand.NextVector2Circular(30 * currentScale, 30 * currentScale),
                            1, 1, DustID.PurpleTorch, 0, 0, 80, default, 1.2f);
                        d2.noGravity = true;
                        d2.velocity *= 0.3f;
                    }
                    break;

                case 1:
                    for (int i = 0; i < (IsBarrageMode ? 6 : 4); i++)
                    {
                        Dust d = Dust.NewDustDirect(Projectile.Center + Main.rand.NextVector2Circular(25 * currentScale, 25 * currentScale),
                            1, 1, DustID.WhiteTorch, 0, 0, 0, Color.White, IsBarrageMode ? 2.2f : 1.8f);
                        d.noGravity = true;
                        d.velocity = Projectile.velocity.RotatedByRandom(0.5f) * -0.3f + Main.rand.NextVector2Circular(2, 2);
                        d.fadeIn = 1.4f;
                    }
                    for (int i = 0; i < (IsBarrageMode ? 3 : 2); i++)
                    {
                        Dust d2 = Dust.NewDustDirect(Projectile.Center,
                            1, 1, DustID.PurpleTorch, 0, 0, 60, default, 1.5f);
                        d2.noGravity = true;
                        d2.velocity = Main.rand.NextVector2Circular(3, 3);
                    }
                    if (Main.rand.NextBool(3))
                    {
                        Dust d3 = Dust.NewDustDirect(Projectile.Center + Main.rand.NextVector2Circular(40 * currentScale, 40 * currentScale),
                            1, 1, DustID.Vortex, 0, 0, 120, default, 1.3f);
                        d3.noGravity = true;
                    }
                    break;

                case 2:
                    if (Main.rand.NextBool(2))
                    {
                        Dust d = Dust.NewDustDirect(Projectile.Center + Main.rand.NextVector2Circular(15 * currentScale, 15 * currentScale),
                            1, 1, DustID.WhiteTorch, 0, 0, 50, Color.White, 1.3f);
                        d.noGravity = true;
                        d.velocity = -Projectile.velocity * 0.15f + Main.rand.NextVector2Circular(1, 1);
                    }
                    if (Main.rand.NextBool(3))
                    {
                        Dust d2 = Dust.NewDustDirect(Projectile.Center,
                            1, 1, DustID.Shadowflame, 0, 0, 100, default, 1f);
                        d2.noGravity = true;
                        d2.velocity = -Projectile.velocity * 0.1f;
                    }
                    break;

                case 3:
                    if (Main.rand.NextBool(3))
                    {
                        Dust d = Dust.NewDustDirect(Projectile.Center,
                            1, 1, DustID.WhiteTorch, 0, 0, 80, Color.LightGoldenrodYellow, 1f);
                        d.noGravity = true;
                        d.velocity = -Projectile.velocity * 0.1f;
                    }
                    break;

                case 4:
                    if (Main.rand.NextBool(2))
                    {
                        Dust d = Dust.NewDustDirect(Projectile.Center + Main.rand.NextVector2Circular(20, 20),
                            1, 1, DustID.PurpleTorch, 0, 0, 100, default, 1.2f);
                        d.noGravity = true;
                        d.velocity = Main.rand.NextVector2Circular(3, 3);
                    }
                    break;
            }
        }

        private void WindupPhase(NPC boss, Player target)
        {
            if (boss == null || target == null)
            {
                CurrentPhase = IsBarrageMode ? 4 : 2;
                Timer = 0;
                return;
            }

            float windupDuration = IsBarrageMode ? 18f : 50f;
            float progress = Timer / windupDuration;

            if (IsBarrageMode)
                targetScale = MathHelper.Lerp(0.5f, 2.0f, progress);
            else
                targetScale = MathHelper.Lerp(0.3f, 1.4f, progress);

            float angleToPlayer = (target.Center - boss.Center).ToRotation();
            float swingBack = MathHelper.ToRadians(IsBarrageMode ? 60 : 80) * (1f - EaseOutQuad(progress));
            float offsetAngle = angleToPlayer + MathHelper.Pi + swingBack;
            float offsetDist = MathHelper.Lerp(IsBarrageMode ? 60f : 80f, IsBarrageMode ? 120f : 160f, progress);

            Projectile.Center = boss.Center + offsetAngle.ToRotationVector2() * offsetDist;
            Projectile.rotation += MathHelper.ToRadians(IsBarrageMode ? -18f : -12f) * (1f - progress * 0.7f);

            if (!IsBarrageMode && Timer > 35 && Main.netMode != NetmodeID.Server)
            {
                float shake = (Timer - 35f) / (windupDuration - 35f) * 3f;
                Main.LocalPlayer.GetModPlayer<ScreenShakePlayer>().AddShake(shake);
            }

            if (Timer >= (int)windupDuration)
            {
                CurrentPhase = 1;
                Timer = 0;
                SwingAngle = angleToPlayer - MathHelper.ToRadians(IsBarrageMode ? 80 : 100);

                SoundEngine.PlaySound(CrossSwingSound, Projectile.Center);

                int particles = IsBarrageMode ? 10 : 15;
                for (int i = 0; i < particles; i++)
                {
                    Dust d = Dust.NewDustDirect(Projectile.Center, 1, 1,
                        DustID.WhiteTorch, 0, 0, 0, Color.White, 2.5f);
                    d.noGravity = true;
                    d.velocity = Main.rand.NextVector2Circular(8, 8);
                    d.fadeIn = 1.5f;
                }
            }
        }

        private void SwingPhase(NPC boss, Player target)
        {
            if (boss == null)
            {
                CurrentPhase = IsBarrageMode ? 4 : 2;
                Timer = 0;
                return;
            }

            float swingDuration = IsBarrageMode ? 12f : 25f;
            float progress = Timer / swingDuration;
            float easedProgress = EaseInOutCubic(progress);

            if (IsBarrageMode)
                targetScale = MathHelper.Lerp(2.0f, 3.2f, EaseOutQuad(progress));
            else
                targetScale = MathHelper.Lerp(1.4f, 2.8f, EaseOutQuad(progress));

            float arcDegrees = IsBarrageMode ? 160f : 200f;
            float currentAngle = SwingAngle + MathHelper.ToRadians(arcDegrees) * easedProgress;
            float swingRadius = MathHelper.Lerp(
                IsBarrageMode ? 100f : 160f,
                IsBarrageMode ? 200f : 280f,
                easedProgress);

            Projectile.Center = boss.Center + currentAngle.ToRotationVector2() * swingRadius;
            Projectile.rotation = currentAngle + MathHelper.PiOver2;
            Projectile.velocity = (Projectile.Center - (boss.Center + (currentAngle - 0.1f).ToRotationVector2() * swingRadius)).SafeNormalize(Vector2.Zero) * 15f;

            if (Main.netMode != NetmodeID.Server)
            {
                float shakeAmount = IsBarrageMode ? 3f + 5f * EaseOutQuad(progress) : 2f + 4f * EaseOutQuad(progress);
                Main.LocalPlayer.GetModPlayer<ScreenShakePlayer>().AddShake(shakeAmount);
            }

            if (Timer >= (int)swingDuration)
            {
                if (IsBarrageMode)
                {
                    CurrentPhase = 4;
                    Timer = 0;
                    Projectile.velocity = Vector2.Zero;
                }
                else
                {
                    CurrentPhase = 2;
                    Timer = 0;
                    if (target != null)
                        Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 14f;
                    else
                        Projectile.velocity = Projectile.rotation.ToRotationVector2() * 14f;
                }

                SoundEngine.PlaySound(SoundID.Item60 with { Pitch = -0.3f, Volume = 0.6f }, Projectile.Center);

                for (int i = 0; i < 20; i++)
                {
                    float angle = MathHelper.TwoPi / 20f * i;
                    Dust d = Dust.NewDustDirect(Projectile.Center, 1, 1,
                        DustID.WhiteTorch, 0, 0, 0, Color.White, 2f);
                    d.noGravity = true;
                    d.velocity = angle.ToRotationVector2() * Main.rand.NextFloat(3f, 7f);
                    d.fadeIn = 1.2f;
                }
            }
        }

        private void FadeOutPhase()
        {
            float fadeDuration = 40f;
            float progress = Timer / fadeDuration;

            targetScale = MathHelper.Lerp(2.5f, 0.2f, progress);
            Projectile.Opacity = MathHelper.Lerp(0.9f, 0f, progress);
            Projectile.rotation += MathHelper.ToRadians(3f);

            if (Main.rand.NextBool(6))
                Projectile.position += Main.rand.NextVector2Circular(4, 4);

            if (Timer >= (int)fadeDuration || Projectile.Opacity <= 0.02f)
                Projectile.Kill();
        }

        private void FlyToPlayerPhase(Player target)
        {
            float pulse = (float)Math.Sin(Timer * 0.15f) * 0.3f;
            targetScale = 2.0f + pulse;

            if (target != null && Timer < 70)
            {
                Vector2 dirToPlayer = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                float speed = MathHelper.Lerp(14f, 10f, Timer / 70f);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, dirToPlayer * speed, 0.05f);
            }

            Projectile.rotation += MathHelper.ToRadians(14f);

            if (Timer > 70 && Timer < 100)
            {
                Projectile.velocity *= 0.94f;
                targetScale = MathHelper.Lerp(2.3f, 1.6f, (Timer - 70f) / 30f);
            }

            if (Timer >= 100)
            {
                CurrentPhase = 3;
                Timer = 0;
                SoundEngine.PlaySound(SoundID.Item7 with { Pitch = 0.5f }, Projectile.Center);

                for (int i = 0; i < 10; i++)
                {
                    Dust d = Dust.NewDustDirect(Projectile.Center, 1, 1,
                        DustID.PurpleTorch, 0, 0, 50, default, 1.8f);
                    d.noGravity = true;
                    d.velocity = Main.rand.NextVector2Circular(5, 5);
                }
            }
        }

        private void ReturnToBossPhase(NPC boss)
        {
            if (boss == null || !boss.active)
            {
                targetScale *= 0.9f;
                Projectile.Opacity -= 0.03f;
                Projectile.rotation += MathHelper.ToRadians(20f);
                if (Projectile.Opacity <= 0f || currentScale < 0.1f)
                    Projectile.Kill();
                return;
            }

            float distToBoss = Vector2.Distance(Projectile.Center, boss.Center);
            targetScale = MathHelper.Lerp(0.5f, 1.8f, MathHelper.Clamp(distToBoss / 600f, 0f, 1f));

            Vector2 dirToBoss = (boss.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
            float speed = 6f + Timer * 0.4f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, dirToBoss * speed, 0.1f);
            Projectile.rotation -= MathHelper.ToRadians(10f + Timer * 0.5f);

            if (distToBoss < 80f)
            {
                Projectile.Kill();
                for (int i = 0; i < 20; i++)
                {
                    Dust d = Dust.NewDustDirect(boss.Center, 1, 1,
                        DustID.WhiteTorch, 0, 0, 0, Color.White, 2.5f);
                    d.noGravity = true;
                    d.velocity = Main.rand.NextVector2Circular(8, 8);
                    d.fadeIn = 1.3f;
                }
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.6f }, boss.Center);
            }
        }

        private Player FindClosestPlayer()
        {
            float closest = float.MaxValue;
            Player result = null;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead)
                {
                    float dist = Vector2.Distance(Projectile.Center, Main.player[i].Center);
                    if (dist < closest) { closest = dist; result = Main.player[i]; }
                }
            }
            return result;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = tex.Size() / 2f;
            SpriteBatch sb = Main.spriteBatch;

            if (trailPositions.Count > 1)
                for (int i = trailPositions.Count - 1; i >= 0; i--)
                {
                    float trailProgress = (float)i / trailPositions.Count;
                    float trailOpacity = (1f - trailProgress) * 0.5f * Projectile.Opacity;
                    float tScale = trailScales.Count > i ? trailScales[i] : currentScale;
                    float tRot = trailRotations.Count > i ? trailRotations[i] : Projectile.rotation;

                    sb.Draw(tex, trailPositions[i] - Main.screenPosition,
                        null, Color.White * trailOpacity * 0.6f, tRot, origin, tScale * 0.95f, SpriteEffects.None, 0f);

                    Color colorTrail = Color.Lerp(Color.MediumPurple, Color.Cyan, trailProgress) * trailOpacity * 0.4f;
                    sb.Draw(tex, trailPositions[i] - Main.screenPosition,
                        null, colorTrail, tRot, origin, tScale * 1.05f, SpriteEffects.None, 0f);
                }

            float glitchAmount = CurrentPhase == 1 ? 5f : (CurrentPhase >= 2 ? 3f : 1.5f);
            if (IsBarrageMode) glitchAmount *= 1.5f;

            if (Main.rand.NextBool(CurrentPhase == 1 ? 1 : 3))
            {
                Vector2 ro = new Vector2(Main.rand.NextFloat(-glitchAmount, glitchAmount), Main.rand.NextFloat(-glitchAmount, glitchAmount));
                Vector2 bo = new Vector2(Main.rand.NextFloat(-glitchAmount, glitchAmount), Main.rand.NextFloat(-glitchAmount, glitchAmount));
                sb.Draw(tex, Projectile.Center - Main.screenPosition + ro, null, Color.Red * 0.25f * Projectile.Opacity,
                    Projectile.rotation, origin, currentScale, SpriteEffects.None, 0f);
                sb.Draw(tex, Projectile.Center - Main.screenPosition + bo, null, Color.Blue * 0.25f * Projectile.Opacity,
                    Projectile.rotation, origin, currentScale, SpriteEffects.None, 0f);
            }

            float glowPulse = (float)Math.Sin(Main.GameUpdateCount * 0.12f) * 0.15f + 0.85f;
            sb.Draw(tex, Projectile.Center - Main.screenPosition, null,
                Color.White * 0.2f * glowIntensity * glowPulse * Projectile.Opacity,
                Projectile.rotation, origin, currentScale * 1.25f, SpriteEffects.None, 0f);
            sb.Draw(tex, Projectile.Center - Main.screenPosition, null,
                Color.MediumPurple * 0.15f * glowIntensity * Projectile.Opacity,
                Projectile.rotation, origin, currentScale * 1.35f, SpriteEffects.None, 0f);

            Color mainColor = Color.White * Projectile.Opacity;
            if (Main.rand.NextBool(20))
                mainColor = new Color(Main.rand.Next(200, 256), Main.rand.Next(200, 256), Main.rand.Next(200, 256)) * Projectile.Opacity;

            Vector2 drawOffset = CurrentPhase == 1 ? Main.rand.NextVector2Circular(2f, 2f) : Vector2.Zero;
            sb.Draw(tex, Projectile.Center - Main.screenPosition + drawOffset, null, mainColor,
                Projectile.rotation, origin, currentScale, SpriteEffects.None, 0f);

            if (CurrentPhase == 1)
                sb.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White * 0.6f * Projectile.Opacity,
                    Projectile.rotation, origin, currentScale * 0.6f, SpriteEffects.None, 0f);

            return false;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            targetScale += 0.5f;
            if (Main.rand.NextBool(3)) target.AddBuff(BuffID.Confused, 60);
            if (Main.rand.NextBool(4)) target.AddBuff(BuffID.Slow, 90);
            if (Main.rand.NextBool(5)) target.AddBuff(BuffID.Blackout, 45);

            for (int i = 0; i < 12; i++)
            {
                Dust d = Dust.NewDustDirect(target.Center, 1, 1, DustID.WhiteTorch, 0, 0, 0, Color.White, 2.2f);
                d.noGravity = true;
                d.velocity = Main.rand.NextVector2Circular(10, 10);
                d.fadeIn = 1.5f;
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 15; i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.Center, 1, 1, DustID.WhiteTorch, 0, 0, 50, Color.White, 1.5f);
                d.noGravity = true;
                d.velocity = Main.rand.NextVector2Circular(6, 6);
            }
        }

        private float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
        private float EaseInOutCubic(float t) => t < 0.5f ? 4f * t * t * t : 1f - (float)Math.Pow(-2f * t + 2f, 3) / 2f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float sw = Projectile.width * currentScale;
            float sh = Projectile.height * currentScale;
            Rectangle scaled = new Rectangle(
                (int)(Projectile.Center.X - sw / 2f), (int)(Projectile.Center.Y - sh / 2f),
                (int)sw, (int)sh);
            return scaled.Intersects(targetHitbox);
        }
    }
}