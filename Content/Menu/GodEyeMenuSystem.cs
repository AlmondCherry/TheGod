using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace GodEyeMod.Content.Menu
{
    public class GodEyeMenuSystem : ModSystem
    {
        private delegate void OrigInstanceQuit(Main self);
        private delegate void OrigStaticQuit();
        private delegate void OrigGameExit(Game self);
        private delegate void OrigDecideMusic(Main self);

        private static int lastOverlayFrame = -1;
        private static bool overlayBroken;

        private static MethodInfo quitMethod;
        private static bool allowQuit;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            On_Main.DrawMenu += WrapDrawMenu;
            On_Main.DrawThickCursor += DrawOverCursor;

            HookQuit();
            HookMusic();
        }

        // в титрах игра ставит свою музыку поверх темы меню — подменяем её решение
        private void HookMusic()
        {
            try
            {
                var decide = typeof(Main).GetMethod("UpdateAudio_DecideOnNewMusic", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (decide == null)
                {
                    Mod.Logger.Warn("Credits music: UpdateAudio_DecideOnNewMusic not found");
                    return;
                }
                MonoModHooks.Add(decide, (Action<OrigDecideMusic, Main>)DecideMusicHook);
            }
            catch (Exception e)
            {
                Mod.Logger.Warn("Credits music hook failed: " + e.Message);
            }
        }

        private static void DecideMusicHook(OrigDecideMusic orig, Main self)
        {
            orig(self);

            if (!Main.gameMenu)
                return;
            var menu = GodEyeMenu.Instance;
            if (menu == null)
                return;
            if (menu.DreadActive)
            {
                Main.newMusic = 0;
                return;
            }
            int slot = menu.CreditsMusic;
            if (slot > 0)
                Main.newMusic = slot;
        }

        public override void Unload()
        {
            if (!Main.dedServ)
            {
                On_Main.DrawMenu -= WrapDrawMenu;
                On_Main.DrawThickCursor -= DrawOverCursor;
            }

            GodEyeMenu.Instance?.RestoreVolumes();
            MenuHoverText.Restore();
            MenuFx.DisposeTextures();
            GodEyeMenu.Instance = null;
            lastOverlayFrame = -1;
            overlayBroken = false;
            quitMethod = null;
            allowQuit = false;
        }

        // выход из главного меню идёт через Main.QuitGame — ловим его, а не клик,
        // так не важно, в каком порядке игра рисует кнопки и обрабатывает нажатия
        private void HookQuit()
        {
            try
            {
                quitMethod = typeof(Main).GetMethod("QuitGame", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);

                if (quitMethod != null && quitMethod.IsStatic)
                    MonoModHooks.Add(quitMethod, (Action<OrigStaticQuit>)StaticQuitHook);
                else if (quitMethod != null)
                    MonoModHooks.Add(quitMethod, (Action<OrigInstanceQuit, Main>)InstanceQuitHook);
                else
                {
                    var exit = typeof(Game).GetMethod("Exit", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
                    if (exit != null)
                        MonoModHooks.Add(exit, (Action<OrigGameExit, Game>)GameExitHook);
                }

                Mod.Logger.Info("Exit screamer hooked: " + (quitMethod != null ? "Main.QuitGame" : "Game.Exit"));
            }
            catch (Exception e)
            {
                quitMethod = null;
                Mod.Logger.Warn("Exit screamer disabled: " + e.Message);
            }
        }

        // true — выход придержали ради скримера
        private static bool HoldQuit()
        {
            if (allowQuit || !Main.gameMenu || MenuFx.NoScreamer)
                return false;

            var menu = GodEyeMenu.Instance;
            if (menu == null)
                return false;

            if (!menu.Screaming)
                menu.StartExitScream();
            return true;
        }

        private static void StaticQuitHook(OrigStaticQuit orig)
        {
            if (!HoldQuit())
                orig();
        }

        private static void InstanceQuitHook(OrigInstanceQuit orig, Main self)
        {
            if (!HoldQuit())
                orig(self);
        }

        private static void GameExitHook(OrigGameExit orig, Game self)
        {
            if (!HoldQuit())
                orig(self);
        }

        // после скримера выходим по-настоящему
        internal static void QuitForReal()
        {
            allowQuit = true;
            try
            {
                if (quitMethod != null)
                {
                    quitMethod.Invoke(quitMethod.IsStatic ? null : Main.instance, null);
                    return;
                }
            }
            catch
            {
            }
            Main.instance.Exit();
        }

        private static void WrapDrawMenu(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime)
        {
            GodEyeMenu.Instance?.BeforeMenuDraw();
            orig(self, gameTime);
            MenuHoverText.ProbeFocus();
        }

        // курсор рисуется последним, поэтому всё, что тут, ложится поверх кнопок
        private static Vector2 DrawOverCursor(On_Main.orig_DrawThickCursor orig, bool smart)
        {
            if (!Main.gameMenu)
            {
                MenuHoverText.Restore();
                return orig(smart);
            }

            MenuHoverText.ProbeFocus();

            var menu = GodEyeMenu.Instance;
            if (menu != null && !overlayBroken && lastOverlayFrame != GodEyeMenu.DrawFrame)
            {
                lastOverlayFrame = GodEyeMenu.DrawFrame;
                try
                {
                    menu.DrawOverlay(Main.spriteBatch);
                }
                catch (Exception e)
                {
                    overlayBroken = true;
                    ModContent.GetInstance<global::GodEyeMod.GodEyeMod>().Logger.Warn("Menu overlay disabled: " + e.Message);
                }
            }

            return orig(smart);
        }
    }
}
