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
using Terraria.UI;

namespace GodEyeMod.Content.NPCs.GodEye
{
    public enum GodQuestType
    {
        ClickTheEye,
        StandStill,
        PrayToMe,
        DontAttack,
        Obey,
        Scream,
    }

    public class GodEyeQuestSystem : ModSystem
    {
        public static bool QuestActive = false;
        public static GodQuestType CurrentQuest;
        public static int QuestTimer = 0;
        public static int QuestMaxTime = 0;
        public static bool QuestFailed = false;
        public static bool QuestSuccess = false;
        public static int ResultTimer = 0;
        public static string QuestText = "";
        public static string QuestSubText = "";
        public static Color QuestColor = Color.Red;

        private static bool warningActive = false;
        private static int warningTimer = 0;
        private static int warningMaxTime = 180;
        private static string warningQuestHint = "";

        private static float blackOverlayOpacity = 0f;
        private static float eyePulse = 0f;
        private static float glitchIntensity = 0f;
        private static float screenDistortion = 0f;
        private static int screenFlashTimer = 0;
        private static Color screenFlashColor = Color.White;
        private static List<ScreenEye> screenEyes = new List<ScreenEye>();
        private static List<GlitchText> glitchTexts = new List<GlitchText>();
        private static float fakeStaticNoise = 0f;
        private static int jumpscareCooldown = 0;
        private static float heartbeatTimer = 0f;
        private static bool heartbeatActive = false;

        private static Vector2 eyeClickPosition = Vector2.Zero;
        private static float eyeClickSize = 80f;
        private static float standStillAccumulator = 0f;
        private static float standStillRequired = 180f;
        private static float prayAccumulator = 0f;
        private static bool playerAttacked = false;
        private static float dontAttackAccumulator = 0f;
        private static float obeyAccumulator = 0f;
        private static float screamAccumulator = 0f;
        private static int screamJumps = 0;
        private static float screamDistance = 0f;
        private static Vector2 screamLastPos = Vector2.Zero;

        private static Vector2 playerLastPos = Vector2.Zero;
        private static bool wasMouseLeft = false;
        private static int questCooldown = 0;

        private static int successStreak = 0;
        private static int failStreak = 0;
        private static int totalCompleted = 0;
        private static int totalFailed = 0;

        private static float warningPulse = 0f;
        private static float warningShake = 0f;

        private static string[] bgMessages = new string[]
        {
            "I SEE THROUGH YOUR SCREEN", "YOUR MOUSE BELONGS TO ME",
            "ALT+F4 WON'T SAVE YOU", "I'M IN YOUR SAVE FILE",
            "CHECK BEHIND YOU", "YOUR WEBCAM IS ON",
            "I KNOW YOUR NAME", "TASK MANAGER CAN'T HELP",
            "I'M NOT IN THE GAME", "I'M IN YOUR HEAD",
            "THIS ISN'T A GAME ANYMORE", "DO YOU HEAR IT?",
            "THE WALLS ARE WATCHING", "YOUR PC TEMPERATURE: 6̶6̸6̵°",
            "RAM USAGE: Y̶O̵U̸R̷ ̶S̸O̵U̸L̶", "PROCESS: fear.exe RUNNING",
            "FIREWALL: BREACHED", "ANTIVIRUS: DISABLED",
            "SAVING SCREENSHOT...", "UPLOADING YOUR FACE...",
            "CONNECTING TO: H̸̛̻E̵̢̛L̸̨̛L̸̢̛", "PING: ∞",
            "PACKETS LOST: YOUR SANITY",
            "C:\\Users\\YOU\\Desktop\\secrets.txt FOUND",
        };

        private static string[] warningMessages = new string[]
        {
            "PREPARING TEST...", "YOUR OBEDIENCE WILL BE TESTED",
            "A TRIAL APPROACHES", "DO NOT RESIST",
            "COMPLIANCE CHECK INCOMING", "STAND BY FOR ORDERS",
            "INITIATING PROTOCOL...", "I HAVE A TASK FOR YOU",
        };

        private class ScreenEye
        {
            public Vector2 Position;
            public float Size;
            public float Rotation;
            public float Pulse;
            public float Opacity;
            public bool IsReal;
            public bool IsFake;
            public int Index;
            public Color Tint;
            public float GlitchOffset;
            public bool Clicked;
            public float LifeTimer;
            public bool NumberVisible;

            public ScreenEye()
            {
                Pulse = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Opacity = 1f;
                Tint = Color.Red;
                GlitchOffset = 0f;
                Clicked = false;
                LifeTimer = 0f;
                NumberVisible = false;
            }
        }

        private class GlitchText
        {
            public Vector2 Position;
            public string Text;
            public Color TextColor;
            public float Scale;
            public float Life;
            public float MaxLife;
            public float Velocity;

            public GlitchText(string text, Vector2 pos, Color color, float scale = 1f, float life = 60f)
            {
                Text = text; Position = pos; TextColor = color;
                Scale = scale; Life = life; MaxLife = life;
                Velocity = Main.rand.NextFloat(-2f, 2f);
            }
        }

        public static bool IsBossPaused()
        {
            return warningActive || (QuestActive && !QuestFailed && !QuestSuccess);
        }

        public static void StartQuest()
        {
            if (QuestActive || questCooldown > 0 || warningActive) return;

            bool bossAlive = false;
            for (int i = 0; i < Main.maxNPCs; i++)
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<GodEyeBoss>())
                { bossAlive = true; break; }
            if (!bossAlive) return;

            Array values = Enum.GetValues(typeof(GodQuestType));
            CurrentQuest = (GodQuestType)values.GetValue(Main.rand.Next(values.Length));

            warningQuestHint = GetQuestHint(CurrentQuest);

            warningActive = true;
            warningTimer = warningMaxTime;
            warningPulse = 0f;
            warningShake = 0f;

            PauseBoss();

            SoundEngine.PlaySound(SoundID.Item4, Main.LocalPlayer.Center);

            if (Main.netMode != NetmodeID.Server)
            {
                string streakInfo = "";
                if (successStreak > 0)
                    streakInfo = $" [Win streak: {successStreak}]";
                else if (failStreak > 0)
                    streakInfo = $" [Fail streak: {failStreak}]";

                Main.NewText("══════════════════════════════", Color.Yellow);
                Main.NewText($"  ⚠ {warningMessages[Main.rand.Next(warningMessages.Length)]}{streakInfo}", Color.Yellow);
                Main.NewText($"  → {warningQuestHint}", Color.Orange);
                Main.NewText("══════════════════════════════", Color.Yellow);
            }
        }

