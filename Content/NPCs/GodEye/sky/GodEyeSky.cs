using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using GodEyeMod.Content.Items;

namespace GodEyeMod.Content.NPCs.GodEye
{
    public class GodEyeSky : CustomSky
    {
        private bool _isActive;
        private float _opacity;
        private float _timer;

        private Asset<Texture2D> _eyeTexture;
        private const int EYE_FRAME_COUNT = 6;
        private const int EYE_TEX_WIDTH = 400;
        private const int EYE_TEX_HEIGHT = 2400;
        private const int EYE_FRAME_HEIGHT = EYE_TEX_HEIGHT / EYE_FRAME_COUNT;

        private bool _bossMode = false;

        private int _currentPhase = 0;
        private bool _bossWasAlive = false;

        private int _phaseFlashTimer = 0;
        private Color _phaseFlashColor = Color.Transparent;
        private float _transitionShake = 0f;

        private struct CorruptedStar
        {
            public Vector2 Position;
            public float BaseSize, Speed, Phase, FlickerSpeed, Brightness, TwinkleOffset, Depth;
            public int ColorType;
            public bool HasCross, HasHalo;
        }
        private CorruptedStar[] _stars;
        private const int STAR_COUNT = 250;

        private struct SkyEyeEntity
        {
            public Vector2 Position, Velocity;
            public float Scale, Rotation, RotationSpeed, Opacity, FrameTimer;
            public int CurrentFrame, BehaviorType;
            public float LifeTime, MaxLifeTime, BehaviorParam, Depth, PulsePhase;
            public Color Tint;
        }
        private SkyEyeEntity[] _skyEyes;
        private const int MAX_SKY_EYES = 50;
        private int _eyeSpawnTimer;

        private struct SkyLightning
        {
            public List<Vector2> Points;
            public float Life, MaxLife, Thickness, GlowSize;
            public Color BaseColor;
            public List<LightningBranch> Branches;
        }
        private struct LightningBranch
        {
            public List<Vector2> Points;
            public float Thickness, OpacityMul;
        }
        private SkyLightning[] _lightnings;
        private const int MAX_LIGHTNINGS = 15;
        private int _lightningTimer;

        private struct Nebula
        {
            public Vector2 BasePosition;
            public float Size, DriftSpeed, DriftPhase, PulseSpeed, Rotation, RotationSpeed;
            public Color BaseColor;
            public int LayerCount;
        }
        private Nebula[] _nebulae;
        private const int NEBULA_COUNT = 10;

        private struct ShootingStar
        {
            public Vector2 Position, Velocity;
            public float Life, MaxLife, Thickness, TrailLength;
            public Color StarColor;
        }
        private ShootingStar[] _shootingStars;
        private const int MAX_SHOOTING_STARS = 6;

        private struct CosmicParticle
        {
            public Vector2 Position;
            public float SpeedX, SpeedY, Size, Phase;
            public Color ParticleColor;
        }
        private CosmicParticle[] _cosmicParticles;
        private const int MAX_COSMIC = 80;

        private struct PulseRing
        {
            public float Radius, MaxRadius, Speed, Opacity;
            public Color RingColor;
        }
        private PulseRing[] _pulseRings;
        private const int MAX_RINGS = 8;
        private int _ringSpawnTimer;

        private struct VoidShadow
        {
            public Vector2 Position;
            public float Size, Opacity, Phase, DriftX, DriftY;
        }
        private VoidShadow[] _voidShadows;
        private const int MAX_SHADOWS = 15;

        private static readonly Color VOID_TOP = new Color(2, 0, 8);
        private static readonly Color VOID_MID = new Color(8, 0, 18);
        private static readonly Color VOID_BOT = new Color(5, 0, 10);

        private static readonly Color COSMIC_TOP = new Color(3, 0, 15);
        private static readonly Color COSMIC_MID = new Color(15, 2, 35);
        private static readonly Color COSMIC_BOT = new Color(8, 0, 20);

        private static readonly Color OMNI_TOP = new Color(5, 0, 12);
        private static readonly Color OMNI_MID = new Color(12, 0, 28);
        private static readonly Color OMNI_BOT = new Color(8, 0, 15);

        private Color _curTop, _curMid, _curBot;

        public override void OnLoad()
        {
            _eyeTexture = ModContent.Request<Texture2D>(
                "GodEyeMod/Content/NPCs/GodEye/GodEyeBoss",
                AssetRequestMode.ImmediateLoad);

            InitStars();
            InitNebulae();

            _lightnings = new SkyLightning[MAX_LIGHTNINGS];
            _skyEyes = new SkyEyeEntity[MAX_SKY_EYES];
            _shootingStars = new ShootingStar[MAX_SHOOTING_STARS];

            _cosmicParticles = new CosmicParticle[MAX_COSMIC];
            InitCosmicParticles();

            _pulseRings = new PulseRing[MAX_RINGS];

            _voidShadows = new VoidShadow[MAX_SHADOWS];
            InitVoidShadows();

            _curTop = OMNI_TOP;
            _curMid = OMNI_MID;
            _curBot = OMNI_BOT;
        }

