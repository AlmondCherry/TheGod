using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GodEyeMod.Content.NPCs.GodEye
{
    public class GodEyeMinion : ModNPC
    {
        private float orbitAngle;
        private int parentIndex = -1;
        private int shootTimer = 0;
        private bool orbiting = false;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 6;
        }

        public override void SetDefaults()
        {
            NPC.width = 400;
            NPC.height = 400;
            NPC.damage = 60;
            NPC.defense = 30;
            NPC.lifeMax = 5000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.knockBackResist = 0.1f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.aiStyle = -1;
            NPC.lavaImmune = true;
            NPC.scale = 0.25f;
        }

        public override void AI()
        {
            parentIndex = (int)NPC.ai[0];
            orbitAngle = NPC.ai[1];

            if (parentIndex >= 0 && parentIndex < Main.maxNPCs && Main.npc[parentIndex].active
                && Main.npc[parentIndex].type == ModContent.NPCType<GodEyeBoss>())
            {
                orbiting = true;
                NPC parentBoss = Main.npc[parentIndex];

                orbitAngle += 0.02f;
                NPC.ai[1] = orbitAngle;

                float orbitRadius = 350f;
                Vector2 targetPos = parentBoss.Center + orbitAngle.ToRotationVector2() * orbitRadius;

                Vector2 moveDir = (targetPos - NPC.Center).SafeNormalize(Vector2.Zero);
                float dist = Vector2.Distance(NPC.Center, targetPos);
                NPC.velocity = moveDir * Math.Min(dist * 0.1f, 20f);
            }
            else
            {
                orbiting = false;
                NPC.TargetClosest(true);
                Player target = Main.player[NPC.target];

                if (!target.active || target.dead)
                {
                    NPC.velocity.Y -= 1f;
                    NPC.EncourageDespawn(30);
                    return;
                }

                Vector2 moveDir = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero);
                float dist = Vector2.Distance(NPC.Center, target.Center);
                float speed = MathHelper.Clamp(dist * 0.02f, 3f, 12f);
                NPC.velocity = Vector2.Lerp(NPC.velocity, moveDir * speed, 0.06f);
            }

            shootTimer++;
            if (shootTimer >= 120)
            {
                shootTimer = 0;
                NPC.TargetClosest(true);
                Player shootTarget = Main.player[NPC.target];

                if (shootTarget.active && !shootTarget.dead && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 vel = (shootTarget.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 8f;
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                        ModContent.ProjectileType<GodEyeLaser>(), 40, 0, Main.myPlayer);
                }
            }

            NPC.TargetClosest(false);
            Player rotTarget = Main.player[NPC.target];
            float targetRot = (rotTarget.Center - NPC.Center).ToRotation();
            NPC.rotation = MathHelper.Lerp(NPC.rotation, targetRot, 0.1f);

            if (Main.rand.NextBool(10))
            {
                Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Vortex, 0, 0, 100, default, 0.8f);
                dust.noGravity = true;
            }
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;
            if (NPC.frameCounter >= 8)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y >= Main.npcFrameCount[NPC.type] * frameHeight)
                    NPC.frame.Y = 0;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Npc[NPC.type].Value;
            int frameHeight = texture.Height / Main.npcFrameCount[NPC.type];
            Vector2 drawOrigin = new Vector2(texture.Width / 2f, frameHeight / 2f);

            if (Main.rand.NextBool(8))
            {
                Vector2 offset = new Vector2(Main.rand.Next(-3, 3), Main.rand.Next(-3, 3));
                spriteBatch.Draw(texture, NPC.Center - screenPos + offset, NPC.frame,
                    Color.Red * 0.4f, NPC.rotation, drawOrigin, NPC.scale, SpriteEffects.None, 0f);
            }

            spriteBatch.Draw(texture, NPC.Center - screenPos, NPC.frame,
                drawColor, NPC.rotation, drawOrigin, NPC.scale, SpriteEffects.None, 0f);

            return false;
        }
    }
}