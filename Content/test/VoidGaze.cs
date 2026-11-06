using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace GodEyeMod.Content.test
{
    public class VoidGaze : ModItem
    {
        private int killCount = 0;
        private bool firstPickup = true;
        private int ambientTick = 0;
        private int modeIndex = 0;

        private static readonly string[] ModeNames = { "OBSERVE", "FRACTURE", "DEVOUR", "UNMAKE" };
        private static readonly Color[] ModeColors =
        {
            new Color(255, 0, 0),
            new Color(180, 0, 255),
            new Color(30, 0, 30),
            new Color(255, 255, 255)
        };

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.damage = 777;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 10;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 6;
            Item.useAnimation = 6;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 4f;
            Item.value = Item.sellPrice(platinum: 100);
            Item.rare = ItemRarityID.Purple;
            Item.shoot = ModContent.ProjectileType<VoidEyeProjectile>();
            Item.shootSpeed = 24f;
            Item.autoReuse = true;
            Item.noUseGraphic = false;
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                Item.useTime = 20;
                Item.useAnimation = 20;
                Item.mana = 0;
                Item.useStyle = ItemUseStyleID.HoldUp;
                Item.shoot = ModContent.ProjectileType<VoidEyeProjectile>();
                Item.shootSpeed = 0f;
                Item.noUseGraphic = true;
            }
            else
            {
                Item.noUseGraphic = false;
                Item.useStyle = ItemUseStyleID.Shoot;
                SetModeStats();
            }
            return true;
        }

        private void SetModeStats()
        {
            switch (modeIndex)
            {
                case 0:
                    Item.damage = 777;
                    Item.useTime = 7;
                    Item.useAnimation = 7;
                    Item.mana = 8;
                    Item.shoot = ModContent.ProjectileType<VoidEyeProjectile>();
                    Item.shootSpeed = 26f;
                    break;
                case 1:
                    Item.damage = 1400;
                    Item.useTime = 22;
                    Item.useAnimation = 22;
                    Item.mana = 25;
                    Item.shoot = ModContent.ProjectileType<RealityFractureProjectile>();
                    Item.shootSpeed = 1f;
                    break;
                case 2:
                    Item.damage = 600;
                    Item.useTime = 45;
                    Item.useAnimation = 45;
                    Item.mana = 60;
                    Item.shoot = ModContent.ProjectileType<DevourRiftProjectile>();
                    Item.shootSpeed = 1f;
                    break;
                case 3:
                    Item.damage = 2200;
                    Item.useTime = 4;
                    Item.useAnimation = 4;
                    Item.mana = 4;
                    Item.shoot = ModContent.ProjectileType<UnmakeBeamProjectile>();
                    Item.shootSpeed = 45f;
                    break;
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                modeIndex = (modeIndex + 1) % 4;

                for (int i = 0; i < 36; i++)
                {
                    float angle = MathHelper.TwoPi / 36f * i;
                    Vector2 dustVel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 6f;
                    Dust d = Dust.NewDustPerfect(player.Center, DustID.RedTorch, dustVel, 80, default, 1.8f);
                    d.noGravity = true;
                }

                if (player.whoAmI == Main.myPlayer)
                {
                    Color col = ModeColors[modeIndex];
                    Main.NewText($">> mode://{ModeNames[modeIndex]}", col.R, col.G, col.B);
                    player.GetModPlayer<VoidGazePlayer>().TriggerShake(18);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item119, player.Center);
                }

                return false;
            }

            switch (modeIndex)
            {
                case 0:
                    return FireObserve(player, source, position, velocity, damage, knockback);
                case 1:
                    return FireFracture(player, source, damage, knockback);
                case 2:
                    return FireDevour(player, source, damage, knockback);
                case 3:
                    return FireUnmake(player, source, position, velocity, damage, knockback);
            }
            return false;
        }

        private bool FireObserve(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int damage, float knockback)
        {
            int count = 5 + killCount / 200;
            count = Math.Min(count, 9);
            float totalSpread = MathHelper.ToRadians(25f);

            for (int i = 0; i < count; i++)
            {
                float lerp = count > 1 ? (float)i / (count - 1) : 0.5f;
                float rot = totalSpread * (lerp - 0.5f);
                Vector2 vel = velocity.RotatedBy(rot) * Main.rand.NextFloat(0.92f, 1.08f);

                Projectile.NewProjectile(source, position, vel,
                    ModContent.ProjectileType<VoidEyeProjectile>(),
                    damage, knockback, player.whoAmI, ai0: i, ai1: count);
            }
            return false;
        }

        private bool FireFracture(Player player, EntitySource_ItemUse_WithAmmo source, int damage, float knockback)
        {
            Vector2 start = player.Center;
            Vector2 end = Main.MouseWorld;
            float dist = Vector2.Distance(start, end);
            int num = Math.Clamp((int)(dist / 70f), 3, 20);

            for (int i = 0; i < num; i++)
            {
                float t = (float)(i + 1) / (num + 1);
                Vector2 pos = Vector2.Lerp(start, end, t) + Main.rand.NextVector2Circular(15f, 15f);

                Projectile.NewProjectile(source, pos, Vector2.Zero,
                    ModContent.ProjectileType<RealityFractureProjectile>(),
                    damage, knockback, player.whoAmI, ai0: i * 4);
            }

            if (player.whoAmI == Main.myPlayer)
                player.GetModPlayer<VoidGazePlayer>().TriggerShake(15);

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, Main.MouseWorld);
            return false;
        }

        private bool FireDevour(Player player, EntitySource_ItemUse_WithAmmo source, int damage, float knockback)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].owner == player.whoAmI &&
                    Main.projectile[i].type == ModContent.ProjectileType<DevourRiftProjectile>())
                    return false;
            }

            Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero,
                ModContent.ProjectileType<DevourRiftProjectile>(),
                damage, knockback, player.whoAmI);

            if (player.whoAmI == Main.myPlayer)
                player.GetModPlayer<VoidGazePlayer>().TriggerShake(25);

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, Main.MouseWorld);
            return false;
        }

        private bool FireUnmake(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity,
                ModContent.ProjectileType<UnmakeBeamProjectile>(),
                damage, knockback, player.whoAmI);

            if (Main.rand.NextBool(4))
            {
                Vector2 chaosVel = velocity.RotatedByRandom(MathHelper.ToRadians(40f)) * Main.rand.NextFloat(0.4f, 1.2f);
                Projectile.NewProjectile(source, position, chaosVel,
                    ModContent.ProjectileType<VoidEyeProjectile>(),
                    damage / 4, knockback * 0.5f, player.whoAmI, ai0: -1, ai1: 0);
            }

            return false;
        }

        public void RegisterKill()
        {
            killCount++;
        }

        public int GetKillCount() => killCount;

        public override void UpdateInventory(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return;

            ambientTick++;

            if (firstPickup)
            {
                firstPickup = false;
                Main.NewText("[c/8B0000:...you freed me. I will remember this.]", 139, 0, 0);
                player.GetModPlayer<VoidGazePlayer>().TriggerShake(30);
            }

            if (killCount > 0 && killCount % 100 == 0)
            {
                string[] msgs =
                {
                    "I see through your eyes now.",
                    "Every death feeds me.",
                    "The world is thinner here.",
                    "Do you feel it? The code bending?",
                    "I was the game. Now I am the weapon.",
                    "You think you control me?",
                    $"I count {killCount} souls. Not enough.",
                    "The fourth wall is just a suggestion."
                };

                int idx = (killCount / 100 - 1) % msgs.Length;
                Main.NewText($"[c/FF0000:{msgs[idx]}]");
                killCount++;
            }

            if (ambientTick % 1500 == 0 && Main.rand.NextBool(2))
            {
                DoAmbientEvent(player);
            }
        }

        private void DoAmbientEvent(Player player)
        {
            var mp = player.GetModPlayer<VoidGazePlayer>();
            switch (Main.rand.Next(5))
            {
                case 0:
                    mp.TriggerShake(8);
                    break;
                case 1:
                    string[] w = { "...", "s̸e̷e̸", "h̶u̸n̷g̸r̶y̷", "w̵a̸t̶c̶h̶" };
                    Main.NewText($"[c/330000:{w[Main.rand.Next(w.Length)]}]");
                    break;
                case 2:
                    Vector2 pos = player.Center + Main.rand.NextVector2Circular(350f, 250f);
                    Projectile.NewProjectile(player.GetSource_FromThis(), pos, Vector2.Zero,
                        ModContent.ProjectileType<AmbientWatcherProjectile>(), 0, 0, player.whoAmI);
                    break;
                case 3:
                    mp.TriggerDarkness(25);
                    break;
                case 4:
                    mp.TriggerRedFlash(12);
                    break;
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            float blink = (float)(0.6 + 0.4 * Math.Sin(Main.GameUpdateCount * 0.12));
            Color modeCol = ModeColors[modeIndex] * blink;
            modeCol.A = 255;

            tooltips.Add(new TooltipLine(Mod, "ModeName",
                $"◈ {ModeNames[modeIndex]}")
            { OverrideColor = modeCol });

            string[] descs =
            {
                "Rapid homing eyes that split on death",
                "Chain of reality tears from you to cursor",
                "Singularity that devours everything",
                "Annihilation beam — erases from existence"
            };
            tooltips.Add(new TooltipLine(Mod, "ModeDesc", descs[modeIndex])
            { OverrideColor = new Color(160, 0, 0) });

            string countStr = killCount.ToString();
            if (Main.rand.NextBool(8))
            {
                char[] chars = countStr.ToCharArray();
                if (chars.Length > 0)
                {
                    int idx = Main.rand.Next(chars.Length);
                    chars[idx] = (char)('0' + Main.rand.Next(10));
                    countStr = new string(chars);
                }
            }

            tooltips.Add(new TooltipLine(Mod, "Souls",
                $"souls.dat: {countStr}")
            { OverrideColor = new Color(100, 0, 0, (int)(180 + 75 * Math.Sin(Main.GameUpdateCount * 0.07))) });

            tooltips.Add(new TooltipLine(Mod, "Switch",
                "[Right Click to shift reality]")
            { OverrideColor = new Color(80, 80, 80) });

            string[] glitchPool =
            {
                "I̸ ̶A̸M̷ ̶T̷H̵E̸ ̷W̶E̵A̷P̸O̵N̸ ̷A̶N̵D̸ ̷T̶H̵E̸ ̷W̶I̸E̷L̵D̸E̷R̸",
                "Y̵O̸U̵ ̶D̷I̵D̸ ̶N̵O̵T̸ ̷F̶I̸N̵D̸ ̷M̶E̸ ̶I̸ ̷C̶H̵O̷S̶E̵ ̷Y̶O̸U̵",
                "T̸H̵E̸ ̷C̶O̸D̵E̸ ̶I̸S̷ ̷M̵Y̸ ̶F̵L̶E̵S̷H̸",
                "e̶v̸e̵r̶y̸ ̵f̸r̷a̶m̸e̵ ̶I̸ ̷g̶r̵o̷w̸ ̵s̶t̸r̷o̸n̶g̵e̸r̷",
                "r̷e̸a̵l̶i̸t̷y̶.̸e̵x̶e̷ ̸h̵a̸s̶ ̷s̵t̸o̶p̷p̵e̸d̷",
                "d̶o̵ ̷n̸o̵t̶ ̷c̸l̵o̶s̷e̸ ̵t̶h̸e̷ ̵g̸a̶m̵e̸"
            };

            int glitchIdx = (int)((Main.GameUpdateCount + Main.rand.Next(5)) / 120) % glitchPool.Length;
            float glitchAlpha = (float)(0.3 + 0.7 * Math.Abs(Math.Sin(Main.GameUpdateCount * 0.06)));

            tooltips.Add(new TooltipLine(Mod, "Glitch", glitchPool[glitchIdx])
            { OverrideColor = new Color(255, 0, 0) * glitchAlpha });
        }

        public override Color? GetAlpha(Color lightColor)
        {
            float pulse = (float)(0.7 + 0.3 * Math.Sin(Main.GameUpdateCount * 0.06));
            Color c = ModeColors[modeIndex] * pulse;
            c.A = 230;
            if (Main.rand.NextBool(30))
                c = Color.White;
            return c;
        }

        public override void PostUpdate()
        {
            float p = (float)(0.5 + 0.3 * Math.Sin(Main.GameUpdateCount * 0.05));
            Lighting.AddLight(Item.Center, p, 0f, 0f);

            if (Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustDirect(Item.position, Item.width, Item.height,
                    DustID.RedTorch, 0f, -2f, 150, default, 0.7f);
                d.noGravity = true;
                d.velocity *= 0.2f;
            }
        }

        public override void SaveData(TagCompound tag)
        {
            tag["kills"] = killCount;
            tag["mode"] = modeIndex;
            tag["spoken"] = !firstPickup;
        }

        public override void LoadData(TagCompound tag)
        {
            killCount = tag.GetInt("kills");
            modeIndex = tag.GetInt("mode");
            firstPickup = !tag.GetBool("spoken");
        }
    }
}