        private void InitStars()
        {
            _stars = new CorruptedStar[STAR_COUNT];
            for (int i = 0; i < STAR_COUNT; i++)
            {
                _stars[i] = new CorruptedStar
                {
                    Position = new Vector2(Main.rand.NextFloat(), Main.rand.NextFloat(0f, 0.75f)),
                    BaseSize = Main.rand.NextFloat(1f, 4.5f),
                    Speed = Main.rand.NextFloat(0.00003f, 0.0004f),
                    Phase = Main.rand.NextFloat(MathHelper.TwoPi),
                    ColorType = Main.rand.Next(5),
                    FlickerSpeed = Main.rand.NextFloat(1f, 7f),
                    Brightness = Main.rand.NextFloat(0.3f, 1f),
                    HasCross = Main.rand.NextBool(5),
                    HasHalo = Main.rand.NextBool(10),
                    TwinkleOffset = Main.rand.NextFloat(100f),
                    Depth = Main.rand.NextFloat(0.3f, 1f)
                };
            }
        }

        private void InitNebulae()
        {
            _nebulae = new Nebula[NEBULA_COUNT];
            for (int i = 0; i < NEBULA_COUNT; i++)
            {
                _nebulae[i] = new Nebula
                {
                    BasePosition = new Vector2(Main.rand.NextFloat(0.05f, 0.95f), Main.rand.NextFloat(0.05f, 0.5f)),
                    Size = Main.rand.NextFloat(80f, 250f),
                    BaseColor = new Color(Main.rand.Next(50, 140), Main.rand.Next(0, 15), Main.rand.Next(60, 170)),
                    DriftSpeed = Main.rand.NextFloat(0.01f, 0.03f),
                    DriftPhase = Main.rand.NextFloat(MathHelper.TwoPi),
                    LayerCount = Main.rand.Next(3, 6),
                    PulseSpeed = Main.rand.NextFloat(0.3f, 1f),
                    Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
                    RotationSpeed = Main.rand.NextFloat(-0.001f, 0.001f)
                };
            }
        }

        private void InitCosmicParticles()
        {
            for (int i = 0; i < MAX_COSMIC; i++)
            {
                _cosmicParticles[i] = new CosmicParticle
                {
                    Position = new Vector2(Main.rand.NextFloat(Main.screenWidth), Main.rand.NextFloat(Main.screenHeight * 0.7f)),
                    SpeedX = Main.rand.NextFloat(-0.3f, 0.3f),
                    SpeedY = Main.rand.NextFloat(-0.5f, -0.1f),
                    Size = Main.rand.NextFloat(1f, 3.5f),
                    Phase = Main.rand.NextFloat(MathHelper.TwoPi),
                    ParticleColor = Main.rand.Next(4) switch
                    {
                        0 => new Color(140, 60, 255),
                        1 => new Color(255, 130, 40),
                        2 => new Color(80, 200, 255),
                        _ => new Color(200, 80, 255)
                    }
                };
            }
        }

        private void InitVoidShadows()
        {
            for (int i = 0; i < MAX_SHADOWS; i++)
            {
                _voidShadows[i] = new VoidShadow
                {
                    Position = new Vector2(Main.rand.NextFloat(Main.screenWidth), Main.rand.NextFloat(Main.screenHeight * 0.6f)),
                    Size = Main.rand.NextFloat(60, 200),
                    Opacity = Main.rand.NextFloat(0.02f, 0.08f),
                    Phase = Main.rand.NextFloat(MathHelper.TwoPi),
                    DriftX = Main.rand.NextFloat(-0.2f, 0.2f),
                    DriftY = Main.rand.NextFloat(-0.1f, 0.1f)
                };
            }
        }

        private void ResetDynamicElements()
        {
            _currentPhase = 0;
            _phaseFlashTimer = 0;
            _transitionShake = 0f;
            for (int i = 0; i < MAX_SKY_EYES; i++) _skyEyes[i].LifeTime = 0;
            for (int i = 0; i < MAX_LIGHTNINGS; i++) _lightnings[i].Life = 0;
            for (int i = 0; i < MAX_SHOOTING_STARS; i++) _shootingStars[i].Life = 0;
            for (int i = 0; i < MAX_RINGS; i++) _pulseRings[i].Opacity = 0;
            _eyeSpawnTimer = 0;
            _lightningTimer = 0;
            _ringSpawnTimer = 0;
        }

        public override void Update(GameTime gameTime)
        {
            if (_isActive)
                _opacity = Math.Min(1f, _opacity + 0.006f);
            else
                _opacity = Math.Max(0f, _opacity - 0.015f);

            if (_opacity <= 0f && !_isActive) return;

            _timer += 0.016f;

            bool bossAlive = false;
            int bossPhase = 0;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.type == ModContent.NPCType<GodEyeBoss>())
                {
                    bossAlive = true;
                    float lp = (float)npc.life / npc.lifeMax;
                    bossPhase = lp > 0.7f ? 1 : lp > 0.4f ? 2 : lp > 0.15f ? 3 : 4;
                    break;
                }
            }

            if (bossAlive && !_bossWasAlive) ResetDynamicElements();
            _bossWasAlive = bossAlive;
            _bossMode = bossAlive;

            if (bossAlive)
            {
                if (bossPhase != _currentPhase && bossPhase > 0)
                {
                    int old = _currentPhase;
                    _currentPhase = bossPhase;
                    if (old > 0) OnPhaseChange(old, _currentPhase);
                }
            }
            else
            {
                _currentPhase = 0;
            }

            Color targetTop, targetMid, targetBot;
            GodEyeSkyAccessoryMode accMode = GodEyeSkyAccessoryBridge.CurrentMode;

