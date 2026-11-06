using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using GodEyeMod.Content.NPCs.GodEye.summon;

namespace GodEyeMod.Content.NPCs.GodEye
{
    public class GodEyeRitualSystem : ModSystem
    {
        public static int DeathCount = 0;
        public static int BlocksBroken = 0;
        public static int BlocksPlaced = 0;
        public static int EclipseCount = 0;
        public static int OutdoorNightCount = 0;
        public static double TimePlayedTicks = 0;

        public static bool Sign1_MoonLord = false;
        public static bool Sign2_Deaths = false;
        public static bool Sign3_Time = false;
        public static bool Sign4_Broken = false;
        public static bool Sign5_Placed = false;
        public static bool Sign6_Eclipse = false;
        public static bool Sign7_Nights = false;

        public static bool ItemGiven = false;
        public static bool BossEverDefeated = false;

        private static bool wasOutdoorThisNight = false;
        private static bool wasNight = false;
        private static int consecutiveOutdoorNights = 0;
        private static bool eclipseCountedToday = false;

        private static bool pendingItemGive = false;
        private static int pendingItemTimer = 0;
        private static int signCompletedFlashTimer = 0;
        private static int lastCompletedSign = 0;

        private static float signGlitchIntensity = 0f;
        private static float eyePulse = 0f;
        private static int ambientMessageTimer = 0;

        private static bool itemAppearAnimation = false;
        private static int itemAppearTimer = 0;

        public override void PostUpdateEverything()
        {
            if (Main.netMode == NetmodeID.Server) return;

            Player player = Main.LocalPlayer;
            if (!player.active || player.dead) return;

            TimePlayedTicks += 1;

            CheckSigns(player);
            UpdateVisuals(player);
            TrackOutdoorNights(player);
            TrackEclipses();

            if (pendingItemGive)
            {
                pendingItemTimer--;
                if (pendingItemTimer <= 0)
                {
                    pendingItemGive = false;
                    GiveItemToPlayer(player);
                }
            }

            if (itemAppearAnimation)
            {
                itemAppearTimer--;
                if (itemAppearTimer <= 0)
                    itemAppearAnimation = false;
            }
        }

        private void CheckSigns(Player player)
        {
            if (ItemGiven) return;

            if (!Sign1_MoonLord && NPC.downedMoonlord)
            {
                Sign1_MoonLord = true;
                OnSignCompleted(1, player);
            }

            if (!Sign2_Deaths && DeathCount >= 10)
            {
                Sign2_Deaths = true;
                OnSignCompleted(2, player);
            }

            if (!Sign3_Time && TimePlayedTicks >= 2160000)
            {
                Sign3_Time = true;
                OnSignCompleted(3, player);
            }

            if (!Sign4_Broken && BlocksBroken >= 666)
            {
                Sign4_Broken = true;
                OnSignCompleted(4, player);
            }

            if (!Sign5_Placed && BlocksPlaced >= 666)
            {
                Sign5_Placed = true;
                OnSignCompleted(5, player);
            }

            if (!Sign6_Eclipse && EclipseCount >= 3)
            {
                Sign6_Eclipse = true;
                OnSignCompleted(6, player);
            }

            if (!Sign7_Nights && OutdoorNightCount >= 5)
            {
                Sign7_Nights = true;
                OnSignCompleted(7, player);
            }

            if (AllSignsDone() && !ItemGiven && !pendingItemGive)
            {
                if (!Main.dayTime && Main.time >= 24000 && Main.time <= 25200)
                {
                    pendingItemGive = true;
                    pendingItemTimer = 300;
                    StartItemAppearSequence(player);
                }
            }
        }

        private void TrackOutdoorNights(Player player)
        {
            bool currentlyNight = !Main.dayTime;

            if (currentlyNight && !wasNight)
            {
                wasNight = true;
                wasOutdoorThisNight = true;
            }

            if (!currentlyNight && wasNight)
            {
                wasNight = false;

                if (wasOutdoorThisNight)
                {
                    consecutiveOutdoorNights++;
                    OutdoorNightCount = consecutiveOutdoorNights;
                }
                else
                {
                    consecutiveOutdoorNights = 0;
                }

                wasOutdoorThisNight = true;
                eclipseCountedToday = false;
            }

            if (currentlyNight && wasOutdoorThisNight)
            {
                int tileX = (int)(player.Center.X / 16);
                int tileY = (int)(player.Center.Y / 16);
                bool hasRoof = false;

                for (int y = tileY - 1; y >= Math.Max(0, tileY - 40); y--)
                {
                    Tile tile = Main.tile[tileX, y];
                    if (tile.HasTile && Main.tileSolid[tile.TileType])
                    {
                        hasRoof = true;
                        break;
                    }
                }

                bool isOverworld = player.ZoneOverworldHeight;

                if (hasRoof || !isOverworld)
                {
                    wasOutdoorThisNight = false;
                    consecutiveOutdoorNights = 0;
                }
            }
        }

