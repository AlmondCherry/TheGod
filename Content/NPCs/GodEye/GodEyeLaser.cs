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
    public class GodEyeLaser : ModProjectile
    {
        private Color primaryColor = Color.Cyan;
        private Color secondaryColor = Color.Purple;
        private Color glowColor = Color.White;

        private float BehaviorType => Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 12;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 360;
            Projectile.alpha = 0;
            Projectile.light = 1.2f;
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            UpdatePhaseColors();

            float pulse = 1f + (float)Math.Sin(Main.GameUpdateCount * 0.15f + Projectile.whoAmI * 0.5f) * 0.1f;
            Projectile.scale = pulse;

            Vector3 lightColor = primaryColor.ToVector3();
            Lighting.AddLight(Projectile.Center, lightColor.X * 1.5f, lightColor.Y * 1.5f, lightColor.Z * 1.5f);

            for (int i = 0; i < Projectile.oldPos.Length; i += 4)
            {
                if (Projectile.oldPos[i] != Vector2.Zero)
                {
                    float fade = 1f - (float)i / Projectile.oldPos.Length;
                    Vector2 trailPoint = Projectile.oldPos[i] + Projectile.Size / 2f;
                    Lighting.AddLight(trailPoint, lightColor * fade * 0.6f);
                }
            }

            switch ((int)BehaviorType)
            {
                case 1:
                    HomingBehavior();
                    break;

                case 2:
                    DeceleratingBehavior();
                    break;

                default:
                    break;
            }

            if (Main.rand.NextBool(3))
            {
                Vector2 perpendicular = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.PiOver2);
                Vector2 dustOffset = perpendicular * Main.rand.NextFloat(-5f, 5f);

                Dust dust = Dust.NewDustDirect(
                    Projectile.Center + dustOffset - new Vector2(4, 4), 1, 1,
                    DustID.Vortex, 0, 0, 100, default, Main.rand.NextFloat(0.5f, 1f));
                dust.noGravity = true;
                dust.velocity = -Projectile.velocity * 0.1f;
                dust.fadeIn = 1.1f;
            }
        }

        private void HomingBehavior()
        {
            if (Projectile.timeLeft > 240)
            {
                Player target = Main.player[(int)Projectile.ai[1]];
                if (target.active && !target.dead)
                {
                    float targetAngle = (target.Center - Projectile.Center).ToRotation();
                    float currentAngle = Projectile.velocity.ToRotation();

                    float turnSpeed = MathHelper.ToRadians(1.5f);
                    float angleDiff = MathHelper.WrapAngle(targetAngle - currentAngle);
                    angleDiff = MathHelper.Clamp(angleDiff, -turnSpeed, turnSpeed);

                    Projectile.velocity = Projectile.velocity.RotatedBy(angleDiff);
                }

                if (Main.rand.NextBool(6))
                {
                    Dust d = Dust.NewDustDirect(Projectile.Center - new Vector2(3, 3), 1, 1,
                        DustID.CursedTorch, 0, 0, 100, default, 0.5f);
                    d.noGravity = true;
                    d.velocity *= 0.1f;
                }
            }
        }

        private void DeceleratingBehavior()
        {
            int aliveTime = 360 - Projectile.timeLeft;

            if (aliveTime < 60)
            {
                Projectile.velocity *= 0.97f;

                Projectile.scale = 1f + aliveTime / 60f * 0.5f;

                if (aliveTime > 40)
                {
                    Dust d = Dust.NewDustDirect(Projectile.Center - new Vector2(5, 5), 10, 10,
                        DustID.Torch, 0, 0, 100, default, 1f);
                    d.noGravity = true;
                    d.velocity = Main.rand.NextVector2Circular(2, 2);
                }
            }
            else if (aliveTime == 60)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                    float baseAngle = (target.Center - Projectile.Center).ToRotation();

                    for (int i = -2; i <= 2; i++)
                    {
                        float angle = baseAngle + MathHelper.ToRadians(20f * i);
                        Vector2 vel = angle.ToRotationVector2() * 7f;
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                            Projectile.Center, vel, Projectile.type,
                            Projectile.damage / 2, 0, Main.myPlayer, 0f, 0f);
                    }

                    for (int i = 0; i < 10; i++)
                    {
                        Dust d = Dust.NewDustDirect(Projectile.Center, 1, 1,
                            DustID.Vortex, 0, 0, 100, default, 1.5f);
                        d.noGravity = true;
                        d.velocity = Main.rand.NextVector2Circular(6, 6);
                    }
                    SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
                }

                Projectile.Kill();
            }
        }

        private void UpdatePhaseColors()
        {
            float bossLifeRatio = 1f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<GodEyeBoss>())
                {
                    bossLifeRatio = (float)Main.npc[i].life / Main.npc[i].lifeMax;
                    break;
                }
            }

            if (bossLifeRatio > 0.7f)
            {
                primaryColor = Color.Cyan;
                secondaryColor = Color.MediumPurple;
                glowColor = Color.LightCyan;
            }
            else if (bossLifeRatio > 0.4f)
            {
                primaryColor = Color.BlueViolet;
                secondaryColor = Color.DeepPink;
                glowColor = Color.Lavender;
            }
            else if (bossLifeRatio > 0.15f)
            {
                primaryColor = Color.OrangeRed;
                secondaryColor = Color.DarkRed;
                glowColor = Color.Orange;
            }
            else
            {
                float hue = (Main.GameUpdateCount * 0.02f + Projectile.whoAmI * 0.1f) % 1f;
                primaryColor = Main.hslToRgb(hue, 1f, 0.6f);
                secondaryColor = Main.hslToRgb((hue + 0.3f) % 1f, 1f, 0.4f);
                glowColor = Main.hslToRgb((hue + 0.15f) % 1f, 0.8f, 0.8f);
            }

            if ((int)BehaviorType == 1)
            {
                primaryColor = Color.Lerp(primaryColor, Color.LimeGreen, 0.4f);
                glowColor = Color.Lerp(glowColor, Color.GreenYellow, 0.3f);
            }

            if ((int)BehaviorType == 2)
            {
                primaryColor = Color.Lerp(primaryColor, Color.Orange, 0.5f);
                glowColor = Color.Lerp(glowColor, Color.Gold, 0.4f);
            }
        }

        private bool IsLatePhase()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<GodEyeBoss>())
                    return (float)Main.npc[i].life / Main.npc[i].lifeMax < 0.4f;
            }
            return false;
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
                float opacity = (1f - progress) * 0.6f;
                float scale = Projectile.scale * (1f - progress * 0.5f);

                Color trailColor = Color.Lerp(primaryColor, secondaryColor, progress) * opacity;
                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;

                Main.EntitySpriteDraw(texture, trailPos, null, trailColor,
                    Projectile.oldRot[i], origin, scale, SpriteEffects.None, 0);
            }

            float glowPulse = 1f + (float)Math.Sin(Main.GameUpdateCount * 0.12f + Projectile.whoAmI) * 0.15f;

            Main.EntitySpriteDraw(texture, drawCenter, null,
                secondaryColor * 0.2f, Projectile.rotation, origin,
                Projectile.scale * 2.5f * glowPulse, SpriteEffects.None, 0);

            Main.EntitySpriteDraw(texture, drawCenter, null,
                primaryColor * 0.35f, Projectile.rotation, origin,
                Projectile.scale * 1.8f * glowPulse, SpriteEffects.None, 0);

            float aberration = IsLatePhase() ? 2.5f : 1.2f;

            Main.EntitySpriteDraw(texture, drawCenter + new Vector2(-aberration, 0), null,
                Color.Red * 0.2f, Projectile.rotation, origin,
                Projectile.scale * 1.05f, SpriteEffects.None, 0);

            Main.EntitySpriteDraw(texture, drawCenter + new Vector2(aberration, 0), null,
                Color.Blue * 0.2f, Projectile.rotation, origin,
                Projectile.scale * 1.05f, SpriteEffects.None, 0);

            Main.EntitySpriteDraw(texture, drawCenter, null,
                glowColor, Projectile.rotation, origin,
                Projectile.scale * 1.2f, SpriteEffects.None, 0);

            Main.EntitySpriteDraw(texture, drawCenter, null,
                Color.White * 0.8f, Projectile.rotation, origin,
                Projectile.scale * 0.5f, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.White;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                    DustID.Vortex, 0, 0, 100, default, Main.rand.NextFloat(1f, 1.5f));
                dust.noGravity = true;
                dust.velocity = Main.rand.NextVector2Circular(4, 4);
                dust.fadeIn = 1.2f;
            }

            for (int i = 0; i < 5; i++)
            {
                Dust spark = Dust.NewDustDirect(Projectile.Center - new Vector2(2, 2), 1, 1,
                    DustID.FireworksRGB, 0, 0, 0, primaryColor, Main.rand.NextFloat(0.4f, 0.7f));
                spark.noGravity = true;
                spark.velocity = Main.rand.NextVector2Circular(5, 5);
            }
        }
    }
}