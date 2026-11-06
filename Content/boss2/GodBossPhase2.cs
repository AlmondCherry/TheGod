using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using GodEyeMod.Content.boss2.Projectiles;

namespace GodEyeMod.Content.boss2
{
    [AutoloadBossHead]
    public class GodBossPhase2 : ModNPC
    {
        private enum Phase
        {
            Spawning, SlowReveal, GentleFloat, Aggressive,
            ChargeBurst, CameraLockTransition, RockPhase, FastDash,
            RunAway, Explosion, Survival, Darkening,
            WorldAbsorb, Empowered, WorldManipulate, MiniExplosion,
            DemonicRock, InterferingAdd, Calm, GlitchBuild,
            MegaFight, EnergyBurst, MegaStrong, CircleAttack,
            WorldAbsorb2, EvenStronger, BlinkBarrage, BarrageWind,
            CameraLock2, FinalForm, FinalBurst, MegaHard,
            BossCalm2, AllSidesAttack, ProjectileRain, RainPlusAttacks,
            RainPlusAll, DeathCheck, DeathAnimation, WhiteOut, End
        }

        private struct PT { public float T; public Phase P; public PT(float t, Phase p) { T = t; P = p; } }

        private static readonly PT[] TL = {
            new(0f, Phase.Spawning), new(11.30f, Phase.SlowReveal),
            new(22.21f, Phase.GentleFloat), new(44.05f, Phase.Aggressive),
            new(60.41f, Phase.ChargeBurst), new(70.08f, Phase.CameraLockTransition),
            new(71.31f, Phase.RockPhase), new(93.23f, Phase.FastDash),
            new(112.33f, Phase.RunAway), new(115.11f, Phase.Explosion),
            new(115.50f, Phase.Survival), new(138.90f, Phase.Darkening),
            new(143.36f, Phase.WorldAbsorb), new(146.04f, Phase.Empowered),
            new(173.31f, Phase.WorldManipulate), new(175.37f, Phase.MiniExplosion),
            new(176.07f, Phase.DemonicRock), new(186.97f, Phase.InterferingAdd),
            new(196.85f, Phase.Calm), new(197.87f, Phase.GlitchBuild),
            new(208.78f, Phase.MegaFight), new(216.97f, Phase.EnergyBurst),
            new(219.71f, Phase.MegaStrong), new(244.92f, Phase.CircleAttack),
            new(267.43f, Phase.WorldAbsorb2), new(268.79f, Phase.EvenStronger),
            new(289.51f, Phase.BlinkBarrage), new(290.60f, Phase.BarrageWind),
            new(314.24f, Phase.CameraLock2), new(318.73f, Phase.FinalForm),
            new(321.73f, Phase.FinalBurst), new(346.29f, Phase.MegaHard),
            new(349.01f, Phase.BossCalm2), new(351.05f, Phase.AllSidesAttack),
            new(362.66f, Phase.ProjectileRain), new(373.44f, Phase.RainPlusAttacks),
            new(384.77f, Phase.RainPlusAll), new(393.01f, Phase.DeathCheck),
            new(393.37f, Phase.DeathAnimation),  new(401.11f, Phase.WhiteOut),
            new(435.00f, Phase.End),
        };

        private readonly float[] BurstT = { 60.41f, 63.21f, 65.85f, 68.58f };
        private readonly float[] DeathT = { 393.37f, 394.06f, 394.78f, 395.65f, 396.64f, 397.82f, 399.25f };
        private readonly float[] EnergyT = { 218.34f, 218.68f, 218.02f, 219.35f, 219.71f };
        private readonly float[] CircS = { 244.92f, 250.38f };
        private readonly float[] CircE = { 245.44f, 250.91f };