        private void TrackEclipses()
        {
            if (Main.eclipse && Main.dayTime && !eclipseCountedToday)
            {
                eclipseCountedToday = true;
                EclipseCount++;
            }
        }

        private void OnSignCompleted(int sign, Player player)
        {
            signCompletedFlashTimer = 180;
            lastCompletedSign = sign;
            signGlitchIntensity = 1f;

            string[] signNames = new string[]
            {
                "",
                "THE END HAS BEEN WITNESSED",
                "YOUR DEATHS HAVE BEEN COUNTED",
                "YOUR TIME HAS BEEN RECORDED",
                "EVERY BLOCK BROKEN — REMEMBERED",
                "EVERY BLOCK PLACED — REMEMBERED",
                "THREE ECLIPSES. THREE WARNINGS",
                "FIVE NIGHTS ALONE. WATCHED",
            };

            string[] signIcons = { "", "💀", "☠", "⏳", "⛏", "🧱", "🌑", "🌙" };

            int done = CountSignsDone();

            SoundEngine.PlaySound(SoundID.Item4 with { Pitch = -0.6f, Volume = 1.2f }, player.Center);

            Main.NewText("═══════════════════════════", new Color(100, 0, 0));
            Main.NewText($"  {signIcons[sign]} SIGN {sign}/7: {signNames[sign]}", new Color(200, 0, 0));
            Main.NewText($"  [{done}/7] signs completed", new Color(150, 0, 0));
            Main.NewText("═══════════════════════════", new Color(100, 0, 0));

            if (done < 7)
            {
                string[] waitMsgs = {
                    "I am almost ready.", "Continue.", "Not enough.",
                    "More.", "I'm still watching.", "Keep going."
                };
                Main.NewText(waitMsgs[Main.rand.Next(waitMsgs.Length)], new Color(80, 0, 0));
            }
            else
            {
                Main.NewText("T̸̛H̸̛E̸̛ ̸̛L̸̛Ơ̸G̸̛ ̸̛I̸̛S̸̛ ̸̛C̸̛Ơ̸M̸̛P̸̛L̸̛E̸̛T̸̛E̸̛.", new Color(255, 0, 0));
                Main.NewText("Wait for nightfall.", new Color(200, 0, 0));
            }

            for (int i = 0; i < 15; i++)
            {
                Dust d = Dust.NewDustDirect(player.Center + Main.rand.NextVector2Circular(50, 50),
                    1, 1, DustID.Shadowflame, 0, 0, 150, default, 2f);
                d.noGravity = true;
                d.velocity = Main.rand.NextVector2Circular(4, 4);
            }
        }

        private void StartItemAppearSequence(Player player)
        {
            itemAppearAnimation = true;
            itemAppearTimer = 300;

            SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.5f, Pitch = -1f }, player.Center);

