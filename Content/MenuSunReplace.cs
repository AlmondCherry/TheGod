using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace GodEyeMod.Content
{
    public class MenuSunReplace : ModSystem
    {
        private Asset<Texture2D> originalSun;
        private Asset<Texture2D> originalSun2;
        private Asset<Texture2D> originalSun3;
        private Asset<Texture2D>[] originalMoon;

        private Asset<Texture2D> eyeTexture;
        private Asset<Texture2D> eyeMoonTexture;

        private bool savedOriginals = false;

        public override void Load()
        {
            eyeTexture = ModContent.Request<Texture2D>(
                "GodEyeMod/Content/EyeSun", AssetRequestMode.AsyncLoad);

            eyeMoonTexture = ModContent.Request<Texture2D>(
                "GodEyeMod/Content/EyeMoon", AssetRequestMode.AsyncLoad);

            On_Main.Update += OnUpdate;
        }

        public override void Unload()
        {
            On_Main.Update -= OnUpdate;

            RestoreOriginals();

            originalSun = null;
            originalSun2 = null;
            originalSun3 = null;
            originalMoon = null;
            eyeTexture = null;
            eyeMoonTexture = null;
            savedOriginals = false;
        }

        private void SaveOriginals()
        {
            if (savedOriginals)
                return;

            if (TextureAssets.Sun == null || TextureAssets.Moon == null)
                return;

            originalSun = TextureAssets.Sun;
            originalSun2 = TextureAssets.Sun2;
            originalSun3 = TextureAssets.Sun3;

            originalMoon = new Asset<Texture2D>[TextureAssets.Moon.Length];
            for (int i = 0; i < TextureAssets.Moon.Length; i++)
            {
                originalMoon[i] = TextureAssets.Moon[i];
            }

            savedOriginals = true;
        }

        private void RestoreOriginals()
        {
            if (!savedOriginals)
                return;

            TextureAssets.Sun = originalSun;
            TextureAssets.Sun2 = originalSun2;
            TextureAssets.Sun3 = originalSun3;

            if (originalMoon != null)
            {
                for (int i = 0; i < originalMoon.Length && i < TextureAssets.Moon.Length; i++)
                {
                    TextureAssets.Moon[i] = originalMoon[i];
                }
            }
        }

        private void ApplyCustomTextures()
        {
            TextureAssets.Sun = eyeTexture;
            TextureAssets.Sun2 = eyeTexture;
            TextureAssets.Sun3 = eyeTexture;

            for (int i = 0; i < TextureAssets.Moon.Length; i++)
            {
                TextureAssets.Moon[i] = eyeMoonTexture;
            }
        }

        private void OnUpdate(On_Main.orig_Update orig, Main self, Microsoft.Xna.Framework.GameTime gameTime)
        {
            SaveOriginals();

            if (savedOriginals && eyeTexture != null && eyeTexture.IsLoaded
                && eyeMoonTexture != null && eyeMoonTexture.IsLoaded)
            {
                if (Main.gameMenu)
                    ApplyCustomTextures();
                else
                    RestoreOriginals();
            }

            orig(self, gameTime);
        }
    }
}