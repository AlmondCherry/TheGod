using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content.NPCs.GodEye.summon
{
    public class GodEyeStatisticsOrb : ModItem
    {
        private static int useCount = 0;

        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.value = Item.sellPrice(gold: 10);
            Item.rare = ItemRarityID.Purple;
            Item.maxStack = 1;
            Item.consumable = false;
            Item.noMelee = true;
        }

        public override bool? UseItem(Player player)
        {
            if (Main.myPlayer != player.whoAmI) return true;

            useCount++;

            if (GodEyeRitualSystem.AllSignsDone())
            {
                GiveGodEyeSummonItem(player);
            }

            DisplayProgress(player);
            PlayEffects(player);

            return true;
        }

        private void GiveGodEyeSummonItem(Player player)
        {
            Item.NewItem(
                player.GetSource_GiftOrReward(),
                player.Hitbox,
                ModContent.ItemType<GodEyeSummonItem>(),
                1);

            SoundEngine.PlaySound(SoundID.Roar with { Pitch = -1f, Volume = 1f }, player.Center);

            for (int i = 0; i < 30; i++)
            {
                float angle = MathHelper.TwoPi / 30f * i;
                Dust d = Dust.NewDustDirect(player.Center, 1, 1,
                    DustID.Shadowflame, 0, 0, 100, default, 2.5f);
                d.noGravity = true;
                d.velocity = angle.ToRotationVector2() * 8f;
            }

            Main.NewText("", Color.Black);
            Main.NewText("══════════════════════════════════", new Color(255, 0, 0));
            Main.NewText("  The God Eye Summon Item has appeared.", new Color(255, 50, 50));
            Main.NewText("  Use it when you are ready.", new Color(200, 0, 0));
            Main.NewText("══════════════════════════════════", new Color(255, 0, 0));
        }

        private void DisplayProgress(Player player)
        {
            int signs = GodEyeRitualSystem.CountSignsDone();
            double hoursPlayed = GodEyeRitualSystem.TimePlayedTicks / 216000.0;

            Main.NewText("", Color.Black);
            Main.NewText("══════════════════════════════════", new Color(120, 0, 180));
            Main.NewText("  RITUAL PROGRESS — " + signs + "/7 Signs", new Color(180, 0, 220));
            Main.NewText("══════════════════════════════════", new Color(120, 0, 180));

            if (GodEyeRitualSystem.Sign1_MoonLord)
                Main.NewText("  [DONE] Defeat Moon Lord", new Color(0, 200, 0));
            else
                Main.NewText("  [    ] Defeat Moon Lord", new Color(150, 150, 150));

            if (GodEyeRitualSystem.Sign2_Deaths)
                Main.NewText("  [DONE] Die 10 times (" + GodEyeRitualSystem.DeathCount + "/10)", new Color(0, 200, 0));
            else
                Main.NewText("  [    ] Die 10 times (" + GodEyeRitualSystem.DeathCount + "/10)", new Color(150, 150, 150));

            if (GodEyeRitualSystem.Sign3_Time)
                Main.NewText("  [DONE] Play for 10 hours (" + hoursPlayed.ToString("F1") + "h/10h)", new Color(0, 200, 0));
            else
                Main.NewText("  [    ] Play for 10 hours (" + hoursPlayed.ToString("F1") + "h/10h)", new Color(150, 150, 150));

            if (GodEyeRitualSystem.Sign4_Broken)
                Main.NewText("  [DONE] Break 666 blocks (" + GodEyeRitualSystem.BlocksBroken + "/666)", new Color(0, 200, 0));
            else
                Main.NewText("  [    ] Break 666 blocks (" + GodEyeRitualSystem.BlocksBroken + "/666)", new Color(150, 150, 150));

            if (GodEyeRitualSystem.Sign5_Placed)
                Main.NewText("  [DONE] Place 666 blocks (" + GodEyeRitualSystem.BlocksPlaced + "/666)", new Color(0, 200, 0));
            else
                Main.NewText("  [    ] Place 666 blocks (" + GodEyeRitualSystem.BlocksPlaced + "/666)", new Color(150, 150, 150));

            if (GodEyeRitualSystem.Sign6_Eclipse)
                Main.NewText("  [DONE] Survive 3 solar eclipses (" + GodEyeRitualSystem.EclipseCount + "/3)", new Color(0, 200, 0));
            else
                Main.NewText("  [    ] Survive 3 solar eclipses (" + GodEyeRitualSystem.EclipseCount + "/3)", new Color(150, 150, 150));

            if (GodEyeRitualSystem.Sign7_Nights)
                Main.NewText("  [DONE] Spend 5 consecutive nights outdoors (" + GodEyeRitualSystem.OutdoorNightCount + "/5)", new Color(0, 200, 0));
            else
                Main.NewText("  [    ] Spend 5 consecutive nights outdoors (" + GodEyeRitualSystem.OutdoorNightCount + "/5)", new Color(150, 150, 150));

            Main.NewText("══════════════════════════════════", new Color(120, 0, 180));

            if (signs >= 7)
            {
                Main.NewText("  STATUS: All signs complete! Summon item given.", new Color(255, 50, 50));
            }
            else
            {
                int remaining = 7 - signs;
                Main.NewText("  STATUS: " + remaining + " sign" + (remaining > 1 ? "s" : "") + " remaining.", new Color(200, 150, 50));
            }

            Main.NewText("", Color.Black);
            DisplayWorldInfo(player);

            if (signs >= 1)
            {
                Main.NewText("", Color.Black);
                DisplayGodComment(signs);
            }
        }

        private void DisplayWorldInfo(Player player)
        {
            string biome = GetBiome(player);
            string weather = GetWeather();
            int bossCount = CountBossesDefeated();
            int npcCount = CountTownNPCs();

            string[] moonPhases = { "Full Moon", "Waning Gibbous", "Third Quarter",
                "Waning Crescent", "New Moon", "Waxing Crescent", "First Quarter", "Waxing Gibbous" };
            string moonPhase = moonPhases[Main.moonPhase % 8];

            Main.NewText("  WORLD INFO", new Color(100, 180, 255));
            Main.NewText("  Biome: " + biome + " | Weather: " + weather, new Color(200, 200, 200));
            Main.NewText("  Moon: " + moonPhase + " | Bosses defeated: " + bossCount, new Color(200, 200, 200));
            Main.NewText("  Town NPCs: " + npcCount + " | HP: " + player.statLife + "/" + player.statLifeMax2, new Color(200, 200, 200));
        }

        private void DisplayGodComment(int signs)
        {
            bool chatDisabled = false;
            try
            {
                GodEyeConfig c = ModContent.GetInstance<GodEyeConfig>();
                if (c != null && c.DisableChatMessages) chatDisabled = true;
            }
            catch { }

            if (chatDisabled) return;

            string comment;

            if (signs >= 7)
            {
                string[] s7 = {
                    "\"All signs complete. I am almost free.\"",
                    "\"You did everything I asked. Good.\"",
                    "\"The seal is broken. Use the artifact.\"",
                };
                comment = s7[Main.rand.Next(s7.Length)];
            }
            else if (signs >= 5)
            {
                string[] s5 = {
                    "\"Almost. I can feel the barrier weakening.\"",
                    "\"So close. Don't stop now.\"",
                    "\"" + (7 - signs) + " more. I'm patient. Are you?\"",
                };
                comment = s5[Main.rand.Next(s5.Length)];
            }
            else if (signs >= 3)
            {
                string[] s3 = {
                    "\"Continue. I'm watching your progress.\"",
                    "\"Every sign brings me closer.\"",
                    "\"You're halfway there. Keep going.\"",
                };
                comment = s3[Main.rand.Next(s3.Length)];
            }
            else
            {
                string[] s1 = {
                    "\"I see you started. Good.\"",
                    "\"The ritual has begun. There's no turning back.\"",
                    "\"I'm counting everything you do.\"",
                };
                comment = s1[Main.rand.Next(s1.Length)];
            }

            if (signs >= 5 && Main.rand.NextBool(4))
                comment = LightCorrupt(comment, signs);

            Main.NewText("  " + comment, new Color(150, 0, 0));
        }

        private string LightCorrupt(string input, int signs)
        {
            char[] chars = input.ToCharArray();
            int corruptions = Math.Min(signs / 2, 3);
            for (int i = 0; i < corruptions; i++)
            {
                int pos = Main.rand.Next(chars.Length);
                if (chars[pos] != '"' && chars[pos] != ' ' && chars[pos] != '.')
                    chars[pos] = (char)Main.rand.Next(33, 90);
            }
            return new string(chars);
        }

        private void PlayEffects(Player player)
        {
            int signs = GodEyeRitualSystem.CountSignsDone();

            SoundEngine.PlaySound(SoundID.Item4 with
            {
                Pitch = -0.3f - signs * 0.08f,
                Volume = 0.6f + signs * 0.05f
            }, player.Center);

            int dustCount = 8 + signs * 3;
            int dustType = signs >= 5 ? DustID.Shadowflame : DustID.PurpleTorch;

            for (int i = 0; i < dustCount; i++)
            {
                Dust d = Dust.NewDustDirect(
                    player.Center + Main.rand.NextVector2Circular(30 + signs * 5, 30 + signs * 5),
                    1, 1, dustType, 0, 0, 100, default, 1f + signs * 0.1f);
                d.noGravity = true;
                d.velocity = (player.Center - d.position).SafeNormalize(Vector2.Zero) * (1f + signs * 0.3f);
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int signs = GodEyeRitualSystem.CountSignsDone();

            foreach (var line in tooltips)
            {
                if (line.Name == "ItemName")
                {
                    string original = line.Text;
                    char[] chars = original.ToCharArray();
                    int tick = (int)(Main.GameUpdateCount);

                    int glitchCount = 1 + (tick % 5 == 0 ? 2 : 0);
                    for (int i = 0; i < glitchCount; i++)
                    {
                        int pos = (tick * 7 + i * 13) % chars.Length;
                        if (chars[pos] != ' ')
                        {
                            if (tick % 3 == 0)
                                chars[pos] = "▓░▒█▀▄"[Main.rand.Next(6)];
                            else if (tick % 5 == 0)
                                chars[pos] = (char)Main.rand.Next(33, 90);
                            else
                                chars[pos] = "̸̷̢̛"[Main.rand.Next(4)];
                        }
                    }
                    line.Text = new string(chars);

                    float pulse = (float)Math.Sin(Main.GameUpdateCount * 0.15f);
                    float pulse2 = (float)Math.Sin(Main.GameUpdateCount * 0.23f);

                    int r, g, b;
                    if (signs >= 7)
                    {
                        r = (int)(200 + pulse * 55);
                        g = (int)(Math.Max(0, pulse2 * 40));
                        b = (int)(Math.Max(0, pulse * 30));
                    }
                    else if (signs >= 4)
                    {
                        r = (int)(150 + pulse * 60);
                        g = (int)(Math.Max(0, pulse2 * 20));
                        b = (int)(180 + pulse2 * 50);
                    }
                    else
                    {
                        r = (int)(120 + pulse * 40);
                        g = (int)(Math.Max(0, 20 + pulse2 * 30));
                        b = (int)(200 + pulse * 40);
                    }

                    if (Main.GameUpdateCount % 37 == 0)
                    {
                        r = Main.rand.Next(180, 256);
                        g = 0;
                        b = Main.rand.Next(0, 100);
                    }

                    line.OverrideColor = new Color(
                        Math.Clamp(r, 0, 255),
                        Math.Clamp(g, 0, 255),
                        Math.Clamp(b, 0, 255));
                    break;
                }
            }

            string desc1Text = "Use to check ritual progress and requirements";
            string desc2Text = "Shows all 7 signs needed to summon The GOD";

            if (Main.GameUpdateCount % 60 < 8)
            {
                char[] c1 = desc1Text.ToCharArray();
                int pos = (int)(Main.GameUpdateCount * 3) % c1.Length;
                if (c1[pos] != ' ') c1[pos] = (char)Main.rand.Next(33, 90);
                desc1Text = new string(c1);
            }
            if (Main.GameUpdateCount % 90 < 5)
            {
                char[] c2 = desc2Text.ToCharArray();
                int pos = (int)(Main.GameUpdateCount * 7) % c2.Length;
                if (c2[pos] != ' ') c2[pos] = "░▒▓█"[Main.rand.Next(4)];
                desc2Text = new string(c2);
            }

            float descPulse = (float)Math.Sin(Main.GameUpdateCount * 0.08f) * 0.15f;
            Color descColor1 = new Color(
                (int)(180 + descPulse * 40),
                (int)(180 + descPulse * 40),
                255);
            Color descColor2 = new Color(
                (int)(150 + descPulse * 30),
                (int)(150 + descPulse * 30),
                220);

            tooltips.Add(new TooltipLine(Mod, "Desc1", desc1Text) { OverrideColor = descColor1 });
            tooltips.Add(new TooltipLine(Mod, "Desc2", desc2Text) { OverrideColor = descColor2 });

            string bar = "[";
            bar += GodEyeRitualSystem.Sign1_MoonLord ? "X" : "-";
            bar += GodEyeRitualSystem.Sign2_Deaths ? "X" : "-";
            bar += GodEyeRitualSystem.Sign3_Time ? "X" : "-";
            bar += GodEyeRitualSystem.Sign4_Broken ? "X" : "-";
            bar += GodEyeRitualSystem.Sign5_Placed ? "X" : "-";
            bar += GodEyeRitualSystem.Sign6_Eclipse ? "X" : "-";
            bar += GodEyeRitualSystem.Sign7_Nights ? "X" : "-";
            bar += "]";

            Color barColor = signs >= 7 ? new Color(0, 200, 0) :
                             signs >= 4 ? new Color(200, 200, 0) :
                             new Color(200, 100, 100);

            string barText = "Signs: " + bar + " " + signs + "/7";
            if (Main.GameUpdateCount % 45 < 3)
            {
                char[] bc = barText.ToCharArray();
                bc[Main.rand.Next(bc.Length)] = (char)Main.rand.Next(48, 58);
                barText = new string(bc);
            }

            tooltips.Add(new TooltipLine(Mod, "Progress", barText) { OverrideColor = barColor });

            if (signs < 7)
            {
                tooltips.Add(new TooltipLine(Mod, "Remaining", "Remaining:")
                { OverrideColor = new Color(200, 150, 50) });

                if (!GodEyeRitualSystem.Sign1_MoonLord)
                    tooltips.Add(new TooltipLine(Mod, "R1", "  - Defeat Moon Lord")
                    { OverrideColor = new Color(150, 150, 150) });
                if (!GodEyeRitualSystem.Sign2_Deaths)
                    tooltips.Add(new TooltipLine(Mod, "R2",
                        "  - Die 10 times (" + GodEyeRitualSystem.DeathCount + "/10)")
                    { OverrideColor = new Color(150, 150, 150) });
                if (!GodEyeRitualSystem.Sign3_Time)
                    tooltips.Add(new TooltipLine(Mod, "R3",
                        "  - Play 10 hours (" + (GodEyeRitualSystem.TimePlayedTicks / 216000.0).ToString("F1") + "h)")
                    { OverrideColor = new Color(150, 150, 150) });
                if (!GodEyeRitualSystem.Sign4_Broken)
                    tooltips.Add(new TooltipLine(Mod, "R4",
                        "  - Break 666 blocks (" + GodEyeRitualSystem.BlocksBroken + "/666)")
                    { OverrideColor = new Color(150, 150, 150) });
                if (!GodEyeRitualSystem.Sign5_Placed)
                    tooltips.Add(new TooltipLine(Mod, "R5",
                        "  - Place 666 blocks (" + GodEyeRitualSystem.BlocksPlaced + "/666)")
                    { OverrideColor = new Color(150, 150, 150) });
                if (!GodEyeRitualSystem.Sign6_Eclipse)
                    tooltips.Add(new TooltipLine(Mod, "R6",
                        "  - Survive 3 eclipses (" + GodEyeRitualSystem.EclipseCount + "/3)")
                    { OverrideColor = new Color(150, 150, 150) });
                if (!GodEyeRitualSystem.Sign7_Nights)
                    tooltips.Add(new TooltipLine(Mod, "R7",
                        "  - 5 consecutive outdoor nights (" + GodEyeRitualSystem.OutdoorNightCount + "/5)")
                    { OverrideColor = new Color(150, 150, 150) });
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "Ready",
                    "All signs complete — Use to get a summon item!")
                { OverrideColor = new Color(255, 50, 50) });
            }

            tooltips.Add(new TooltipLine(Mod, "Drop", "Drops from Moon Lord")
            { OverrideColor = new Color(100, 100, 200) });
        }

        public override bool PreDrawInInventory(SpriteBatch sb, Vector2 position, Rectangle frame,
            Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D tex = TextureAssets.Item[Item.type].Value;
            int signs = GodEyeRitualSystem.CountSignsDone();

            float pulse = (float)Math.Sin(Main.GameUpdateCount * 0.06f) * 0.12f;
            float pulse2 = (float)Math.Sin(Main.GameUpdateCount * 0.1f) * 0.08f;

            float glowScale = scale * (1.3f + pulse);
            Color glowColor;
            if (signs >= 7)
                glowColor = new Color(255, 0, 0) * (0.2f + pulse2);
            else if (signs >= 4)
                glowColor = new Color(180, 0, 200) * (0.15f + pulse2);
            else
                glowColor = new Color(100, 0, 180) * (0.1f + pulse2);

            sb.Draw(tex, position, frame, glowColor, 0f, origin, glowScale, SpriteEffects.None, 0f);

            if (signs >= 3)
            {
                float splitAmount = 1.5f + signs * 0.3f;
                if (Main.GameUpdateCount % 30 < 3)
                    splitAmount *= 3f;

                Vector2 redOff = new Vector2(-splitAmount + (float)Math.Sin(Main.GameUpdateCount * 0.2f) * 0.5f, 0);
                Vector2 blueOff = new Vector2(splitAmount + (float)Math.Cos(Main.GameUpdateCount * 0.2f) * 0.5f, 0);

                sb.Draw(tex, position + redOff, frame, Color.Red * 0.25f, 0f, origin, scale, SpriteEffects.None, 0f);
                sb.Draw(tex, position + blueOff, frame, Color.Blue * 0.25f, 0f, origin, scale, SpriteEffects.None, 0f);
            }

            Vector2 shake = Vector2.Zero;
            if (signs >= 5 && Main.GameUpdateCount % 20 < 4)
                shake = new Vector2(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2));

            Color mainColor = drawColor;
            if (Main.GameUpdateCount % 50 < 2)
                mainColor = Color.White;

            sb.Draw(tex, position + shake, frame, mainColor, 0f, origin, scale + pulse * 0.3f, SpriteEffects.None, 0f);

            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch sb, Color lightColor, Color alphaColor,
            ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D tex = TextureAssets.Item[Item.type].Value;
            Vector2 pos = Item.Center - Main.screenPosition;
            Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
            int signs = GodEyeRitualSystem.CountSignsDone();

            float pulse = (float)Math.Sin(Main.GameUpdateCount * 0.06f);
            float floatY = (float)Math.Sin(Main.GameUpdateCount * 0.03f) * 4f;
            pos.Y += floatY;

            Color glowColor = signs >= 7 ? Color.Red : signs >= 4 ? Color.Purple : new Color(100, 0, 180);
            float glowAlpha = 0.15f + pulse * 0.1f;

            for (int i = 0; i < 3; i++)
            {
                float glowScale = scale * (1.2f + i * 0.15f + pulse * 0.05f);
                sb.Draw(tex, pos, null, glowColor * (glowAlpha - i * 0.03f),
                    rotation, origin, glowScale, SpriteEffects.None, 0f);
            }

            if (signs >= 3)
            {
                float split = 2f;
                if (Main.GameUpdateCount % 25 < 3) split = 5f;

                sb.Draw(tex, pos + new Vector2(-split, 0), null, Color.Red * 0.3f,
                    rotation, origin, scale, SpriteEffects.None, 0f);
                sb.Draw(tex, pos + new Vector2(split, 0), null, Color.Blue * 0.3f,
                    rotation, origin, scale, SpriteEffects.None, 0f);
            }

            Vector2 shake = Vector2.Zero;
            if (signs >= 5 && Main.GameUpdateCount % 15 < 3)
                shake = new Vector2(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2));

            Color mainColor = lightColor;
            if (Main.GameUpdateCount % 40 < 2) mainColor = Color.White;

            sb.Draw(tex, pos + shake, null, mainColor, rotation, origin, scale, SpriteEffects.None, 0f);

            if (Main.rand.NextBool(8))
            {
                int dustType = signs >= 5 ? DustID.Shadowflame : DustID.PurpleTorch;
                Dust d = Dust.NewDustDirect(Item.Center + Main.rand.NextVector2Circular(15, 15),
                    1, 1, dustType, 0, 0, 100, default, 1f);
                d.noGravity = true;
                d.velocity = (Item.Center - d.position).SafeNormalize(Vector2.Zero) * 0.5f;
            }

            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.White;
        }

        private string GetBiome(Player p)
        {
            if (p.ZoneCorrupt) return "Corruption";
            if (p.ZoneCrimson) return "Crimson";
            if (p.ZoneHallow) return "Hallow";
            if (p.ZoneJungle) return "Jungle";
            if (p.ZoneSnow) return "Snow";
            if (p.ZoneDesert) return "Desert";
            if (p.ZoneDungeon) return "Dungeon";
            if (p.ZoneUnderworldHeight) return "Underworld";
            if (p.ZoneBeach) return "Ocean";
            if (p.ZoneGlowshroom) return "Mushroom";
            if (p.ZoneSkyHeight) return "Space";
            if (p.ZoneOverworldHeight) return "Surface";
            if (p.ZoneDirtLayerHeight) return "Underground";
            if (p.ZoneRockLayerHeight) return "Caverns";
            return "Unknown";
        }

        private string GetWeather()
        {
            if (Main.eclipse) return "Solar Eclipse";
            if (Main.bloodMoon) return "Blood Moon";
            if (Main.pumpkinMoon) return "Pumpkin Moon";
            if (Main.snowMoon) return "Frost Moon";
            if (Main.raining) return "Rain";
            if (Main.cloudAlpha > 0.3f) return "Cloudy";
            return "Clear";
        }

        private int CountBossesDefeated()
        {
            int c = 0;
            if (NPC.downedSlimeKing) c++;
            if (NPC.downedBoss1) c++;
            if (NPC.downedBoss2) c++;
            if (NPC.downedQueenBee) c++;
            if (NPC.downedBoss3) c++;
            if (NPC.downedDeerclops) c++;
            if (Main.hardMode) c++;
            if (NPC.downedQueenSlime) c++;
            if (NPC.downedMechBoss1) c++;
            if (NPC.downedMechBoss2) c++;
            if (NPC.downedMechBoss3) c++;
            if (NPC.downedPlantBoss) c++;
            if (NPC.downedGolemBoss) c++;
            if (NPC.downedEmpressOfLight) c++;
            if (NPC.downedFishron) c++;
            if (NPC.downedAncientCultist) c++;
            if (NPC.downedMoonlord) c++;
            return c;
        }

        private int CountTownNPCs()
        {
            int c = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
                if (Main.npc[i].active && Main.npc[i].townNPC) c++;
            return c;
        }
    }

    public class GodEyeMoonLordDrop : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (npc.type == NPCID.MoonLordCore)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<GodEyeStatisticsOrb>(), 1));
            }
        }
    }
}