        private static string GetQuestHint(GodQuestType quest)
        {
            return quest switch
            {
                GodQuestType.ClickTheEye => "You will need to CLICK something on screen",
                GodQuestType.StandStill => "You will need to STOP MOVING",
                GodQuestType.PrayToMe => "You will need to CROUCH and stay still",
                GodQuestType.DontAttack => "You will need to STOP ATTACKING",
                GodQuestType.Obey => "You will need to DO NOTHING at all",
                GodQuestType.Scream => "You will need to MOVE and JUMP like crazy",
                _ => "PREPARE YOURSELF"
            };
        }

        private static void ActuallyStartQuest()
        {
            QuestActive = true;
            QuestFailed = false;
            QuestSuccess = false;
            ResultTimer = 0;
            blackOverlayOpacity = 0f;
            glitchIntensity = 0f;
            screenDistortion = 0f;
            eyePulse = 0f;
            screenEyes.Clear();
            glitchTexts.Clear();
            heartbeatActive = true;
            heartbeatTimer = 0f;
            fakeStaticNoise = 0f;
            playerAttacked = false;

            Player player = Main.LocalPlayer;
            playerLastPos = player.Center;

            SetupQuest(player);

            SoundEngine.PlaySound(SoundID.Zombie104, player.Center);
        }

