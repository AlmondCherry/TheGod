using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace GodEyeMod.Content.NPCs.GodEye.sky
{
    public class GodEyeSkyRegistration : ModSystem
    {
        public override void Load()
        {
            if (Main.dedServ)
                return;

            SkyManager.Instance["GodEyeMod:GodEyeSky"] = new GodEyeSky();
        }

        public override void Unload()
        {
        }
    }
}