            for (int i = 0; i < 30; i++)
            {
                Dust d = Dust.NewDustDirect(player.Center + Main.rand.NextVector2Circular(100, 100),
                    1, 1, DustID.Shadowflame, 0, 0, 200, default, 2.5f);
                d.noGravity = true;
                d.velocity = (player.Center - d.position).SafeNormalize(Vector2.Zero) * 3f;
            }
        }

        private void GiveItemToPlayer(Player player)
        {
            ItemGiven = true;

            Item.NewItem(
                player.GetSource_GiftOrReward(),
                player.Hitbox,
                ModContent.ItemType<GodEyeSummonItem>(),
                1);

            SoundEngine.PlaySound(SoundID.Item4 with { Pitch = -0.8f, Volume = 2f }, player.Center);
            SoundEngine.PlaySound(SoundID.Roar with { Volume = 1f, Pitch = -0.5f }, player.Center);

            for (int i = 0; i < 40; i++)
            {
                float angle = MathHelper.TwoPi / 40f * i;
                Dust d = Dust.NewDustDirect(player.Center, 1, 1,
                    DustID.Shadowflame, 0, 0, 100, default, 2.5f);
                d.noGravity = true;
                d.velocity = angle.ToRotationVector2() * 8f;
            }
            for (int i = 0; i < 20; i++)
            {
                Dust d2 = Dust.NewDustDirect(player.Center + Main.rand.NextVector2Circular(40, 40),
                    1, 1, DustID.Vortex, 0, 0, 150, default, 2f);
                d2.noGravity = true;
                d2.velocity = Main.rand.NextVector2Circular(6, 6);
            }
        }

       private void UpdateVisuals(Player player)
{
    signGlitchIntensity *= 0.98f;
    if (signCompletedFlashTimer > 0)
        signCompletedFlashTimer--;

    eyePulse += 0.05f;
    if (eyePulse > MathHelper.TwoPi) eyePulse -= MathHelper.TwoPi;
}

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            bool needOverlay = signCompletedFlashTimer > 0 || itemAppearAnimation;
            if (!needOverlay) return;

            layers.Add(new LegacyGameInterfaceLayer(
                "GodEyeMod: Ritual Overlay",
                delegate
                {
                    DrawRitualOverlay(Main.spriteBatch);
                    return true;
                },
                InterfaceScaleType.None
            ));
        }

        private void DrawRitualOverlay(SpriteBatch sb)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            if (signCompletedFlashTimer > 0)
            {
                float alpha = signCompletedFlashTimer / 180f;

                float flashPulse = (float)Math.Sin(signCompletedFlashTimer * 0.2f) * 0.3f + 0.3f;
                sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                    Color.DarkRed * alpha * flashPulse * 0.4f);

                for (int i = 0; i < 6; i++)
                {
                    float ratio = (float)i / 6;
                    int margin = (int)(Math.Min(Main.screenWidth, Main.screenHeight) * 0.4f * (1f - ratio));
                    float a = alpha * ratio * 0.2f;
                    sb.Draw(pixel, new Rectangle(0, 0, margin, Main.screenHeight), Color.Black * a);
                    sb.Draw(pixel, new Rectangle(Main.screenWidth - margin, 0, margin, Main.screenHeight), Color.Black * a);
                    sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, margin), Color.Black * a);
                    sb.Draw(pixel, new Rectangle(0, Main.screenHeight - margin, Main.screenWidth, margin), Color.Black * a);
                }

                if (Main.rand.NextBool(3))
                {
                    int gy = Main.rand.Next(Main.screenHeight);
                    int gh = Main.rand.Next(2, 15);
                    sb.Draw(pixel, new Rectangle(0, gy, Main.screenWidth, gh),
                        Color.Red * alpha * 0.2f);
                }

                if (alpha > 0.3f)
                {
                    float edgeSize = 30f * alpha;
                    sb.Draw(pixel, new Rectangle(0, 0, (int)edgeSize, Main.screenHeight),
                        Color.Red * 0.1f * alpha);
                    sb.Draw(pixel, new Rectangle(Main.screenWidth - (int)edgeSize, 0, (int)edgeSize, Main.screenHeight),
                        Color.Blue * 0.1f * alpha);
                }

                if (signCompletedFlashTimer > 60)
                {
                    float textAlpha = (signCompletedFlashTimer - 60) / 120f;
                    string signText = $"SIGN {lastCompletedSign}/7";

                    if (Main.rand.NextBool(6))
                    {
                        char[] c = signText.ToCharArray();
                        c[Main.rand.Next(c.Length)] = (char)Main.rand.Next(33, 126);
                        signText = new string(c);
                    }

                    Vector2 size = FontAssets.DeathText.Value.MeasureString(signText);
                    Vector2 pos = new Vector2(Main.screenWidth / 2f - size.X / 2f,
                        Main.screenHeight / 2f - size.Y / 2f);

                    if (textAlpha > 0.3f)
                    {
                        float off = 3f * textAlpha;
                        Utils.DrawBorderString(sb, signText, pos + new Vector2(-off, 0),
                            Color.Red * textAlpha * 0.4f, 1f);
                        Utils.DrawBorderString(sb, signText, pos + new Vector2(off, 0),
                            Color.Blue * textAlpha * 0.4f, 1f);
                    }

                    Vector2 shake = Vector2.Zero;
                    if (Main.rand.NextBool(4))
                        shake = Main.rand.NextVector2Circular(4, 4);

                    Utils.DrawBorderString(sb, signText, pos + shake,
                        Color.Red * textAlpha, 1f);

                    int done = CountSignsDone();
                    int barW = 300;
                    int barH = 6;
                    int barX = Main.screenWidth / 2 - barW / 2;
                    int barY = (int)(pos.Y + size.Y + 20);

                    sb.Draw(pixel, new Rectangle(barX, barY, barW, barH), Color.Black * 0.6f * textAlpha);

                    int fillW = (int)(barW * (done / 7f));
                    Color barColor = done >= 7 ? Color.Red : Color.DarkRed;
                    sb.Draw(pixel, new Rectangle(barX, barY, fillW, barH), barColor * textAlpha);

                    sb.Draw(pixel, new Rectangle(barX, barY, barW, 1), Color.Red * 0.3f * textAlpha);
                    sb.Draw(pixel, new Rectangle(barX, barY + barH - 1, barW, 1), Color.Red * 0.3f * textAlpha);
                }
            }

            if (itemAppearAnimation)
            {
                float progress = 1f - (itemAppearTimer / 300f);

                float darkAlpha;
                if (progress < 0.4f)
                    darkAlpha = progress / 0.4f;
                else if (progress < 0.7f)
                    darkAlpha = 1f;
                else
                    darkAlpha = (1f - progress) / 0.3f;

                sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                    Color.Black * darkAlpha * 0.9f);

                if (progress > 0.2f && progress < 0.8f)
                {
                    float eyeAlpha = 1f;
                    if (progress < 0.3f) eyeAlpha = (progress - 0.2f) / 0.1f;
                    if (progress > 0.7f) eyeAlpha = (0.8f - progress) / 0.1f;

                    Texture2D eyeTex;
                    try
                    {
                        eyeTex = ModContent.Request<Texture2D>("GodEyeMod/Content/NPCs/GodEye/ScreenEye",
                            ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                    }
                    catch
                    {
                        eyeTex = TextureAssets.Npc[NPCID.DemonEye].Value;
                    }

                    Vector2 eyePos = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
                    Vector2 eyeOrigin = new Vector2(eyeTex.Width / 2f, eyeTex.Height / 2f);
                    float eyeScale = 2f + (float)Math.Sin(progress * MathHelper.TwoPi) * 0.3f;
                    float eyeRot = (float)Math.Sin(progress * 5f) * 0.1f;

                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 gOff = Main.rand.NextVector2Circular(3, 3);
                        sb.Draw(eyeTex, eyePos + gOff, null,
                            Color.Red * eyeAlpha * 0.3f,
                            eyeRot, eyeOrigin, eyeScale * 1.3f,
                            SpriteEffects.None, 0f);
                    }

                    float rgbOff = 4f;
                    sb.Draw(eyeTex, eyePos + new Vector2(-rgbOff, 0), null,
                        Color.Red * eyeAlpha * 0.3f,
                        eyeRot, eyeOrigin, eyeScale, SpriteEffects.None, 0f);
                    sb.Draw(eyeTex, eyePos + new Vector2(rgbOff, 0), null,
                        Color.Blue * eyeAlpha * 0.3f,
                        eyeRot, eyeOrigin, eyeScale, SpriteEffects.None, 0f);

                    sb.Draw(eyeTex, eyePos, null,
                        Color.White * eyeAlpha,
                        eyeRot, eyeOrigin, eyeScale, SpriteEffects.None, 0f);
                }

                if (progress > 0.5f && progress < 0.9f)
                {
                    float textAlpha = 1f;
                    if (progress < 0.6f) textAlpha = (progress - 0.5f) / 0.1f;
                    if (progress > 0.8f) textAlpha = (0.9f - progress) / 0.1f;

                    string text = "IT HAS FOUND YOU.";
                    if (Main.rand.NextBool(8))
                    {
                        char[] c = text.ToCharArray();
                        c[Main.rand.Next(c.Length)] = (char)Main.rand.Next(33, 126);
                        text = new string(c);
                    }

                    Vector2 tSize = FontAssets.DeathText.Value.MeasureString(text);
                    Vector2 tPos = new Vector2(
                        Main.screenWidth / 2f - tSize.X * 0.7f / 2f,
                        Main.screenHeight / 2f + 80);

                    if (Main.rand.NextBool(5))
                        tPos += Main.rand.NextVector2Circular(3, 3);

                    Utils.DrawBorderString(sb, text, tPos, Color.Red * textAlpha, 0.7f);
                }

                if (progress > 0.1f)
                {
                    for (int i = 0; i < (int)(progress * 80); i++)
                    {
                        int x = Main.rand.Next(Main.screenWidth);
                        int y = Main.rand.Next(Main.screenHeight);
                        int w = Main.rand.Next(1, 8);
                        int h = Main.rand.Next(1, 2);
                        sb.Draw(pixel, new Rectangle(x, y, w, h),
                            Color.White * Main.rand.NextFloat(0.02f, 0.08f));
                    }
                }

                int scanOff = (int)(Main.GameUpdateCount * 2) % 4;
                for (int y = scanOff; y < Main.screenHeight; y += 4)
                {
                    sb.Draw(pixel, new Rectangle(0, y, Main.screenWidth, 1),
                        Color.Black * 0.05f * darkAlpha);
                }
            }
        }

        public override void ModifyScreenPosition()
        {
            if (signCompletedFlashTimer > 0)
            {
                float shake = signCompletedFlashTimer / 180f * 8f;
                Main.screenPosition += Main.rand.NextVector2Circular(shake, shake);
            }

            if (itemAppearAnimation)
            {
                float progress = 1f - (itemAppearTimer / 300f);
                if (progress > 0.3f && progress < 0.6f)
                {
                    float shake = 12f;
                    Main.screenPosition += Main.rand.NextVector2Circular(shake, shake);
                }
            }
        }

        public static void OnPlayerDied() { DeathCount++; }
        public static void OnBlockBroken() { BlocksBroken++; }
        public static void OnBlockPlaced() { BlocksPlaced++; }

        public static int CountSignsDone()
        {
            int c = 0;
            if (Sign1_MoonLord) c++;
            if (Sign2_Deaths) c++;
            if (Sign3_Time) c++;
            if (Sign4_Broken) c++;
            if (Sign5_Placed) c++;
            if (Sign6_Eclipse) c++;
            if (Sign7_Nights) c++;
            return c;
        }

        public static bool AllSignsDone()
        {
            return Sign1_MoonLord && Sign2_Deaths && Sign3_Time &&
                   Sign4_Broken && Sign5_Placed && Sign6_Eclipse && Sign7_Nights;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["RitualDeathCount"] = DeathCount;
            tag["RitualBlocksBroken"] = BlocksBroken;
            tag["RitualBlocksPlaced"] = BlocksPlaced;
            tag["RitualEclipseCount"] = EclipseCount;
            tag["RitualOutdoorNightCount"] = OutdoorNightCount;
            tag["RitualTimePlayed"] = TimePlayedTicks;
            tag["RitualSign1"] = Sign1_MoonLord;
            tag["RitualSign2"] = Sign2_Deaths;
            tag["RitualSign3"] = Sign3_Time;
            tag["RitualSign4"] = Sign4_Broken;
            tag["RitualSign5"] = Sign5_Placed;
            tag["RitualSign6"] = Sign6_Eclipse;
            tag["RitualSign7"] = Sign7_Nights;
            tag["RitualItemGiven"] = ItemGiven;
            tag["RitualBossDefeated"] = BossEverDefeated;
            tag["RitualConsecutiveNights"] = consecutiveOutdoorNights;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            DeathCount = tag.GetInt("RitualDeathCount");
            BlocksBroken = tag.GetInt("RitualBlocksBroken");
            BlocksPlaced = tag.GetInt("RitualBlocksPlaced");
            EclipseCount = tag.GetInt("RitualEclipseCount");
            OutdoorNightCount = tag.GetInt("RitualOutdoorNightCount");
            TimePlayedTicks = tag.Get<double>("RitualTimePlayed");
            Sign1_MoonLord = tag.GetBool("RitualSign1");
            Sign2_Deaths = tag.GetBool("RitualSign2");
            Sign3_Time = tag.GetBool("RitualSign3");
            Sign4_Broken = tag.GetBool("RitualSign4");
            Sign5_Placed = tag.GetBool("RitualSign5");
            Sign6_Eclipse = tag.GetBool("RitualSign6");
            Sign7_Nights = tag.GetBool("RitualSign7");
            ItemGiven = tag.GetBool("RitualItemGiven");
            BossEverDefeated = tag.GetBool("RitualBossDefeated");
            consecutiveOutdoorNights = tag.GetInt("RitualConsecutiveNights");
        }

        public override void OnWorldLoad()
        {
            pendingItemGive = false;
            pendingItemTimer = 0;
            signCompletedFlashTimer = 0;
            signGlitchIntensity = 0f;
            itemAppearAnimation = false;
            itemAppearTimer = 0;
            ambientMessageTimer = 0;
            wasNight = false;
            wasOutdoorThisNight = false;
            eclipseCountedToday = false;
        }
    }

    public class GodEyeRitualBlockTracker : GlobalTile
    {
        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (!fail && !effectOnly)
                GodEyeRitualSystem.OnBlockBroken();
        }

        public override void PlaceInWorld(int i, int j, int type, Item item)
        {
            GodEyeRitualSystem.OnBlockPlaced();
        }
    }
}