        private static void PauseBoss()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.type == ModContent.NPCType<GodEyeBoss>())
                {
                    npc.dontTakeDamage = true;
                    npc.damage = 0;
                    npc.velocity *= 0.1f;
                    break;
                }
            }

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].hostile)
                    Main.projectile[i].Kill();
            }
        }

        private static void ResumeBoss()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.type == ModContent.NPCType<GodEyeBoss>())
                {
                    npc.dontTakeDamage = false;
                    npc.damage = npc.defDamage;
                    break;
                }
            }
        }

        private static void SetupQuest(Player player)
        {
            switch (CurrentQuest)
            {
                case GodQuestType.ClickTheEye:
                    QuestText = "CLICK THE EYE";
                    QuestSubText = "before it's too late";
                    QuestMaxTime = 300;
                    QuestColor = Color.Red;
                    eyeClickPosition = new Vector2(
                        Main.rand.Next(150, Main.screenWidth - 150),
                        Main.rand.Next(150, Main.screenHeight - 150));
                    eyeClickSize = 70f;
                    screenEyes.Add(new ScreenEye
                    {
                        Position = eyeClickPosition,
                        Size = eyeClickSize,
                        IsReal = true,
                        Index = 0,
                        Tint = Color.Red
                    });
                    break;

                case GodQuestType.StandStill:
                    QuestText = "D̵O̷N̸'̴T̶ ̸M̵O̷V̵E̴";
                    QuestSubText = "I'm counting...";
                    QuestMaxTime = 360;
                    QuestColor = Color.Cyan;
                    standStillAccumulator = 0f;
                    standStillRequired = 180f;
                    break;

                case GodQuestType.PrayToMe:
                    QuestText = "K̸N̷E̵E̸L̸ ̴B̸E̷F̵O̶R̷E̸ ̷M̴E̶";
                    QuestSubText = "Hold DOWN and don't move (3s)";
                    QuestMaxTime = 420;
                    QuestColor = Color.White;
                    prayAccumulator = 0f;
                    break;

                case GodQuestType.DontAttack:
                    QuestText = "PUT DOWN YOUR WEAPON";
                    QuestSubText = "Don't attack for 5 seconds";
                    QuestMaxTime = 420;
                    QuestColor = Color.Yellow;
                    playerAttacked = false;
                    dontAttackAccumulator = 0f;
                    break;

                case GodQuestType.Obey:
                    QuestText = "Ơ̸̧B̸̧̛Ȩ̸̛Y̸̧̛";
                    QuestSubText = "Do nothing. Absolutely nothing. (4s)";
                    QuestMaxTime = 360;
                    QuestColor = Color.White;
                    obeyAccumulator = 0f;
                    break;

                case GodQuestType.Scream:
                    QuestText = "S̶C̷R̸E̵A̶M̶";
                    QuestSubText = "JUMP! MOVE! GO CRAZY!";
                    QuestMaxTime = 300;
                    QuestColor = Color.HotPink;
                    screamAccumulator = 0f;
                    screamJumps = 0;
                    screamDistance = 0f;
                    screamLastPos = player.Center;
                    break;
            }

            QuestTimer = QuestMaxTime;

            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText("══════════════════════════════", QuestColor);
                Main.NewText($"  👁 {QuestText}", QuestColor);
                Main.NewText($"  {QuestSubText}", QuestColor * 0.7f);
                Main.NewText("══════════════════════════════", QuestColor);
            }
        }

        public override void PostUpdateEverything()
        {
            if (questCooldown > 0) questCooldown--;
            if (jumpscareCooldown > 0) jumpscareCooldown--;

            if (warningActive)
            {
                warningTimer--;
                warningPulse += 0.1f;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && npc.type == ModContent.NPCType<GodEyeBoss>())
                    {
                        npc.damage = 0;
                        npc.velocity *= 0.9f;
                        break;
                    }
                }

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active && Main.projectile[i].hostile)
                        Main.projectile[i].Kill();
                }

                float warningProgress = 1f - (float)warningTimer / warningMaxTime;
                warningShake = warningProgress * 4f;

                if (warningTimer % 30 == 0 && warningTimer > 0)
                    SoundEngine.PlaySound(SoundID.MenuTick, Main.LocalPlayer.Center);

                if (warningTimer == 60)
                    SoundEngine.PlaySound(SoundID.Item4 with { Pitch = 0.5f }, Main.LocalPlayer.Center);

                if (warningTimer <= 0)
                {
                    warningActive = false;
                    ActuallyStartQuest();
                }

                return;
            }

            if (!QuestActive) return;

            if (!QuestFailed && !QuestSuccess)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && npc.type == ModContent.NPCType<GodEyeBoss>())
                    {
                        npc.damage = 0;
                        npc.velocity *= 0.9f;
                        break;
                    }
                }

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active && Main.projectile[i].hostile)
                        Main.projectile[i].Kill();
                }
            }

            if (ResultTimer > 0)
            {
                ResultTimer--;
                blackOverlayOpacity = MathHelper.Lerp(blackOverlayOpacity, 0f, 0.04f);
                glitchIntensity = MathHelper.Lerp(glitchIntensity, 0f, 0.05f);
                if (ResultTimer <= 0)
                {
                    QuestActive = false;
                    blackOverlayOpacity = 0f;
                    glitchIntensity = 0f;
                    screenDistortion = 0f;
                    heartbeatActive = false;
                    fakeStaticNoise = 0f;
                    ResumeBoss();
                }
                return;
            }

            if (QuestFailed || QuestSuccess) return;

            QuestTimer--;
            eyePulse += 0.12f;

            float targetOverlay = MathHelper.Lerp(0.5f, 0.92f, 1f - (float)QuestTimer / QuestMaxTime);
            blackOverlayOpacity = MathHelper.Lerp(blackOverlayOpacity, targetOverlay, 0.06f);

            float timeRatio = 1f - (float)QuestTimer / QuestMaxTime;
            glitchIntensity = MathHelper.Lerp(0f, 1f, timeRatio);

            fakeStaticNoise = MathHelper.Lerp(fakeStaticNoise, timeRatio * 0.3f, 0.02f);

            if (heartbeatActive)
            {
                float heartRate = MathHelper.Lerp(0.02f, 0.08f, timeRatio);
                heartbeatTimer += heartRate;
                if (heartbeatTimer >= MathHelper.TwoPi) heartbeatTimer -= MathHelper.TwoPi;
            }

            if (Main.rand.NextBool((int)MathHelper.Lerp(120, 20, timeRatio)))
                SpawnBackgroundGlitchText();

            foreach (var eye in screenEyes)
            {
                eye.Pulse += 0.08f;
                if (glitchIntensity > 0.3f && Main.rand.NextBool(60))
                    eye.GlitchOffset = Main.rand.NextFloat(-8, 8) * glitchIntensity;
                else
                    eye.GlitchOffset = MathHelper.Lerp(eye.GlitchOffset, 0f, 0.1f);
                eye.LifeTimer++;
            }

            for (int i = glitchTexts.Count - 1; i >= 0; i--)
            {
                glitchTexts[i].Life--;
                glitchTexts[i].Position.X += glitchTexts[i].Velocity;
                if (glitchTexts[i].Life <= 0)
                    glitchTexts.RemoveAt(i);
            }

            Player player = Main.LocalPlayer;

            CheckQuestProgress(player);

            bool mouseClicked = Main.mouseLeft && !wasMouseLeft;
            if (mouseClicked)
                HandleClick(new Vector2(Main.mouseX, Main.mouseY));
            wasMouseLeft = Main.mouseLeft;

            if (QuestTimer <= 0 && !QuestSuccess && !QuestFailed)
                FailQuest(player);

            playerLastPos = player.Center;
        }

        private void CheckQuestProgress(Player player)
        {
            switch (CurrentQuest)
            {
                case GodQuestType.StandStill:
                    if (player.velocity.Length() < 0.15f && !player.controlLeft && !player.controlRight
                        && !player.controlUp && !player.controlDown)
                    {
                        standStillAccumulator++;
                        if (standStillAccumulator >= standStillRequired)
                            SuccessQuest(player);
                    }
                    else
                    {
                        standStillAccumulator = Math.Max(0, standStillAccumulator - 4f);
                        if (standStillAccumulator > 30 && Main.rand.NextBool(3))
                            SpawnBackgroundGlitchText();
                    }
                    QuestSubText = $"Stay still: {Math.Min(100, (int)(standStillAccumulator / standStillRequired * 100))}%";
                    break;

                case GodQuestType.PrayToMe:
                    if (player.velocity.Y == 0f && player.controlDown
                        && !player.controlLeft && !player.controlRight && !player.controlJump)
                    {
                        prayAccumulator++;
                        if (prayAccumulator >= 180f)
                            SuccessQuest(player);
                        if (Main.rand.NextBool(5))
                        {
                            Dust d = Dust.NewDustDirect(player.position, player.width, player.height,
                                DustID.Shadowflame, 0, -2, 100, default, 1.5f);
                            d.noGravity = true;
                        }
                    }
                    else
                    {
                        prayAccumulator = Math.Max(0, prayAccumulator - 3f);
                    }
                    QuestSubText = $"Kneel: {Math.Min(100, (int)(prayAccumulator / 180f * 100))}%";
                    break;

                case GodQuestType.DontAttack:
                    if (player.itemAnimation > 0 && player.HeldItem.damage > 0)
                    {
                        playerAttacked = true;
                        FailQuest(player);
                        if (Main.netMode != NetmodeID.Server)
                            Main.NewText("I SAID DON'T ATTACK!", Color.Red);
                        return;
                    }
                    dontAttackAccumulator++;
                    if (dontAttackAccumulator >= 300f)
                        SuccessQuest(player);
                    QuestSubText = $"Don't attack: {(int)(dontAttackAccumulator / 300f * 100)}%";
                    break;

                case GodQuestType.Obey:
                    bool isObedient = player.velocity.Length() < 0.2f
                        && player.itemAnimation == 0
                        && !player.controlJump
                        && !player.controlUseItem;
                    if (isObedient)
                    {
                        obeyAccumulator++;
                        if (obeyAccumulator >= 240f)
                            SuccessQuest(player);
                    }
                    else
                    {
                        obeyAccumulator = Math.Max(0, obeyAccumulator - 5f);
                        if (obeyAccumulator > 60 && Main.rand.NextBool(30))
                        {
                            if (Main.netMode != NetmodeID.Server)
                                Main.NewText("I SAID DO NOTHING", Color.Red);
                        }
                    }
                    QuestSubText = $"Obedience: {Math.Min(100, (int)(obeyAccumulator / 240f * 100))}%";
                    break;

                case GodQuestType.Scream:
                    float dist2 = Vector2.Distance(player.Center, screamLastPos);
                    screamDistance += dist2;
                    screamLastPos = player.Center;

                    if (player.justJumped)
                        screamJumps++;

                    screamAccumulator = (screamJumps * 30f) + (screamDistance * 0.1f);
                    if (screamAccumulator >= 300f)
                        SuccessQuest(player);

                    QuestSubText = $"Chaos: {Math.Min(100, (int)(screamAccumulator / 300f * 100))}% | Jumps: {screamJumps}";
                    break;
            }
        }

        private void HandleClick(Vector2 mousePos)
        {
            switch (CurrentQuest)
            {
                case GodQuestType.ClickTheEye:
                    foreach (var eye in screenEyes)
                    {
                        if (eye.IsReal && !eye.Clicked)
                        {
                            float dist = Vector2.Distance(mousePos, eye.Position);
                            if (dist <= eye.Size)
                            {
                                eye.Clicked = true;
                                SuccessQuest(Main.LocalPlayer);
                                return;
                            }
                        }
                    }
                    foreach (var eye in screenEyes)
                    {
                        if (eye.IsReal)
                        {
                            eye.Position = new Vector2(
                                Main.rand.Next(100, Main.screenWidth - 100),
                                Main.rand.Next(150, Main.screenHeight - 100));
                            eyeClickSize = Math.Max(30f, eyeClickSize - 5f);
                            eye.Size = eyeClickSize;
                            if (Main.netMode != NetmodeID.Server)
                                Main.NewText("MISSED. IT MOVED.", Color.Red);
                        }
                    }
                    break;
            }
        }

        private void SuccessQuest(Player player)
        {
            QuestSuccess = true;
            ResultTimer = 90;
            totalCompleted++;
            questCooldown = 600 + Main.rand.Next(600);
            heartbeatActive = false;
            screenFlashTimer = 20;
            screenFlashColor = Color.Green;

            failStreak = 0;
            successStreak++;

            SoundEngine.PlaySound(SoundID.Item4, player.Center);

            string[] msgs = successStreak switch
            {
                1 => new[] { "...acceptable.", "You obeyed. Good worm.", "OBEDIENCE_SCORE++" },
                2 => new[] { "Again? Interesting.", "Your compliance is noted.", "REWARD: less suffering" },
                3 => new[] { "Three in a row... suspicious.", "Are you... learning?", "STREAK_BONUS: applied" },
                4 => new[] { "Impressive obedience.", "You're almost... useful.", "COMPLIANCE: HIGH" },
                _ => new[] { "...remarkable.", "OBEDIENCE_LEVEL: MAXIMUM", "I'm almost... impressed." }
            };

            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText(msgs[Main.rand.Next(msgs.Length)], Color.Lime);
                if (successStreak >= 2)
                    Main.NewText($"[Win streak: {successStreak} — Boss weakened more!]", Color.Cyan);
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.type == ModContent.NPCType<GodEyeBoss>())
                {
                    npc.life = Math.Min(npc.life + 1000, npc.lifeMax);

                    (npc.ModNPC as GodEyeBoss)?.ApplyQuestResult(true, successStreak);
                    break;
                }
            }
        }

        private void FailQuest(Player player)
        {
            QuestFailed = true;
            ResultTimer = 120;
            totalFailed++;
            questCooldown = 300 + Main.rand.Next(300);
            heartbeatActive = false;
            screenFlashTimer = 40;
            screenFlashColor = Color.Red;

            successStreak = 0;
            failStreak++;

            SoundEngine.PlaySound(SoundID.NPCDeath10, player.Center);

            string[] msgs = failStreak switch
            {
                1 => new[] { "P̸̛A̸̛T̸̛H̸̛E̸̛T̸̛I̸̛C̸̛", "YOU FAILED ME", "PUNISHMENT PROTOCOL ACTIVATED" },
                2 => new[] { "AGAIN?!", "TWICE NOW", "YOUR INCOMPETENCE GROWS", "EXECUTING punishment_v2.exe" },
                3 => new[] { "THREE FAILURES", "I'M GETTING STRONGER", "DO YOU EVEN TRY?", "RAGE_LEVEL: HIGH" },
                4 => new[] { "FOUR! AHAHAHAHA", "UNLIMITED POWER", "YOUR PAIN FEEDS ME ENDLESSLY" },
                _ => new[] { "Ÿ̸̛Ơ̸Ư̸ ̸̛A̸̛R̸̛E̸̛ ̸̛P̸̛A̸̛T̸̛H̸̛E̸̛T̸̛I̸̛C̸̛", "G̸̛Ơ̸D̸̛_̸̛M̸̛Ơ̸D̸̛E̸̛: ACTIVATED", "I AM INVINCIBLE" }
            };

            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText(msgs[Main.rand.Next(msgs.Length)], Color.Red);
                if (failStreak >= 2)
                    Main.NewText($"[Fail streak: {failStreak} — Boss enraged more!]", Color.OrangeRed);
            }

            int severity = Math.Min(failStreak, 5);

            player.AddBuff(BuffID.Slow, 120 + severity * 40);
            player.AddBuff(BuffID.Weak, 120 + severity * 40);
            player.AddBuff(BuffID.BrokenArmor, 120 + severity * 40);
            player.AddBuff(BuffID.Bleeding, 120 + severity * 40);
            player.AddBuff(BuffID.Darkness, 180 + severity * 40);

            player.Hurt(PlayerDeathReason.ByCustomReason(
                player.name + " failed The GOD's test"), 30 + severity * 15, 0);

            if (severity >= 2)
            {
                player.AddBuff(BuffID.Cursed, 40 + severity * 20);
                player.AddBuff(BuffID.Silenced, 80);
            }
            if (severity >= 3)
            {
                player.AddBuff(BuffID.Frozen, 40);
                player.AddBuff(BuffID.Blackout, 200);
                if (Main.netMode != NetmodeID.Server)
                    Main.GameZoomTarget = Main.rand.NextFloat(0.5f, 1.8f);
            }
            if (severity >= 4)
            {
                player.AddBuff(BuffID.Obstructed, 120);
                player.gravDir *= -1f;
            }
            if (severity >= 5)
            {
                for (int i = 0; i < Player.MaxBuffs; i++)
                {
                    if (player.buffType[i] > 0 && !Main.debuff[player.buffType[i]])
                    { player.DelBuff(i); i--; }
                }
            }

            for (int j = 0; j < Main.maxNPCs; j++)
            {
                if (Main.npc[j].active && Main.npc[j].type == ModContent.NPCType<GodEyeBoss>())
                {
                    int healAmount = 20000 + failStreak * 8000;
                    Main.npc[j].life = Math.Min(Main.npc[j].life + healAmount, Main.npc[j].lifeMax);

                    (Main.npc[j].ModNPC as GodEyeBoss)?.ApplyQuestResult(false, failStreak);
                    break;
                }
            }
        }

        private void SpawnBackgroundGlitchText()
        {
            if (glitchTexts.Count > 60) return;
            string text = bgMessages[Main.rand.Next(bgMessages.Length)];
            Vector2 pos = new Vector2(
                Main.rand.Next(20, Main.screenWidth - 20),
                Main.rand.Next(20, Main.screenHeight - 20));
            Color col = new Color(
                Main.rand.Next(100, 255),
                Main.rand.Next(0, 80),
                Main.rand.Next(0, 80)) * Main.rand.NextFloat(0.15f, 0.5f);
            glitchTexts.Add(new GlitchText(text, pos, col,
                Main.rand.NextFloat(0.4f, 1.2f),
                Main.rand.Next(30, 120)));
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (!QuestActive && !warningActive) return;

            layers.Add(new LegacyGameInterfaceLayer(
                "GodEyeMod: Quest Overlay",
                delegate
                {
                    if (warningActive)
                        DrawWarningOverlay(Main.spriteBatch);
                    else
                        DrawQuestOverlay(Main.spriteBatch);
                    return true;
                },
                InterfaceScaleType.None
            ));
        }

        private void DrawWarningOverlay(SpriteBatch sb)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            float progress = 1f - (float)warningTimer / warningMaxTime;

            float darkAlpha = MathHelper.Lerp(0.1f, 0.6f, progress);
            sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                Color.Black * darkAlpha);

            float borderPulse = (float)Math.Sin(warningPulse) * 0.3f + 0.7f;
            Color borderColor = Color.Yellow * borderPulse;
            int borderThickness = 4;

            sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, borderThickness), borderColor);
            sb.Draw(pixel, new Rectangle(0, Main.screenHeight - borderThickness, Main.screenWidth, borderThickness), borderColor);
            sb.Draw(pixel, new Rectangle(0, 0, borderThickness, Main.screenHeight), borderColor);
            sb.Draw(pixel, new Rectangle(Main.screenWidth - borderThickness, 0, borderThickness, Main.screenHeight), borderColor);

            int boxW = 600;
            int boxH = 200;
            int boxX = Main.screenWidth / 2 - boxW / 2;
            int boxY = Main.screenHeight / 2 - boxH / 2;

            if (warningShake > 0.5f)
            {
                boxX += (int)(Main.rand.NextFloat(-warningShake, warningShake));
                boxY += (int)(Main.rand.NextFloat(-warningShake, warningShake));
            }

            sb.Draw(pixel, new Rectangle(boxX, boxY, boxW, boxH), Color.Black * 0.85f);

            Color boxBorder = Color.Lerp(Color.Yellow, Color.Red, progress) * borderPulse;
            int brd = 3;
            sb.Draw(pixel, new Rectangle(boxX, boxY, boxW, brd), boxBorder);
            sb.Draw(pixel, new Rectangle(boxX, boxY + boxH - brd, boxW, brd), boxBorder);
            sb.Draw(pixel, new Rectangle(boxX, boxY, brd, boxH), boxBorder);
            sb.Draw(pixel, new Rectangle(boxX + boxW - brd, boxY, brd, boxH), boxBorder);

            string icon = "⚠";
            if (Main.GameUpdateCount % 20 < 10) icon = "👁";
            Utils.DrawBorderString(sb, icon,
                new Vector2(boxX + boxW / 2f - 15, boxY + 15), Color.Yellow, 2f);

            string warningText = "WARNING";
            if (progress > 0.7f && Main.rand.NextBool(4))
            {
                char[] c = warningText.ToCharArray();
                c[Main.rand.Next(c.Length)] = (char)Main.rand.Next(33, 126);
                warningText = new string(c);
            }

            Vector2 wSize = FontAssets.DeathText.Value.MeasureString(warningText);
            float wScale = 1.2f;

            if (progress > 0.5f)
            {
                float rgbOff = progress * 3f;
                Utils.DrawBorderString(sb, warningText,
                    new Vector2(boxX + boxW / 2f - wSize.X * wScale / 2f - rgbOff, boxY + 55),
                    Color.Red * 0.3f, wScale);
                Utils.DrawBorderString(sb, warningText,
                    new Vector2(boxX + boxW / 2f - wSize.X * wScale / 2f + rgbOff, boxY + 55),
                    Color.Blue * 0.3f, wScale);
            }

            Utils.DrawBorderString(sb, warningText,
                new Vector2(boxX + boxW / 2f - wSize.X * wScale / 2f, boxY + 55),
                Color.Lerp(Color.Yellow, Color.Red, progress), wScale);

            Utils.DrawBorderString(sb, warningQuestHint,
                new Vector2(boxX + 20, boxY + 100),
                Color.Orange * 0.9f, 0.9f);

            float secsLeft = warningTimer / 60f;
            string countText = $"Starting in: {secsLeft:F1}s";

            Color countColor = Color.White;
            if (secsLeft < 1f && Main.GameUpdateCount % 8 < 4)
                countColor = Color.Red;

            Utils.DrawBorderString(sb, countText,
                new Vector2(boxX + boxW / 2f - 80, boxY + 140),
                countColor, 1f);

            int barX = boxX + 20;
            int barY = boxY + boxH - 25;
            int barW = boxW - 40;
            int barH = 8;

            sb.Draw(pixel, new Rectangle(barX, barY, barW, barH), Color.Black * 0.5f);

            float barProgress = (float)warningTimer / warningMaxTime;
            Color barColor = Color.Lerp(Color.Red, Color.Yellow, barProgress);
            if (barProgress < 0.2f && Main.GameUpdateCount % 6 < 3)
                barColor = Color.White;

            sb.Draw(pixel, new Rectangle(barX, barY, (int)(barW * barProgress), barH), barColor);

            if (Main.GameUpdateCount % 30 < 15)
            {
                float sideAlpha = progress * 0.15f;
                sb.Draw(pixel, new Rectangle(0, 0, 30, Main.screenHeight), Color.Yellow * sideAlpha);
                sb.Draw(pixel, new Rectangle(Main.screenWidth - 30, 0, 30, Main.screenHeight), Color.Yellow * sideAlpha);
            }

            if (successStreak > 0 || failStreak > 0)
            {
                string streakText = successStreak > 0
                    ? $"Current win streak: {successStreak}"
                    : $"Current fail streak: {failStreak}";
                Color streakColor = successStreak > 0 ? Color.Cyan : Color.OrangeRed;
                Utils.DrawBorderString(sb, streakText,
                    new Vector2(boxX + boxW / 2f - 90, boxY + boxH + 10),
                    streakColor * 0.8f, 0.8f);
            }
        }

        private void DrawQuestOverlay(SpriteBatch sb)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            if (blackOverlayOpacity > 0.01f)
                sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                    Color.Black * blackOverlayOpacity);

            if (fakeStaticNoise > 0.01f)
                DrawStaticNoise(sb, pixel, fakeStaticNoise);

            if (glitchIntensity > 0.2f)
                DrawScanLines(sb, pixel);

            if (glitchIntensity > 0.4f)
                DrawEdgeDistortion(sb, pixel);

            if (screenFlashTimer > 0)
            {
                screenFlashTimer--;
                float flashAlpha = screenFlashTimer / 40f;
                sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                    screenFlashColor * flashAlpha * 0.5f);
            }

            if (heartbeatActive)
            {
                float beat = (float)Math.Sin(heartbeatTimer) * 0.5f + 0.5f;
                float vignetteStr = beat * 0.3f * glitchIntensity;
                DrawVignette(sb, pixel, vignetteStr);
            }

            if (QuestSuccess)
            {
                DrawSuccess(sb, pixel);
                return;
            }
            if (QuestFailed)
            {
                DrawFailure(sb, pixel);
                return;
            }

            foreach (var gt in glitchTexts)
            {
                float alpha = gt.Life / gt.MaxLife;
                Color c = gt.TextColor * alpha;
                Vector2 offset = Vector2.Zero;
                if (Main.rand.NextBool(20))
                    offset = new Vector2(Main.rand.Next(-10, 10), Main.rand.Next(-3, 3));
                Utils.DrawBorderString(sb, gt.Text, gt.Position + offset, c, gt.Scale);
            }

            DrawScreenEyes(sb);
            DrawQuestBox(sb, pixel);
            DrawCustomCursor(sb);
            DrawEyeHover(sb);
        }

        private void DrawStaticNoise(SpriteBatch sb, Texture2D pixel, float intensity)
        {
            for (int i = 0; i < (int)(intensity * 200); i++)
            {
                int x = Main.rand.Next(Main.screenWidth);
                int y = Main.rand.Next(Main.screenHeight);
                int w = Main.rand.Next(1, (int)(10 * intensity) + 2);
                int h = Main.rand.Next(1, 3);
                float a = Main.rand.NextFloat(0.05f, 0.2f) * intensity;
                sb.Draw(pixel, new Rectangle(x, y, w, h), Color.White * a);
            }
        }

        private void DrawScanLines(SpriteBatch sb, Texture2D pixel)
        {
            int scanOffset = (int)(Main.GameUpdateCount * 2) % 4;
            for (int y = scanOffset; y < Main.screenHeight; y += 4)
            {
                float a = 0.03f * glitchIntensity;
                sb.Draw(pixel, new Rectangle(0, y, Main.screenWidth, 1), Color.Black * a);
            }

            if (Main.rand.NextBool((int)(15 / Math.Max(glitchIntensity, 0.1f))))
            {
                int gy = Main.rand.Next(Main.screenHeight);
                int gh = Main.rand.Next(2, (int)(20 * glitchIntensity) + 3);
                int gxOff = Main.rand.Next(-30, 30);
                sb.Draw(pixel, new Rectangle(gxOff, gy, Main.screenWidth, gh),
                    new Color(Main.rand.Next(200, 255), Main.rand.Next(0, 50), Main.rand.Next(0, 50))
                    * 0.15f * glitchIntensity);
            }
        }

        private void DrawEdgeDistortion(SpriteBatch sb, Texture2D pixel)
        {
            float edgeSize = 40f * glitchIntensity;
            Color redTint = Color.Red * 0.1f * glitchIntensity;
            Color blueTint = Color.Blue * 0.1f * glitchIntensity;

            sb.Draw(pixel, new Rectangle(0, 0, (int)edgeSize, Main.screenHeight), redTint);
            sb.Draw(pixel, new Rectangle(Main.screenWidth - (int)edgeSize, 0, (int)edgeSize, Main.screenHeight), blueTint);
            sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, (int)edgeSize), redTint);
            sb.Draw(pixel, new Rectangle(0, Main.screenHeight - (int)edgeSize, Main.screenWidth, (int)edgeSize), blueTint);
        }

        private void DrawVignette(SpriteBatch sb, Texture2D pixel, float strength)
        {
            if (strength < 0.01f) return;
            int layers = 8;
            for (int i = 0; i < layers; i++)
            {
                float ratio = (float)i / layers;
                int margin = (int)(Math.Min(Main.screenWidth, Main.screenHeight) * 0.5f * (1f - ratio));
                float alpha = strength * ratio * 0.15f;
                sb.Draw(pixel, new Rectangle(0, 0, margin, Main.screenHeight), Color.Black * alpha);
                sb.Draw(pixel, new Rectangle(Main.screenWidth - margin, 0, margin, Main.screenHeight), Color.Black * alpha);
                sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, margin), Color.Black * alpha);
                sb.Draw(pixel, new Rectangle(0, Main.screenHeight - margin, Main.screenWidth, margin), Color.Black * alpha);
            }
        }

        private void DrawScreenEyes(SpriteBatch sb)
        {
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

            foreach (var eye in screenEyes)
            {
                if (eye.Clicked) continue;
                if (eye.IsFake && eye.Opacity < 0.1f) continue;

                Vector2 origin = new Vector2(eyeTex.Width / 2f, eyeTex.Height / 2f);
                float pulse = 1f + (float)Math.Sin(eye.Pulse) * 0.12f;
                float scale = (eye.Size * 2f / eyeTex.Width) * pulse;
                float rotation = (float)Math.Sin(eye.Pulse * 0.3f) * 0.15f;

                Vector2 glitchOff = new Vector2(eye.GlitchOffset, 0);
                Vector2 drawPos = eye.Position + glitchOff;

                Color tint = eye.Tint * eye.Opacity;
                if (eye.Clicked) tint = Color.Green;

                Color glowCol = Color.Lerp(tint, Color.Yellow,
                    (float)Math.Sin(eye.Pulse * 0.5f) * 0.3f + 0.3f) * 0.3f;
                for (int g = 0; g < 2; g++)
                {
                    Vector2 gOff = Main.rand.NextVector2Circular(3, 3);
                    sb.Draw(eyeTex, drawPos + gOff, null, glowCol, rotation, origin,
                        scale * 1.25f, SpriteEffects.None, 0f);
                }

                if (glitchIntensity > 0.3f)
                {
                    float rgbOff = 2f + glitchIntensity * 4f;
                    sb.Draw(eyeTex, drawPos + new Vector2(-rgbOff, 0), null, Color.Red * 0.3f,
                        rotation, origin, scale, SpriteEffects.None, 0f);
                    sb.Draw(eyeTex, drawPos + new Vector2(rgbOff, 0), null, Color.Blue * 0.3f,
                        rotation, origin, scale, SpriteEffects.None, 0f);
                }

                sb.Draw(eyeTex, drawPos, null, tint, rotation, origin, scale, SpriteEffects.None, 0f);
                sb.Draw(eyeTex, drawPos, null, Color.White * 0.15f, rotation, origin,
                    scale * 0.7f, SpriteEffects.None, 0f);
            }
        }

        private void DrawQuestBox(SpriteBatch sb, Texture2D pixel)
        {
            float timeRatio = (float)QuestTimer / QuestMaxTime;

            Vector2 boxGlitch = Vector2.Zero;
            if (glitchIntensity > 0.5f && Main.rand.NextBool(8))
                boxGlitch = new Vector2(Main.rand.Next(-15, 15), Main.rand.Next(-5, 5));

            int boxW = 550;
            int boxH = 100;
            int boxX = Main.screenWidth / 2 - boxW / 2 + (int)boxGlitch.X;
            int boxY = 50 + (int)boxGlitch.Y;

            Color bgCol = Color.Black * 0.85f;
            if (timeRatio < 0.3f)
            {
                float danger = (float)Math.Sin(Main.GameUpdateCount * 0.3f) * 0.5f + 0.5f;
                bgCol = Color.Lerp(Color.Black * 0.85f, Color.DarkRed * 0.85f, danger * (1f - timeRatio * 3f));
            }
            sb.Draw(pixel, new Rectangle(boxX, boxY, boxW, boxH), bgCol);

            Color borderCol = QuestColor;
            if (glitchIntensity > 0.6f && Main.rand.NextBool(4))
                borderCol = new Color(Main.rand.Next(256), Main.rand.Next(256), Main.rand.Next(256));

            int brd = 3;
            sb.Draw(pixel, new Rectangle(boxX, boxY, boxW, brd), borderCol);
            sb.Draw(pixel, new Rectangle(boxX, boxY + boxH - brd, boxW, brd), borderCol);
            sb.Draw(pixel, new Rectangle(boxX, boxY, brd, boxH), borderCol);
            sb.Draw(pixel, new Rectangle(boxX + boxW - brd, boxY, brd, boxH), borderCol);

            string eyeIcon = "👁";
            if (Main.rand.NextBool(30)) eyeIcon = Main.rand.NextBool() ? "◉" : "⊙";
            Utils.DrawBorderString(sb, eyeIcon, new Vector2(boxX + 20, boxY + 15), QuestColor, 1.5f);

            string questDisplay = QuestText;
            if (glitchIntensity > 0.5f && Main.rand.NextBool(6))
            {
                char[] c = questDisplay.ToCharArray();
                for (int i = 0; i < Math.Max(1, (int)(glitchIntensity * 3)); i++)
                {
                    int idx = Main.rand.Next(c.Length);
                    c[idx] = (char)Main.rand.Next(33, 126);
                }
                questDisplay = new string(c);
            }

            Vector2 qSize = FontAssets.DeathText.Value.MeasureString(questDisplay);
            float qScale = Math.Min(1f, (boxW - 80) / qSize.X);

            if (glitchIntensity > 0.3f)
            {
                float ca = glitchIntensity * 3f;
                Utils.DrawBorderString(sb, questDisplay,
                    new Vector2(boxX + boxW / 2f - qSize.X * qScale / 2f - ca, boxY + 15),
                    Color.Red * 0.4f, qScale);
                Utils.DrawBorderString(sb, questDisplay,
                    new Vector2(boxX + boxW / 2f - qSize.X * qScale / 2f + ca, boxY + 15),
                    Color.Blue * 0.4f, qScale);
            }

            Utils.DrawBorderString(sb, questDisplay,
                new Vector2(boxX + boxW / 2f - qSize.X * qScale / 2f, boxY + 15),
                QuestColor, qScale);

            if (!string.IsNullOrEmpty(QuestSubText))
            {
                Utils.DrawBorderString(sb, QuestSubText,
                    new Vector2(boxX + 55, boxY + 48),
                    QuestColor * 0.7f, 0.8f);
            }

            int barX = boxX + 10;
            int barY = boxY + boxH - 22;
            int barW = boxW - 20;
            int barH = 10;

            sb.Draw(pixel, new Rectangle(barX, barY, barW, barH), Color.Black * 0.5f);

            Color barCol = timeRatio > 0.5f ? Color.Lime :
                          (timeRatio > 0.25f ? Color.Yellow : Color.Red);
            if (timeRatio < 0.2f && Main.GameUpdateCount % 8 < 4)
                barCol = Color.White;

            int fillW = (int)(barW * timeRatio);

            if (glitchIntensity > 0.5f && Main.rand.NextBool(10))
            {
                int segments = Main.rand.Next(3, 6);
                for (int s = 0; s < segments; s++)
                {
                    int segW = fillW / segments;
                    int segX = barX + s * (barW / segments) + Main.rand.Next(-4, 4);
                    sb.Draw(pixel, new Rectangle(segX, barY + Main.rand.Next(-2, 2), segW, barH), barCol);
                }
            }
            else
            {
                sb.Draw(pixel, new Rectangle(barX, barY, fillW, barH), barCol);
            }

            float secsLeft = QuestTimer / 60f;
            string timerStr = $"{secsLeft:F1}s";
            if (secsLeft < 2f && Main.GameUpdateCount % 6 < 3)
                timerStr = $"!!{secsLeft:F1}s!!";

            Vector2 timerSize = FontAssets.MouseText.Value.MeasureString(timerStr);
            Utils.DrawBorderString(sb, timerStr,
                new Vector2(barX + barW / 2f - timerSize.X * 0.35f, barY - 2),
                Color.White, 0.7f);
        }

        private void DrawSuccess(SpriteBatch sb, Texture2D pixel)
        {
            float alpha = ResultTimer / 90f;

            sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                Color.Green * alpha * 0.25f);

            string successText = "ACCEPTED";
            if (Main.rand.NextBool(10)) successText = "A̸C̶C̵E̸P̷T̸E̶D̴";

            Vector2 size = FontAssets.DeathText.Value.MeasureString(successText);
            float textScale = 1.5f;

            Vector2 textPos = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f - 30);
            if (Main.rand.NextBool(8))
                textPos += Main.rand.NextVector2Circular(5, 5);

            Utils.DrawBorderString(sb, successText,
                textPos - size * textScale / 2f,
                Color.Lime * alpha, textScale);

            if (ResultTimer > 30)
            {
                string eyeStr = "👁 👁 👁";
                Utils.DrawBorderString(sb, eyeStr,
                    new Vector2(Main.screenWidth / 2f - 60, Main.screenHeight / 2f + 30),
                    Color.White * alpha * 0.7f, 1f);
            }

            if (ResultTimer > 20 && ResultTimer < 70)
            {
                string weakText = successStreak >= 2
                    ? $"BOSS WEAKENED x{successStreak}"
                    : "BOSS WEAKENED";
                Utils.DrawBorderString(sb, weakText,
                    new Vector2(Main.screenWidth / 2f - 100, Main.screenHeight / 2f + 60),
                    Color.Cyan * alpha * 0.8f, 0.9f);
            }
        }

        private void DrawFailure(SpriteBatch sb, Texture2D pixel)
        {
            float alpha = ResultTimer / 120f;

            float flashIntensity = (float)Math.Sin(ResultTimer * 0.3f) * 0.3f + 0.3f;
            sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                Color.Red * alpha * flashIntensity);

            for (int i = 0; i < (int)(alpha * 15); i++)
            {
                int gy = Main.rand.Next(Main.screenHeight);
                int gh = Main.rand.Next(5, 25);
                sb.Draw(pixel, new Rectangle(Main.rand.Next(-20, 0), gy, Main.screenWidth + 40, gh),
                    Color.Red * Main.rand.NextFloat(0.05f, 0.2f));
            }

            string failText = "F̸̛A̸̛I̸̛L̸̛E̸̛D̸̛";
            if (Main.rand.NextBool(5))
            {
                char[] c = failText.ToCharArray();
                c[Main.rand.Next(c.Length)] = (char)Main.rand.Next(33, 126);
                failText = new string(c);
            }

            Vector2 size = FontAssets.DeathText.Value.MeasureString(failText);
            float textScale = 2f;

            Vector2 textPos = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f - 40);
            textPos += Main.rand.NextVector2Circular(8 * alpha, 8 * alpha);

            float rgbOff = 5f * alpha;
            Utils.DrawBorderString(sb, failText,
                textPos - size * textScale / 2f + new Vector2(-rgbOff, 0),
                Color.Red * alpha * 0.5f, textScale);
            Utils.DrawBorderString(sb, failText,
                textPos - size * textScale / 2f + new Vector2(rgbOff, 0),
                Color.Blue * alpha * 0.5f, textScale);
            Utils.DrawBorderString(sb, failText,
                textPos - size * textScale / 2f,
                Color.Red * alpha, textScale);

            if (ResultTimer > 40 && Main.rand.NextBool(3))
            {
                string extraText = bgMessages[Main.rand.Next(bgMessages.Length)];
                Vector2 extraPos = new Vector2(
                    Main.rand.Next(50, Main.screenWidth - 50),
                    Main.rand.Next(50, Main.screenHeight - 50));
                Utils.DrawBorderString(sb, extraText, extraPos,
                    Color.Red * alpha * Main.rand.NextFloat(0.3f, 0.7f),
                    Main.rand.NextFloat(0.5f, 1.2f));
            }

            if (ResultTimer > 20 && Main.rand.NextBool(4))
            {
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
                Vector2 eyePos = new Vector2(
                    Main.rand.Next(100, Main.screenWidth - 100),
                    Main.rand.Next(100, Main.screenHeight - 100));
                sb.Draw(eyeTex, eyePos, null, Color.Red * alpha * 0.5f, Main.rand.NextFloat(-0.3f, 0.3f),
                    new Vector2(eyeTex.Width / 2f, eyeTex.Height / 2f),
                    Main.rand.NextFloat(0.3f, 0.8f), SpriteEffects.None, 0f);
            }

            if (ResultTimer > 30 && ResultTimer < 90)
            {
                string boostText = failStreak >= 2
                    ? $"BOSS ENRAGED x{failStreak}"
                    : "BOSS ENRAGED";
                if (Main.rand.NextBool(6))
                {
                    char[] c = boostText.ToCharArray();
                    c[Main.rand.Next(c.Length)] = (char)Main.rand.Next(33, 126);
                    boostText = new string(c);
                }
                Utils.DrawBorderString(sb, boostText,
                    new Vector2(Main.screenWidth / 2f - 100, Main.screenHeight / 2f + 50),
                    Color.OrangeRed * alpha * 0.9f, 1.1f);
            }
        }

        private void DrawCustomCursor(SpriteBatch sb)
        {
            Texture2D cursorTex = TextureAssets.Cursors[0].Value;
            Vector2 cursorPos = new Vector2(Main.mouseX, Main.mouseY);

            if (glitchIntensity > 0.5f && Main.rand.NextBool(8))
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 phantomPos = cursorPos + Main.rand.NextVector2Circular(
                        20 * glitchIntensity, 20 * glitchIntensity);
                    sb.Draw(cursorTex, phantomPos, Color.Red * 0.3f);
                }
            }

            Color cursorColor = Color.White;
            float timeRatio = (float)QuestTimer / QuestMaxTime;
            if (timeRatio < 0.3f)
                cursorColor = Color.Lerp(Color.White, Color.Red, 1f - timeRatio * 3.3f);

            sb.Draw(cursorTex, cursorPos, cursorColor);
        }

        private void DrawEyeHover(SpriteBatch sb)
        {
            Vector2 mousePos = new Vector2(Main.mouseX, Main.mouseY);
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            foreach (var eye in screenEyes)
            {
                if (eye.Clicked) continue;
                if (eye.IsFake && eye.Opacity < 0.1f) continue;

                float dist = Vector2.Distance(mousePos, eye.Position + new Vector2(eye.GlitchOffset, 0));
                if (dist <= eye.Size)
                {
                    float pulse = (float)Math.Sin(Main.GameUpdateCount * 0.15f) * 0.3f + 0.7f;
                    int highlightSize = (int)(eye.Size * 2.5f);
                    Rectangle highlight = new Rectangle(
                        (int)(eye.Position.X + eye.GlitchOffset - highlightSize / 2),
                        (int)(eye.Position.Y - highlightSize / 2),
                        highlightSize, highlightSize);
                    sb.Draw(pixel, highlight, Color.White * 0.08f * pulse);
                    break;
                }
            }
        }

        public override void ModifyScreenPosition()
        {
            if (warningActive && warningShake > 0.5f)
            {
                Main.screenPosition += Main.rand.NextVector2Circular(warningShake, warningShake);
            }

            if (!QuestActive) return;

            float timeRatio = 1f - (float)QuestTimer / Math.Max(QuestMaxTime, 1);
            float shakeAmount = 0f;

            if (QuestFailed && ResultTimer > 0)
            {
                shakeAmount = ResultTimer / 120f * 12f;
            }
            else if (timeRatio > 0.7f)
            {
                shakeAmount = (timeRatio - 0.7f) / 0.3f * 6f;
            }

            if (shakeAmount > 0.1f)
            {
                Main.screenPosition += Main.rand.NextVector2Circular(shakeAmount, shakeAmount);
            }

            if (heartbeatActive && QuestActive && !QuestFailed && !QuestSuccess)
            {
                Main.screenPosition += new Vector2(
                    (float)Math.Sin(heartbeatTimer * 1.1f) * 2f * glitchIntensity,
                    (float)Math.Cos(heartbeatTimer * 0.9f) * 1.5f * glitchIntensity);
            }
        }
        public override void OnWorldLoad()
{
    ResetAll();
}

