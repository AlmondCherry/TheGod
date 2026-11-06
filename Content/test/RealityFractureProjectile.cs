using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using System;

namespace GodEyeMod.Content.test
{
    public class RealityFractureProjectile : ModProjectile
    {
        private int delayFrames = 0;
        private bool activated = false;
        private float crackAngle;
        private float crackLength;
        private float activationFlash = 0f;
        private float[] branchAngles = new float[4];
        private float[] branchLengths = new float[4];

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 130;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }

        public override void AI()
        {
            int delay = (int)Projectile.ai[0];

            if (delayFrames < delay)
            {
                delayFrames++;
                Projectile.damage = 0;

                float warn = (float)delayFrames / Math.Max(delay, 1);

                if (warn > 0.3f)
                {
                    int count = (int)(4 * warn);
                    for (int i = 0; i < count; i++)
                    {
                        Vector2 dp = Projectile.Center + Main.rand.NextVector2Circular(6f * warn, 6f * warn);
                        Dust d = Dust.NewDustPerfect(dp, DustID.RedTorch, Vector2.Zero, 150, default, warn * 0.5f);
                        d.noGravity = true;
                        d.fadeIn = 0.8f;
                    }

                    if (warn > 0.7f && Main.rand.NextBool(3))
                    {
                        float preAngle = Main.rand.NextFloat(-0.5f, 0.5f);
                        Vector2 dir = new Vector2((float)Math.Cos(preAngle + MathHelper.PiOver2),
                            (float)Math.Sin(preAngle + MathHelper.PiOver2));
                        Vector2 linePos = Projectile.Center + dir * Main.rand.NextFloat(-30f, 30f);
                        Dust ld = Dust.NewDustPerfect(linePos, DustID.WhiteTorch, Vector2.Zero, 200, default, 0.15f);
                        ld.noGravity = true;
                    }
                }
                return;
            }

            if (!activated)
            {
                activated = true;
                activationFlash = 1f;
                crackAngle = Main.rand.NextFloat(-0.4f, 0.4f);
                crackLength = Main.rand.NextFloat(90f, 160f);
                Projectile.rotation = crackAngle;
                Projectile.damage = Projectile.originalDamage;

                for (int i = 0; i < branchAngles.Length; i++)
                {
                    branchAngles[i] = crackAngle + MathHelper.PiOver2 + Main.rand.NextFloat(-0.8f, 0.8f);
                    branchLengths[i] = Main.rand.NextFloat(20f, 50f);
                }

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122 with { Pitch = -0.3f, Volume = 0.8f }, Projectile.Center);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath55 with { Pitch = -0.8f, Volume = 0.3f }, Projectile.Center);

                Vector2 crackDir = new Vector2((float)Math.Cos(crackAngle + MathHelper.PiOver2),
                    (float)Math.Sin(crackAngle + MathHelper.PiOver2));
                Vector2 crackPerp = new Vector2(-crackDir.Y, crackDir.X);

                for (int i = 0; i < 40; i++)
                {
                    float offset = Main.rand.NextFloat(-crackLength / 2f, crackLength / 2f);
                    Vector2 pos = Projectile.Center + crackDir * offset;
                    Vector2 vel = crackPerp * Main.rand.NextFloat(-8f, 8f) + crackDir * Main.rand.NextFloat(-2f, 2f);

                    Dust d = Dust.NewDustPerfect(pos, DustID.RedTorch, vel, 40, default, Main.rand.NextFloat(1f, 1.8f));
                    d.noGravity = true;
                    d.fadeIn = 1.2f;
                }

                for (int i = 0; i < 15; i++)
                {
                    float offset = Main.rand.NextFloat(-crackLength / 3f, crackLength / 3f);
                    Vector2 pos = Projectile.Center + crackDir * offset;
                    Dust w = Dust.NewDustPerfect(pos, DustID.WhiteTorch,
                        crackPerp * Main.rand.NextFloat(-3f, 3f), 50, default, 1f);
                    w.noGravity = true;
                }

