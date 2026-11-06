using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Achievements;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace GodEyeMod.Content.Menu
{
    // глаз вместо иконки ачивки
    internal class AchievementEye : UIElement
    {
        public bool Locked;
        public int Seed;

        public AchievementEye()
        {
            IgnoresMouseInteraction = true;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var frames = ModListEyes.Frames;
            if (frames == null)
                return;

            int frame = MenuExtras.EyeFrame(Seed, Locked, IsMouseHovering || Parent?.IsMouseHovering == true);
            var tex = frames[frame];
            if (tex == null || !tex.IsLoaded)
                return;

            Color color = Locked ? new Color(150, 60, 70) : Color.White;
            spriteBatch.Draw(tex.Value, GetDimensions().ToRectangle(), color);
        }
    }

    // ачивки и настройки в стиле мода
    public class MenuExtras : ModSystem
    {
        private static readonly string[] AchievementLines =
        {
            "God is here.",
            "God is here. He saw you do this.",
            "He was watching when you did this.",
            "God is here. God was always here.",
            "It counted this one.",
            "You did this. He remembers.",
            "God is here. Look behind you.",
            "Achievement unlocked: being watched.",
            "Every step. Every swing. God is here.",
            "It didn't matter. God is here.",
            "He liked this one.",
            "God is here. You are not alone."
        };

        private static readonly Dictionary<string, string[]> SettingsFakes = new Dictionary<string, string[]>
        {
            ["General"] = new[] { "Generally watched", "General: GOD" },
            ["Interface"] = new[] { "Interface with me", "I am the interface" },
            ["Video"] = new[] { "Video: recording you", "Camera: ON" },
            ["Volume"] = new[] { "Can you hear me?", "Volume: screaming", "Turn it up" },
            ["Cursor"] = new[] { "Your hand is shaking", "Cursor: watched" },
            ["Controls"] = new[] { "You control nothing", "Controls: mine" },
            ["Language"] = new[] { "Speak to me", "Eye speak all" },
            ["Back"] = new[] { "No going back", "Back? Where?", "Behind you" },
            ["Resolution"] = new[] { "Resolution: none" },
            ["Full Screen"] = new[] { "Full Eye" },
            ["Brightness"] = new[] { "Darkness" },
        };

        private class SavedText
        {
            public string Original;
            public string LastSet;
        }

        private static readonly Dictionary<LocalizedText, SavedText> changedTexts = new Dictionary<LocalizedText, SavedText>();
        private static readonly FieldInfo achievementField =
            typeof(UIAchievementListItem).GetField("_achievement", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo achievementIconField =
            typeof(UIAchievementListItem).GetField("_achievementIcon", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private static LocalizedText titleSwap;
        private static float titleSwapTime;

        private static List<(List<LocalizedText> Texts, string[] Fakes)> settingsTexts;
        private static string settingsCulture;
        private static List<LocalizedText> settingsSwap;
        private static float settingsSwapTime;
        private static float settingsCooldown = 2f;

        private static bool broken;

        public override void Load()
        {
            if (Main.dedServ)
                return;
            On_UserInterface.Update += AfterUiUpdate;
        }

        public override void Unload()
        {
            if (!Main.dedServ)
                On_UserInterface.Update -= AfterUiUpdate;

            RestoreAllTexts();
            RemoveEyes();
            settingsTexts = null;
            titleSwap = null;
            settingsSwap = null;
            broken = false;
        }

        // кадр анимации глаза: обычно смотрит прямо, иногда косится; закрытая ачивка — прищур
        internal static int EyeFrame(int seed, bool locked, bool hover)
        {
            if (hover)
                return 0;

            float t = (float)Main.timeForVisualEffects / 60f + (seed % 1000) * 0.137f;
            float cycle = 5f + seed % 5;
            float pos = t % cycle;

            if (locked)
                return pos < cycle - 0.6f ? 3 : 2;

            int rest = (seed % 3) switch { 0 => 0, 1 => 2, _ => 5 };
            if (pos < cycle - 1.3f)
                return rest;
            int glance = (seed / 3 % 3) switch { 0 => 1, 1 => 3, _ => 4 };
            return pos < cycle - 0.4f ? glance : 2;
        }

        private static void AfterUiUpdate(On_UserInterface.orig_Update orig, UserInterface self, GameTime time)
        {
            orig(self, time);

            if (self != Main.MenuUI || broken)
                return;

            try
            {
                UIState state = Main.MenuUI.CurrentState;
                bool onAchievements = Main.gameMenu && GodEyeMenu.Instance != null && state is UIState && state.GetType().Name == "UIAchievementsMenu";

                if (onAchievements)
                    ApplyAchievements(state, 1f / 60f);
                else
                    RestoreAchievementTexts();
            }
            catch (Exception e)
            {
                broken = true;
                RestoreAllTexts();
                ModContent.GetInstance<global::GodEyeMod.GodEyeMod>().Logger.Warn("Achievements restyle disabled: " + e.Message);
            }
        }

        private static void ApplyAchievements(UIElement root, float dt)
        {
            var items = new List<UIAchievementListItem>();
            Collect(root, items);

            foreach (var item in items)
            {
                bool hover = item.IsMouseHovering;
                item.BackgroundColor = hover ? new Color(24, 20, 32, 235) : new Color(6, 6, 9, 235);
                item.BorderColor = hover ? new Color(120, 95, 170) : new Color(38, 32, 52);

                var achievement = achievementField?.GetValue(item) as Achievement;
                PlaceEye(item, achievement);
                if (achievement != null && !MenuFx.NoGlitchText)
                    ApplyAchievementText(achievement);
            }

            UpdateTitleGlitch(items, dt);
        }

        private static void Collect(UIElement element, List<UIAchievementListItem> items)
        {
            foreach (var child in element.Children)
            {
                if (child is UIAchievementListItem item)
                    items.Add(item);
                else
                    Collect(child, items);
            }
        }

        private static void PlaceEye(UIAchievementListItem item, Achievement achievement)
        {
            if (achievementIconField?.GetValue(item) is not UIElement icon)
                return;

            AchievementEye eye = null;
            foreach (var child in item.Children)
            {
                if (child is AchievementEye found)
                {
                    eye = found;
                    break;
                }
            }

            if (eye == null)
            {
                eye = new AchievementEye { Seed = RuntimeHelpers.GetHashCode(item) & 0x7FFFFFFF };
                item.Append(eye);
            }

            if (icon is UIImageFramed framed)
                framed.Color = Color.Transparent;

            eye.Locked = achievement != null && !achievement.IsCompleted;

            // глаз ровно поверх старой иконки
            var iconDims = icon.GetDimensions();
            var inner = item.GetInnerDimensions();
            float left = iconDims.X - inner.X;
            float top = iconDims.Y - inner.Y;
            if (eye.Left.Pixels != left || eye.Top.Pixels != top || eye.Width.Pixels != iconDims.Width || eye.Height.Pixels != iconDims.Height)
            {
                eye.Left.Set(left, 0f);
                eye.Top.Set(top, 0f);
                eye.Width.Set(iconDims.Width, 0f);
                eye.Height.Set(iconDims.Height, 0f);
                eye.Recalculate();
            }
        }

        private static void ApplyAchievementText(Achievement achievement)
        {
            var desc = achievement.Description;
            if (desc == null || changedTexts.ContainsKey(desc) && changedTexts[desc].LastSet == desc.Value)
                return;

            string original = changedTexts.TryGetValue(desc, out var saved) ? saved.Original : desc.Value;
            int seed = (achievement.Name ?? "").GetHashCode() & 0x7FFFFFFF;
            string line = AchievementLines[seed % AchievementLines.Length];
            // половина описаний полностью заменена, у половины "God is here." дописано в конец
            string value = seed % 2 == 0 ? line : original + " God is here.";

            SetTracked(desc, original, value);
        }

        private static void UpdateTitleGlitch(List<UIAchievementListItem> items, float dt)
        {
            if (titleSwap != null)
            {
                titleSwapTime -= dt;
                if (titleSwapTime <= 0f)
                {
                    RestoreText(titleSwap);
                    titleSwap = null;
                }
                return;
            }

            if (items.Count == 0 || MenuFx.NoGlitchText || !MenuFx.Chance(dt * 0.6f))
                return;

            var achievement = achievementField?.GetValue(items[MenuFx.Rand.Next(items.Count)]) as Achievement;
            var title = achievement?.FriendlyName;
            if (title == null || changedTexts.ContainsKey(title))
                return;

            titleSwap = title;
            titleSwapTime = MenuFx.NextFloat(0.15f, 0.4f);
            SetTracked(title, title.Value, "GOD IS HERE");
        }

        // настройки: названия кнопок иногда подменяются
        internal static void UpdateSettings(float dt)
        {
            bool inSettings = Main.gameMenu && Main.menuMode != 0 && Main.MenuUI?.CurrentState == null && !MenuFx.NoGlitchText;
            if (!inSettings)
            {
                RestoreSettingsSwap();
                return;
            }

            ResolveSettingsTexts();
            if (settingsTexts == null || settingsTexts.Count == 0)
                return;

            if (settingsSwap != null)
            {
                settingsSwapTime -= dt;
                if (settingsSwapTime <= 0f)
                {
                    RestoreSettingsSwap();
                    settingsCooldown = MenuFx.NextFloat(2f, 5f);
                }
                return;
            }

            settingsCooldown -= dt;
            if (settingsCooldown > 0f)
                return;

            var pick = settingsTexts[MenuFx.Rand.Next(settingsTexts.Count)];
            string fake = MenuFx.Pick(pick.Fakes);
            settingsSwap = pick.Texts;
            settingsSwapTime = MenuFx.NextFloat(0.7f, 1.4f);
            foreach (var text in pick.Texts)
                SetTracked(text, text.Value, fake);
        }

        private static void RestoreSettingsSwap()
        {
            if (settingsSwap == null)
                return;
            foreach (var text in settingsSwap)
                RestoreText(text);
            settingsSwap = null;
        }

        private static void ResolveSettingsTexts()
        {
            string culture = Language.ActiveCulture?.CultureInfo?.Name ?? "";
            if (settingsTexts != null && culture == settingsCulture)
                return;

            settingsCulture = culture;
            settingsTexts = new List<(List<LocalizedText>, string[])>();
            if (!culture.StartsWith("en"))
                return;

            // одно и то же слово может лежать под несколькими ключами — меняем их все разом
            var byWord = new Dictionary<string, List<LocalizedText>>();
            foreach (var pair in MenuHoverText.AllTexts())
            {
                if (!(pair.Key.StartsWith("LegacyMenu.") || pair.Key.StartsWith("UI.") || pair.Key.StartsWith("tModLoader.")))
                    continue;
                string word = pair.Value.Value;
                if (!SettingsFakes.ContainsKey(word))
                    continue;
                if (!byWord.TryGetValue(word, out var list))
                    byWord[word] = list = new List<LocalizedText>();
                list.Add(pair.Value);
            }

            foreach (var pair in byWord)
                settingsTexts.Add((pair.Value, SettingsFakes[pair.Key]));
        }

        private static void SetTracked(LocalizedText text, string original, string value)
        {
            if (!MenuHoverText.SetText(text, value))
                return;

            if (changedTexts.TryGetValue(text, out var saved))
                saved.LastSet = value;
            else
                changedTexts[text] = new SavedText { Original = original, LastSet = value };
        }

        private static void RestoreText(LocalizedText text)
        {
            if (text == null || !changedTexts.TryGetValue(text, out var saved))
                return;

            // если текст поменял кто-то другой (смена языка), не трогаем
            if (text.Value == saved.LastSet)
                MenuHoverText.SetText(text, saved.Original);
            changedTexts.Remove(text);
        }

        private static void RestoreAchievementTexts()
        {
            if (changedTexts.Count == 0)
                return;

            foreach (var text in new List<LocalizedText>(changedTexts.Keys))
            {
                if (settingsSwap == null || !settingsSwap.Contains(text))
                    RestoreText(text);
            }
            titleSwap = null;
        }

        private static void RestoreAllTexts()
        {
            foreach (var text in new List<LocalizedText>(changedTexts.Keys))
                RestoreText(text);
            titleSwap = null;
            settingsSwap = null;
        }

        // при выгрузке мода убираем свои элементы из меню ачивок, иначе игра упадёт на чужом коде
        private static void RemoveEyes()
        {
            try
            {
                var roots = new List<UIElement>();
                var field = typeof(Main).GetField("AchievementsMenu", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (field?.GetValue(null) is UIElement menu)
                    roots.Add(menu);
                if (Main.MenuUI?.CurrentState != null)
                    roots.Add(Main.MenuUI.CurrentState);

                foreach (var root in roots)
                {
                    var items = new List<UIAchievementListItem>();
                    Collect(root, items);
                    foreach (var item in items)
                    {
                        foreach (var child in new List<UIElement>(item.Children))
                        {
                            if (child is AchievementEye)
                                child.Remove();
                        }
                        if (achievementIconField?.GetValue(item) is UIImageFramed framed)
                            framed.Color = Color.White;
                    }
                }
            }
            catch
            {
            }
        }
    }
}
