using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using System.Collections.Generic;

namespace GodEyeMod.Content.test
{
    public class VoidGazeOverlaySystem : ModSystem
    {
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int idx = layers.FindIndex(l => l.Name.Equals("Vanilla: Mouse Text"));
            if (idx != -1)
            {
                layers.Insert(idx, new LegacyGameInterfaceLayer("GodEyeMod: VoidOverlay",
                    delegate
                    {
                        if (Main.LocalPlayer != null)
                        {
                            Main.LocalPlayer.GetModPlayer<VoidGazePlayer>()
                                .DrawScreenEffects(Main.spriteBatch);
                        }
                        return true;
                    },
                    InterfaceScaleType.UI));
            }
        }
    }
}