using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using System;

namespace GodEyeMod.Content.test
{
    public class VoidGazePlayer : ModPlayer
    {
        private int shakeTimer = 0;
        private float shakeIntensity = 0f;
        private int darkTimer = 0;
        private int flashTimer = 0;
        private int sentinelCooldown = 0;

        public void TriggerShake(int duration, float intensity = -1f)
        {
            shakeTimer = Math.Max(shakeTimer, duration);
            shakeIntensity = intensity > 0 ? intensity : duration * 0.5f;
        }

        public void TriggerDarkness(int duration) => darkTimer = Math.Max(darkTimer, duration);
        public void TriggerRedFlash(int duration) => flashTimer = Math.Max(flashTimer, duration);

        public override void PostUpdate()
        {
            if (darkTimer > 0) darkTimer--;
            if (flashTimer > 0) flashTimer--;

            bool holding = Player.HeldItem.ModItem is VoidGaze;

            if (holding)
            {
                float pulse = (float)(0.35 + 0.25 * Math.Sin(Main.GameUpdateCount * 0.08));
                Lighting.AddLight(Player.Center, pulse, 0f, 0f);

                if (Main.rand.NextBool(6))
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(90f, 90f);
                    Vector2 toCenter = -offset * 0.02f;
                    Dust d = Dust.NewDustPerfect(Player.Center + offset,
                        DustID.RedTorch, toCenter, 180, default, 0.35f);
                    d.noGravity = true;
                }

                sentinelCooldown--;
                if (sentinelCooldown <= 0 && Player.whoAmI == Main.myPlayer)
                {
                    sentinelCooldown = 360;
                    SpawnSentinels();
                }
            }
        }

        private void SpawnSentinels()
        {
            int existing = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].owner == Player.whoAmI &&
                    Main.projectile[i].type == ModContent.ProjectileType<SentinelEyeProjectile>())
                    existing++;
            }

            int maxSentinels = 3;
            for (int i = existing; i < maxSentinels; i++)
            {
                float baseAngle = MathHelper.TwoPi / maxSentinels * i;
                Vector2 pos = Player.Center + new Vector2((float)Math.Cos(baseAngle), (float)Math.Sin(baseAngle)) * 100f;

                Projectile.NewProjectile(Player.GetSource_FromThis(), pos, Vector2.Zero,
                    ModContent.ProjectileType<SentinelEyeProjectile>(),
                    Math.Max(Player.HeldItem.damage / 3, 100), 2f, Player.whoAmI,
                    ai0: baseAngle);
            }
        }

        public override void ModifyScreenPosition()
        {
            if (shakeTimer > 0)
            {
                shakeTimer--;
                float fade = (float)shakeTimer / Math.Max(shakeIntensity * 2f, 1f);
                float str = shakeIntensity * fade;
                str = Math.Min(str, 14f);

                Main.screenPosition.X += Main.rand.NextFloat(-str, str);
                Main.screenPosition.Y += Main.rand.NextFloat(-str, str);
            }
        }

        public void DrawScreenEffects(SpriteBatch sb)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle screen = new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);

            if (flashTimer > 0)
            {
                float a = (float)flashTimer / 15f * 0.35f;
                sb.Draw(pixel, screen, new Color(255, 0, 0, (int)(a * 255)));
            }

            if (darkTimer > 0)
            {
                float a = (float)darkTimer / 30f * 0.6f;
                sb.Draw(pixel, screen, new Color(0, 0, 0, (int)(a * 255)));
            }

            if (Player.HeldItem.ModItem is VoidGaze)
            {
                float vignette = (float)(0.03 + 0.02 * Math.Sin(Main.GameUpdateCount * 0.04));
                sb.Draw(pixel, screen, new Color(0, 0, 0, (int)(vignette * 255)));
            }
        }
    }
}