public override void OnWorldUnload()
{
    ResetAll();
}

public static void ResetAll()
{
    QuestActive = false;
    QuestFailed = false;
    QuestSuccess = false;
    QuestTimer = 0;
    QuestMaxTime = 0;
    ResultTimer = 0;
    QuestText = "";
    QuestSubText = "";
    QuestColor = Color.Red;

    warningActive = false;
    warningTimer = 0;
    warningQuestHint = "";
    warningPulse = 0f;
    warningShake = 0f;

    blackOverlayOpacity = 0f;
    eyePulse = 0f;
    glitchIntensity = 0f;
    screenDistortion = 0f;
    screenFlashTimer = 0;
    screenEyes.Clear();
    glitchTexts.Clear();
    fakeStaticNoise = 0f;
    jumpscareCooldown = 0;
    heartbeatTimer = 0f;
    heartbeatActive = false;

    questCooldown = 0;
    successStreak = 0;
    failStreak = 0;
    totalCompleted = 0;
    totalFailed = 0;

    standStillAccumulator = 0f;
    prayAccumulator = 0f;
    playerAttacked = false;
    dontAttackAccumulator = 0f;
    obeyAccumulator = 0f;
    screamAccumulator = 0f;
    screamJumps = 0;
    screamDistance = 0f;
    wasMouseLeft = false;
}
    }
}