using GodEyeMod.Content.Ui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content.NPCs.GodEye
{
    [AutoloadBossHead]
    public class GodEyeBoss : ModNPC
    {
        private enum Phase { Phase1, Phase2, Phase3, Phase4 }

        private Phase CurrentPhase
        {
            get => (Phase)(int)NPC.ai[0];
            set => NPC.ai[0] = (int)value;
        }

        private float AttackTimer
        {
            get => NPC.ai[1];
            set => NPC.ai[1] = value;
        }

        private float SpecialTimer
        {
            get => NPC.ai[2];
            set => NPC.ai[2] = value;
        }

        private float GlitchTimer
        {
            get => NPC.ai[3];
            set => NPC.ai[3] = value;
        }

        private int baseDamage = 120;
        private int trueMaxLife = 777777;
        private int displayedMaxLife;
        private bool hasTransitionedPhase2 = false;
        private bool hasTransitionedPhase3 = false;
        private bool hasTransitionedPhase4 = false;
        private int hpGlitchCooldown = 0;
        private int spiralAngle = 0;
        private int teleportCooldown = 0;
        private int eyeWallCooldown = 0;
        private int phaseTransitionTimer = 0;
        private bool doingTransition = false;
        private int blackHoleCooldown = 0;
        private int fakeDeathTimer = 0;
        private bool fakeDeathTriggered = false;
        private int rageModeTimer = 0;
        private List<Vector2> afterimagePositions = new List<Vector2>();

        private float orbitAngle = 0f;
        private int chatSpamTimer = 0;
        private bool npcsKilled = false;

        private int gravityFlipCooldown = 0;
        private int teleportPlayerCooldown = 0;
        private int rainProjectileCooldown = 0;
        private int mazeWallCooldown = 0;
        private int blindCooldown = 0;
        private int zoomCooldown = 0;
        private int eyeQuestCooldown = 0;
        private int crossAttackCooldown = 0;

        private bool crossCharging = false;
        private bool crossRetreating = false;
        private int crossChargeTimer = 0;
        private int crossRetreatTimer = 0;
        private float crossChargeSpeed = 0f;

        private bool barrageActive = false;
        private int barrageState = 0;
        private int barrageTimer = 0;
        private int barrageSwingsLeft = 0;
        private int barrageSwingInterval = 0;
        private int barrageSwingCounter = 0;
        private Vector2 barrageTargetPos = Vector2.Zero;
        private Vector2 barrageDirection = Vector2.Zero;
        private int barrageCooldown = 0;
        private float barrageSpeed = 0f;
        private int barrageWarningTimer = 0;
        private int barrageGrabTimer = 0;
        private float barrageOrbitAngle = 0f;
        private float barrageOrbitRadius = 250f;
        private float barrageOrbitSpeed = 0f;
        private int barrageFlightTimer = 0;
        private int barrageMaxFlightTime = 0;

        private float questSpeedMultiplier = 1f;
        private float questDamageMultiplier = 1f;
        private float questDefenseModifier = 0f;
        private int questDebuffTimer = 0;
        private bool questBuffActive = false;
        private bool questIsWeakened = false;
        private float questOrbitSpeedMultiplier = 1f;
        private int questEffectVisualTimer = 0;
        private float questBaseSpeedMult = 1f;
        private float questBaseDamageMult = 1f;
        private float questBaseDefenseMod = 0f;
        private float questBaseOrbitMult = 1f;
        private int questBaseDuration = 0;

        private static string[] godMessages = new string[]
        {
            "I SEE YOU", "THERE IS NO ESCAPE", "YOUR WORLD IS MINE",
            "ERROR ERROR ERROR", "DO YOU FEEL IT?", "REALITY IS A LIE",
            "I AM EVERYWHERE", "YOU CANNOT WIN", "THIS IS MY DOMAIN",
            "NULL NULL NULL", "SEGFAULT IN YOUR SOUL",
            "THE VOID CALLS", "STOP RESISTING",
            "Y̸O̵U̷ ̶A̴R̶E̷ ̵N̸O̷T̶H̸I̷N̶G̸",
            "WORLD.DAT CORRUPTED", "PLAYER.LIFE = 0",
            "HAHAHAHAHAHA", "WHY DO YOU FIGHT?",
            "I CONTROL EVERYTHING", "LOOK BEHIND YOU",
            "TERRARIA.EXE HAS STOPPED", "STACK OVERFLOW",
            "YOUR SAVE IS GONE", "I AM THE GAME",
            "YOU ARE THE BUG", "PATCHING REALITY...", "PATCH FAILED"
        };

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 6;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);

            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Scale = 0.4f,
                PortraitScale = 0.5f
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);

            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.ShadowFlame] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 400;
            NPC.height = 400;
            NPC.damage = 0;
            NPC.defense = 80;
            NPC.lifeMax = 777777;
            trueMaxLife = 777777;
            displayedMaxLife = 777777;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.value = Item.buyPrice(platinum: 50);
            NPC.boss = true;
            NPC.npcSlots = 30f;
            NPC.aiStyle = -1;
            NPC.lavaImmune = true;
            NPC.scale = 1f;
            NPC.BossBar = ModContent.GetInstance<GodEyeBossBar>();
            Music = MusicLoader.GetMusicSlot(Mod, "Content/Music/GodEyeTheme");
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            return false;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
                new FlavorTextBestiaryInfoElement(
                    "The omniscient deity that exists beyond the boundaries of this world. " +
                    "It sees all, knows all, and can reshape reality itself. " +
                    "It is not a boss. It is the game itself.")
            });
        }

        public override void AI()
        {
            NPC.damage = 0;

            if (GodEyeQuestSystem.IsBossPaused())
            {
                NPC.velocity *= 0.9f;
                NPC.dontTakeDamage = true;
                Player questTarget = Main.player[NPC.target];
                if (questTarget.active && !questTarget.dead)
                {
                    float targetRot = (questTarget.Center - NPC.Center).ToRotation();
                    NPC.rotation = MathHelper.Lerp(NPC.rotation, targetRot, 0.05f);
                }
                return;
            }

            NPC.dontTakeDamage = doingTransition;

            if (trueMaxLife == 777777 && NPC.lifeMax != 777777)
            {
                trueMaxLife = NPC.lifeMax;
                displayedMaxLife = trueMaxLife;
            }

            Player target = Main.player[NPC.target];

            if (!target.active || target.dead)
            {
                NPC.TargetClosest(true);
                target = Main.player[NPC.target];
                if (!target.active || target.dead)
                {
                    if (GodEyeGlitchSystem.DeathChaosActive)
                    {
                        NPC.velocity *= 0.95f;
                        NPC.rotation += 0.01f;
                        NPC.dontTakeDamage = true;
                        if (Main.rand.NextBool(10))
                            NPC.position += Main.rand.NextVector2Circular(40, 40);
                        if (GodEyeGlitchSystem.KickTimer <= 0 && GodEyeGlitchSystem.KickTimer != -1)
                            NPC.active = false;
                        return;
                    }
                    bool anyAlive = false;
                    for (int i = 0; i < Main.maxPlayers; i++)
                        if (Main.player[i].active && !Main.player[i].dead) { anyAlive = true; break; }
                    if (!anyAlive) { NPC.velocity *= 0.95f; return; }
                    return;
                }
            }

            NPC.TargetClosest(true);

            UpdateQuestEffects();

            if (Main.GameUpdateCount % 3 == 0)
            {
                afterimagePositions.Insert(0, NPC.Center);
                if (afterimagePositions.Count > 10)
                    afterimagePositions.RemoveAt(afterimagePositions.Count - 1);
            }

            float lifePercent = (float)NPC.life / trueMaxLife;
            if (lifePercent > 0.7f) CurrentPhase = Phase.Phase1;
            else if (lifePercent > 0.4f) CurrentPhase = Phase.Phase2;
            else if (lifePercent > 0.15f) CurrentPhase = Phase.Phase3;
            else CurrentPhase = Phase.Phase4;

            HandlePhaseTransitions(lifePercent, target);
            if (doingTransition) { PhaseTransitionBehavior(target); return; }

            if (barrageActive)
            {
                HandleBarrage(target);
                float tr = (target.Center - NPC.Center).ToRotation();
                NPC.rotation = MathHelper.Lerp(NPC.rotation, tr, 0.12f);
                HandleChatSpam();
                DecrementCooldowns();
                return;
            }

            HandleCrossMovement(target);

            switch (CurrentPhase)
            {
                case Phase.Phase1: Phase1AI(target); break;
                case Phase.Phase2: Phase2AI(target); break;
                case Phase.Phase3: Phase3AI(target); break;
                case Phase.Phase4: Phase4AI(target); break;
            }

            float targetRotation = (target.Center - NPC.Center).ToRotation();
            NPC.rotation = MathHelper.Lerp(NPC.rotation, targetRotation, 0.08f);
            HandleChatSpam();
            DecrementCooldowns();
        }

        private void DecrementCooldowns()
        {
            if (hpGlitchCooldown > 0) hpGlitchCooldown--;
            if (teleportCooldown > 0) teleportCooldown--;
            if (eyeWallCooldown > 0) eyeWallCooldown--;
            if (blackHoleCooldown > 0) blackHoleCooldown--;
            if (gravityFlipCooldown > 0) gravityFlipCooldown--;
            if (teleportPlayerCooldown > 0) teleportPlayerCooldown--;
            if (rainProjectileCooldown > 0) rainProjectileCooldown--;
            if (mazeWallCooldown > 0) mazeWallCooldown--;
            if (blindCooldown > 0) blindCooldown--;
            if (zoomCooldown > 0) zoomCooldown--;
            if (eyeQuestCooldown > 0) eyeQuestCooldown--;
            if (crossAttackCooldown > 0) crossAttackCooldown--;
            if (barrageCooldown > 0) barrageCooldown--;
        }

        #region Quest Effects

        public void ApplyQuestResult(bool success, int streak)
        {
            int s = Math.Clamp(streak, 1, 5);

            questBuffActive = true;
            questIsWeakened = success;

            if (success)
            {
                questBaseDuration = 480 + s * 120;
                questBaseSpeedMult = 1f - (0.12f + s * 0.06f);
                questBaseDamageMult = 1f - (0.10f + s * 0.05f);
                questBaseDefenseMod = -(5f + s * 6f);
                questBaseOrbitMult = 1f - (0.10f + s * 0.08f);

                questDebuffTimer = questBaseDuration;
                questEffectVisualTimer = questBaseDuration;
                questSpeedMultiplier = questBaseSpeedMult;
                questDamageMultiplier = questBaseDamageMult;
                questDefenseModifier = questBaseDefenseMod;
                questOrbitSpeedMultiplier = questBaseOrbitMult;

                if (Main.netMode != NetmodeID.Server)
                {
                    string[] weakMsgs = s switch
                    {
                        1 => new[] { "...a minor glitch...", "SYSTEM_INTEGRITY: 95%", "barely noticeable..." },
                        2 => new[] { "YOUR OBEDIENCE... WEAKENS ME?", "PROCESSING POWER: REDUCED", "what is happening..." },
                        3 => new[] { "SYSTEM_INTEGRITY: DEGRADING", "compliance.exe IS CORRUPTING MY CODE", "S̸T̵O̸P̷" },
                        4 => new[] { "W̸H̵A̸T̷ ̶H̵A̶V̸E̷ ̶Y̸O̵U̷ ̶D̸O̶N̷E̸", "ERROR: POWER_LEVEL CRITICAL", "I... CAN'T..." },
                        _ => new[] { "I̸̛M̸̛P̸̛Ơ̸S̸̛S̸̛I̸̛B̸̛L̸̛E̸̛", "CORE MELTDOWN", "N̸̛Ơ̸.̸̛.̸̛.̸̛ ̸̛N̸̛Ơ̸.̸̛.̸̛.̸̛" }
                    };
                    Main.NewText(weakMsgs[Main.rand.Next(weakMsgs.Length)], Color.Cyan);
                    if (s >= 3)
                        Main.NewText($"[BOSS WEAKENED x{s}]", Color.Cyan);
                    else
                        Main.NewText("[BOSS WEAKENED]", Color.Cyan);
                }

                SoundEngine.PlaySound(SoundID.Item29 with { Pitch = -0.5f + s * 0.1f, Volume = 0.8f + s * 0.1f }, NPC.Center);

                for (int i = 0; i < 10 + s * 4; i++)
                {
                    Dust d = Dust.NewDustDirect(NPC.Center + Main.rand.NextVector2Circular(NPC.width, NPC.height),
                        1, 1, DustID.IceTorch, 0, 0, 100, Color.Cyan, 1.5f + s * 0.2f);
                    d.noGravity = true;
                    d.velocity = (NPC.Center - d.position).SafeNormalize(Vector2.Zero) * (2f + s);
                }
            }
            else
            {
                questBaseDuration = 600 + s * 150;
                questBaseSpeedMult = 1f + (0.15f + s * 0.08f);
                questBaseDamageMult = 1f + (0.12f + s * 0.06f);
                questBaseDefenseMod = 8f + s * 7f;
                questBaseOrbitMult = 1f + (0.15f + s * 0.10f);

                questDebuffTimer = questBaseDuration;
                questEffectVisualTimer = questBaseDuration;
                questSpeedMultiplier = questBaseSpeedMult;
                questDamageMultiplier = questBaseDamageMult;
                questDefenseModifier = questBaseDefenseMod;
                questOrbitSpeedMultiplier = questBaseOrbitMult;

                if (Main.netMode != NetmodeID.Server)
                {
                    string[] strongMsgs = s switch
                    {
                        1 => new[] { "YOUR FAILURE AMUSES ME", "a small boost...", "POWER_LEVEL: +1" },
                        2 => new[] { "YOUR FAILURE FEEDS ME", "I GROW STRONGER", "DISOBEDIENCE = MY STRENGTH" },
                        3 => new[] { "RAGE_MODE: AMPLIFIED", "EACH FAILURE MAKES ME MORE", "THANK YOU FOR YOUR WEAKNESS" },
                        4 => new[] { "EXECUTING: rage_boost.exe", "I CAN FEEL YOUR FEAR", "UNLIMITED POWER" },
                        _ => new[] { "Ÿ̸̛Ơ̸Ư̸R̸̛ ̸̛P̸̛A̸̛I̸̛N̸̛ ̸̛I̸̛S̸̛ ̸̛M̸̛Y̸̛ ̸̛F̸̛Ư̸E̸̛L̸̛", "G̸̛Ơ̸D̸̛_̸̛M̸̛Ơ̸D̸̛E̸̛", "AHAHAHAHAHA" }
                    };
                    Main.NewText(strongMsgs[Main.rand.Next(strongMsgs.Length)], Color.OrangeRed);
                    if (s >= 3)
                        Main.NewText($"[BOSS ENRAGED x{s}]", Color.OrangeRed);
                    else
                        Main.NewText("[BOSS ENRAGED]", Color.OrangeRed);
                }

                SoundEngine.PlaySound(SoundID.Roar with { Pitch = -0.2f + s * 0.15f, Volume = 1f + s * 0.1f }, NPC.Center);

                for (int i = 0; i < 12 + s * 5; i++)
                {
                    float angle = MathHelper.TwoPi / (12 + s * 5) * i;
                    Dust d = Dust.NewDustDirect(NPC.Center, 1, 1,
                        DustID.Torch, 0, 0, 50, Color.Red, 1.5f + s * 0.3f);
                    d.noGravity = true;
                    d.velocity = angle.ToRotationVector2() * (4f + s * 1.5f);
                }
                for (int i = 0; i < 5 + s * 2; i++)
                {
                    Dust d2 = Dust.NewDustDirect(NPC.Center + Main.rand.NextVector2Circular(NPC.width, NPC.height),
                        1, 1, DustID.Shadowflame, 0, 0, 100, default, 1.5f + s * 0.2f);
                    d2.noGravity = true;
                    d2.velocity = Main.rand.NextVector2Circular(4 + s, 4 + s);
                }
            }
        }

        private void UpdateQuestEffects()
        {
            if (!questBuffActive) return;

            questDebuffTimer--;
            questEffectVisualTimer--;

            float fadeStart = 180f;
            if (questDebuffTimer <= fadeStart && questDebuffTimer > 0)
            {
                float fadeout = questDebuffTimer / fadeStart;
                questSpeedMultiplier = MathHelper.Lerp(1f, questBaseSpeedMult, fadeout);
                questDamageMultiplier = MathHelper.Lerp(1f, questBaseDamageMult, fadeout);
                questDefenseModifier = MathHelper.Lerp(0f, questBaseDefenseMod, fadeout);
                questOrbitSpeedMultiplier = MathHelper.Lerp(1f, questBaseOrbitMult, fadeout);
            }

            if (questIsWeakened)
                NPC.defense = Math.Max(0, NPC.defense + (int)questDefenseModifier);
            else
                NPC.defense += (int)questDefenseModifier;

            if (questIsWeakened)
            {
                if (Main.rand.NextBool(4))
                {
                    Dust d = Dust.NewDustDirect(
                        NPC.position + new Vector2(Main.rand.Next(NPC.width), Main.rand.Next(NPC.height)),
                        1, 1, DustID.IceTorch, 0, -1f, 100, Color.Cyan, 1.2f);
                    d.noGravity = true;
                    d.velocity *= 0.3f;
                }

                if (!barrageActive && !doingTransition)
                {
                    float weakPulse = (float)Math.Sin(Main.GameUpdateCount * 0.08f) * 0.03f;
                    float targetScale = MathHelper.Lerp(1f, 0.85f, 1f - questSpeedMultiplier / questBaseSpeedMult);
                    NPC.scale = MathHelper.Lerp(NPC.scale, targetScale + weakPulse, 0.05f);
                }

                NPC.velocity *= questSpeedMultiplier;
            }
            else
            {
                if (Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustDirect(
                        NPC.position + new Vector2(Main.rand.Next(NPC.width), Main.rand.Next(NPC.height)),
                        1, 1, DustID.Torch, 0, 0, 50, Color.OrangeRed, 1.5f);
                    d.noGravity = true;
                    d.velocity = Main.rand.NextVector2Circular(3, 3);
                }
                if (Main.rand.NextBool(6))
                {
                    Dust d2 = Dust.NewDustDirect(NPC.Center + Main.rand.NextVector2Circular(NPC.width / 2, NPC.height / 2),
                        1, 1, DustID.Shadowflame, 0, 0, 80, default, 1.3f);
                    d2.noGravity = true;
                }

                if (!barrageActive && !doingTransition)
                {
                    float ragePulse = (float)Math.Sin(Main.GameUpdateCount * 0.15f) * 0.04f;
                    float targetScale = MathHelper.Lerp(1f, 1.25f, (questSpeedMultiplier - 1f) / (questBaseSpeedMult - 1f + 0.001f));
                    NPC.scale = MathHelper.Lerp(NPC.scale, targetScale + ragePulse, 0.05f);
                }
            }

            if (questDebuffTimer % 240 == 0 && questDebuffTimer > 0 && Main.netMode != NetmodeID.Server)
            {
                if (questIsWeakened)
                {
                    string[] msgs = { "...still... weak...", "SYSTEMS RECOVERING...",
                        "integrity: rebuilding", "I will... remember this..." };
                    Main.NewText(msgs[Main.rand.Next(msgs.Length)], Color.Cyan * 0.7f);
                }
                else
                {
                    string[] msgs = { "HAHAHAHA", "FEEL MY POWER",
                        "MORE! MORE!", "YOUR MISTAKE FUELS ME" };
                    Main.NewText(msgs[Main.rand.Next(msgs.Length)], Color.OrangeRed * 0.7f);
                }
            }

            if (questDebuffTimer <= 0)
            {
                questBuffActive = false;
                questSpeedMultiplier = 1f;
                questDamageMultiplier = 1f;
                questDefenseModifier = 0f;
                questOrbitSpeedMultiplier = 1f;
                NPC.scale = 1f;

                if (Main.netMode != NetmodeID.Server)
                {
                    if (questIsWeakened)
                        Main.NewText("SYSTEMS RESTORED. FULL POWER.", Color.Yellow);
                    else
                        Main.NewText("Rage subsiding... for now.", Color.Gray);
                }

                SoundEngine.PlaySound(SoundID.Item4 with { Pitch = 0f, Volume = 0.8f }, NPC.Center);
            }
        }

        private float GetQuestOrbitMultiplier()
        {
            return questBuffActive ? questOrbitSpeedMultiplier : 1f;
        }

        private float GetQuestSpeedMultiplier()
        {
            return questBuffActive ? questSpeedMultiplier : 1f;
        }

        private int GetModifiedDamage(int damage)
        {
            if (questBuffActive)
                return (int)(damage * questDamageMultiplier);
            return damage;
        }
