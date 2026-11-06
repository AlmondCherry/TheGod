using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace GodEyeMod.Content.Menu
{
    // в списке модов: чёрные плашки и вместо иконок моргающие глаза
    public class ModListEyes : ModSystem
    {
        private class SavedIcon
        {
            public object Texture;
            public object RawTexture;
        }

        private const int FrameCount = 6;

        private static Asset<Texture2D>[] frames;
        internal static Asset<Texture2D>[] Frames => frames;
        private static readonly ConditionalWeakTable<UIImage, SavedIcon> saved = new ConditionalWeakTable<UIImage, SavedIcon>();
        private static readonly Dictionary<Type, FieldInfo> iconFields = new Dictionary<Type, FieldInfo>();
        private static readonly Stopwatch clock = Stopwatch.StartNew();

        private static readonly FieldInfo imageTextureField =
            typeof(UIImage).GetField("_texture", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo imageRawTextureField =
            typeof(UIImage).GetField("_nonReloadingTexture", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private static bool broken;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            frames = new Asset<Texture2D>[FrameCount];
            for (int i = 0; i < FrameCount; i++)
                frames[i] = ModContent.Request<Texture2D>("GodEyeMod/Content/Menu/ModIcons/EyeIcon" + i, AssetRequestMode.AsyncLoad);

            On_UserInterface.Update += AfterUiUpdate;
        }

        public override void Unload()
        {
            if (!Main.dedServ)
                On_UserInterface.Update -= AfterUiUpdate;

            RestoreIcons();
            frames = null;
            iconFields.Clear();
            broken = false;
        }

        private static void AfterUiUpdate(On_UserInterface.orig_Update orig, UserInterface self, GameTime time)
        {
            orig(self, time);

            if (self == Main.MenuUI)
                Apply();
        }

        // вызывается после обновления UI, чтобы наши цвета перебивали ховер-цвета tML
        internal static void Apply()
        {
            if (broken || frames == null || !Main.gameMenu || GodEyeMenu.Instance == null)
                return;

            UIState state = Main.MenuUI?.CurrentState;
            if (state == null || state.GetType().Name != "UIMods")
                return;

            try
            {
                Walk(state);
            }
            catch (Exception e)
            {
                broken = true;
                ModContent.GetInstance<global::GodEyeMod.GodEyeMod>().Logger.Warn("Mod list restyle disabled: " + e.Message);
            }
        }

        private static void Walk(UIElement element)
        {
            foreach (UIElement child in element.Children)
            {
                if (child is UIPanel panel && child.GetType().Name == "UIModItem")
                {
                    Style(panel);
                    continue;
                }
                Walk(child);
            }
        }

        private static void Style(UIPanel item)
        {
            bool hover = item.IsMouseHovering;
            item.BackgroundColor = hover ? new Color(24, 20, 32, 235) : new Color(6, 6, 9, 235);
            item.BorderColor = hover ? new Color(120, 95, 170) : new Color(38, 32, 52);

            UIImage icon = FindIcon(item);
            if (icon == null)
                return;

            var frame = frames[PickFrame(item, hover)];
            if (!frame.IsLoaded)
                return;

            if (!saved.TryGetValue(icon, out _))
            {
                saved.Add(icon, new SavedIcon
                {
                    Texture = imageTextureField?.GetValue(icon),
                    RawTexture = imageRawTextureField?.GetValue(icon)
                });
            }

            icon.SetImage(frame);
        }

        private static UIImage FindIcon(UIPanel item)
        {
            Type type = item.GetType();
            if (!iconFields.TryGetValue(type, out FieldInfo field))
            {
                field = type.GetField("_modIcon", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                iconFields[type] = field;
            }

            if (field?.GetValue(item) is UIImage icon && icon.Parent != null)
                return icon;

            // запасной вариант: квадратная картинка 80x80 прямо внутри плашки
            foreach (UIElement child in item.Children)
            {
                if (child is UIImage img && Math.Abs(img.Width.Pixels - 80f) < 1f && Math.Abs(img.Height.Pixels - 80f) < 1f)
                    return img;
            }
            return null;
        }

        // глаз в основном смотрит прямо, иногда косится, а на наведение — пялится
        private static int PickFrame(UIPanel item, bool hover)
        {
            if (hover)
                return 0;

            int id = RuntimeHelpers.GetHashCode(item) & 0x7FFFFFFF;
            float t = (float)clock.Elapsed.TotalSeconds + (id % 1000) * 0.137f;
            float cycle = 5f + id % 5;
            float pos = t % cycle;

            int rest = (id % 3) switch { 0 => 0, 1 => 2, _ => 5 };
            if (pos < cycle - 1.3f)
                return rest;

            int glance = (id / 3 % 3) switch { 0 => 1, 1 => 3, _ => 4 };
            return pos < cycle - 0.4f ? glance : 2;
        }

        private static void RestoreIcons()
        {
            if (imageTextureField == null)
                return;

            try
            {
                foreach (KeyValuePair<UIImage, SavedIcon> pair in (IEnumerable<KeyValuePair<UIImage, SavedIcon>>)saved)
                {
                    imageTextureField.SetValue(pair.Key, pair.Value.Texture);
                    imageRawTextureField?.SetValue(pair.Key, pair.Value.RawTexture);
                }
                saved.Clear();
            }
            catch
            {
            }
        }
    }
}