            if (_bossMode)
            {
                targetTop = OMNI_TOP; targetMid = OMNI_MID; targetBot = OMNI_BOT;
            }
            else
            {
                switch (accMode)
                {
                    case GodEyeSkyAccessoryMode.Void:
                        targetTop = VOID_TOP; targetMid = VOID_MID; targetBot = VOID_BOT;
                        break;
                    case GodEyeSkyAccessoryMode.Cosmic:
                        targetTop = COSMIC_TOP; targetMid = COSMIC_MID; targetBot = COSMIC_BOT;
                        break;
                    case GodEyeSkyAccessoryMode.Omniscient:
                        targetTop = OMNI_TOP; targetMid = OMNI_MID; targetBot = OMNI_BOT;
                        break;
                    default:
                        targetTop = OMNI_TOP; targetMid = OMNI_MID; targetBot = OMNI_BOT;
                        break;
                }
            }

            _curTop = LerpColor(_curTop, targetTop, 0.01f);
            _curMid = LerpColor(_curMid, targetMid, 0.01f);
            _curBot = LerpColor(_curBot, targetBot, 0.01f);

            if (_transitionShake > 0) _transitionShake *= 0.93f;
            if (_phaseFlashTimer > 0) _phaseFlashTimer--;

            UpdateStars();
            UpdateNebulae();

            if (_bossMode)
            {
                UpdateLightnings_Boss();
                UpdateSkyEyes_Boss();
            }
            else
            {
                switch (accMode)
                {
                    case GodEyeSkyAccessoryMode.Void:
                        UpdateVoidMode();
                        break;
                    case GodEyeSkyAccessoryMode.Cosmic:
                        UpdateCosmicMode();
                        break;
                    case GodEyeSkyAccessoryMode.Omniscient:
                        UpdateOmniscientMode();
                        break;
                }
            }