public void ResetQuestEffects()
{
    questBuffActive = false;
    questSpeedMultiplier = 1f;
    questDamageMultiplier = 1f;
    questDefenseModifier = 0f;
    questOrbitSpeedMultiplier = 1f;
    questDebuffTimer = 0;
    questEffectVisualTimer = 0;
    questIsWeakened = false;
    questBaseSpeedMult = 1f;
    questBaseDamageMult = 1f;
    questBaseDefenseMod = 0f;
    questBaseOrbitMult = 1f;
    questBaseDuration = 0;
    NPC.scale = 1f;
}

        #endregion

        #region Barrage

        private void StartBarrage(Player target)
        {
            if (barrageCooldown > 0 || barrageActive) return;
            if (crossCharging || crossRetreating) return;

            barrageActive = true;
            barrageState = 1;
            barrageTimer = 0;
            barrageWarningTimer = 0;
            barrageGrabTimer = 0;
            barrageSwingCounter = 0;
            barrageFlightTimer = 0;
            barrageOrbitAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            barrageOrbitSpeed = 0.02f;
            barrageOrbitRadius = 250f;

            barrageSwingsLeft = CurrentPhase switch
            {
                Phase.Phase4 => 7,
                Phase.Phase3 => 5,
                _ => 4
            };
            barrageSwingInterval = CurrentPhase switch
            {
                Phase.Phase4 => 18,
                Phase.Phase3 => 22,
                _ => 26
            };
            barrageSpeed = CurrentPhase switch
            {
                Phase.Phase4 => 18f,
                Phase.Phase3 => 15f,
                _ => 12f
            };
            barrageMaxFlightTime = CurrentPhase switch
            {
                Phase.Phase4 => 180,
                Phase.Phase3 => 160,
                _ => 140
            };

            barrageCooldown = CurrentPhase switch
            {
                Phase.Phase4 => 420,
                Phase.Phase3 => 540,
                _ => 660
            };

            NPC.velocity *= 0.1f;

            if (Main.netMode != NetmodeID.Server)
                Main.NewText("...", new Color(255, 200, 200));

            SoundEngine.PlaySound(SoundID.Item119 with { Volume = 0.8f, Pitch = -0.8f }, NPC.Center);
        }

        private void HandleBarrage(Player target)
        {
            barrageTimer++;
            switch (barrageState)
            {
                case 1: HandleBarrageWarning(target); break;
                case 2: HandleBarrageGrab(target); break;
                case 3: HandleBarrageAttack(target); break;
            }
        }

        private void HandleBarrageWarning(Player target)
        {
            barrageWarningTimer++;
            NPC.velocity *= 0.9f;

            float shakeProgress = barrageWarningTimer / 120f;
            if (Main.rand.NextBool(Math.Max(1, (int)(8 - shakeProgress * 7))))
                NPC.position += Main.rand.NextVector2Circular(3f + shakeProgress * 8f, 3f + shakeProgress * 8f);

            NPC.scale = 1f + (float)Math.Sin(barrageWarningTimer * 0.15f) * (0.05f + shakeProgress * 0.15f);

            barrageOrbitSpeed = MathHelper.Lerp(0.03f, 0.2f, shakeProgress);
            barrageOrbitAngle += barrageOrbitSpeed;
            barrageOrbitRadius = MathHelper.Lerp(280f, 150f, shakeProgress);

            Vector2 crossOrbitPos = NPC.Center + barrageOrbitAngle.ToRotationVector2() * barrageOrbitRadius;

            float crossSize = 1.5f + shakeProgress;
            for (int i = 0; i < (int)(2 + shakeProgress * 4); i++)
            {
                Vector2 offset;
                if (Main.rand.NextBool(2))
                    offset = new Vector2(Main.rand.NextFloat(-20, 20) * crossSize, Main.rand.NextFloat(-3, 3) * crossSize);
                else
                    offset = new Vector2(Main.rand.NextFloat(-3, 3) * crossSize, Main.rand.NextFloat(-20, 20) * crossSize);

                Dust d = Dust.NewDustDirect(crossOrbitPos + offset, 1, 1,
                    DustID.WhiteTorch, 0, 0, 50, Color.White, 1.5f + shakeProgress);
                d.noGravity = true;
                d.velocity = (NPC.Center - d.position).SafeNormalize(Vector2.Zero) * 0.5f;
                d.fadeIn = 1.2f;
            }

            if (Main.rand.NextBool(2))
            {
                Dust d2 = Dust.NewDustDirect(crossOrbitPos, 1, 1,
                    DustID.PurpleTorch, 0, 0, 80, default, 1.2f);
                d2.noGravity = true;
                d2.velocity *= 0.3f;
            }

            if (barrageWarningTimer == 40)
                SoundEngine.PlaySound(SoundID.Item119 with { Volume = 1.2f, Pitch = -0.4f }, NPC.Center);
            if (barrageWarningTimer == 80)
                SoundEngine.PlaySound(SoundID.Item119 with { Volume = 1.6f, Pitch = 0f }, NPC.Center);

            if (Main.netMode != NetmodeID.Server)
                Main.LocalPlayer.GetModPlayer<ScreenShakePlayer>().AddShake(shakeProgress * 4f);

            if (barrageWarningTimer == 30 && Main.netMode != NetmodeID.Server)
                Main.NewText("...!", new Color(255, 150, 150));
            if (barrageWarningTimer == 70 && Main.netMode != NetmodeID.Server)
            {
                string msg = CurrentPhase switch
                {
                    Phase.Phase4 => "R̸̛A̵̛G̶̛E̷̛",
                    Phase.Phase3 => "BARRAGE",
                    _ => "STRIKE"
                };
                Main.NewText(msg, new Color(255, 80, 80));
            }

            int warningDuration = CurrentPhase switch
            {
                Phase.Phase4 => 90,
                Phase.Phase3 => 110,
                _ => 120
            };

            if (barrageWarningTimer >= warningDuration)
            {
                barrageState = 2;
                barrageGrabTimer = 0;
                SoundEngine.PlaySound(SoundID.Item119 with { Volume = 2f, Pitch = 0.5f }, NPC.Center);
            }
        }

        private void HandleBarrageGrab(Player target)
        {
            barrageGrabTimer++;
            float grabDuration = 20f;
            float progress = barrageGrabTimer / grabDuration;

            barrageOrbitRadius = MathHelper.Lerp(150f, 0f, EaseInQuad(progress));
            barrageOrbitAngle += 0.3f;

            Vector2 crossPos = NPC.Center + barrageOrbitAngle.ToRotationVector2() * barrageOrbitRadius;

            for (int i = 0; i < (int)(3 + progress * 8); i++)
            {
                Dust d = Dust.NewDustDirect(crossPos + Main.rand.NextVector2Circular(15, 15), 1, 1,
                    DustID.WhiteTorch, 0, 0, 0, Color.White, 2f + progress);
                d.noGravity = true;
                d.velocity = (NPC.Center - d.position).SafeNormalize(Vector2.Zero) * (3f + progress * 8f);
                d.fadeIn = 1.3f;
            }

            NPC.position += Main.rand.NextVector2Circular(6, 6);
            NPC.velocity *= 0.9f;
            NPC.scale = 1f + progress * 0.3f;

            if (Main.netMode != NetmodeID.Server)
                Main.LocalPlayer.GetModPlayer<ScreenShakePlayer>().AddShake(5f + progress * 5f);

            if (barrageGrabTimer >= (int)grabDuration)
            {
                barrageState = 3;
                barrageFlightTimer = 0;
                barrageSwingCounter = barrageSwingInterval - 1;

                barrageTargetPos = target.Center;
                barrageDirection = (barrageTargetPos - NPC.Center).SafeNormalize(Vector2.UnitX);

                for (int i = 0; i < 25; i++)
                {
                    float angle = MathHelper.TwoPi / 25f * i;
                    Dust d = Dust.NewDustDirect(NPC.Center, 1, 1,
                        DustID.WhiteTorch, 0, 0, 0, Color.White, 3f);
                    d.noGravity = true;
                    d.velocity = angle.ToRotationVector2() * 10f;
                    d.fadeIn = 1.5f;
                }
                for (int i = 0; i < 10; i++)
                {
                    Dust d2 = Dust.NewDustDirect(NPC.Center, 1, 1,
                        DustID.Vortex, 0, 0, 100, default, 2.5f);
                    d2.noGravity = true;
                    d2.velocity = Main.rand.NextVector2Circular(8, 8);
                }

                SoundEngine.PlaySound(SoundID.Roar with { Volume = 2f, Pitch = 0.3f }, NPC.Center);

                if (Main.netMode != NetmodeID.Server)
                {
                    Main.LocalPlayer.GetModPlayer<ScreenShakePlayer>().AddShake(12f);
                    string msg = CurrentPhase switch
                    {
                        Phase.Phase4 => "R̸̛A̵̛G̶̛E̷̛_̸̛M̵̛Ơ̶D̷̛E̸̛.̵̛E̶̛X̷̛E̵̛",
                        Phase.Phase3 => "BARRAGE_PROTOCOL: EXECUTE",
                        _ => "MULTI_STRIKE: GO"
                    };
                    Main.NewText(msg, new Color(255, 50, 50));
                }
            }
        }

        private void HandleBarrageAttack(Player target)
        {
            barrageFlightTimer++;

            Vector2 toPlayer = (target.Center - NPC.Center).SafeNormalize(barrageDirection);

            float correctionStrength = CurrentPhase switch
            {
                Phase.Phase4 => 0.012f,
                Phase.Phase3 => 0.008f,
                _ => 0.005f
            };

            barrageDirection = Vector2.Lerp(barrageDirection, toPlayer, correctionStrength).SafeNormalize(barrageDirection);

            float currentSpeed = MathHelper.Lerp(barrageSpeed * 0.5f, barrageSpeed,
                MathHelper.Clamp(barrageFlightTimer / 30f, 0f, 1f)) * GetQuestSpeedMultiplier();
            NPC.velocity = Vector2.Lerp(NPC.velocity, barrageDirection * currentSpeed, 0.08f);

            if (Main.rand.NextBool(4))
                NPC.position += Main.rand.NextVector2Circular(4, 4);

            NPC.scale = 1.1f + (float)Math.Sin(barrageTimer * 0.4f) * 0.08f;

            for (int i = 0; i < 2; i++)
            {
                Dust d = Dust.NewDustDirect(NPC.Center + Main.rand.NextVector2Circular(NPC.width / 3, NPC.height / 3),
                    1, 1, DustID.WhiteTorch, 0, 0, 50, Color.White, 1.5f);
                d.noGravity = true;
                d.velocity = -NPC.velocity * 0.12f + Main.rand.NextVector2Circular(1.5f, 1.5f);
            }
            if (Main.rand.NextBool(3))
            {
                Dust d2 = Dust.NewDustDirect(NPC.Center, 1, 1,
                    DustID.PurpleTorch, 0, 0, 60, default, 1.3f);
                d2.noGravity = true;
                d2.velocity = -NPC.velocity * 0.08f;
            }

            if (Main.netMode != NetmodeID.Server)
                Main.LocalPlayer.GetModPlayer<ScreenShakePlayer>().AddShake(2.5f);

            barrageSwingCounter++;
            if (barrageSwingCounter >= barrageSwingInterval && barrageSwingsLeft > 0)
            {
                barrageSwingCounter = 0;
                barrageSwingsLeft--;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<GodEyeCross>(),
                        GetModifiedDamage(baseDamage * 3 / 4), 0, Main.myPlayer,
                        NPC.whoAmI, 1f);
                }

                for (int i = 0; i < 6; i++)
                {
                    Dust d = Dust.NewDustDirect(NPC.Center, 1, 1,
                        DustID.WhiteTorch, 0, 0, 0, Color.White, 2.5f);
                    d.noGravity = true;
                    d.velocity = Main.rand.NextVector2Circular(8, 8);
                    d.fadeIn = 1.3f;
                }

                if (Main.netMode != NetmodeID.Server)
                    Main.LocalPlayer.GetModPlayer<ScreenShakePlayer>().AddShake(6f);
            }

            bool outOfSwings = barrageSwingsLeft <= 0;
            bool outOfTime = barrageFlightTimer >= barrageMaxFlightTime;
            float distToPlayer = Vector2.Distance(NPC.Center, target.Center);
            bool flewPast = barrageFlightTimer > 60 && distToPlayer > 1500f;

            if (outOfSwings || outOfTime || flewPast)
                EndBarrage();
        }

        private void EndBarrage()
        {
            barrageActive = false;
            barrageState = 0;
            NPC.scale = 1f;
            NPC.velocity *= 0.3f;

            crossRetreating = true;
            crossRetreatTimer = GetRetreatDuration() + 40;

            for (int i = 0; i < 15; i++)
            {
                float angle = MathHelper.TwoPi / 15f * i;
                Dust d = Dust.NewDustDirect(NPC.Center, 1, 1,
                    DustID.WhiteTorch, 0, 0, 0, Color.White, 2f);
                d.noGravity = true;
                d.velocity = angle.ToRotationVector2() * 6f;
            }

            SoundEngine.PlaySound(SoundID.Item14 with { Pitch = 0.3f, Volume = 0.8f }, NPC.Center);
        }

        private float EaseInQuad(float t) => t * t;

        #endregion

        #region Cross Movement

        private void HandleCrossMovement(Player target)
        {
            if (crossCharging)
            {
                crossChargeTimer--;
                Vector2 dirToPlayer = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero);
                float dist = Vector2.Distance(NPC.Center, target.Center);
                float targetDist = GetCrossStrikeDistance();

                if (dist > targetDist + 50f)
                {
                    float actualChargeSpeed = crossChargeSpeed * GetQuestSpeedMultiplier();
                    NPC.velocity = Vector2.Lerp(NPC.velocity, dirToPlayer * actualChargeSpeed, 0.15f);
                }
                else
                    NPC.velocity *= 0.85f;

                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustDirect(NPC.Center + Main.rand.NextVector2Circular(NPC.width / 2, NPC.height / 2),
                        1, 1, DustID.WhiteTorch, 0, 0, 100, Color.White, 1.5f);
                    d.noGravity = true;
                    d.velocity = -NPC.velocity * 0.2f;
                }

                if (crossChargeTimer <= 0)
                {
                    crossCharging = false;
                    crossRetreating = true;
                    crossRetreatTimer = GetRetreatDuration();
                }
            }
            else if (crossRetreating)
            {
                crossRetreatTimer--;
                float dist = Vector2.Distance(NPC.Center, target.Center);
                float safeDistance = GetNormalOrbitRadius();

                if (dist < safeDistance - 100f)
                {
                    Vector2 pushDir = (NPC.Center - target.Center).SafeNormalize(Vector2.Zero);
                    float retreatSpeed = MathHelper.Lerp(8f, 16f, 1f - (float)crossRetreatTimer / GetRetreatDuration());
                    retreatSpeed *= GetQuestSpeedMultiplier();
                    NPC.velocity = Vector2.Lerp(NPC.velocity, pushDir * retreatSpeed, 0.1f);
                }
                else
                    NPC.velocity *= 0.92f;

                if (crossRetreatTimer <= 0)
                    crossRetreating = false;
            }
        }

        private float GetCrossStrikeDistance() => CurrentPhase switch
        {
            Phase.Phase4 => 350f, Phase.Phase3 => 400f,
            Phase.Phase2 => 450f, _ => 500f
        };

        private float GetNormalOrbitRadius() => CurrentPhase switch
        {
            Phase.Phase4 => 1000f, Phase.Phase3 => 900f,
            Phase.Phase2 => 850f, _ => 800f
        };

        private int GetRetreatDuration() => CurrentPhase switch
        {
            Phase.Phase4 => 60, Phase.Phase3 => 75,
            Phase.Phase2 => 90, _ => 100
        };

        private void StartCrossCharge()
        {
            crossCharging = true;
            crossRetreating = false;
            crossChargeTimer = CurrentPhase switch
            {
                Phase.Phase4 => 80, Phase.Phase3 => 90,
                Phase.Phase2 => 100, _ => 110
            };
            crossChargeSpeed = CurrentPhase switch
            {
                Phase.Phase4 => 22f, Phase.Phase3 => 18f,
                Phase.Phase2 => 15f, _ => 12f
            };

            if (Main.netMode != NetmodeID.Server)
            {
                string msg = CurrentPhase switch
                {
                    Phase.Phase4 => "D̸E̵L̶E̷T̸E̴.̵E̶X̷E̵",
                    Phase.Phase3 => "CROSS_STRIKE.EXE",
                    _ => "MELEE_OVERRIDE"
                };
                Main.NewText(msg, new Color(255, 220, 220));
            }
        }

        #endregion

        private void HandleChatSpam()
{
    if (Main.netMode == NetmodeID.Server) return;

    if (ModContent.GetInstance<GodEyeConfig>().DisableChatMessages) return;

    int spamRate = CurrentPhase switch
    {
        Phase.Phase1 => 360, Phase.Phase2 => 180,
        Phase.Phase3 => 90, Phase.Phase4 => 60, _ => 360
    };
    chatSpamTimer++;
    if (chatSpamTimer >= spamRate)
    {
        chatSpamTimer = 0;
        Main.NewText(godMessages[Main.rand.Next(godMessages.Length)],
            new Color(Main.rand.Next(150, 255), Main.rand.Next(0, 100), Main.rand.Next(100, 255)));
    }
}

        private void OrbitPlayer(Player target, float radius, float speed)
        {
            if (crossCharging || crossRetreating || barrageActive) return;

            float actualSpeed = speed * GetQuestOrbitMultiplier();

            orbitAngle += actualSpeed;
            if (orbitAngle > MathHelper.TwoPi) orbitAngle -= MathHelper.TwoPi;

            Vector2 targetPos = target.Center + orbitAngle.ToRotationVector2() * radius;
            Vector2 moveDir = targetPos - NPC.Center;
            float dist = moveDir.Length();
            if (dist > 1f)
            {
                moveDir.Normalize();
                float moveSpeed = MathHelper.Clamp(dist * 0.05f, 2f, 8f) * GetQuestSpeedMultiplier();
                NPC.velocity = Vector2.Lerp(NPC.velocity, moveDir * moveSpeed, 0.06f);
            }

            float playerDist = Vector2.Distance(NPC.Center, target.Center);
            float minDist = CurrentPhase switch
            {
                Phase.Phase4 => 900f, Phase.Phase3 => 800f,
                Phase.Phase2 => 750f, _ => 700f
            };

            if (playerDist < minDist)
            {
                Vector2 pushDir = (NPC.Center - target.Center).SafeNormalize(Vector2.Zero);
                float pushForce = (1f - playerDist / minDist) * 35f * GetQuestSpeedMultiplier();
                NPC.velocity += pushDir * pushForce * 0.3f;
            }
        }

        private void HandlePhaseTransitions(float lifePercent, Player target)
        {
            if (lifePercent <= 0.7f && !hasTransitionedPhase2)
            {
                hasTransitionedPhase2 = true;
                doingTransition = true;
                phaseTransitionTimer = 120;
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<GodEyeShockwave>(), 0, 0, Main.myPlayer);
                NPC.defense += 20;
                baseDamage += 30;
                if (Main.netMode != NetmodeID.Server)
                    Main.NewText("Reality begins to fracture...", Color.Red);
            }

            if (lifePercent <= 0.4f && !hasTransitionedPhase3)
            {
                hasTransitionedPhase3 = true;
                doingTransition = true;
                phaseTransitionTimer = 180;
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                NPC.defense += 20;
                baseDamage += 40;
                if (Main.netMode != NetmodeID.Server)
                    Main.NewText("Y̸O̵U̷ ̶C̵A̴N̶N̷O̵T̸ ̵K̸I̷L̶L̸ ̴A̵ ̶G̸O̶D̵", Color.DarkRed);
            }

            if (lifePercent <= 0.15f && !hasTransitionedPhase4)
            {
                hasTransitionedPhase4 = true;
                doingTransition = true;
                phaseTransitionTimer = 240;
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                NPC.defense += 30;
                baseDamage += 50;
                if (Main.netMode != NetmodeID.Server)
                    Main.NewText("T̷̢̛H̵̨̛I̷̧̛S̵̢̛ ̷̧̛W̵̨̛Ơ̵̢Ŗ̷̛L̵̨̛D̷̢̛ ̵̧̛Į̷̛S̵̢̛ ̷̧̛M̵̨̛I̷̢̛Ņ̵̛Ę̷̛", Color.Purple);
                KillAllTownNPCs();
            }
        }

        private void KillAllTownNPCs()
        {
            if (npcsKilled) return;
            npcsKilled = true;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.townNPC && npc.type != NPCID.OldMan)
                {
                    npc.life = 0;
                    npc.checkDead();
                    npc.active = false;
                    if (Main.netMode != NetmodeID.Server)
                        Main.NewText((npc.GivenName != "" ? npc.GivenName : npc.TypeName) + " was erased.", Color.DarkRed);
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                }
            }
        }

        private void PhaseTransitionBehavior(Player target)
        {
            phaseTransitionTimer--;
            NPC.dontTakeDamage = true;
            NPC.scale = 1f + (float)Math.Sin(phaseTransitionTimer * 0.1f) * 0.3f;
            for (int i = 0; i < 5; i++)
            {
                Dust d = Dust.NewDustDirect(NPC.Center + Main.rand.NextVector2Circular(NPC.width, NPC.height),
                    1, 1, DustID.Vortex, 0, 0, 100, default, 2f);
                d.noGravity = true;
                d.velocity = (NPC.Center - d.position).SafeNormalize(Vector2.Zero) * 3f;
            }
            NPC.velocity *= 0.9f;
            if (phaseTransitionTimer <= 0)
            {
                doingTransition = false;
                NPC.dontTakeDamage = false;
                NPC.scale = 1f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    for (int i = 0; i < 10; i++)
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center,
                            (MathHelper.TwoPi / 10f * i).ToRotationVector2() * 7f,
                            ModContent.ProjectileType<GodEyeLaser>(), GetModifiedDamage(baseDamage), 0, Main.myPlayer);
            }
        }

        #region God Powers

        private void GodPower_GravityFlip(Player target)
        {
            if (gravityFlipCooldown > 0) return;
            gravityFlipCooldown = 900;
            target.gravDir *= -1f;
            if (Main.netMode != NetmodeID.Server) Main.NewText("GRAVITY_OVERRIDE", Color.Cyan);
            SoundEngine.PlaySound(SoundID.Item8, target.Center);
        }

        private void GodPower_TeleportPlayer(Player target)
        {
            if (teleportPlayerCooldown > 0) return;
            teleportPlayerCooldown = 720;
            target.Teleport(target.Center + Main.rand.NextVector2CircularEdge(400, 400), 1);
            if (Main.netMode != NetmodeID.Server) Main.NewText("PLAYER_POSITION_OVERRIDE", Color.Yellow);
        }

        private void GodPower_ProjectileRain(Player target)
        {
            if (rainProjectileCooldown > 0 || Main.netMode == NetmodeID.MultiplayerClient) return;
            rainProjectileCooldown = 480;
            if (Main.netMode != NetmodeID.Server) Main.NewText("EXECUTING: rain.exe", Color.OrangeRed);
            for (int i = 0; i < 8; i++)
                Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    target.Center + new Vector2(Main.rand.Next(-500, 500), -800),
                    new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(4f, 7f)),
                    ModContent.ProjectileType<GodEyeLaser>(), GetModifiedDamage(baseDamage / 3), 0, Main.myPlayer);
        }

        private void GodPower_MazeWalls(Player target)
        {
            if (mazeWallCooldown > 0 || Main.netMode == NetmodeID.MultiplayerClient) return;
            mazeWallCooldown = 600;
            if (Main.netMode != NetmodeID.Server) Main.NewText("GENERATING: maze.dat", Color.Purple);
            for (int w = 0; w < 2; w++)
            {
                float yOff = -300 + w * 400;
                int hole = Main.rand.Next(1, 5);
                for (int i = 0; i < 6; i++)
                {
                    if (Math.Abs(i - hole) <= 1) continue;
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        new Vector2(target.Center.X - 300 + i * 100, target.Center.Y + yOff),
                        Vector2.Zero,
                        ModContent.ProjectileType<GodEyeWallProjectile>(), GetModifiedDamage(baseDamage / 3), 0, Main.myPlayer,
                        0f, w % 2 == 0 ? 4f : -4f);
                }
            }
        }

        private void GodPower_Blind(Player target)
        {
            if (blindCooldown > 0) return;
            blindCooldown = 900;
            target.AddBuff(BuffID.Blackout, 120);
            target.AddBuff(BuffID.Darkness, 120);
            if (Main.netMode != NetmodeID.Server) Main.NewText("RENDER_PIPELINE: disabled", Color.DarkGray);
        }

        private void GodPower_ZoomGlitch(Player target)
        {
            if (zoomCooldown > 0) return;
            zoomCooldown = 420;
            if (Main.netMode != NetmodeID.Server) Main.GameZoomTarget = Main.rand.NextFloat(0.6f, 1.8f);
        }

        private void GodPower_ClosingRing(Player target)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            for (int i = 0; i < 12; i++)
            {
                float a = MathHelper.TwoPi / 12f * i;
                Vector2 spawnPos = target.Center + a.ToRotationVector2() * 700f;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), spawnPos,
                    (target.Center - spawnPos).SafeNormalize(Vector2.Zero) * 3f,
                    ModContent.ProjectileType<GodEyeLaser>(), GetModifiedDamage(baseDamage / 3), 0, Main.myPlayer);
            }
        }

        private void GodPower_EyeQuest()
        {
            if (eyeQuestCooldown > 0) return;
            if (GodEyeQuestSystem.QuestActive) return;
            eyeQuestCooldown = 900;
            GodEyeQuestSystem.StartQuest();
        }

        private void GodPower_CrossAttack(Player target)
        {
            if (crossAttackCooldown > 0 || Main.netMode == NetmodeID.MultiplayerClient) return;
            if (crossCharging || crossRetreating || barrageActive) return;

            crossAttackCooldown = CurrentPhase switch
            {
                Phase.Phase4 => 300, Phase.Phase3 => 420,
                Phase.Phase2 => 600, _ => 720
            };

            StartCrossCharge();

            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero,
                ModContent.ProjectileType<GodEyeCross>(), GetModifiedDamage(baseDamage * 2 / 3), 0, Main.myPlayer,
                NPC.whoAmI, 0f);
        }

        #endregion

        #region Phase AIs

        private void Phase1AI(Player target)
        {
            AttackTimer++;
            OrbitPlayer(target, 800f, 0.012f);

            if (AttackTimer % 180 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = -1; i <= 1; i++)
                {
                    float angle = (target.Center - NPC.Center).ToRotation() + MathHelper.ToRadians(18 * i);
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center,
                        angle.ToRotationVector2() * 6f,
                        ModContent.ProjectileType<GodEyeLaser>(), GetModifiedDamage(baseDamage / 2), 0, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item33, NPC.Center);
            }

            if (AttackTimer % 360 == 180 && Main.netMode != NetmodeID.MultiplayerClient)
                for (int i = 0; i < 8; i++)
                {
                    float angle = MathHelper.TwoPi / 8f * i;
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center,
                        angle.ToRotationVector2() * 4f,
                        ModContent.ProjectileType<GodEyeLaser>(), GetModifiedDamage(baseDamage / 3), 0, Main.myPlayer);
                }
        }

        private void Phase2AI(Player target)
        {
            AttackTimer++;
            SpecialTimer++;
            OrbitPlayer(target, 850f, 0.018f);

            if (hpGlitchCooldown <= 0 && Main.rand.NextBool(240))
            {
                hpGlitchCooldown = 120;
                displayedMaxLife = trueMaxLife + Main.rand.Next(-100000, 200000);
                NPC.lifeMax = Math.Max(NPC.life + 1, displayedMaxLife);
                if (Main.rand.NextBool(6))
                    NPC.life = Math.Min(NPC.life + Main.rand.Next(10000, 40000), trueMaxLife);
            }

            if (teleportCooldown <= 0 && Main.rand.NextBool(420))
            { teleportCooldown = 180; GlitchTeleport(target); }

            if (AttackTimer % 240 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 5; i++)
                {
                    float angle = (target.Center - NPC.Center).ToRotation() + MathHelper.ToRadians(-20 + 10f * i);
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center,
                        angle.ToRotationVector2() * 6f,
                        ModContent.ProjectileType<GodEyeLaser>(), GetModifiedDamage(baseDamage / 2), 0, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item33, NPC.Center);
            }

            if (eyeWallCooldown <= 0 && SpecialTimer % 720 == 0)
            { eyeWallCooldown = 420; SpawnEyeWall(target); }

            if (AttackTimer % 480 == 240 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                spiralAngle += 15;
                for (int burst = 0; burst < 6; burst++)
                    for (int s = 0; s < 2; s++)
                    {
                        float angle = MathHelper.ToRadians(spiralAngle + burst * 30 + 180 * s);
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center,
                            angle.ToRotationVector2() * 5f,
                            ModContent.ProjectileType<GodEyeLaser>(), GetModifiedDamage(baseDamage / 3), 0, Main.myPlayer);
                    }
            }

            if (AttackTimer % 600 == 300) GodPower_CrossAttack(target);

            if (Main.rand.NextBool(1500)) GodPower_ZoomGlitch(target);
            if (SpecialTimer % 720 == 360) GodPower_ProjectileRain(target);
            if (Main.rand.NextBool(1200)) GodPower_EyeQuest();
            if (Main.rand.NextBool(200)) NPC.position += Main.rand.NextVector2Circular(8, 8);
        }

        private void Phase3AI(Player target)
        {
            AttackTimer++;
            SpecialTimer++;
            GlitchTimer++;

            if (hpGlitchCooldown <= 0 && Main.rand.NextBool(120))
            {
                hpGlitchCooldown = 60;
                if (Main.rand.NextBool(10))
                {
                    trueMaxLife += Main.rand.Next(8000, 20000);
                    NPC.lifeMax = trueMaxLife;
                    NPC.life = Math.Min(NPC.life + Main.rand.Next(3000, 10000), trueMaxLife);
                }
                else
                {
                    displayedMaxLife = trueMaxLife + Main.rand.Next(-150000, 300000);
                    NPC.lifeMax = Math.Max(NPC.life + 1, displayedMaxLife);
                }
            }

            OrbitPlayer(target, 900f + (float)Math.Sin(AttackTimer * 0.008f) * 80f, 0.025f);

            if (teleportCooldown <= 0 && Main.rand.NextBool(240))
            { teleportCooldown = 120; GlitchTeleport(target); }

            if (AttackTimer % 300 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                spiralAngle += 12;
                for (int burst = 0; burst < 8; burst++)
                    for (int s = 0; s < 3; s++)
                    {
                        float angle = MathHelper.ToRadians(spiralAngle + burst * 15 + 120 * s);
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center,
                            angle.ToRotationVector2() * 5.5f,
                            ModContent.ProjectileType<GodEyeLaser>(), GetModifiedDamage(baseDamage / 3), 0, Main.myPlayer);
                    }
                SoundEngine.PlaySound(SoundID.Item33, NPC.Center);
            }

            if (AttackTimer % 240 == 120 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 5f;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                    ModContent.ProjectileType<GodEyeLaser>(), GetModifiedDamage(baseDamage / 2), 0, Main.myPlayer, 1f, target.whoAmI);
            }

            if (eyeWallCooldown <= 0 && SpecialTimer % 600 == 0)
            { eyeWallCooldown = 300; SpawnEyeWall(target); SpawnEyeWall(target, true); }

            if (blackHoleCooldown <= 0 && GlitchTimer % 960 == 480 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                blackHoleCooldown = 540;
                Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    target.Center + Main.rand.NextVector2Circular(300, 300),
                    Vector2.Zero, ModContent.ProjectileType<GodEyeBlackHole>(), GetModifiedDamage(baseDamage), 0, Main.myPlayer, NPC.whoAmI);
            }

            if (AttackTimer % 420 == 210) GodPower_CrossAttack(target);
            if (AttackTimer % 900 == 600) StartBarrage(target);

            if (Main.rand.NextBool(900)) GodPower_GravityFlip(target);
            if (Main.rand.NextBool(1200)) GodPower_TeleportPlayer(target);
            if (SpecialTimer % 600 == 300) GodPower_ProjectileRain(target);
            if (Main.rand.NextBool(1200)) GodPower_Blind(target);
            if (Main.rand.NextBool(900)) GodPower_ZoomGlitch(target);
            if (GlitchTimer % 900 == 0) GodPower_ClosingRing(target);
            if (Main.rand.NextBool(600)) GodPower_EyeQuest();
            if (Main.rand.NextBool(100)) NPC.position += Main.rand.NextVector2Circular(15, 15);
        }

        private void Phase4AI(Player target)
        {
            AttackTimer++;
            SpecialTimer++;
            GlitchTimer++;

            if (!fakeDeathTriggered && NPC.life < trueMaxLife * 0.05f)
            {
                fakeDeathTriggered = true;
                fakeDeathTimer = 180;
                NPC.dontTakeDamage = true;
                NPC.velocity = Vector2.Zero;
                if (Main.netMode != NetmodeID.Server)
                    Main.NewText("The GOD has been defeated!", Color.Yellow);
            }

            if (fakeDeathTimer > 0)
            {
                fakeDeathTimer--;
                NPC.velocity = Vector2.Zero;
                NPC.scale = MathHelper.Lerp(0.3f, 1f, (180 - fakeDeathTimer) / 180f);
                NPC.Opacity = Main.rand.NextFloat(0.3f, 1f);
                if (fakeDeathTimer == 60 && Main.netMode != NetmodeID.Server)
                    Main.NewText("...", Color.White);
                if (fakeDeathTimer <= 0)
                {
                    NPC.dontTakeDamage = false;
                    NPC.Opacity = 1f;
                    NPC.scale = 1.2f;
                    NPC.life = Math.Min(NPC.life + (int)(trueMaxLife * 0.15f), trueMaxLife);
                    trueMaxLife += 80000;
                    NPC.lifeMax = trueMaxLife;
                    if (Main.netMode != NetmodeID.Server)
                        Main.NewText("D̶̛̫I̶̛̫D̶̛̫ ̶̛̫Y̶̛̫Ơ̶̫Ư̶̫ ̶̛̫T̶̛̫H̶̛̫I̶̛̫N̶̛̫K̶̛̫?̶̛̫", Color.Red);
                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        for (int i = 0; i < 12; i++)
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center,
                                (MathHelper.TwoPi / 12f * i).ToRotationVector2() * 8f,
                                ModContent.ProjectileType<GodEyeLaser>(), GetModifiedDamage(baseDamage), 0, Main.myPlayer);
                    rageModeTimer = 480;
                }
                return;
            }

            if (rageModeTimer > 0)
            {
                rageModeTimer--;
                NPC.scale = 1.2f + (float)Math.Sin(rageModeTimer * 0.3f) * 0.06f;
            }

            if (hpGlitchCooldown <= 0)
            {
                hpGlitchCooldown = 45;
                if (Main.rand.NextBool(6))
                {
                    trueMaxLife += Main.rand.Next(3000, 12000);
                    NPC.lifeMax = trueMaxLife;
                    NPC.life = Math.Min(NPC.life + Main.rand.Next(1000, 6000), trueMaxLife);
                }
                displayedMaxLife = trueMaxLife + Main.rand.Next(-200000, 400000);
                NPC.lifeMax = Math.Max(NPC.life + 1, displayedMaxLife);
            }

            OrbitPlayer(target, 1000f, 0.018f);

            if (teleportCooldown <= 0 && Main.rand.NextBool(120))
            { teleportCooldown = 60; GlitchTeleportFar(target); }

            if (AttackTimer % 240 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                spiralAngle += 10;
                for (int burst = 0; burst < 6; burst++)
                    for (int s = 0; s < 4; s++)
                    {
                        float angle = MathHelper.ToRadians(spiralAngle + burst * 15 + 90 * s);
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center,
                            angle.ToRotationVector2() * 6f,
                            ModContent.ProjectileType<GodEyeLaser>(), GetModifiedDamage(baseDamage / 3), 0, Main.myPlayer);
                    }
            }

            if (AttackTimer % 300 == 150 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 8f;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                    ModContent.ProjectileType<GodEyeLaser>(), GetModifiedDamage(baseDamage / 2), 0, Main.myPlayer, 2f, 0f);
            }

            if (eyeWallCooldown <= 0 && AttackTimer % 480 == 0)
            {
                eyeWallCooldown = 240;
                SpawnEyeWall(target); SpawnEyeWall(target, true); SpawnEyeWallVertical(target);
            }

            if (blackHoleCooldown <= 0 && AttackTimer % 600 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                blackHoleCooldown = 360;
                Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    target.Center + Main.rand.NextVector2Circular(350, 350),
                    Vector2.Zero, ModContent.ProjectileType<GodEyeBlackHole>(), GetModifiedDamage(baseDamage), 0, Main.myPlayer, NPC.whoAmI);
            }

            if (AttackTimer % 300 == 150) GodPower_CrossAttack(target);
            if (AttackTimer % 600 == 400) StartBarrage(target);

            if (Main.rand.NextBool(720)) GodPower_GravityFlip(target);
            if (Main.rand.NextBool(900)) GodPower_TeleportPlayer(target);
            if (SpecialTimer % 600 == 270) GodPower_ProjectileRain(target);
            if (GlitchTimer % 720 == 360) GodPower_MazeWalls(target);
            if (Main.rand.NextBool(1500)) GodPower_Blind(target);
            if (Main.rand.NextBool(720)) GodPower_ZoomGlitch(target);
            if (GlitchTimer % 780 == 0) GodPower_ClosingRing(target);

            if (Main.rand.NextBool(480)) GodPower_EyeQuest();
            if (Main.rand.NextBool(50)) NPC.position += Main.rand.NextVector2Circular(25, 25);
        }

        #endregion

        #region Teleports & Walls

        private void GlitchTeleport(Player t)
        {
            TeleportEffect();
            float teleportRadius = 800f;
            if (questBuffActive)
            {
                if (questIsWeakened) teleportRadius = 1100f;
                else teleportRadius = 600f;
            }
            NPC.Center = t.Center + Main.rand.NextVector2CircularEdge(teleportRadius, teleportRadius);
            NPC.velocity = Vector2.Zero;
            TeleportEffect();
            SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
            NPC.netUpdate = true;
        }

        private void GlitchTeleportFar(Player t)
        {
            TeleportEffect();
            float teleportRadius = 1000f;
            if (questBuffActive)
            {
                if (questIsWeakened) teleportRadius = 1400f;
                else teleportRadius = 750f;
            }
            NPC.Center = t.Center + Main.rand.NextVector2CircularEdge(teleportRadius, teleportRadius);
            NPC.velocity = Vector2.Zero;
            TeleportEffect();
            SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
            NPC.netUpdate = true;
        }

        private void TeleportEffect()
        {
            for (int i = 0; i < 15; i++)
            {
                Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height,
                    DustID.Vortex, 0, 0, 100, default, 2f);
                d.noGravity = true;
                d.velocity = Main.rand.NextVector2Circular(8, 8);
            }
        }

        private void SpawnEyeWall(Player t, bool right = false)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            float xO = right ? 1200 : -1200;
            float xV = right ? -5f : 5f;
            int totalSegments = 8;
            int gapCenter = Main.rand.Next(2, totalSegments - 2);
            for (int i = 0; i < totalSegments; i++)
            {
                if (i >= gapCenter - 1 && i <= gapCenter + 1) continue;
                float yPos = t.Center.Y - 400 + i * 100;
                Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    new Vector2(t.Center.X + xO, yPos), Vector2.Zero,
                    ModContent.ProjectileType<GodEyeWallProjectile>(), GetModifiedDamage(baseDamage / 2), 0, Main.myPlayer, xV, 0f);
            }
        }

        private void SpawnEyeWallVertical(Player t)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            int totalSegments = 8;
            int gapCenter = Main.rand.Next(2, totalSegments - 2);
            for (int i = 0; i < totalSegments; i++)
            {
                if (i >= gapCenter - 1 && i <= gapCenter + 1) continue;
                float xPos = t.Center.X - 400 + i * 100;
                Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    new Vector2(xPos, t.Center.Y - 1200), Vector2.Zero,
                    ModContent.ProjectileType<GodEyeWallProjectile>(), GetModifiedDamage(baseDamage / 2), 0, Main.myPlayer, 0f, 5f);
            }
        }

        #endregion

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;
            int speed = barrageActive ? 2 : (CurrentPhase >= Phase.Phase3 ? 4 : 6);
            if (NPC.frameCounter >= speed)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y >= Main.npcFrameCount[NPC.type] * frameHeight)
                    NPC.frame.Y = 0;
            }
        }

        public override bool PreDraw(SpriteBatch sb, Vector2 sp, Color dc)
        {
            Texture2D tex = Terraria.GameContent.TextureAssets.Npc[NPC.type].Value;
            int fh = tex.Height / Main.npcFrameCount[NPC.type];
            Vector2 orig = new Vector2(tex.Width / 2f, fh / 2f);

            if (CurrentPhase >= Phase.Phase2)
                for (int i = 0; i < afterimagePositions.Count; i++)
                {
                    float op = (1f - (float)i / afterimagePositions.Count) * (barrageActive ? 0.5f : 0.3f);
                    Vector2 go = CurrentPhase >= Phase.Phase3
                        ? new Vector2(Main.rand.Next(-4, 4), Main.rand.Next(-4, 4)) : Vector2.Zero;
                    sb.Draw(tex, afterimagePositions[i] - sp + go, NPC.frame, dc * op,
                        NPC.rotation, orig, NPC.scale, SpriteEffects.None, 0f);
                }

            if (CurrentPhase >= Phase.Phase2 && Main.rand.NextBool(CurrentPhase >= Phase.Phase4 ? 3 : 5))
            {
                float sa = barrageActive ? 8f : (CurrentPhase >= Phase.Phase4 ? 6f : (CurrentPhase >= Phase.Phase3 ? 3f : 1.5f));
                sb.Draw(tex, NPC.Center - sp + new Vector2(Main.rand.NextFloat(-sa, sa), Main.rand.NextFloat(-sa, sa)),
                    NPC.frame, Color.Red * 0.25f, NPC.rotation, orig, NPC.scale, SpriteEffects.None, 0f);
                sb.Draw(tex, NPC.Center - sp + new Vector2(Main.rand.NextFloat(-sa, sa), Main.rand.NextFloat(-sa, sa)),
                    NPC.frame, Color.Blue * 0.25f, NPC.rotation, orig, NPC.scale, SpriteEffects.None, 0f);
            }

            if (crossCharging)
            {
                Color chargeGlow = Color.Red * 0.4f * (float)Math.Sin(Main.GameUpdateCount * 0.2f);
                sb.Draw(tex, NPC.Center - sp, NPC.frame, chargeGlow,
                    NPC.rotation, orig, NPC.scale * 1.1f, SpriteEffects.None, 0f);
            }

            if (barrageActive)
            {
                Color barrageGlow = Color.White * 0.3f * ((float)Math.Sin(Main.GameUpdateCount * 0.3f) * 0.3f + 0.7f);
                sb.Draw(tex, NPC.Center - sp, NPC.frame, barrageGlow,
                    NPC.rotation, orig, NPC.scale * 1.15f, SpriteEffects.None, 0f);
                sb.Draw(tex, NPC.Center - sp, NPC.frame, Color.Red * 0.2f,
                    NPC.rotation, orig, NPC.scale * 1.25f, SpriteEffects.None, 0f);
            }

            Color mc = dc;
            if (CurrentPhase >= Phase.Phase3 && Main.rand.NextBool(15))
                mc = new Color(Main.rand.Next(256), Main.rand.Next(256), Main.rand.Next(256));
            Vector2 mgo = CurrentPhase >= Phase.Phase3 && Main.rand.NextBool(12)
                ? new Vector2(Main.rand.Next(-10, 10), Main.rand.Next(-10, 10)) : Vector2.Zero;
            if (barrageActive && Main.rand.NextBool(4))
                mgo += Main.rand.NextVector2Circular(6, 6);

            sb.Draw(tex, NPC.Center - sp + mgo, NPC.frame, mc, NPC.rotation, orig, NPC.scale, SpriteEffects.None, 0f);

            if (CurrentPhase >= Phase.Phase2)
            {
                Color gc = (CurrentPhase >= Phase.Phase4 ? Color.Red : Color.Purple)
                    * ((float)Math.Sin(Main.GameUpdateCount * 0.05f) * 0.2f + 0.4f) * 0.3f;
                for (int i = 0; i < 3; i++)
                    sb.Draw(tex, NPC.Center - sp + new Vector2(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2)),
                        NPC.frame, gc, NPC.rotation, orig, NPC.scale * 1.04f, SpriteEffects.None, 0f);
            }

            if (questBuffActive && questEffectVisualTimer > 0)
            {
                float visualFade = Math.Min(1f, questEffectVisualTimer / 60f);

                if (questIsWeakened)
                {
                    float weakAlpha = ((float)Math.Sin(Main.GameUpdateCount * 0.1f) * 0.15f + 0.2f) * visualFade;
                    Color weakColor = Color.Cyan * weakAlpha;
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 weakOff = Main.rand.NextVector2Circular(3, 3);
                        sb.Draw(tex, NPC.Center - sp + weakOff, NPC.frame, weakColor,
                            NPC.rotation, orig, NPC.scale * 1.08f, SpriteEffects.None, 0f);
                    }

                    if (Main.rand.NextBool(6))
                    {
                        sb.Draw(tex, NPC.Center - sp + new Vector2(Main.rand.NextFloat(-4, 4), Main.rand.NextFloat(-4, 4)),
                            NPC.frame, Color.White * 0.3f * visualFade,
                            NPC.rotation + Main.rand.NextFloat(-0.05f, 0.05f), orig,
                            NPC.scale * Main.rand.NextFloat(0.95f, 1.05f), SpriteEffects.None, 0f);
                    }
                }
                else
                {
                    float rageAlpha = ((float)Math.Sin(Main.GameUpdateCount * 0.2f) * 0.2f + 0.3f) * visualFade;
                    Color rageColor = Color.OrangeRed * rageAlpha;
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 rageOff = Main.rand.NextVector2Circular(4, 4);
                        sb.Draw(tex, NPC.Center - sp + rageOff, NPC.frame, rageColor,
                            NPC.rotation, orig, NPC.scale * (1.1f + i * 0.03f), SpriteEffects.None, 0f);
                    }

                    if (Main.rand.NextBool(4))
                    {
                        sb.Draw(tex, NPC.Center - sp + Main.rand.NextVector2Circular(8, 8),
                            NPC.frame, Color.Yellow * 0.15f * visualFade,
                            NPC.rotation, orig, NPC.scale * 1.2f, SpriteEffects.None, 0f);
                    }
                }
            }

            return false;
        }

        public override bool CheckDead()
        {
            if (!GodEyeGlitchSystem.DeathChaosActive)
            {
                NPC.life = 1;
                NPC.dontTakeDamage = true;
                NPC.velocity = Vector2.Zero;
                barrageActive = false;
                crossCharging = false;
                crossRetreating = false;

                for (int i = 0; i < Main.maxProjectiles; i++)
                    if (Main.projectile[i].active && Main.projectile[i].hostile)
                        Main.projectile[i].Kill();

                GodEyeGlitchSystems.StartDeathChaos(NPC.Center);

                SoundEngine.PlaySound(SoundID.Item14, NPC.Center);
                for (int d = 0; d < 60; d++)
                {
                    Dust dust = Dust.NewDustDirect(NPC.Center, 1, 1, DustID.Shadowflame,
                        0, 0, 200, default, 3f);
                    dust.noGravity = true;
                    dust.velocity = Main.rand.NextVector2Circular(20, 20);
                }
                return false;
            }
            return true;
        }

        public override void OnKill() { }
        public override bool CheckActive() => false;

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }
    }
}