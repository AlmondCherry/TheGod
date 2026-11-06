using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;

namespace GodEyeMod.Content.Menu
{
    // при наведении на кнопки главного меню их текст иногда подменяется на другой
    internal static class MenuHoverText
    {
        private class Entry
        {
            public string[] Keys;
            public string English;
            public string[] Fakes;
            public LocalizedText Text;
        }

        private static readonly Entry[] entries =
        {
            new Entry { Keys = new[] { "LegacyMenu.12" }, English = "Single Player",
                Fakes = new[] { "Single Prayer", "Alone. Again.", "Just you and me", "No one else",
                    "Nobody will hear you", "Play with me", "Always alone", "You came back", "I waited for you" } },
            new Entry { Keys = new[] { "LegacyMenu.13" }, English = "Multiplayer",
                Fakes = new[] { "Nobody's coming", "They all left", "Multiple eyes", "We are many",
                    "Bring them to me", "Who is with you?", "Server: me", "They can see you too", "0 players online" } },
            new Entry { Keys = new[] { "UI.Achievements" }, English = "Achievements",
                Fakes = new[] { "I counted them", "Every death", "Nothing you did" } },
            new Entry { Keys = new[] { "tModLoader.MenuWorkshop", "tModLoader.Workshop", "tModLoader.WorkshopMenu" }, English = "Workshop",
                Fakes = new[] { "I'm in the files", "Your mods are mine", "Look closer" } },
            new Entry { Keys = new[] { "LegacyMenu.14" }, English = "Settings",
                Fakes = new[] { "You can't fix me", "Nothing to change", "Brightness: 0" } },
            new Entry { Keys = new[] { "UI.Credits" }, English = "Credits",
                Fakes = new[] { "Nobody made me", "I made you", "In memory of you" } },
            new Entry { Keys = new[] { "LegacyMenu.15" }, English = "Exit",
                Fakes = new[] { "Don't", "Stay", "Not yet", "You can't leave", "Don't leave me",
                    "I'll be here", "Close your eyes", "There is no exit", "Leaving?", "I'll follow you" } },
        };

        public const int ExitIndex = 6;