            UpdateShootingStars();
        }

        private void OnPhaseChange(int from, int to)
        {
            _transitionShake = 10f + to * 3f;
            _phaseFlashColor = to switch
            {
                2 => new Color(100, 0, 180),
                3 => new Color(180, 0, 100),
                4 => new Color(200, 50, 255),
                _ => Color.Transparent
            };
            _phaseFlashTimer = 10 + to * 5;

            int eyeCount = to switch { 2 => 5, 3 => 10, 4 => 18, _ => 0 };
            int boltCount = to switch { 2 => 3, 3 => 6, 4 => 10, _ => 0 };

            for (int i = 0; i < eyeCount; i++) SpawnSkyEye(0.12f);
            for (int i = 0; i < boltCount; i++) SpawnLightning(new Color(150, 30, 230), 2.5f);
        }

        private void UpdateVoidMode()
        {
            for (int i = 0; i < MAX_SHADOWS; i++)
            {
                ref VoidShadow s = ref _voidShadows[i];
                s.Position.X += s.DriftX + (float)Math.Sin(_timer * 0.3f + s.Phase) * 0.15f;
                s.Position.Y += s.DriftY + (float)Math.Cos(_timer * 0.2f + s.Phase) * 0.1f;

                if (s.Position.X < -s.Size) s.Position.X = Main.screenWidth + s.Size;
                if (s.Position.X > Main.screenWidth + s.Size) s.Position.X = -s.Size;
                if (s.Position.Y < -s.Size) s.Position.Y = Main.screenHeight * 0.5f;
                if (s.Position.Y > Main.screenHeight * 0.6f) s.Position.Y = -s.Size;
            }

            _eyeSpawnTimer++;
            int active = 0;
            for (int i = 0; i < MAX_SKY_EYES; i++) if (_skyEyes[i].LifeTime > 0) active++;

            if (_eyeSpawnTimer >= 200 && active < 5)
            {
                _eyeSpawnTimer = 0;
                SpawnSkyEye(0.06f, new Color(120, 80, 160));
            }

            UpdateSkyEyes_Generic();

            _lightningTimer++;
            if (_lightningTimer >= 180)
            {
                _lightningTimer = 0;
                SpawnLightning(new Color(80, 30, 120), 1.5f);
            }
            UpdateLightnings_Generic();
        }

        private void UpdateCosmicMode()
        {
            for (int i = 0; i < MAX_COSMIC; i++)
            {
                ref CosmicParticle p = ref _cosmicParticles[i];
                p.Position.X += p.SpeedX + (float)Math.Sin(_timer * 1.2f + p.Phase) * 0.2f;
                p.Position.Y += p.SpeedY;

                if (p.Position.Y < -10)
                {
                    p.Position.Y = Main.screenHeight * 0.65f;
                    p.Position.X = Main.rand.NextFloat(Main.screenWidth);
                }
                if (p.Position.X < -10) p.Position.X = Main.screenWidth + 5;
                if (p.Position.X > Main.screenWidth + 10) p.Position.X = -5;
            }

            _eyeSpawnTimer++;
            int active = 0;
            for (int i = 0; i < MAX_SKY_EYES; i++) if (_skyEyes[i].LifeTime > 0) active++;

            if (_eyeSpawnTimer >= 80 && active < 10)
            {
                _eyeSpawnTimer = 0;
                SpawnSkyEye(0.09f, new Color(160, 130, 255));
            }

            UpdateSkyEyes_Generic();

            _lightningTimer++;
            if (_lightningTimer >= 40)
            {
                _lightningTimer = 0;
                Color col = Main.rand.Next(3) switch
                {
                    0 => new Color(100, 150, 255),
                    1 => new Color(200, 100, 255),
                    _ => new Color(150, 200, 255)
                };
                SpawnLightning(col, 2.5f);
            }
            UpdateLightnings_Generic();
        }

        private void UpdateOmniscientMode()
        {
            for (int i = 0; i < MAX_COSMIC; i++)
            {
                ref CosmicParticle p = ref _cosmicParticles[i];
                p.Position.X += p.SpeedX;
                p.Position.Y += p.SpeedY;
                if (p.Position.Y < -10)
                {
                    p.Position.Y = Main.screenHeight * 0.65f;
                    p.Position.X = Main.rand.NextFloat(Main.screenWidth);
                }
            }

            for (int i = 0; i < MAX_SHADOWS; i++)
            {
                ref VoidShadow s = ref _voidShadows[i];
                s.Position.X += s.DriftX;
                s.Position.Y += s.DriftY;
                if (s.Position.X < -s.Size) s.Position.X = Main.screenWidth;
                if (s.Position.Y < -s.Size) s.Position.Y = Main.screenHeight * 0.5f;
            }

            _eyeSpawnTimer++;
            int active = 0;
            for (int i = 0; i < MAX_SKY_EYES; i++) if (_skyEyes[i].LifeTime > 0) active++;

            if (_eyeSpawnTimer >= 40 && active < 20)
            {
                _eyeSpawnTimer = 0;
                SpawnSkyEye(0.12f, new Color(200, 160, 255));
            }

            UpdateSkyEyes_Generic();

            _lightningTimer++;
            if (_lightningTimer >= 25)
            {
                _lightningTimer = 0;
                SpawnLightning(new Color(170, 50, 240), 3f);
            }
            UpdateLightnings_Generic();

            _ringSpawnTimer++;
            if (_ringSpawnTimer >= 120)
            {
                _ringSpawnTimer = 0;
                SpawnPulseRing();
            }
            UpdatePulseRings();
        }

        private void UpdateLightnings_Boss()
        {
            _lightningTimer++;
            int rate = _currentPhase switch { 1 => 80, 2 => 40, 3 => 18, 4 => 8, _ => 120 };
            if (_lightningTimer >= rate)
            {
                _lightningTimer = 0;
                SpawnLightning(new Color(150, 30, 230), 2f + _currentPhase * 0.4f);
            }
            UpdateLightnings_Generic();
        }

        private void UpdateSkyEyes_Boss()
        {
            _eyeSpawnTimer++;
            int max = _currentPhase switch { 1 => 5, 2 => 12, 3 => 22, 4 => 35, _ => 3 };
            int rate = _currentPhase switch { 1 => 120, 2 => 50, 3 => 25, 4 => 12, _ => 200 };

            int active = 0;
            for (int i = 0; i < MAX_SKY_EYES; i++) if (_skyEyes[i].LifeTime > 0) active++;

            if (_eyeSpawnTimer >= rate && active < max)
            {
                _eyeSpawnTimer = 0;
                SpawnSkyEye(0.07f + _currentPhase * 0.025f);
            }
            UpdateSkyEyes_Generic();
        }

        private void UpdateStars()
        {
            for (int i = 0; i < STAR_COUNT; i++)
            {
                _stars[i].Position.X += _stars[i].Speed * _stars[i].Depth;
                if (_stars[i].Position.X > 1.02f) _stars[i].Position.X = -0.02f;
            }
        }

        private void UpdateNebulae()
        {
            for (int i = 0; i < NEBULA_COUNT; i++)
                _nebulae[i].Rotation += _nebulae[i].RotationSpeed;
        }

        private void UpdateLightnings_Generic()
        {
            for (int i = 0; i < MAX_LIGHTNINGS; i++)
                if (_lightnings[i].Life > 0) _lightnings[i].Life--;
        }

        private void UpdateSkyEyes_Generic()
        {
            for (int i = 0; i < MAX_SKY_EYES; i++)
            {
                if (_skyEyes[i].LifeTime <= 0) continue;
                ref SkyEyeEntity eye = ref _skyEyes[i];
                eye.LifeTime--;

                eye.FrameTimer += 0.07f;
                if (eye.FrameTimer >= 1f)
                {
                    eye.FrameTimer = 0f;
                    eye.CurrentFrame = (eye.CurrentFrame + 1) % EYE_FRAME_COUNT;
                }

                float lr = eye.LifeTime / eye.MaxLifeTime;
                if (lr > 0.85f) eye.Opacity = (1f - lr) / 0.15f;
                else if (lr < 0.15f) eye.Opacity = lr / 0.15f;
                else eye.Opacity = 1f;

                eye.Position += eye.Velocity;
                eye.Velocity *= 0.998f;
                eye.Rotation += eye.RotationSpeed;

                float m = 80f;
                if (eye.Position.X < -m) eye.Position.X = Main.screenWidth + m;
                if (eye.Position.X > Main.screenWidth + m) eye.Position.X = -m;
                if (eye.Position.Y < -m) eye.Position.Y = Main.screenHeight * 0.5f;
                if (eye.Position.Y > Main.screenHeight * 0.65f) eye.Position.Y = -m;
            }
        }

        private void UpdateShootingStars()
        {
            if (Main.rand.NextBool(60)) SpawnShootingStar();
            for (int i = 0; i < MAX_SHOOTING_STARS; i++)
            {
                if (_shootingStars[i].Life <= 0) continue;
                _shootingStars[i].Life--;
                _shootingStars[i].Position += _shootingStars[i].Velocity;
            }
        }

        private void UpdatePulseRings()
        {
            for (int i = 0; i < MAX_RINGS; i++)
            {
                ref PulseRing r = ref _pulseRings[i];
                if (r.Opacity <= 0) continue;
                r.Radius += r.Speed;
                if (r.Radius > r.MaxRadius * 0.7f)
                    r.Opacity -= 0.008f;
                if (r.Opacity <= 0) r.Opacity = 0;
            }
        }

        private void SpawnLightning(Color color, float thicknessMul)
        {
            for (int i = 0; i < MAX_LIGHTNINGS; i++)
            {
                if (_lightnings[i].Life > 0) continue;

                Vector2 start = new Vector2(
                    Main.rand.NextFloat(Main.screenWidth * 0.05f, Main.screenWidth * 0.95f),
                    Main.rand.NextFloat(0, Main.screenHeight * 0.1f));
                Vector2 end = new Vector2(
                    start.X + Main.rand.NextFloat(-400, 400),
                    Main.rand.NextFloat(Main.screenHeight * 0.15f, Main.screenHeight * 0.65f));

                var points = GenPath(start, end, 12, 55f);
                var branches = new List<LightningBranch>();
                int bc = 1 + Main.rand.Next(3);
                for (int b = 0; b < bc; b++)
                {
                    if (points.Count < 3) break;
                    int bs = Main.rand.Next(2, points.Count - 1);
                    branches.Add(new LightningBranch
                    {
                        Points = GenPath(points[bs], points[bs] + new Vector2(Main.rand.NextFloat(-150, 150), Main.rand.NextFloat(20, 200)), 6, 28f),
                        Thickness = Main.rand.NextFloat(0.5f, 1.5f),
                        OpacityMul = Main.rand.NextFloat(0.25f, 0.6f)
                    });
                }

                color.R = (byte)MathHelper.Clamp(color.R + Main.rand.Next(-30, 30), 0, 255);
                color.B = (byte)MathHelper.Clamp(color.B + Main.rand.Next(-30, 30), 0, 255);

                _lightnings[i] = new SkyLightning
                {
                    Points = points,
                    Life = Main.rand.Next(4, 14),
                    MaxLife = 14,
                    BaseColor = color,
                    Thickness = Main.rand.NextFloat(1.5f, 3f) * thicknessMul,
                    GlowSize = Main.rand.NextFloat(8f, 18f) * thicknessMul,
                    Branches = branches
                };
                break;
            }
        }

        private void SpawnSkyEye(float maxScale, Color? tint = null)
        {
            for (int i = 0; i < MAX_SKY_EYES; i++)
            {
                if (_skyEyes[i].LifeTime > 0) continue;

                Vector2 pos = new Vector2(
                    Main.rand.NextBool() ? Main.rand.Next(-40, 0) : Main.rand.Next(Main.screenWidth, Main.screenWidth + 40),
                    Main.rand.NextFloat(20, Main.screenHeight * 0.5f));
                Vector2 vel = new Vector2(
                    pos.X < Main.screenWidth / 2 ? Main.rand.NextFloat(0.15f, 0.7f) : Main.rand.NextFloat(-0.7f, -0.15f),
                    Main.rand.NextFloat(-0.1f, 0.1f));

                Color t = tint ?? new Color(180 + Main.rand.Next(-20, 20), 140 + Main.rand.Next(-20, 20), 230 + Main.rand.Next(-15, 15));
                float life = Main.rand.NextFloat(250, 550);

                _skyEyes[i] = new SkyEyeEntity
                {
                    Position = pos, Velocity = vel,
                    Scale = Main.rand.NextFloat(0.025f, maxScale),
                    Rotation = Main.rand.NextFloat(-0.2f, 0.2f),
                    RotationSpeed = Main.rand.NextFloat(-0.003f, 0.003f),
                    Opacity = 0f,
                    FrameTimer = Main.rand.NextFloat(1f),
                    CurrentFrame = Main.rand.Next(EYE_FRAME_COUNT),
                    LifeTime = life, MaxLifeTime = life,
                    BehaviorType = 0,
                    BehaviorParam = 0,
                    Depth = Main.rand.NextFloat(0.4f, 1f),
                    Tint = t,
                    PulsePhase = Main.rand.NextFloat(MathHelper.TwoPi)
                };
                break;
            }
        }

        private void SpawnShootingStar()
        {
            for (int i = 0; i < MAX_SHOOTING_STARS; i++)
            {
                if (_shootingStars[i].Life > 0) continue;
                float angle = Main.rand.NextFloat(0.3f, 1.2f);
                float speed = Main.rand.NextFloat(8f, 18f);

                _shootingStars[i] = new ShootingStar
                {
                    Position = new Vector2(Main.rand.NextFloat(-80, Main.screenWidth), Main.rand.NextFloat(-30, Main.screenHeight * 0.15f)),
                    Velocity = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed),
                    Life = Main.rand.Next(15, 40), MaxLife = 40,
                    StarColor = Main.rand.Next(3) switch
                    {
                        0 => new Color(180, 100, 255),
                        1 => new Color(255, 200, 100),
                        _ => new Color(120, 180, 255)
                    },
                    Thickness = Main.rand.NextFloat(1f, 2.5f),
                    TrailLength = Main.rand.NextFloat(40f, 100f)
                };
                break;
            }
        }

        private void SpawnPulseRing()
        {
            for (int i = 0; i < MAX_RINGS; i++)
            {
                if (_pulseRings[i].Opacity > 0) continue;
                _pulseRings[i] = new PulseRing
                {
                    Radius = 0,
                    MaxRadius = Main.rand.NextFloat(200, 500),
                    Speed = Main.rand.NextFloat(0.8f, 2f),
                    Opacity = Main.rand.NextFloat(0.1f, 0.25f),
                    RingColor = new Color(130 + Main.rand.Next(60), 20, 200 + Main.rand.Next(55))
                };
                break;
            }
        }

        private List<Vector2> GenPath(Vector2 start, Vector2 end, int segs, float maxOff)
        {
            var pts = new List<Vector2> { start };
            Vector2 diff = end - start;
            Vector2 perp = new Vector2(-diff.Y, diff.X);
            if (perp.Length() > 0) perp.Normalize();
            for (int s = 1; s < segs; s++)
            {
                float t = (float)s / segs;
                float off = (float)Math.Sin(t * MathHelper.Pi) * maxOff;
                pts.Add(start + diff * t + perp * Main.rand.NextFloat(-off, off));
            }
            pts.Add(end);
            return pts;
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (_opacity <= 0f) return;
            if (!(minDepth <= 1f && maxDepth >= 1f)) return;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            GodEyeSkyAccessoryMode accMode = _bossMode ? GodEyeSkyAccessoryMode.Omniscient : GodEyeSkyAccessoryBridge.CurrentMode;

            Vector2 shake = Vector2.Zero;
            if (_transitionShake > 0.5f)
                shake = Main.rand.NextVector2Circular(_transitionShake, _transitionShake);

            DrawGradient(spriteBatch, pixel, shake);

            if (accMode == GodEyeSkyAccessoryMode.Void || accMode == GodEyeSkyAccessoryMode.Omniscient)
                DrawVoidShadows(spriteBatch, pixel, shake);

            DrawNebulae(spriteBatch, pixel, shake);
            DrawStars(spriteBatch, pixel, shake);
            DrawShootingStars(spriteBatch, pixel, shake);

            if (accMode == GodEyeSkyAccessoryMode.Cosmic || accMode == GodEyeSkyAccessoryMode.Omniscient)
                DrawCosmicParticles(spriteBatch, pixel, shake);

            DrawLightnings(spriteBatch, pixel, shake);
            DrawSkyEyes(spriteBatch, shake);

            if (accMode == GodEyeSkyAccessoryMode.Omniscient || _bossMode)
                DrawPulseRings(spriteBatch, pixel, shake);

            if (_phaseFlashTimer > 0)
                spriteBatch.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                    _phaseFlashColor * (_phaseFlashTimer / 20f * _opacity * 0.35f));

            DrawVignette(spriteBatch, pixel);
        }

        private void DrawGradient(SpriteBatch sb, Texture2D pixel, Vector2 shake)
        {
            int bands = 30;
            for (int i = 0; i < bands; i++)
            {
                float t = (float)i / bands;
                int h = Main.screenHeight / bands + 2;
                int y = (int)(t * Main.screenHeight + shake.Y);

                Color c = t < 0.4f
                    ? LerpColor(_curTop, _curMid, t / 0.4f)
                    : LerpColor(_curMid, _curBot, (t - 0.4f) / 0.6f);

                float w = (float)Math.Sin(_timer * 0.25f + t * 3f) * 0.05f + 1f;
                c = new Color((int)MathHelper.Clamp(c.R * w, 0, 255),
                    (int)MathHelper.Clamp(c.G * w, 0, 255),
                    (int)MathHelper.Clamp(c.B * w, 0, 255));

                sb.Draw(pixel, new Rectangle((int)shake.X, y, Main.screenWidth, h), c * _opacity);
            }
        }

        private void DrawVoidShadows(SpriteBatch sb, Texture2D pixel, Vector2 shake)
        {
            for (int i = 0; i < MAX_SHADOWS; i++)
            {
                ref VoidShadow s = ref _voidShadows[i];
                float pulse = (float)Math.Sin(_timer * 0.5f + s.Phase) * 0.3f + 0.7f;
                float size = s.Size * pulse;
                float x = s.Position.X + shake.X;
                float y = s.Position.Y + shake.Y;

                for (int layer = 3; layer >= 0; layer--)
                {
                    float ls = size * (0.5f + layer * 0.2f);
                    float la = s.Opacity * pulse * _opacity / (layer + 1);
                    sb.Draw(pixel,
                        new Rectangle((int)(x - ls), (int)(y - ls * 0.5f), (int)(ls * 2), (int)ls),
                        Color.Black * la);
                }
            }
        }

        private void DrawCosmicParticles(SpriteBatch sb, Texture2D pixel, Vector2 shake)
        {
            for (int i = 0; i < MAX_COSMIC; i++)
            {
                ref CosmicParticle p = ref _cosmicParticles[i];
                float fl = (float)Math.Sin(_timer * 2f + p.Phase) * 0.3f + 0.7f;
                float alpha = fl * _opacity * 0.2f;
                int sz = Math.Max(1, (int)(p.Size * fl));
                sb.Draw(pixel,
                    new Rectangle((int)(p.Position.X + shake.X), (int)(p.Position.Y + shake.Y), sz, sz),
                    p.ParticleColor * alpha);
            }
        }

        private void DrawPulseRings(SpriteBatch sb, Texture2D pixel, Vector2 shake)
        {
            Vector2 center = new Vector2(Main.screenWidth * 0.5f + shake.X, Main.screenHeight * 0.3f + shake.Y);

            for (int i = 0; i < MAX_RINGS; i++)
            {
                ref PulseRing r = ref _pulseRings[i];
                if (r.Opacity <= 0) continue;

                int segments = 50;
                for (int s = 0; s < segments; s++)
                {
                    float angle = (float)s / segments * MathHelper.TwoPi;
                    float x = center.X + (float)Math.Cos(angle) * r.Radius;
                    float y = center.Y + (float)Math.Sin(angle) * r.Radius * 0.4f;
                    int dotSize = Math.Max(1, (int)(2 + r.Opacity * 3));

                    sb.Draw(pixel,
                        new Rectangle((int)x, (int)y, dotSize, dotSize),
                        r.RingColor * (r.Opacity * _opacity));
                }
            }
        }

        private void DrawNebulae(SpriteBatch sb, Texture2D pixel, Vector2 shake)
        {
            for (int n = 0; n < NEBULA_COUNT; n++)
            {
                ref Nebula neb = ref _nebulae[n];
                float dX = (float)Math.Sin(_timer * neb.DriftSpeed + neb.DriftPhase) * 0.03f;
                float dY = (float)Math.Cos(_timer * neb.DriftSpeed * 0.7f + neb.DriftPhase) * 0.015f;
                float x = (neb.BasePosition.X + dX) * Main.screenWidth + shake.X;
                float y = (neb.BasePosition.Y + dY) * Main.screenHeight + shake.Y;
                float pulse = (float)Math.Sin(_timer * neb.PulseSpeed + n) * 0.15f + 0.85f;
                float size = neb.Size * pulse;

                for (int layer = neb.LayerCount; layer >= 0; layer--)
                {
                    float lt = (float)layer / neb.LayerCount;
                    float ls = size * (0.2f + lt * 0.8f);
                    float la = (1f - lt * 0.7f) * 0.035f * _opacity * pulse;
                    float w = ls * 2f;
                    float h = ls * 0.5f;
                    sb.Draw(pixel, new Rectangle((int)(x - w / 2), (int)(y - h / 2), (int)w, (int)h), neb.BaseColor * la);
                }
            }
        }

        private void DrawStars(SpriteBatch sb, Texture2D pixel, Vector2 shake)
        {
            for (int i = 0; i < STAR_COUNT; i++)
            {
                ref CorruptedStar star = ref _stars[i];
                float tw = (float)Math.Sin(_timer * star.FlickerSpeed + star.Phase) * 0.3f + 0.7f;
                tw *= (float)Math.Sin(_timer * star.FlickerSpeed * 2.1f + star.TwinkleOffset) * 0.2f + 0.8f;

                float alpha = tw * star.Brightness * _opacity;
                if (alpha <= 0.01f) continue;

                float x = star.Position.X * Main.screenWidth + shake.X * star.Depth;
                float y = star.Position.Y * Main.screenHeight + shake.Y * star.Depth;
                float size = star.BaseSize * (0.8f + tw * 0.3f);

                Color col = star.ColorType switch
                {
                    0 => Color.Lerp(new Color(110, 40, 210), new Color(170, 80, 255), tw),
                    1 => Color.Lerp(new Color(40, 110, 210), new Color(100, 170, 255), tw),
                    2 => Color.Lerp(new Color(170, 170, 195), new Color(230, 230, 255), tw),
                    3 => Color.Lerp(new Color(190, 90, 230), new Color(255, 140, 200), tw),
                    _ => Color.Lerp(new Color(240, 190, 80), new Color(255, 230, 150), tw),
                };

                int ds = Math.Max(1, (int)size);

                if (star.HasHalo && size > 2f)
                {
                    float hs = size * 3.5f;
                    sb.Draw(pixel, new Rectangle((int)(x - hs / 2), (int)(y - hs / 2), (int)hs, (int)hs), col * (alpha * 0.06f));
                }
                if (star.HasCross && size > 2f)
                {
                    float cl = size * 3f * tw;
                    sb.Draw(pixel, new Rectangle((int)(x - cl), (int)y, (int)(cl * 2), 1), col * (alpha * 0.3f));
                    sb.Draw(pixel, new Rectangle((int)x, (int)(y - cl), 1, (int)(cl * 2)), col * (alpha * 0.3f));
                }
                sb.Draw(pixel, new Rectangle((int)(x - ds / 2), (int)(y - ds / 2), ds, ds), col * alpha);
                if (size > 3f) sb.Draw(pixel, new Rectangle((int)x, (int)y, 1, 1), Color.White * (alpha * 0.7f));
            }
        }

        private void DrawShootingStars(SpriteBatch sb, Texture2D pixel, Vector2 shake)
        {
            for (int i = 0; i < MAX_SHOOTING_STARS; i++)
            {
                ref ShootingStar ss = ref _shootingStars[i];
                if (ss.Life <= 0) continue;
                float lr = ss.Life / ss.MaxLife;
                float alpha = _opacity * (lr < 0.3f ? lr / 0.3f : 1f);
                Vector2 tail = ss.Velocity.Length() > 0 ? -Vector2.Normalize(ss.Velocity) : Vector2.Zero;

                for (int s = 0; s < 12; s++)
                {
                    float st = (float)s / 12;
                    Vector2 p = ss.Position + tail * ss.TrailLength * lr * st + shake;
                    float sa = (1f - st) * alpha * 0.5f;
                    float sz = ss.Thickness * (1f - st * 0.8f);
                    int d = Math.Max(1, (int)sz);
                    sb.Draw(pixel, new Rectangle((int)(p.X - d / 2), (int)(p.Y - d / 2), d, d),
                        Color.Lerp(ss.StarColor, Color.Transparent, st * 0.5f) * sa);
                }
                int hs = Math.Max(1, (int)(ss.Thickness * 2));
                sb.Draw(pixel, new Rectangle((int)(ss.Position.X - hs / 2 + shake.X),
                    (int)(ss.Position.Y - hs / 2 + shake.Y), hs, hs), ss.StarColor * alpha);
            }
        }

        private void DrawLightnings(SpriteBatch sb, Texture2D pixel, Vector2 shake)
        {
            for (int i = 0; i < MAX_LIGHTNINGS; i++)
            {
                ref SkyLightning bolt = ref _lightnings[i];
                if (bolt.Life <= 0 || bolt.Points == null) continue;

                float lr = bolt.Life / bolt.MaxLife;
                float fl = Main.rand.NextFloat(0.7f, 1.2f);
                float alpha = lr * _opacity * 0.85f * fl;

                DrawPath(sb, pixel, bolt.Points, bolt.GlowSize, bolt.BaseColor * (alpha * 0.04f), shake);
                DrawPath(sb, pixel, bolt.Points, bolt.GlowSize * 0.35f, bolt.BaseColor * (alpha * 0.1f), shake);
                DrawPath(sb, pixel, bolt.Points, bolt.Thickness, bolt.BaseColor * alpha, shake);
                DrawPath(sb, pixel, bolt.Points, Math.Max(1, bolt.Thickness * 0.35f), Color.White * (alpha * 0.6f), shake);

                if (bolt.Branches != null)
                    foreach (var br in bolt.Branches)
                    {
                        if (br.Points == null) continue;
                        float ba = alpha * br.OpacityMul;
                        DrawPath(sb, pixel, br.Points, bolt.GlowSize * 0.2f, bolt.BaseColor * (ba * 0.05f), shake);
                        DrawPath(sb, pixel, br.Points, br.Thickness, bolt.BaseColor * ba, shake);
                    }
            }
        }

        private void DrawSkyEyes(SpriteBatch sb, Vector2 shake)
        {
            if (_eyeTexture == null || !_eyeTexture.IsLoaded) return;
            Texture2D tex = _eyeTexture.Value;
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            for (int i = 0; i < MAX_SKY_EYES; i++)
            {
                ref SkyEyeEntity eye = ref _skyEyes[i];
                if (eye.LifeTime <= 0 || eye.Opacity <= 0.01f) continue;

                Rectangle src = new Rectangle(0, eye.CurrentFrame * EYE_FRAME_HEIGHT, EYE_TEX_WIDTH, EYE_FRAME_HEIGHT);
                Vector2 origin = new Vector2(EYE_TEX_WIDTH / 2f, EYE_FRAME_HEIGHT / 2f);

                float pulse = (float)Math.Sin(_timer * 1.8f + eye.PulsePhase) * 0.06f + 1f;
                float drawScale = eye.Scale * pulse;
                float drawAlpha = eye.Opacity * _opacity * 0.3f * eye.Depth;
                Vector2 pos = eye.Position + shake * eye.Depth;

                float gs = EYE_FRAME_HEIGHT * drawScale * 0.5f;
                sb.Draw(pixel, new Rectangle((int)(pos.X - gs), (int)(pos.Y - gs * 0.5f), (int)(gs * 2), (int)gs),
                    eye.Tint * (drawAlpha * 0.05f));

                sb.Draw(tex, pos, src, eye.Tint * drawAlpha, eye.Rotation, origin, drawScale, SpriteEffects.None, 0f);
            }
        }

        private void DrawVignette(SpriteBatch sb, Texture2D pixel)
        {
            int bands = 8;
            for (int i = 0; i < bands; i++)
            {
                float t = (float)i / bands;
                float alpha = t * t * 0.12f * _opacity;
                int thickness = Math.Max(1, Main.screenHeight / (bands * 4));
                Color bc = new Color(3, 0, 8) * alpha;
                sb.Draw(pixel, new Rectangle(0, i * thickness, Main.screenWidth, thickness), bc);
                sb.Draw(pixel, new Rectangle(0, Main.screenHeight - (i + 1) * thickness, Main.screenWidth, thickness), bc * 0.5f);
                sb.Draw(pixel, new Rectangle(i * thickness, 0, thickness, Main.screenHeight), bc * 0.3f);
                sb.Draw(pixel, new Rectangle(Main.screenWidth - (i + 1) * thickness, 0, thickness, Main.screenHeight), bc * 0.3f);
            }
        }

        private void DrawPath(SpriteBatch sb, Texture2D pixel, List<Vector2> pts, float thick, Color col, Vector2 shake)
        {
            for (int p = 0; p < pts.Count - 1; p++)
            {
                Vector2 s = pts[p] + shake, e = pts[p + 1] + shake;
                Vector2 d = e - s;
                float len = d.Length();
                if (len < 0.5f) continue;
                sb.Draw(pixel, new Rectangle((int)s.X, (int)s.Y, (int)len, Math.Max(1, (int)thick)),
                    null, col, (float)Math.Atan2(d.Y, d.X), new Vector2(0, 0.5f), SpriteEffects.None, 0f);
            }
        }

        private Color LerpColor(Color a, Color b, float t) =>
            new Color((int)MathHelper.Lerp(a.R, b.R, t), (int)MathHelper.Lerp(a.G, b.G, t),
                (int)MathHelper.Lerp(a.B, b.B, t));

        public override void Activate(Vector2 position, params object[] args) => _isActive = true;
        public override void Deactivate(params object[] args) => _isActive = false;
        public override void Reset() { _isActive = false; _opacity = 0f; _currentPhase = 0; _bossWasAlive = false; }
        public override bool IsActive() => _isActive || _opacity > 0.001f;

        public override Color OnTileColor(Color inColor)
        {
            float ts = _opacity * 0.2f;
            return new Color(
                (int)MathHelper.Clamp(MathHelper.Lerp(inColor.R, inColor.R * 0.7f + 15, ts), 0, 255),
                (int)MathHelper.Clamp(MathHelper.Lerp(inColor.G, inColor.G * 0.5f, ts), 0, 255),
                (int)MathHelper.Clamp(MathHelper.Lerp(inColor.B, inColor.B * 0.6f + 20, ts), 0, 255),
                inColor.A);
        }

        public override float GetCloudAlpha() => 1f - _opacity;
    }
}