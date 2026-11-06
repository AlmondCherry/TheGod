using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace GodEyeMod.Content.Menu
{
    // белая пыль, которая поднимается снизу и летит по ветру
    internal class MenuWind
    {
        private class Mote
        {
            public Vector2 Pos;
            public Vector2 Vel;
            public float Life;
            public float MaxLife;
            public float Size;
            public float Depth;
            public float Drag;
            public float Rise;
            public float Phase;
            public float FlutterFreq;
            public float FlutterAmp;
            public bool Spark;
        }

        private const int MaxMotes = 240;

        private readonly List<Mote> motes = new List<Mote>();

        private float time;
        private float gust;
        private float gustTarget;
        private float gustTimer;
        private float spawnAcc;
        private Vector2 lastMouse;
        private bool hasMouse;

        public float Wind { get; private set; }

        public void Reset()
        {
            motes.Clear();
            time = MenuFx.NextFloat(0f, 100f);
            gust = 0f;
            gustTarget = 0f;
            gustTimer = MenuFx.NextFloat(2f, 5f);
            spawnAcc = 0f;
            hasMouse = false;

            Wind = 40f * BaseWind(time);
            for (int i = 0; i < 110; i++)
                Spawn(true);
        }

        private static float BaseWind(float t)
        {
            return 0.6f * MathF.Sin(t * 0.13f)
                 + 0.3f * MathF.Sin(t * 0.31f + 1.7f)
                 + 0.15f * MathF.Sin(t * 0.73f + 4.1f);
        }

        // порыв ветра можно вызвать снаружи, например во время глюка
        public void Gust(float strength)
        {
            float dir = Wind >= 0f ? 1f : -1f;
            gustTarget = dir * strength;
            gustTimer = MenuFx.NextFloat(1.2f, 2.2f);
        }

        public void Update(float dt)
        {
            int w = Main.screenWidth;
            int h = Main.screenHeight;
            time += dt;

            gustTimer -= dt;
            if (gustTimer <= 0f)
            {
                if (gustTarget == 0f && MenuFx.Chance(0.65))
                {
                    float dir = BaseWind(time) >= 0f ? 1f : -1f;
                    if (MenuFx.Chance(0.2))
                        dir = -dir;
                    gustTarget = dir * MenuFx.NextFloat(60f, 150f);
                    gustTimer = MenuFx.NextFloat(1.5f, 3.5f);
                }
                else
                {
                    gustTarget = 0f;
                    gustTimer = MenuFx.NextFloat(3f, 9f);
                }
            }
            gust += (gustTarget - gust) * (1f - MathF.Exp(-dt * 1.1f));
            Wind = 45f * BaseWind(time) + gust;

            Vector2 mouse = Main.MouseScreen;
            Vector2 mouseVel = Vector2.Zero;
            if (hasMouse && dt > 0f)
            {
                mouseVel = (mouse - lastMouse) / dt;
                if (mouseVel.Length() > 2500f)
                    mouseVel = Vector2.Normalize(mouseVel) * 2500f;
            }
            lastMouse = mouse;
            hasMouse = true;
            float mouseSpeed = mouseVel.Length();

            float rate = 16f + Math.Abs(gust) * 0.08f;
            spawnAcc += rate * dt;
            while (spawnAcc >= 1f)
            {
                spawnAcc -= 1f;
                Spawn(false);
            }

            float smooth = 1f - MathF.Exp(-dt * 6f);

            for (int i = motes.Count - 1; i >= 0; i--)
            {
                Mote m = motes[i];
                m.Life -= dt;

                if (m.Life <= 0f || m.Pos.Y < -40f || m.Pos.X < -120f || m.Pos.X > w + 120f || m.Pos.Y > h + 80f)
                {
                    motes.RemoveAt(i);
                    continue;
                }

                // чем выше над землёй, тем сильнее дует
                float height = MathHelper.Clamp(1f - m.Pos.Y / h, 0f, 1f);
                float windHere = Wind * (0.45f + 0.55f * m.Depth) * (0.6f + 0.6f * height);

                float turbX = MathF.Sin(m.Pos.Y * 0.011f + time * 1.3f + m.Phase) * 16f
                            + MathF.Sin(m.Pos.X * 0.006f - time * 0.8f) * 9f;
                float turbY = MathF.Cos(m.Pos.X * 0.009f + time * 1.1f + m.Phase) * 12f;

                float flutter = MathF.Sin(time * m.FlutterFreq + m.Phase) * m.FlutterAmp;

                Vector2 target = new Vector2(windHere + turbX + flutter, -m.Rise + turbY);
                m.Vel += (target - m.Vel) * (1f - MathF.Exp(-m.Drag * dt));

                // курсор раздувает пыль
                if (mouseSpeed > 30f)
                {
                    float radius = 90f + 70f * m.Depth;
                    float dist = Vector2.Distance(m.Pos, mouse);
                    if (dist < radius)
                    {
                        float k = 1f - dist / radius;
                        k *= k;
                        m.Vel += mouseVel * (k * 0.35f * smooth);
                        if (dist > 0.5f)
                            m.Vel += (m.Pos - mouse) / dist * (k * mouseSpeed * 0.04f * smooth);
                    }
                }

                m.Pos += m.Vel * dt;
            }
        }

        private void Spawn(bool anywhere)
        {
            if (motes.Count >= MaxMotes)
                return;

            int w = Main.screenWidth;
            int h = Main.screenHeight;

            float depth = MenuFx.NextFloat();
            bool spark = MenuFx.Chance(0.05);

            Vector2 pos;
            if (anywhere)
            {
                pos = new Vector2(MenuFx.NextFloat(-40f, w + 40f), MenuFx.NextFloat(h * 0.3f, h + 20f));
            }
            else if (Math.Abs(Wind) > 50f && MenuFx.Chance(0.3))
            {
                // при сильном ветре подкидываем пыль с наветренной стороны, чтобы край не пустел
                pos = new Vector2(Wind > 0f ? -30f : w + 30f, MenuFx.NextFloat(h * 0.55f, h + 10f));
            }
            else
            {
                pos = new Vector2(MenuFx.NextFloat(-100f, w + 100f), h + MenuFx.NextFloat(5f, 40f));
            }

            float life = spark ? MenuFx.NextFloat(3f, 6f) : MenuFx.NextFloat(7f, 16f);

            motes.Add(new Mote
            {
                Pos = pos,
                Vel = new Vector2(Wind * 0.5f, -MenuFx.NextFloat(10f, 40f)),
                Life = anywhere ? life * MenuFx.NextFloat(0.3f, 1f) : life,
                MaxLife = life,
                Size = spark ? MenuFx.NextFloat(1.2f, 2f) : MathHelper.Lerp(1.1f, 3.8f, depth * depth),
                Depth = depth,
                Drag = MathHelper.Lerp(2.6f, 1.1f, depth) * MenuFx.NextFloat(0.8f, 1.2f),
                Rise = MathHelper.Lerp(14f, 46f, depth) * MenuFx.NextFloat(0.7f, 1.3f) * (spark ? 1.8f : 1f),
                Phase = MenuFx.NextFloat(0f, MathHelper.TwoPi),
                FlutterFreq = MenuFx.NextFloat(1.5f, 4f),
                FlutterAmp = MenuFx.NextFloat(4f, 18f),
                Spark = spark
            });
        }

        // рисуем слоями: дальние за глазами, ближние поверх
        public void Draw(SpriteBatch sb, float minDepth, float maxDepth)
        {
            if (motes.Count == 0)
                return;

            Texture2D dot = MenuFx.SoftDot;
            Vector2 origin = new Vector2(dot.Width / 2f, dot.Height / 2f);
            float texSize = dot.Width;
            int h = Main.screenHeight;

            foreach (Mote m in motes)
            {
                if (m.Depth < minDepth || m.Depth >= maxDepth)
                    continue;

                float lifeRatio = m.Life / m.MaxLife;
                float fade = 1f;
                if (lifeRatio > 0.9f)
                    fade = (1f - lifeRatio) / 0.1f;
                else if (lifeRatio < 0.25f)
                    fade = lifeRatio / 0.25f;

                // наверху пыль растворяется
                float heightFade = MathHelper.Clamp((m.Pos.Y - h * 0.08f) / (h * 0.45f), 0f, 1f);

                float alpha = MathHelper.Lerp(0.16f, 0.7f, m.Depth) * fade * heightFade;
                if (m.Spark)
                    alpha = Math.Min(1f, alpha * 1.6f + 0.15f * fade * heightFade);
                if (alpha <= 0.005f)
                    continue;

                float speed = m.Vel.Length();
                float stretch = 1f + MathHelper.Clamp(speed / 260f, 0f, 1.4f);
                float rot = speed > 1f ? MathF.Atan2(m.Vel.Y, m.Vel.X) : 0f;
                float baseScale = m.Size * 2.2f / texSize;

                Color tint = m.Spark ? new Color(255, 250, 240) : Color.Lerp(new Color(200, 205, 230), Color.White, m.Depth);

                sb.Draw(dot, m.Pos, null, tint * (alpha * 0.22f), rot, origin, new Vector2(baseScale * 3.2f * stretch, baseScale * 3.2f), SpriteEffects.None, 0f);
                sb.Draw(dot, m.Pos, null, tint * alpha, rot, origin, new Vector2(baseScale * stretch, baseScale), SpriteEffects.None, 0f);
            }
        }
    }
}