        private static readonly MethodInfo setValueMethod =
            typeof(LocalizedText).GetMethod("SetValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(string) }, null);
        private static readonly MethodInfo valueSetter =
            typeof(LocalizedText).GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetSetMethod(true);

        // focusMenu у Main закрытый, читаем через рефлексию
        private static readonly FieldInfo focusMenuField =
            typeof(Main).GetField("focusMenu", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        private static int FocusMenu
        {
            get
            {
                if (focusMenuField == null || focusMenuField.FieldType != typeof(int))
                    return -1;
                try
                {
                    object owner = focusMenuField.IsStatic ? null : Main.instance;
                    if (!focusMenuField.IsStatic && owner == null)
                        return -1;
                    return (int)focusMenuField.GetValue(owner);
                }
                catch
                {
                    return -1;
                }
            }
        }


        // игра в какой-то момент кадра сбрасывает focusMenu, поэтому подглядываем в нескольких местах
        // и запоминаем последнее нормальное значение
        private static int seenFocus = -1;
        private static int seenFrame = -100;
        // номер самой нижней кнопки, на которую когда-либо наводились; Exit всегда последняя
        private static int maxSeenFocus = -1;

        // значение считается живым, если его видели в этом или прошлом кадре
        private static int CurrentFocus => GodEyeMenu.DrawFrame - seenFrame <= 1 ? seenFocus : -1;

        public static void ProbeFocus()
        {
            if (!Main.gameMenu || Main.menuMode != 0)
                return;
            int f = FocusMenu;
            if (f >= 0)
            {
                seenFocus = f;
                seenFrame = GodEyeMenu.DrawFrame;
                if (f > maxSeenFocus && f < 20)
                    maxSeenFocus = f;
            }
        }

        private static bool resolved;
        private static bool broken;
        private static string resolvedCulture;

        private static Entry active;
        private static string original;
        private static string fake;
        private static string lastSet;
        private static float swapTime;
        private static float holdTime;

        private static int lastHover = -1;
        private static float hoverTime;
        private static float cooldown;

        public static int HoveredEntry
        {
            get
            {
                if (!Main.gameMenu || Main.menuMode != 0)
                    return -1;
                return MapFocus(CurrentFocus);
            }
        }

        private static void Resolve()
        {
            string culture = Language.ActiveCulture?.CultureInfo?.Name ?? "";
            if (resolved && culture == resolvedCulture)
                return;

            resolved = true;
            resolvedCulture = culture;
            bool english = culture.StartsWith("en");

            foreach (var e in entries)
            {
                e.Text = null;
                foreach (string key in e.Keys)
                {
                    if (!Language.Exists(key))
                        continue;

                    var text = Language.GetText(key);
                    // на английском можно проверить, что ключ правильный
                    if (english && text.Value != e.English)
                        continue;

                    e.Text = text;
                    break;
                }

                // ключ мастерской в разных версиях tML разный — ищем по самому тексту
                if (e.Text == null && english)
                {
                    foreach (var pair in AllTexts())
                    {
                        if (pair.Key.StartsWith("tModLoader.") && pair.Value.Value == e.English)
                        {
                            e.Text = pair.Value;
                            break;
                        }
                    }
                }
            }
        }

        private static readonly FieldInfo allTextsField =
            typeof(LanguageManager).GetField("_localizedTexts", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        // все тексты игры по ключам (закрытое поле LanguageManager)
        internal static IEnumerable<KeyValuePair<string, LocalizedText>> AllTexts()
        {
            Dictionary<string, LocalizedText> dict = null;
            try
            {
                dict = allTextsField?.GetValue(LanguageManager.Instance) as Dictionary<string, LocalizedText>;
            }
            catch
            {
            }
            if (dict == null)
                yield break;
            foreach (var pair in dict)
                yield return pair;
        }

        internal static bool SetText(LocalizedText text, string value) => Set(text, value);

        private static bool Set(LocalizedText text, string value)
        {
            try
            {
                if (setValueMethod != null)
                    setValueMethod.Invoke(text, new object[] { value });
                else if (valueSetter != null)
                    valueSetter.Invoke(text, new object[] { value });
                else
                    return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        // номер кнопки на экране -> наша запись; не зависит от того, нашёлся ли текст
        private static int MapFocus(int focus)
        {
            if (focus < 0)
                return -1;
            if (focus <= 2)
                return focus;

            int extra = Math.Max(0, maxSeenFocus - (entries.Length - 1));
            if (extra == 0)
                return focus < entries.Length ? focus : -1;

            // у tML кнопок больше, чем у нас записей: Exit — последняя, Settings и Credits прямо перед ней
            if (focus == maxSeenFocus)
                return ExitIndex;
            int idx = focus - extra;
            return idx == 4 || idx == 5 ? idx : -1;
        }

        public static void Update(float dt)
        {
            if (broken || (setValueMethod == null && valueSetter == null))
                return;

            bool allowed = Main.gameMenu && Main.menuMode == 0 && !MenuFx.NoGlitchText;
            if (!allowed)
            {
                Restore();
                lastHover = -1;
                hoverTime = 0f;
                return;
            }

            Resolve();
            cooldown -= dt;

            if (active != null)
            {
                UpdateSwap(dt);
                return;
            }

            int hover = MapFocus(CurrentFocus);
            if (hover >= 0 && entries[hover].Text == null)
                hover = -1;
            if (hover != lastHover)
            {
                lastHover = hover;
                hoverTime = 0f;
                if (hover >= 0 && cooldown <= 0f && MenuFx.Chance(0.22))
                    Begin(entries[hover]);
                return;
            }

            if (hover >= 0)
            {
                hoverTime += dt;
                if (hoverTime > 1.2f && cooldown <= 0f && MenuFx.Chance(dt * 0.35f))
                    Begin(entries[hover]);
            }
        }

        private static void Begin(Entry e)
        {
            string current = e.Text.Value;
            if (string.IsNullOrEmpty(current))
                return;

            active = e;
            original = current;
            fake = MenuFx.Pick(e.Fakes);
            lastSet = null;
            swapTime = 0f;
            holdTime = MenuFx.NextFloat(0.6f, 1.4f);

            SoundEngine.PlaySound(SoundID.MenuTick with { Pitch = -0.9f, Volume = 0.55f });
        }

        private static void UpdateSwap(float dt)
        {
            const float morphIn = 0.22f;
            const float morphOut = 0.2f;

            swapTime += dt;
            string show;

            if (swapTime < morphIn)
                show = MenuFx.Morph(original, fake, swapTime / morphIn);
            else if (swapTime < morphIn + holdTime)
                show = MenuFx.Chance(0.06) ? MenuFx.Scramble(fake, 0.15f) : fake;
            else if (swapTime < morphIn + holdTime + morphOut)
                show = MenuFx.Morph(fake, original, (swapTime - morphIn - holdTime) / morphOut);
            else
            {
                Restore();
                cooldown = MenuFx.NextFloat(2f, 4.5f);
                return;
            }

            // если кто-то другой поменял текст (например, смена языка) — не лезем
            if (lastSet != null && active.Text.Value != lastSet)
            {
                active = null;
                lastSet = null;
                return;
            }

            if (show != lastSet)
            {
                if (!Set(active.Text, show))
                {
                    broken = true;
                    active = null;
                    lastSet = null;
                    return;
                }
                lastSet = show;
            }
        }

        public static void Restore()
        {
            if (active == null)
                return;

            if (lastSet != null && active.Text.Value == lastSet)
                Set(active.Text, original);

            active = null;
            lastSet = null;
        }

        public static void Reset()
        {
            Restore();
            resolved = false;
            seenFocus = -1;
            seenFrame = -100;
            lastHover = -1;
            hoverTime = 0f;
            cooldown = 1f;
        }
    }
}
