using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace GodEyeMod.Content.boss2
{
    public class GodBossPhase2Camera : ModSystem
    {
        private static bool lockActive = false;
        private static Vector2 lockTarget;
        private static float lockProgress = 0f;
        private static float lockSpeed = 0.03f;

        public static void LockTo(Vector2 target, float speed = 0.03f)
        {
            lockActive = true;
            lockTarget = target;
            lockSpeed = speed;
            lockProgress = 0f;
        }

        public static void Release()
        {
            lockActive = false;
            lockProgress = 0f;
        }

        public static bool IsLocked => lockActive;

        public override void ModifyScreenPosition()
        {
            if (!lockActive) return;

            var config = ModContent.GetInstance<GodEyeConfig>();
            if (config != null && config.DisableAmbientVisuals) return;

            lockProgress = Math.Clamp(lockProgress + lockSpeed, 0f, 1f);

            Vector2 playerCenter = Main.LocalPlayer.Center;
            Vector2 normalScreen = playerCenter - new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);

            Vector2 bossScreen = lockTarget - new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
            Vector2 midPoint = Vector2.Lerp(normalScreen, bossScreen, 0.6f);

            Main.screenPosition = Vector2.Lerp(Main.screenPosition, midPoint, lockProgress * 0.5f);
        }
    }
}