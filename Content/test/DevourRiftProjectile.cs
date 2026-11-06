using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using System;

namespace GodEyeMod.Content.test
{
    public class DevourRiftProjectile : ModProjectile
    {
        private float currentScale = 0f;
        private const float PEAK = 4f;
        private const int LIFETIME = 300;
        private float innerRotation = 0f;
        private float outerRotation = 0f;

        public override void SetDefaults()
        {
            Projectile.width = 150;
            Projectile.height = 150;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LIFETIME;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override void AI()
        {
            float life = 1f - (float)Projectile.timeLeft / LIFETIME;

            if (life < 0.1f)
                currentScale = MathHelper.SmoothStep(0f, PEAK, life / 0.1f);
            else if (life > 0.85f)
                currentScale = MathHelper.SmoothStep(PEAK, 0f, (life - 0.85f) / 0.15f);
            else
                currentScale = PEAK + 0.3f * (float)Math.Sin(Main.GameUpdateCount * 0.05);

            Projectile.scale = currentScale;

            innerRotation += 0.04f * currentScale;
            outerRotation -= 0.02f * currentScale;

            float pullRadius = 500f * (currentScale / PEAK);
            float pullForce = 7f * (currentScale / PEAK);

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;

                float dist = Vector2.Distance(Projectile.Center, npc.Center);
                if (dist < pullRadius && dist > 5f)
                {
                    Vector2 pull = (Projectile.Center - npc.Center).SafeNormalize(Vector2.Zero);
                    float factor = 1f - dist / pullRadius;
                    factor = factor * factor * factor;
                    npc.velocity += pull * pullForce * factor;

                    if (dist < 60f * currentScale)
                    {
                        npc.AddBuff(BuffID.ShadowFlame, 30);
                        npc.AddBuff(BuffID.OnFire3, 30);
                    }
                }
            }

            for (int i = 0; i < Main.maxItems; i++)
            {
                if (!Main.item[i].active) continue;
                float dist = Vector2.Distance(Projectile.Center, Main.item[i].Center);
                if (dist < pullRadius * 0.3f && dist > 5f)
                {
                    Vector2 pull = (Projectile.Center - Main.item[i].Center).SafeNormalize(Vector2.Zero);
                    Main.item[i].velocity += pull * 0.3f;
                }
            }

            float scaleRatio = currentScale / PEAK;
            int dustCount = (int)(18 * scaleRatio);

            for (int i = 0; i < dustCount; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = Main.rand.NextFloat(20f, 100f) * currentScale;
                Vector2 pos = Projectile.Center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;

                Vector2 toC = (Projectile.Center - pos).SafeNormalize(Vector2.Zero);
                Vector2 tang = new Vector2(-toC.Y, toC.X);
                Vector2 vel = toC * 2f + tang * 4f;

                Dust d = Dust.NewDustPerfect(pos, DustID.RedTorch, vel, 100, default, Main.rand.NextFloat(0.5f, 1f));
                d.noGravity = true;
                d.fadeIn = 1.2f;
            }

            for (int i = 0; i < (int)(8 * scaleRatio); i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = Main.rand.NextFloat(5f, 40f) * currentScale;
                Vector2 pos = Projectile.Center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;

                Dust dark = Dust.NewDustPerfect(pos, DustID.Smoke, Vector2.Zero, 240, Color.Black,
                    Main.rand.NextFloat(1f, 2f));
                dark.noGravity = true;
            }

            if (Main.rand.NextBool(2))
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = (85f + Main.rand.NextFloat(-5f, 5f)) * currentScale;
                Vector2 pos = Projectile.Center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;

                Vector2 toC = (Projectile.Center - pos).SafeNormalize(Vector2.Zero);
                Vector2 tang = new Vector2(-toC.Y, toC.X);

                Dust ring = Dust.NewDustPerfect(pos, DustID.WhiteTorch, tang * 2f, 100, default, 0.3f);
                ring.noGravity = true;
            }

            if (Main.rand.NextBool(4) && currentScale > 2f)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float startR = 80f * currentScale;
                Vector2 start = Projectile.Center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * startR;

                int segs = Main.rand.Next(4, 8);
                Vector2 prev = start;
                for (int s = 0; s < segs; s++)
                {
                    float t = (float)(s + 1) / segs;
                    Vector2 curr = Vector2.Lerp(start, Projectile.Center, t) + Main.rand.NextVector2Circular(10f, 10f);
                    Dust ld = Dust.NewDustPerfect(curr, DustID.RedTorch,
                        (curr - prev).SafeNormalize(Vector2.Zero) * 0.3f, 80, default, 0.3f);
                    ld.noGravity = true;
                    prev = curr;
                }
            }

