using System;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace GodEyeMod.Content.NPCs.GodEye
{
    public class ScreenShakePlayer : ModPlayer
    {
        private float shakeAmount = 0f;
        private float shakeDecay = 0.9f;
        private float maxShake = 25f;

        public void AddShake(float amount)
        {
            shakeAmount = Math.Min(shakeAmount + amount, maxShake);
        }

        public void SetShake(float amount)
        {
            shakeAmount = Math.Min(amount, maxShake);
        }

        public override void ModifyScreenPosition()
        {
            if (shakeAmount > 0.1f)
            {
                float offsetX = Main.rand.NextFloat(-shakeAmount, shakeAmount);
                float offsetY = Main.rand.NextFloat(-shakeAmount, shakeAmount);
                Main.screenPosition += new Vector2(offsetX, offsetY);

                shakeAmount *= shakeDecay;
            }
            else
            {
                shakeAmount = 0f;
            }
        }

        public override void UpdateDead()
        {
            shakeAmount = 0f;
        }
    }
}