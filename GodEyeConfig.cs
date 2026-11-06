using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace GodEyeMod
{
    public class GodEyeConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("VisualEffects")]

        [Label("Disable Ambient Visual Effects")]
        [Tooltip("Turns off screen eyes, scanlines, RGB splits, glitch overlays, and vignette during boss fight.")]
        [DefaultValue(false)]
        public bool DisableAmbientVisuals;

        [Label("Disable Screen Flashing")]
        [Tooltip("Removes all screen flashes, bright pulses, and color flickers.")]
        [DefaultValue(false)]
        public bool DisableFlashing;

        [Label("Disable Screamer")]
        [Tooltip("Removes the jumpscare effect in Phase 3+.")]
        [DefaultValue(false)]
        public bool DisableScreamer;

        [Label("Disable Screen Shake")]
        [Tooltip("Removes all camera shake effects.")]
        [DefaultValue(false)]
        public bool DisableScreenShake;

        [Header("TextAndMessages")]

        [Label("Disable Glitch Text on Screen")]
        [Tooltip("Hides creepy text on screen during the fight.")]
        [DefaultValue(false)]
        public bool DisableGlitchText;

        [Label("Disable Chat Messages")]
        [Tooltip("Stops ALL boss and event related messages in chat.")]
        [DefaultValue(false)]
        public bool DisableChatMessages;

        [Header("ObserverEvents")]

        [Label("Disable Whisper Events")]
        [Tooltip("Stops all whisper text that appears on screen between fights.")]
        [DefaultValue(false)]
        public bool DisableWhispers;

        [Label("Disable Process Detection Messages")]
        [Tooltip("Stops the mod from detecting running programs and whispering about them. The mod does NOT collect or send any data.")]
        [DefaultValue(false)]
        public bool DisableProcessDetection;

        [Label("Disable System Info Whispers")]
        [Tooltip("Stops whispers that mention your PC name, username, IP, etc. All data is local and never sent anywhere.")]
        [DefaultValue(false)]
        public bool DisableSystemInfoWhispers;

        [Label("Disable Eye Events")]
        [Tooltip("Stops all floating eyes, cursor eyes, wall eyes from appearing.")]
        [DefaultValue(false)]
        public bool DisableEyeEvents;

        [Label("Disable Multiplayer Awareness")]
        [Tooltip("Stops the mod from mentioning other players, worlds, or Steam friends.")]
        [DefaultValue(false)]
        public bool DisableMultiplayerAwareness;

        [Header("BossFightConfig")]

        [Label("Disable Boss Weather Control")]
        [Tooltip("Stops the boss from changing rain, wind, time, moon phases during fight.")]
        [DefaultValue(false)]
        public bool DisableBossWeatherControl;
    }
}