            if (Projectile.timeLeft % 22 == 0 && currentScale > 2.5f && Projectile.owner == Main.myPlayer)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 eyeVel = Main.rand.NextVector2CircularEdge(10f, 10f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, eyeVel,
                        ModContent.ProjectileType<VoidEyeProjectile>(),
                        Projectile.damage / 2, 2f, Projectile.owner, ai0: Main.rand.Next(5), ai1: 5);
                }
            }

            Lighting.AddLight(Projectile.Center, scaleRatio * 0.9f, scaleRatio * 0.05f, scaleRatio * 0.05f);

            if (Projectile.owner == Main.myPlayer && Main.rand.NextBool(8))
                Main.LocalPlayer.GetModPlayer<VoidGazePlayer>().TriggerShake(2, 2f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.ShadowFlame, 600);
            target.AddBuff(BuffID.CursedInferno, 600);
            target.AddBuff(BuffID.Slow, 300);

            if (target.life <= 0)
            {
                Player owner = Main.player[Projectile.owner];
                if (owner.HeldItem.ModItem is VoidGaze vg)
                    vg.RegisterKill();

                owner.statMana = Math.Min(owner.statMana + 40, owner.statManaMax2);
                owner.ManaEffect(40);
                owner.statLife = Math.Min(owner.statLife + 15, owner.statLifeMax2);
                owner.HealEffect(15);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (currentScale <= 0.05f) return false;

            SpriteBatch sb = Main.spriteBatch;
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Texture2D glowTex = TextureAssets.Extra[89].Value;
            Vector2 origin = tex.Size() / 2f;
            Vector2 glowOrigin = glowTex.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            float scaleRatio = currentScale / PEAK;

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            sb.Draw(glowTex, drawPos, null, new Color(100, 0, 0, 0) * scaleRatio * 0.25f,
                0f, glowOrigin, currentScale * 3f, SpriteEffects.None, 0f);

            sb.Draw(glowTex, drawPos, null, new Color(200, 0, 0, 0) * scaleRatio * 0.2f,
                0f, glowOrigin, currentScale * 1.8f, SpriteEffects.None, 0f);

            for (int ring = 0; ring < 5; ring++)
            {
                float ringScale = currentScale * (0.6f + ring * 0.25f);
                float ringAlpha = (0.15f - ring * 0.02f) * scaleRatio;
                float ringRot = outerRotation * (1f + ring * 0.3f) * (ring % 2 == 0 ? 1 : -1);

                Color ringCol = ring < 2 ? new Color(255, 50, 50, 0) : new Color(120, 0, 0, 0);

                sb.Draw(tex, drawPos, null, ringCol * ringAlpha,
                    ringRot, origin, ringScale, SpriteEffects.None, 0f);
            }

            sb.Draw(glowTex, drawPos, null, new Color(255, 100, 100, 0) * scaleRatio * 0.3f,
                innerRotation, glowOrigin, currentScale * 0.5f, SpriteEffects.None, 0f);

            int rayCount = 6;
            for (int i = 0; i < rayCount; i++)
            {
                float angle = MathHelper.TwoPi / rayCount * i + innerRotation * 0.5f;
                float rayLen = currentScale * 60f;
                float rayPulse = 0.8f + 0.2f * (float)Math.Sin(Main.GameUpdateCount * 0.15 + i * 1.2f);

                Vector2 rayEnd = drawPos + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * rayLen;
                Vector2 rayMid = Vector2.Lerp(drawPos, rayEnd, 0.5f);

                sb.Draw(glowTex, rayMid, null, new Color(255, 0, 0, 0) * 0.08f * scaleRatio * rayPulse,
                    angle, glowOrigin, new Vector2(rayLen / glowTex.Width * 2f, 0.15f), SpriteEffects.None, 0f);
            }

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            sb.Draw(tex, drawPos, null, new Color(0, 0, 0, 240),
                innerRotation * 0.3f, origin, currentScale * 0.4f, SpriteEffects.None, 0f);

            float eyePulse = 0.7f + 0.3f * (float)Math.Sin(Main.GameUpdateCount * 0.1);
            sb.Draw(tex, drawPos, null, new Color(255, 30, 30, 180) * eyePulse,
                0f, origin, currentScale * 0.2f, SpriteEffects.None, 0f);

            sb.Draw(glowTex, drawPos, null, Color.White * 0.7f * eyePulse,
                0f, glowOrigin, currentScale * 0.04f, SpriteEffects.None, 0f);

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.6f, Volume = 1.2f }, Projectile.Center);

            for (int wave = 0; wave < 3; wave++)
            {
                int count = 25 + wave * 10;
                float speed = 8f + wave * 5f;
                for (int i = 0; i < count; i++)
                {
                    float angle = MathHelper.TwoPi / count * i;
                    Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed;
                    int type = wave == 0 ? DustID.WhiteTorch : DustID.RedTorch;
                    float scale = wave == 0 ? 1.5f : Main.rand.NextFloat(1f, 2f);

                    Dust d = Dust.NewDustPerfect(Projectile.Center, type, vel, 30, default, scale);
                    d.noGravity = true;
                    d.fadeIn = 1.5f;
                }
            }

            for (int i = 0; i < 30; i++)
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.Smoke,
                    Main.rand.NextVector2Circular(12f, 12f), 200, Color.Black, 2.5f).noGravity = true;
            }

            if (Projectile.owner == Main.myPlayer)
                Main.LocalPlayer.GetModPlayer<VoidGazePlayer>().TriggerShake(30, 15f);
        }
    }
}