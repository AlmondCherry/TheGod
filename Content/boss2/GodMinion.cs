using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using GodEyeMod.Content.boss2.Projectiles;

namespace GodEyeMod.Content.boss2
{
    public class GodMinion : ModNPC
    {
        private float orbitAngle;
        private int parentIndex = -1;
        private float shootTimer = 0f;
        private float orbitSpeed = 0.02f;
        private float orbitRadius = 300f;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 40;
            NPC.height = 40;
            NPC.damage = 0;
            NPC.defense = 50;
            NPC.lifeMax = 50000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.dontTakeDamage = true;
            NPC.immortal = true;
            NPC.aiStyle = -1;
            NPC.lavaImmune = true;
            NPC.Opacity = 0.8f;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            parentIndex = (int)NPC.ai[0];
            orbitAngle = NPC.ai[1];
            orbitSpeed = 0.015f + Main.rand.NextFloat(0.01f);
            orbitRadius = 250f + Main.rand.NextFloat(150f);
        }

        public override void AI()
        {
            if (parentIndex < 0 || parentIndex >= Main.maxNPCs || !Main.npc[parentIndex].active)
            {
                NPC.active = false;
                return;
            }

            NPC parent = Main.npc[parentIndex];
            orbitAngle += orbitSpeed;
            Vector2 targetPos = parent.Center + orbitAngle.ToRotationVector2() * orbitRadius;
            NPC.Center = Vector2.Lerp(NPC.Center, targetPos, 0.08f);
            NPC.rotation = (Main.player[parent.target].Center - NPC.Center).ToRotation();

            shootTimer += 1f / 60f;
            if (shootTimer >= 1.5f)
            {
                shootTimer = 0f;
                Player player = Main.player[parent.target];
                if (player.active && !player.dead && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 dir = (player.Center - NPC.Center).SafeNormalize(Vector2.Zero);
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir * 10f,
                        ModContent.ProjectileType<GodMinionProjectile>(), 80, 0f, Main.myPlayer);
                }
            }

            Lighting.AddLight(NPC.Center, 0.8f, 0.1f, 0.1f);

            for (int i = 0; i < 2; i++)
            {
                Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.RedTorch, 0, 0, 150, default, 0.8f);
                d.noGravity = true;
                d.velocity *= 0.3f;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Npc[NPC.type].Value;
            Vector2 drawOrigin = texture.Size() / 2f;
            Vector2 drawPos = NPC.Center - screenPos;

            float pulse = 0.9f + (float)Math.Sin(Main.GameUpdateCount * 0.1f + orbitAngle) * 0.1f;

            Color glowColor = Color.Red * 0.4f * NPC.Opacity;
            for (int g = 0; g < 4; g++)
            {
                Vector2 offset = (MathHelper.PiOver2 * g).ToRotationVector2() * 3f;
                spriteBatch.Draw(texture, drawPos + offset, null, glowColor, NPC.rotation,
                    drawOrigin, pulse * 1.1f, SpriteEffects.None, 0f);
            }

            spriteBatch.Draw(texture, drawPos, null, drawColor * NPC.Opacity, NPC.rotation,
                drawOrigin, pulse, SpriteEffects.None, 0f);

            return false;
        }
    }
}