        private float mt, lt, sc = 0.5f, ts = 0.5f, rt, al;
        private float sv = 1f, dk, wo, sh = 1f, ab, gl, dm = 1f;
        private float dcd;
        private int em;
        private bool dead;
        private Vector2 fp;
        private float pls, ar, eg, pt2;
        private Phase ph = Phase.Spawning;
        private bool[] bd, dd, ed, cd;
        private readonly List<int> mns = new();
        private const int FR = 6, FW = 400, FH = 400;
        private int ft, cf;
        private float hpt, crossA;
        private float atkCD1, atkCD2, atkCD3, atkCD4;
        private int barrierID = -1;
        private bool exitTriggered = false;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = FR;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.TrailCacheLength[NPC.type] = 12;
            NPCID.Sets.TrailingMode[NPC.type] = 3;
            NPCID.Sets.BossBestiaryPriority.Add(NPC.type);
            NPCID.Sets.MPAllowedEnemies[NPC.type] = true;
            NPCID.Sets.NoMultiplayerSmoothingByType[NPC.type] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 120;
            NPC.height = 120;
            NPC.damage = 0;
            NPC.defense = 100;
            NPC.lifeMax = 12_000_000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = null;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.npcSlots = 6f;
            NPC.aiStyle = -1;
            NPC.lavaImmune = true;
            NPC.dontTakeDamage = true;
            if (!Main.dedServ)
                Music = MusicLoader.GetMusicSlot(Mod, "Content/Music/theFINAL");
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
                new FlavorTextBestiaryInfoElement("The GOD. The eye behind reality.")
            });
        }

        public override void OnSpawn(IEntitySource source)
        {
            mt = 0f; ph = Phase.Spawning; sc = 0f; al = 0f;
            NPC.dontTakeDamage = true; em = 0; dead = false;
            dm = 1f; ts = 0.5f; sh = 1f; wo = 0f; dk = 0f; gl = 0f;
            lt = 0f; dcd = 0f; pls = 0f; ar = 0f; eg = 0f; pt2 = 0f;
            crossA = 0f; barrierID = -1; exitTriggered = false;
            atkCD1 = 0; atkCD2 = 0; atkCD3 = 0; atkCD4 = 0;
            hpt = NPC.lifeMax * 0.05f;
            bd = new bool[BurstT.Length]; dd = new bool[DeathT.Length];
            ed = new bool[EnergyT.Length]; cd = new bool[CircS.Length];
            var cfg = ModContent.GetInstance<GodEyeConfig>();
            if (cfg != null && !cfg.DisableBossWeatherControl)
            {
                Main.time = 16200; Main.dayTime = false;
                Main.windSpeedCurrent = 0f; Main.windSpeedTarget = 0f;
                Main.maxRaining = 0f; Main.raining = false;
            }
        }

        public override bool CanHitPlayer(Player t, ref int c) => false;
        public override bool CheckActive() => false;

        public override void FindFrame(int fh)
        {
            ft++;
            int s = ph switch { Phase.Spawning => 12, Phase.SlowReveal => 10,
                Phase.MegaStrong or Phase.FinalForm or Phase.FinalBurst => 4, _ => 6 };
            if (ft >= s) { ft = 0; cf = (cf + 1) % FR; }
            NPC.frame.Y = cf * FH;
        }

        public override bool? DrawHealthBar(byte h, ref float s, ref Vector2 p)
        {
            if (ph is Phase.Spawning or Phase.SlowReveal or Phase.WhiteOut or Phase.End or Phase.DeathAnimation) return false;
            s = 1.5f; return null;
        }

        public override void ModifyIncomingHit(ref NPC.HitModifiers m) { m.FinalDamage *= dm; }

        public override bool CheckDead()
        {
            if (!dead) { NPC.life = 1; NPC.active = true; return false; }
            return true;
        }

        public override void AI()
        {
            if (Main.gamePaused) return;

            if (NPC.target < 0 || NPC.target >= 255 || !Main.player[NPC.target].active || Main.player[NPC.target].dead)
                NPC.TargetClosest(false);

            Player pl = Main.player[NPC.target];
            if (!pl.active || pl.dead) { Despawn(); return; }

            NPC.timeLeft = 36000;
            float dt = 1f / 60f;
            mt += dt; lt += dt; pls += dt; ar += 0.02f;
            eg = 0.5f + (float)Math.Sin(pls * 2f) * 0.3f;
            crossA += 0.015f;
            atkCD1 = Math.Max(0, atkCD1 - dt);
            atkCD2 = Math.Max(0, atkCD2 - dt);
            atkCD3 = Math.Max(0, atkCD3 - dt);
            atkCD4 = Math.Max(0, atkCD4 - dt);

            UpdPhase();
            RunPhase(pl);
            sc = MathHelper.Lerp(sc, ts, 0.03f);
            NPC.rotation = rt;

            if (ph != Phase.End) Lighting.AddLight(NPC.Center, 1.2f * eg, 0.15f, 0.15f);

            if (Main.netMode != NetmodeID.Server)
            {
                float musicLen = 406f;
                float rem = Math.Max(0, musicLen - mt);
                bool showT = ph != Phase.End && ph != Phase.WhiteOut && ph != Phase.DeathAnimation;
                GodBossVisualSystem.SetTimer(rem, showT);
                GodBossVisualSystem.SetDarken(dk);
                GodBossVisualSystem.SetGlitch(gl);

                if (ph == Phase.WhiteOut)
                {
                    GodBossVisualSystem.SetWhiteOut(wo);
                    float textAlpha = Math.Clamp((lt - 3f) / 2f, 0f, 1f);
                    string endStr = lt > 15f ? "END ?" : "END";
                    GodBossVisualSystem.SetEndText(endStr, textAlpha * wo);
                }
                else { GodBossVisualSystem.SetWhiteOut(0f); GodBossVisualSystem.SetEndText("", 0f); }
            }
            AmbDust();
        }

        private void AmbDust()
        {
            if (ph is Phase.End or Phase.WhiteOut) return;
            pt2 += 1f / 60f;
            if (pt2 >= 0.1f)
            {
                pt2 = 0f;
                float a = Main.rand.NextFloat(MathHelper.TwoPi);
                float d = 80f * sc + Main.rand.NextFloat(40f);
                Vector2 p = NPC.Center + a.ToRotationVector2() * d;
                Dust du = Dust.NewDustDirect(p, 1, 1, DustID.RedTorch, 0, 0, 180, default, 0.6f);
                du.noGravity = true;
                du.velocity = (NPC.Center - p).SafeNormalize(Vector2.Zero) * 1.5f;
            }
        }

        private void Despawn()
        {
            NPC.velocity.Y -= 0.5f; NPC.alpha += 5; KillBarrier();
            if (NPC.alpha >= 255)
            {
                NPC.active = false; KillMn();
                if (Main.netMode != NetmodeID.Server) GodBossPhase2Camera.Release();
            }
        }

        private void UpdPhase()
        {
            Phase np = Phase.Spawning;
            for (int i = TL.Length - 1; i >= 0; i--)
                if (mt >= TL[i].T) { np = TL[i].P; break; }
            if (np != ph) { Enter(ph, np); ph = np; lt = 0f; atkCD1 = 0; atkCD2 = 0; atkCD3 = 0; atkCD4 = 0; }
        }

        private void Enter(Phase o, Phase n)
        {
            var cfg = ModContent.GetInstance<GodEyeConfig>();
            bool nf = cfg != null && cfg.DisableFlashing;
            bool ns = cfg != null && cfg.DisableScreenShake;
            switch (n)
            {
                case Phase.SlowReveal:
                    NPC.dontTakeDamage = true;
                    if (!ns) Shk(12f, 20); if (!nf) Fls(Color.Red, 0.5f); break;
                case Phase.GentleFloat:
                    NPC.dontTakeDamage = false; dm = 1f; ts = 0.5f; break;
                case Phase.Aggressive: ts = 0.55f; break;
                case Phase.ChargeBurst: fp = NPC.Center; ts = 0.6f; break;
                case Phase.CameraLockTransition: break;
                case Phase.RockPhase:
                    if (Main.netMode != NetmodeID.Server) GodBossPhase2Camera.Release();
                    ts = 0.65f; if (!nf) Fls(Color.DarkRed, 0.7f);
                    if (!ns) Shk(20f, 25); MkMn(3); break;
                case Phase.FastDash: KillMn(); ts = 0.6f; dcd = 0f; break;
                case Phase.RunAway: fp = NPC.Center; NPC.velocity = Vector2.Zero; break;
                case Phase.Explosion:
                    if (!nf) Fls(Color.White, 0.9f); if (!ns) Shk(30f, 40);
                    Ring(24, 15f, 80); break;
                case Phase.Survival:
                    NPC.dontTakeDamage = false; dm = 0.08f; sv = 1f; fp = NPC.Center;
                    SpawnBarrier(2000f); break;
                case Phase.Darkening:
                    dk = 0f; fp = NPC.Center; KillBarrier(); break;
                case Phase.WorldAbsorb:
                    if (!nf) Fls(Color.Gold, 0.8f); if (!ns) Shk(25f, 30);
                    ab = 0f; dm = 1f; NPC.dontTakeDamage = false; dk = 0f; break;
                case Phase.Empowered: em = 1; ts = 0.7f; break;
                case Phase.WorldManipulate: fp = NPC.Center; break;
                case Phase.MiniExplosion:
                    if (!nf) Fls(Color.OrangeRed, 0.6f); if (!ns) Shk(15f, 12); break;
                case Phase.DemonicRock: ts = 0.75f; break;
                case Phase.InterferingAdd: MkMn(4); MkWl(); break;
                case Phase.Calm: KillMn(); gl = 0f; break;
                case Phase.GlitchBuild: gl = 0.05f; break;
                case Phase.MegaFight: gl = 0f; MkMn(3); break;
                case Phase.EnergyBurst: NPC.velocity = Vector2.Zero; break;
                case Phase.MegaStrong:
                    KillMn(); ts = 0.85f; if (!nf) Fls(Color.Red, 0.9f);
                    if (!ns) Shk(30f, 35); MkMn(5); MkWl();
                    SpawnBarrier(1800f); break;
                case Phase.CircleAttack: KillBarrier(); break;
                case Phase.WorldAbsorb2:
                    fp = NPC.Center; NPC.velocity = Vector2.Zero; ab = 0f; break;
                case Phase.EvenStronger: em = 2; ts = 0.95f; if (!nf) Fls(Color.DarkRed, 0.8f); break;
                case Phase.CameraLock2: break;
                case Phase.FinalForm:
                    if (Main.netMode != NetmodeID.Server) GodBossPhase2Camera.Release();
                    em = 3; ts = 1.0f; if (!nf) Fls(Color.White, 1f); if (!ns) Shk(35f, 40); break;
                case Phase.FinalBurst:
                    if (!nf) Fls(Color.Crimson, 0.9f); if (!ns) Shk(40f, 45);
                    MkMn(6); MkWl(); break;
                case Phase.BossCalm2: KillMn(); KillBarrier(); break;
                case Phase.AllSidesAttack: MkMn(5); SpawnBarrier(1600f); break;
                case Phase.ProjectileRain: KillMn(); KillBarrier(); break;
                case Phase.RainPlusAttacks: MkMn(3); SpawnBarrier(1800f); break;
                case Phase.RainPlusAll: MkMn(4); MkWl(); break;
                case Phase.DeathCheck:
                    KillMn(); KillBarrier(); NPC.velocity = Vector2.Zero;
                    if (NPC.life > hpt)
                    {
                        Player p = Main.player[NPC.target];
                        if (p.active && !p.dead)
                            p.KillMe(PlayerDeathReason.ByCustomReason(p.name + " was erased by The GOD."), 999999, 0);
                        NPC.active = false; return;
                    }
                    NPC.dontTakeDamage = true; dead = true; break;
                case Phase.DeathAnimation: sh = 1f; break;
                case Phase.WhiteOut:
                    wo = 0f;
                    GodBossVisualSystem.StopMusic();
                    break;
                case Phase.End:
                    KillMn(); KillBarrier();
                    GodBossVisualSystem.RestoreMusic();
                    if (Main.netMode != NetmodeID.Server) GodBossPhase2Camera.Release();
                    if (!exitTriggered)
                    {
                        exitTriggered = true;
                        NPC.active = false;
                        WorldGen.SaveAndQuit();
                    }
                    break;
            }
        }

        private void RunPhase(Player pl)
        {
            float t = lt;
            switch (ph)
            {
                case Phase.Spawning:
                    NPC.Center = pl.Center + new Vector2(0, -350);
                    NPC.velocity = Vector2.Zero;
                    float sp = Math.Clamp(t / 11.30f, 0f, 1f);
                    al = sp; sc = MathHelper.Lerp(0f, 0.5f, EaseO(sp));
                    rt = MathHelper.Lerp(MathHelper.TwoPi * 2f, 0f, sp);
                    NPC.Opacity = al;
                    if (t > 2f) DVort(NPC.Center, 120f, sp);
                    if (t >= 11.0f && t < 11.3f) { Fls(Color.DarkRed, 0.4f); Shk(8f, 8); }
                    break;

                case Phase.SlowReveal:
                    NPC.Opacity = 1f; rt = 0f;
                    float fy = (float)Math.Sin(mt * 0.5f) * 25f;
                    NPC.Center = Vector2.Lerp(NPC.Center, pl.Center + new Vector2(0, -300 + fy), 0.02f);
                    NPC.velocity *= 0.95f; sc = MathHelper.Lerp(sc, 0.5f, 0.05f);
                    break;

                case Phase.GentleFloat:
                    Hov(pl, 280f, 0.02f, 1.2f);
                    rt = NPC.velocity.X * 0.008f;
                    if (atkCD1 <= 0) { atkCD1 = 2.0f; Aimed(pl, 6f, 50); }
                    break;

                case Phase.Aggressive:
                    Hov(pl, 260f, 0.03f, 2.0f);
                    rt = NPC.velocity.X * 0.012f;
                    if (atkCD1 <= 0) { atkCD1 = 0.8f; Aimed(pl, 9f, 65); }
                    if (atkCD2 <= 0) { atkCD2 = 3.0f; Spread(pl, 3, 25, 8f, 55); }
                    break;

                case Phase.ChargeBurst:
                    NPC.Center = Vector2.Lerp(NPC.Center, fp, 0.1f);
                    NPC.velocity *= 0.9f; rt += 0.04f;
                    sc = MathHelper.Lerp(sc, 0.65f, 0.02f);
                    for (int i = 0; i < BurstT.Length; i++)
                    {
                        if (!bd[i] && mt >= BurstT[i])
                        {
                            bd[i] = true;
                            Fls(Color.Red, 0.3f + i * 0.1f); Shk(8f + i * 3f, 8);
                            SeqRing(10 + i * 2, 5f + i * 1.5f, 40 + i * 8);
                            DBurst(NPC.Center, 20, 6f, 1.5f);
                        }
                    }
                    break;

                case Phase.CameraLockTransition:
                    NPC.velocity *= 0.95f; rt += 0.08f;
                    if (Main.netMode != NetmodeID.Server) GodBossPhase2Camera.LockTo(NPC.Center, 0.03f);
                    break;

                case Phase.RockPhase:
                    Hov(pl, 240f, 0.04f, 2.8f);
                    rt = NPC.velocity.X * 0.015f;
                    if (atkCD1 <= 0) { atkCD1 = 0.6f; Aimed(pl, 11f, 75); }
                    if (atkCD2 <= 0) { atkCD2 = 2.5f; Spread(pl, 5, 35, 9f, 65); }
                    if (atkCD3 <= 0) { atkCD3 = 4.0f; Spiral(8, 7f, 55); }
                    break;

                case Phase.FastDash:
                    dcd -= 1f / 60f;
                    if (dcd <= 0f)
                    {
                        dcd = 1.5f;
                        NPC.velocity = (pl.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 12f;
                        Shk(6f, 4); BkFan(pl, 5, 9f, 60);
                    }
                    NPC.velocity *= 0.97f; rt = NPC.velocity.ToRotation();
                    if (atkCD1 <= 0) { atkCD1 = 0.7f; Aimed(pl, 12f, 70); }
                    break;

                case Phase.RunAway:
                    NPC.Center = fp; NPC.velocity = Vector2.Zero;
                    sc = MathHelper.Lerp(sc, 0.8f, 0.03f); rt += 0.1f;
                    DBurst(NPC.Center, 3, 8f, 2f); break;

                case Phase.Explosion:
                    NPC.Center = fp; NPC.velocity = Vector2.Zero; break;

                case Phase.Survival:
                    NPC.Center = Vector2.Lerp(NPC.Center, fp, 0.1f);
                    NPC.velocity = Vector2.Zero;
                    float sDur = 138.90f - 115.50f;
                    float sPr = Math.Clamp(t / sDur, 0f, 1f);
                    sv = MathHelper.Lerp(1f, 0.15f, sPr);
                    int pat = (int)(t / 4f) % 4;
                    float wInt = MathHelper.Lerp(1.2f, 2.5f, sPr);
                    if (atkCD1 <= 0)
                    {
                        atkCD1 = wInt;
                        switch (pat)
                        {
                            case 0: HorizLine(pl, true, 7, 7f, 60); break;
                            case 1: VertLine(pl, true, 7, 7f, 60); break;
                            case 2: HorizLine(pl, false, 7, 7f, 60); break;
                            case 3: VertLine(pl, false, 7, 7f, 60); break;
                        }
                    }
                    if (atkCD2 <= 0 && sPr < 0.7f) { atkCD2 = MathHelper.Lerp(2f, 4f, sPr); DiagE(pl, 6f, 55); }
                    break;

                case Phase.Darkening:
                    NPC.Center = Vector2.Lerp(NPC.Center, fp, 0.1f);
                    NPC.velocity = Vector2.Zero;
                    dk = Math.Clamp(t / (143.36f - 138.90f), 0f, 1f); break;

                case Phase.WorldAbsorb:
                    NPC.Center = Vector2.Lerp(NPC.Center, fp, 0.1f);
                    ab = Math.Clamp(t / (146.04f - 143.36f), 0f, 1f);
                    sc = MathHelper.Lerp(0.55f, 0.7f, ab); ts = 0.7f;
                    AbsDust(8, 500f, ab, DustID.GoldFlame); break;

                case Phase.Empowered:
                    Hov(pl, 220f, 0.04f, 3.0f);
                    rt = NPC.velocity.X * 0.015f;
                    if (atkCD1 <= 0) { atkCD1 = 0.5f; Aimed(pl, 12f, 80); }
                    if (atkCD2 <= 0) { atkCD2 = 2.0f; Spread(pl, 5, 40, 10f, 70); }
                    if (atkCD3 <= 0) { atkCD3 = 3.0f; Spiral(10, 8f, 65); }
                    if (atkCD4 <= 0) { atkCD4 = 4.0f; Wave(pl, 8, 9f, 70); }
                    break;

                case Phase.WorldManipulate:
                    NPC.Center = Vector2.Lerp(NPC.Center, fp, 0.1f);
                    NPC.velocity = Vector2.Zero; rt += 0.06f;
                    AbsDust(5, 350f, 0.5f, DustID.Shadowflame);
                    var cw = ModContent.GetInstance<GodEyeConfig>();
                    if (cw != null && !cw.DisableBossWeatherControl)
                    {
                        Main.windSpeedCurrent = MathHelper.Lerp(Main.windSpeedCurrent, 1.2f, 0.03f);
                        Main.maxRaining = MathHelper.Lerp(Main.maxRaining, 0.6f, 0.03f);
                    }
                    break;

                case Phase.MiniExplosion:
                    NPC.velocity = Vector2.Zero;
                    if (t < 0.1f) SeqRing(14, 8f, 65); break;

                case Phase.DemonicRock:
                    Hov(pl, 200f, 0.04f, 3.2f);
                    rt = NPC.velocity.X * 0.018f;
                    if (atkCD1 <= 0) { atkCD1 = 0.4f; Aimed(pl, 14f, 85); }
                    if (atkCD2 <= 0) { atkCD2 = 1.8f; Spread(pl, 7, 50, 12f, 80); }
                    if (atkCD3 <= 0) { atkCD3 = 2.5f; Spiral(12, 9f, 75); }
                    break;

                case Phase.InterferingAdd:
                    Hov(pl, 220f, 0.04f, 2.8f);
                    rt = NPC.velocity.X * 0.015f;
                    if (atkCD1 <= 0) { atkCD1 = 0.5f; Aimed(pl, 12f, 80); }
                    if (atkCD2 <= 0) { atkCD2 = 2.0f; Spread(pl, 5, 40, 10f, 70); }
                    break;

                case Phase.Calm:
                    Hov(pl, 320f, 0.02f, 1.2f);
                    rt = NPC.velocity.X * 0.008f;
                    if (atkCD1 <= 0) { atkCD1 = 1.8f; Aimed(pl, 6f, 45); }
                    break;

                case Phase.GlitchBuild:
                    Hov(pl, 280f, 0.025f, 1.5f);
                    float gp = Math.Clamp(t / (208.78f - 197.87f), 0f, 1f);
                    gl = MathHelper.Lerp(0.05f, 0.5f, gp);
                    if (atkCD1 <= 0) { atkCD1 = MathHelper.Lerp(1.2f, 0.5f, gp); Aimed(pl, 8f + gp * 4f, 55 + (int)(gp * 20)); }
                    break;

                case Phase.MegaFight:
                    Hov(pl, 230f, 0.04f, 3.0f);
                    rt = NPC.velocity.X * 0.015f; gl = 0f;
                    float mp = Math.Clamp(t / (216.97f - 208.78f), 0f, 1f);
                    if (atkCD1 <= 0) { atkCD1 = MathHelper.Lerp(0.5f, 0.3f, mp); Aimed(pl, 10f + mp * 3f, 70 + (int)(mp * 15)); }
                    if (atkCD2 <= 0) { atkCD2 = 2.0f; Spread(pl, 3 + (int)(mp * 2), 35, 9f, 65); }
                    break;

                case Phase.EnergyBurst:
                    NPC.velocity *= 0.95f; rt += 0.08f;
                    for (int i = 0; i < EnergyT.Length; i++)
                    {
                        if (!ed[i] && mt >= EnergyT[i])
                        {
                            ed[i] = true; Fls(Color.Red, 0.5f); Shk(12f, 6);
                            SeqRing(12 + i * 2, 6f + i, 60 + i * 5);
                        }
                    }
                    break;

                case Phase.MegaStrong:
                    Hov(pl, 220f, 0.05f, 3.5f);
                    rt = NPC.velocity.X * 0.02f;
                    if (atkCD1 <= 0) { atkCD1 = 0.35f; Aimed(pl, 14f, 90); }
                    if (atkCD2 <= 0) { atkCD2 = 2.0f; Spread(pl, 5, 45, 12f, 80); }
                    if (atkCD3 <= 0) { atkCD3 = 2.5f; Spiral(14, 10f, 75); }
                    if (atkCD4 <= 0) { atkCD4 = 3.0f; CrossShot(8, 10f, 75); }
                    break;

                case Phase.CircleAttack:
                    Hov(pl, 220f, 0.05f, 3.5f);
                    if (atkCD1 <= 0) { atkCD1 = 0.4f; Aimed(pl, 14f, 90); }
                    for (int i = 0; i < CircS.Length; i++)
                    {
                        if (!cd[i] && mt >= CircS[i])
                        {
                            cd[i] = true;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), pl.Center, Vector2.Zero,
                                    ModContent.ProjectileType<GodCircleProjectile>(), Dmg(100), 0f, Main.myPlayer,
                                    CircE[i] - CircS[i]);
                        }
                    }
                    break;

                case Phase.WorldAbsorb2:
                    NPC.Center = Vector2.Lerp(NPC.Center, fp, 0.1f);
                    NPC.velocity = Vector2.Zero;
                    ab = Math.Clamp(t / (268.79f - 267.43f), 0f, 1f);
                    sc = MathHelper.Lerp(0.85f, 0.95f, ab); ts = 0.95f;
                    AbsDust(10, 600f, ab, DustID.RedTorch); break;

                case Phase.EvenStronger:
                    Hov(pl, 200f, 0.05f, 4.0f);
                    rt = NPC.velocity.X * 0.02f;
                    if (atkCD1 <= 0) { atkCD1 = 0.3f; Aimed(pl, 16f, 100); }
                    if (atkCD2 <= 0) { atkCD2 = 1.8f; Spread(pl, 5, 50, 14f, 90); }
                    if (atkCD3 <= 0) { atkCD3 = 2.0f; Spiral(16, 12f, 85); }
                    if (atkCD4 <= 0) { atkCD4 = 3.5f; ConvergeShot(pl, 8, 11f, 80); }
                    break;

                case Phase.BlinkBarrage:
                    Hov(pl, 220f, 0.04f, 3.5f);
                    NPC.Opacity = 0.4f + (float)Math.Sin(t * 15f) * 0.6f;
                    if (atkCD1 <= 0) { atkCD1 = 0.25f; Aimed(pl, 16f, 100); }
                    break;

                case Phase.BarrageWind:
                    NPC.Opacity = 1f;
                    Hov(pl, 250f, 0.03f, 2.5f);
                    float bp = Math.Clamp(t / (314.24f - 290.60f), 0f, 1f);
                    float bcd = MathHelper.Lerp(0.3f, 0.8f, bp);
                    if (atkCD1 <= 0) { atkCD1 = bcd; CardE(pl, 8f + (1f - bp) * 4f, 80); }
                    if (atkCD2 <= 0) { atkCD2 = bcd * 2f; DiagE(pl, 7f + (1f - bp) * 3f, 75); }
                    break;

                case Phase.CameraLock2:
                    NPC.velocity *= 0.95f;
                    if (Main.netMode != NetmodeID.Server) GodBossPhase2Camera.LockTo(NPC.Center, 0.04f);
                    break;

                case Phase.FinalForm:
                    Hov(pl, 220f, 0.05f, 4.0f);
                    rt = NPC.velocity.X * 0.02f;
                    if (atkCD1 <= 0) { atkCD1 = 0.25f; Aimed(pl, 18f, 110); }
                    if (atkCD2 <= 0) { atkCD2 = 1.5f; Spread(pl, 7, 55, 14f, 100); }
                    if (atkCD3 <= 0) { atkCD3 = 2.5f; CrossShot(10, 12f, 90); }
                    break;

                case Phase.FinalBurst:
                    Hov(pl, 200f, 0.06f, 4.5f);
                    rt = NPC.velocity.X * 0.02f;
                    if (atkCD1 <= 0) { atkCD1 = 0.22f; Aimed(pl, 18f, 120); }
                    if (atkCD2 <= 0) { atkCD2 = 1.5f; Spread(pl, 5, 55, 16f, 110); }
                    if (atkCD3 <= 0) { atkCD3 = 2.0f; Spiral(18, 14f, 100); }
                    if (atkCD4 <= 0) { atkCD4 = 3.0f; ConvergeShot(pl, 10, 13f, 95); }
                    break;

                case Phase.MegaHard:
                    Hov(pl, 200f, 0.06f, 4.5f);
                    if (atkCD1 <= 0) { atkCD1 = 0.2f; Aimed(pl, 20f, 130); }
                    if (atkCD2 <= 0) { atkCD2 = 2.0f; CrossShot(12, 14f, 110); }
                    break;

                case Phase.BossCalm2:
                    Hov(pl, 300f, 0.02f, 1.5f); NPC.velocity *= 0.95f; break;

                case Phase.AllSidesAttack:
                    Hov(pl, 220f, 0.04f, 3.5f);
                    if (atkCD1 <= 0) { atkCD1 = 0.3f; CardE(pl, 12f, 100); }
                    if (atkCD2 <= 0) { atkCD2 = 0.5f; DiagE(pl, 10f, 90); }
                    if (atkCD3 <= 0) { atkCD3 = 2.0f; Spread(pl, 7, 55, 14f, 100); }
                    break;

                case Phase.ProjectileRain:
                    Hov(pl, 250f, 0.04f, 3.0f);
                    if (atkCD1 <= 0) { atkCD1 = 0.2f; Rain(pl, 100); }
                    break;

                case Phase.RainPlusAttacks:
                    Hov(pl, 230f, 0.04f, 3.0f);
                    if (atkCD1 <= 0) { atkCD1 = 0.25f; Rain(pl, 110); }
                    if (atkCD2 <= 0) { atkCD2 = 1.5f; Aimed(pl, 16f, 110); }
                    break;

                case Phase.RainPlusAll:
                    Hov(pl, 230f, 0.04f, 3.2f);
                    if (atkCD1 <= 0) { atkCD1 = 0.25f; Rain(pl, 120); }
                    if (atkCD2 <= 0) { atkCD2 = 1.5f; Aimed(pl, 16f, 110); }
                    if (atkCD3 <= 0) { atkCD3 = 2.5f; Spiral(10, 10f, 90); }
                    break;

                case Phase.DeathAnimation:
                    NPC.velocity = Vector2.Zero; rt += 0.06f * sh;
                    for (int i = 0; i < DeathT.Length; i++)
                    {
                        if (!dd[i] && mt >= DeathT[i])
                        {
                            dd[i] = true;
                            float p = (float)(i + 1) / DeathT.Length;
                            sh = 1f - p * 0.8f;
                            sc = MathHelper.Lerp(ts, 0.1f, p); ts = sc;
                            Fls(Color.White, 0.25f + p * 0.4f); Shk(8f + p * 15f, 6);
                            DBurst(NPC.Center, 25, 8f + p * 8f, 1.5f);
                        }
                    }
                    break;

                    case Phase.WhiteOut:
                    NPC.velocity = Vector2.Zero;
                    wo = Math.Clamp(t / 5f, 0f, 1f);
                    NPC.Opacity = 1f - wo;

                    float endAlpha = Math.Clamp((t - 3f) / 2f, 0f, 1f) * wo;
                    GodBossVisualSystem.SetEndText("END", endAlpha);

                    bool showQ = t > 18f;
                    float qTimer = showQ ? t - 18f : 0f;
                    GodBossVisualSystem.SetEndAnimation(showQ, qTimer);

                    GodBossVisualSystem.SetWhiteOut(wo);
                    break;
            }
        }

        private void HorizLine(Player pl, bool top, int cnt, float spd, int dmg)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            float yOff = top ? -420f : 420f;
            float ySpd = top ? spd : -spd;
            float w = 350f; int skip = Main.rand.Next(cnt);
            for (int i = 0; i < cnt; i++)
            {
                if (i == skip || i == skip + 1) continue;
                float x = pl.Center.X - w / 2f + w / (cnt - 1) * i;
                Fire(new Vector2(x, pl.Center.Y + yOff), new Vector2(0, ySpd), dmg);
            }
        }

        private void VertLine(Player pl, bool left, int cnt, float spd, int dmg)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            float xOff = left ? -420f : 420f;
            float xSpd = left ? spd : -spd;
            float h = 350f; int skip = Main.rand.Next(cnt);
            for (int i = 0; i < cnt; i++)
            {
                if (i == skip || i == skip + 1) continue;
                float y = pl.Center.Y - h / 2f + h / (cnt - 1) * i;
                Fire(new Vector2(pl.Center.X + xOff, y), new Vector2(xSpd, 0), dmg);
            }
        }

        private void CrossShot(int perArm, float spd, int dmg)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            for (int arm = 0; arm < 4; arm++)
            {
                float baseA = crossA + MathHelper.PiOver2 * arm;
                for (int i = 0; i < perArm; i++)
                {
                    float s = spd * (0.4f + (float)i / perArm * 0.8f);
                    Fire(NPC.Center, baseA.ToRotationVector2() * s, dmg);
                }
            }
        }

        private void ConvergeShot(Player pl, int count, float spd, int dmg)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            Vector2 predicted = pl.Center + pl.velocity * 15f;
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi / count * i;
                float dist = 700f + Main.rand.NextFloat(200f);
                Vector2 pos = predicted + angle.ToRotationVector2() * dist;
                Vector2 vel = (predicted - pos).SafeNormalize(Vector2.Zero) * (spd * 0.3f);
                Fire(pos, vel, dmg);
            }
        }

        private void Aimed(Player p, float s, int d)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            Fire(NPC.Center, (p.Center - NPC.Center).SafeNormalize(Vector2.Zero) * s, d);
        }

        private void Spread(Player p, int c, float dg, float s, int d)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            float r = MathHelper.ToRadians(dg);
            float ba = (p.Center - NPC.Center).ToRotation();
            float st = ba - r / 2f;
            float step = c > 1 ? r / (c - 1) : 0;
            for (int i = 0; i < c; i++) Fire(NPC.Center, (st + step * i).ToRotationVector2() * s, d);
        }

        private void Ring(int c, float s, int d)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            float step = MathHelper.TwoPi / c;
            for (int i = 0; i < c; i++) Fire(NPC.Center, (step * i).ToRotationVector2() * s, d);
        }

        private void SeqRing(int c, float s, int d)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            float step = MathHelper.TwoPi / c;
            for (int i = 0; i < c; i++)
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, (step * i).ToRotationVector2() * s,
                    ModContent.ProjectileType<GodDelayedProjectile>(), Dmg(d), 0f, Main.myPlayer, i * 5f, step * i);
        }

        private void Spiral(int c, float s, int d)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            float ba = mt * 3f;
            float step = MathHelper.TwoPi / c;
            for (int i = 0; i < c; i++) Fire(NPC.Center, (ba + step * i).ToRotationVector2() * s, d);
        }

        private void Wave(Player p, int c, float s, int d)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            float ba = (p.Center - NPC.Center).ToRotation();
            for (int i = 0; i < c; i++)
            {
                float w = (float)Math.Sin(i * 0.6f + mt * 5f) * MathHelper.ToRadians(20);
                Fire(NPC.Center, (ba + w + (i - c / 2f) * MathHelper.ToRadians(3)).ToRotationVector2() * s, d);
            }
        }

        private void BkFan(Player p, int c, float s, int d)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            float ba = (NPC.Center - p.Center).ToRotation();
            float sp2 = MathHelper.ToRadians(60);
            float st = ba - sp2 / 2f;
            float step = c > 1 ? sp2 / (c - 1) : 0;
            for (int i = 0; i < c; i++) Fire(NPC.Center, (st + step * i).ToRotationVector2() * s, d);
        }

        private void CardE(Player p, float s, int d)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            int dr = Main.rand.Next(4); float o = Main.rand.NextFloat(-200f, 200f);
            Vector2 sp, v;
            switch (dr)
            {
                case 0: sp = p.Center + new Vector2(o, -500f); v = new Vector2(0, s); break;
                case 1: sp = p.Center + new Vector2(o, 500f); v = new Vector2(0, -s); break;
                case 2: sp = p.Center + new Vector2(-500f, o); v = new Vector2(s, 0); break;
                default: sp = p.Center + new Vector2(500f, o); v = new Vector2(-s, 0); break;
            }
            Fire(sp, v, d);
        }

        private void DiagE(Player p, float s, int d)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            int c = Main.rand.Next(4); float o = Main.rand.NextFloat(-150f, 150f);
            float ds = s * 0.707f; Vector2 sp, v;
            switch (c)
            {
                case 0: sp = p.Center + new Vector2(-450f + o, -450f); v = new Vector2(ds, ds); break;
                case 1: sp = p.Center + new Vector2(450f + o, -450f); v = new Vector2(-ds, ds); break;
                case 2: sp = p.Center + new Vector2(-450f + o, 450f); v = new Vector2(ds, -ds); break;
                default: sp = p.Center + new Vector2(450f + o, 450f); v = new Vector2(-ds, -ds); break;
            }
            Fire(sp, v, d);
        }

        private void Rain(Player p, int d)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            int ty = Main.rand.Next(3); Vector2 sp, v;
            switch (ty)
            {
                case 0: sp = p.Center + new Vector2(Main.rand.NextFloat(-600f, 600f), -500f);
                    v = new Vector2(Main.rand.NextFloat(-2f, 2f), 10f + Main.rand.NextFloat(4f)); break;
                case 1: sp = p.Center + new Vector2(-600f, Main.rand.NextFloat(-300f, 300f));
                    v = new Vector2(10f + Main.rand.NextFloat(4f), Main.rand.NextFloat(-1f, 1f)); break;
                default: sp = p.Center + new Vector2(600f, Main.rand.NextFloat(-300f, 300f));
                    v = new Vector2(-10f - Main.rand.NextFloat(4f), Main.rand.NextFloat(-1f, 1f)); break;
            }
            Fire(sp, v, d);
        }

        private void Fire(Vector2 p, Vector2 v, int d)
        {
            Projectile.NewProjectile(NPC.GetSource_FromAI(), p, v,
                ModContent.ProjectileType<GodCoreProjectile>(), Dmg(d), 0f, Main.myPlayer);
        }

        private void SpawnBarrier(float radius)
        {
            KillBarrier();
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            barrierID = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero,
                ModContent.ProjectileType<GodBarrierProjectile>(), 0, 0f, Main.myPlayer, radius);
        }

        private void KillBarrier()
        {
            if (barrierID >= 0 && barrierID < Main.maxProjectiles && Main.projectile[barrierID].active
                && Main.projectile[barrierID].type == ModContent.ProjectileType<GodBarrierProjectile>())
                Main.projectile[barrierID].Kill();
            barrierID = -1;
        }

        private void Hov(Player p, float d, float l, float m)
        {
            float fy = (float)Math.Sin(mt * 1.5f) * 20f;
            float fx = (float)Math.Cos(mt * 0.8f) * 30f;
            Vector2 tg = p.Center + new Vector2(fx, -d + fy);
            Vector2 df = tg - NPC.Center;
            NPC.velocity = Vector2.Lerp(NPC.velocity, df * l, 0.06f);
            if (NPC.velocity.Length() > m) NPC.velocity = NPC.velocity.SafeNormalize(Vector2.Zero) * m;
        }

        private int Dmg(int b) => (int)(b * (1f + em * 0.15f));

        private void MkMn(int c)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            for (int i = 0; i < c; i++)
            {
                float a = MathHelper.TwoPi / c * i;
                Vector2 sp = NPC.Center + a.ToRotationVector2() * 180f;
                int idx = NPC.NewNPC(NPC.GetSource_FromAI(), (int)sp.X, (int)sp.Y,
                    ModContent.NPCType<GodMinion>(), 0, NPC.whoAmI, a);
                if (idx >= 0 && idx < Main.maxNPCs) mns.Add(idx);
            }
        }

        private void MkWl()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            Player p = Main.player[NPC.target];
            for (int s = 0; s < 2; s++)
            {
                float xo = s == 0 ? -450f : 450f;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), p.Center + new Vector2(xo, 0),
                    new Vector2(s == 0 ? 2.5f : -2.5f, 0),
                    ModContent.ProjectileType<GodWallProjectile>(), Dmg(80), 0f, Main.myPlayer);
            }
        }

        private void KillMn()
        {
            foreach (int i in mns)
                if (i >= 0 && i < Main.maxNPCs && Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<GodMinion>())
                { Main.npc[i].life = 0; Main.npc[i].active = false; DBurst(Main.npc[i].Center, 10, 4f, 1.2f); }
            mns.Clear();
        }

        private void DVort(Vector2 c, float r, float p)
        {
            for (int i = 0; i < 4; i++)
            {
                float a = Main.rand.NextFloat(MathHelper.TwoPi);
                float dd = r * (1.5f - p);
                Vector2 ps = c + a.ToRotationVector2() * dd;
                Dust du = Dust.NewDustDirect(ps, 1, 1, DustID.RedTorch, 0, 0, 180, default, 1.2f);
                du.noGravity = true;
                du.velocity = (a + MathHelper.PiOver2).ToRotationVector2() * 3f + (c - ps).SafeNormalize(Vector2.Zero) * 2f * p;
            }
        }

        private void DBurst(Vector2 c, int n, float s, float sc2)
        {
            for (int i = 0; i < n; i++)
            {
                Dust d = Dust.NewDustDirect(c, 1, 1, DustID.RedTorch, 0, 0, 100, default, sc2);
                d.noGravity = true; d.velocity = Main.rand.NextVector2CircularEdge(s, s);
            }
        }

        private void AbsDust(int n, float md, float p, int ty)
        {
            for (int i = 0; i < n; i++)
            {
                float a = Main.rand.NextFloat(MathHelper.TwoPi);
                float dd = md * (1f - p * 0.4f);
                Vector2 ps = NPC.Center + a.ToRotationVector2() * dd;
                Dust du = Dust.NewDustDirect(ps, 1, 1, ty, 0, 0, 100, default, 1.5f);
                du.noGravity = true;
                du.velocity = (NPC.Center - ps).SafeNormalize(Vector2.Zero) * (4f + p * 6f);
            }
        }

        private void Fls(Color c, float i)
        {
            var cfg = ModContent.GetInstance<GodEyeConfig>();
            if (cfg != null && cfg.DisableFlashing) return;
            if (Main.netMode != NetmodeID.Server) GodBossVisualSystem.TriggerFlash(c, i);
        }

        private void Shk(float i, int d)
        {
            var cfg = ModContent.GetInstance<GodEyeConfig>();
            if (cfg != null && cfg.DisableScreenShake) return;
            if (Main.netMode != NetmodeID.Server) GodBossVisualSystem.TriggerShake(i, d);
        }

        private float EaseO(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * MathF.Pow(t - 1, 3) + c1 * MathF.Pow(t - 1, 2);
        }

        public override bool PreDraw(SpriteBatch sb, Vector2 sp, Color dc)
        {
            if (NPC.Opacity <= 0.01f) return false;
            Texture2D tx = Terraria.GameContent.TextureAssets.Npc[NPC.type].Value;
            Vector2 og = new Vector2(FW / 2f, FH / 2f); Vector2 dp = NPC.Center - sp;

            if (em > 0)
            {
                float a2 = sc * (1.15f + em * 0.05f);
                float ap = 0.1f + (float)Math.Sin(pls * 2.5f) * 0.05f;
                Color ac = em switch { 1 => Color.Red * ap * NPC.Opacity, 2 => Color.OrangeRed * ap * NPC.Opacity,
                    _ => Color.DarkRed * (ap * 1.5f) * NPC.Opacity };
                for (int i = 0; i < 3; i++)
                    sb.Draw(tx, dp, NPC.frame, ac, ar * 0.1f + rt + MathHelper.TwoPi / 3f * i,
                        og, a2 + i * 0.02f, SpriteEffects.None, 0f);
            }

            for (int i = NPCID.Sets.TrailCacheLength[NPC.type] - 1; i >= 0; i--)
            {
                float ta = (1f - (float)i / NPCID.Sets.TrailCacheLength[NPC.type]) * 0.25f * NPC.Opacity;
                Vector2 tp = NPC.oldPos[i] + NPC.Size / 2f - sp;
                sb.Draw(tx, tp, NPC.frame, Color.Lerp(Color.DarkRed, Color.Red, ta) * ta, NPC.oldRot[i],
                    og, sc * (0.85f + 0.15f * ta), SpriteEffects.None, 0f);
            }

            Color gl2 = Color.Red * (0.2f + eg * 0.2f) * NPC.Opacity;
            for (int g = 0; g < 6; g++)
            {
                float ga = MathHelper.TwoPi / 6f * g + ar * 2f;
                float gd = 3f + (float)Math.Sin(pls * 4f + g) * 2f;
                sb.Draw(tx, dp + ga.ToRotationVector2() * gd, NPC.frame, gl2, rt, og, sc * 1.04f, SpriteEffects.None, 0f);
            }

            sb.Draw(tx, dp, NPC.frame, dc * NPC.Opacity, rt, og, sc, SpriteEffects.None, 0f);

            Texture2D px = Terraria.GameContent.TextureAssets.MagicPixel.Value;
            float gs = 6f * sc * eg;
            Color ec = Color.Red * (0.6f + eg * 0.4f) * NPC.Opacity;
            sb.Draw(px, new Rectangle((int)(dp.X - gs / 2f), (int)(dp.Y - gs / 2f), (int)gs, (int)gs), ec);
            return false;
        }
    }
}