                if (Projectile.owner == Main.myPlayer)
                    Main.LocalPlayer.GetModPlayer<VoidGazePlayer>().TriggerShake(8, 5f);
            }

            activationFlash *= 0.93f;

            Vector2 mainDir = new Vector2((float)Math.Cos(crackAngle + MathHelper.PiOver2),
                (float)Math.Sin(crackAngle + MathHelper.PiOver2));
            Vector2 mainPerp = new Vector2(-mainDir.Y, mainDir.X);

            for (int i = 0; i < 5; i++)
            {
                float offset = Main.rand.NextFloat(-crackLength / 2f, crackLength / 2f);
                Vector2 pos = Projectile.Center + mainDir * offset;

                Vector2 vel = mainPerp * Main.rand.NextFloat(-3f, 3f);
                Dust d = Dust.NewDustPerfect(pos + Main.rand.NextVector2Circular(3, 3),
                    DustID.RedTorch, vel, 100, default, Main.rand.NextFloat(0.4f, 0.8f));
                d.noGravity = true;
            }

            for (int i = 0; i < 2; i++)
            {
                float offset = Main.rand.NextFloat(-crackLength / 3f, crackLength / 3f);
                Vector2 pos = Projectile.Center + mainDir * offset;
                Dust vd = Dust.NewDustPerfect(pos, DustID.Smoke,
                    mainPerp * Main.rand.NextFloat(-0.5f, 0.5f), 235, Color.Black, Main.rand.NextFloat(0.6f, 1.2f));
                vd.noGravity = true;
            }

            if (Main.rand.NextBool(3))
            {
                int branchIdx = Main.rand.Next(branchAngles.Length);
                float bOffset = Main.rand.NextFloat(0f, branchLengths[branchIdx]);
                Vector2 bDir = new Vector2((float)Math.Cos(branchAngles[branchIdx]),
                    (float)Math.Sin(branchAngles[branchIdx]));
                float mainOffset = Main.rand.NextFloat(-crackLength / 3f, crackLength / 3f);
                Vector2 branchStart = Projectile.Center + mainDir * mainOffset;
                Vector2 bPos = branchStart + bDir * bOffset;

                Dust bd = Dust.NewDustPerfect(bPos, DustID.RedTorch, Vector2.Zero, 150, default, 0.3f);
                bd.noGravity = true;
            }

            if (Main.rand.NextBool(6))
            {
                float offset = Main.rand.NextFloat(-crackLength / 2f, crackLength / 2f);
                Vector2 startP = Projectile.Center + mainDir * offset;
                Vector2 endP = startP + mainPerp * Main.rand.NextFloat(-40f, 40f);

                int segments = Main.rand.Next(3, 6);
                Vector2 prev = startP;
                for (int s = 0; s < segments; s++)
                {
                    float t = (float)(s + 1) / segments;
                    Vector2 curr = Vector2.Lerp(startP, endP, t) + Main.rand.NextVector2Circular(5f, 5f);
                    Dust ld = Dust.NewDustPerfect(curr, DustID.RedTorch,
                        (curr - prev).SafeNormalize(Vector2.Zero) * 0.5f, 100, default, 0.25f);
                    ld.noGravity = true;
                    prev = curr;
                }
            }

            float lifeRatio = (float)Projectile.timeLeft / 150f;
            Projectile.alpha = lifeRatio < 0.15f ? (int)(255 * (1f - lifeRatio / 0.15f)) : 0;

            Lighting.AddLight(Projectile.Center, 0.9f * lifeRatio, 0.05f * lifeRatio, 0.05f * lifeRatio);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.ShadowFlame, 360);
            target.AddBuff(BuffID.OnFire3, 360);

            for (int i = 0; i < 8; i++)
            {
                Dust.NewDustPerfect(target.Center, DustID.RedTorch,
                    Main.rand.NextVector2Circular(7f, 7f), 50, default, 1.3f).noGravity = true;
            }

            if (target.life <= 0)
            {
                Player owner = Main.player[Projectile.owner];
                if (owner.HeldItem.ModItem is VoidGaze vg)
                    vg.RegisterKill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!activated) return false;

            SpriteBatch sb = Main.spriteBatch;
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Texture2D glowTex = TextureAssets.Extra[89].Value;
            Vector2 origin = tex.Size() / 2f;
            Vector2 glowOrigin = glowTex.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            float lifeRatio = (float)Projectile.timeLeft / 150f;
            float fadeAlpha = lifeRatio < 0.15f ? lifeRatio / 0.15f : 1f;

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Vector2 crackDir = new Vector2((float)Math.Cos(crackAngle + MathHelper.PiOver2),
                (float)Math.Sin(crackAngle + MathHelper.PiOver2));

            int glowPoints = 8;
            for (int i = 0; i < glowPoints; i++)
            {
                float t = (float)i / (glowPoints - 1) - 0.5f;
                Vector2 gPos = drawPos + crackDir * t * crackLength;

                float intensity = (1f - Math.Abs(t) * 1.5f);
                intensity = Math.Max(intensity, 0.1f);
                float flicker = 1f + 0.2f * (float)Math.Sin(Main.GameUpdateCount * 0.5 + i * 1.5);

                sb.Draw(glowTex, gPos, null, new Color(200, 0, 0, 0) * intensity * fadeAlpha * 0.3f * flicker,
                    0f, glowOrigin, 1.2f, SpriteEffects.None, 0f);

                sb.Draw(glowTex, gPos, null, new Color(255, 100, 100, 0) * intensity * fadeAlpha * 0.5f * flicker,
                    0f, glowOrigin, 0.4f, SpriteEffects.None, 0f);
            }

            if (activationFlash > 0.01f)
            {
                sb.Draw(glowTex, drawPos, null, new Color(255, 200, 200, 0) * activationFlash * 0.6f,
                    0f, glowOrigin, activationFlash * 5f, SpriteEffects.None, 0f);
            }

            for (int layer = 4; layer >= 0; layer--)
            {
                float flicker = (float)Math.Sin(Main.GameUpdateCount * 0.6 + layer * 2f) * 1.5f;
                Vector2 crackPerp = new Vector2(-crackDir.Y, crackDir.X);
                Vector2 offset = crackPerp * (flicker + layer * 1f);

                float la = (0.5f - layer * 0.08f) * fadeAlpha;
                Color lc;
                if (layer == 0)
                    lc = new Color(255, 220, 220, 0) * la;
                else if (layer < 2)
                    lc = new Color(255, 50, 50, 0) * la;
                else
                    lc = new Color(150, 0, 0, 0) * la;

                float scaleX = 0.15f + layer * 0.06f;
                float scaleY = (crackLength / tex.Height) * (1f + layer * 0.15f);

                sb.Draw(tex, drawPos + offset, null, lc,
                    crackAngle, origin, new Vector2(scaleX, scaleY), SpriteEffects.None, 0f);
            }

            for (int b = 0; b < branchAngles.Length; b++)
            {
                float mainOffset = (b - branchAngles.Length / 2f) / branchAngles.Length * crackLength * 0.6f;
                Vector2 branchStart = drawPos + crackDir * mainOffset;

                float bScaleY = branchLengths[b] / tex.Height;
                float bFlicker = 0.3f + 0.15f * (float)Math.Sin(Main.GameUpdateCount * 0.4 + b * 3f);

                sb.Draw(tex, branchStart, null, new Color(200, 0, 0, 0) * bFlicker * fadeAlpha,
                    branchAngles[b], origin, new Vector2(0.06f, bScaleY), SpriteEffects.None, 0f);
            }

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            sb.Draw(tex, drawPos, null, Color.White * 0.9f * fadeAlpha,
                crackAngle, origin, new Vector2(0.04f, crackLength / tex.Height * 0.85f), SpriteEffects.None, 0f);

            return false;
        }
    }
}