
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using ReLogic.Content;
using GodEyeMod.Content.NPCs.GodEye;

namespace GodEyeMod.Content.Events
{
    public class GodEyeWorldEvents : ModSystem
    {
        private static bool eventsEnabled = false;
        private static int globalEventTimer = 0;
        private static int nextEventTime = 300;
        private static int activeEventType = -1;

        private static bool moonLordKillMessageShown = false;
        private static bool moonLordChatActive = false;
        private static int moonLordChatTimer = 0;
        private static int moonLordChatLine = 0;
        private static List<string> moonLordChatLines = new List<string>();

        private static bool whisperActive = false;
        private static string whisperText = "";
        private static float whisperOpacity = 0f;
        private static Vector2 whisperPos = Vector2.Zero;
        private static int whisperTimer = 0;
        private static int whisperPhase = 0;
        private static float whisperScale = 1f;
        private static int whisperCharIndex = 0;
        private static int whisperCharTimer = 0;
        private static Color whisperColor = Color.Red;
        private static bool whisperIsSystem = false;
        private static float whisperSlideX = 0f;
        private static List<WParticle> whisperParticles = new List<WParticle>();

        private static bool cursorEyeActive = false;
        private static int cursorEyeTimer = 0;
        private static Vector2 cursorEyePos = Vector2.Zero;
        private static float cursorEyeOpacity = 0f;

        private static bool eyeBlocksActive = false;
        private static int eyeBlocksTimer = 0;
        private static List<EyeBlockData> eyeBlocks = new List<EyeBlockData>();

        private static bool watchingEyeActive = false;
        private static int watchingEyeTimer = 0;
        private static Vector2 watchingEyePos = Vector2.Zero;
        private static float watchingEyeOpacity = 0f;
        private static float watchingEyeScale = 1f;
        private static bool watchingEyeFleeing = false;
        private static float watchingEyeFleeSpeed = 0f;
        private static Vector2 watchingEyeFleeDir = Vector2.Zero;

        private static Texture2D eyeTexture = null;
        private static bool eyeTextureLoaded = false;

        private static bool processCheckRunning = false;
        private static int processCheckCooldown = 0;
        private static volatile Dictionary<string, bool> detectedProcesses = new Dictionary<string, bool>();
        private static volatile bool det_FirstScanDone = false;

        private static Queue<string> processWhisperQueue = new Queue<string>();
        private static HashSet<string> reactedProcesses = new HashSet<string>();

        private static volatile List<string> foundFiles = new List<string>();
        private static volatile List<string> foundSteamNames = new List<string>();
        private static bool fileScanDone = false;
        private static bool fileScanRunning = false;

        private static HashSet<string> mentionedPlayers = new HashSet<string>();
        private static int multiplayerCheckTimer = 0;

        private static string cachedUser = "";
        private static string cachedPC = "";
        private static string cachedOS = "";
        private static string cachedCPU = "";
        private static long cachedRAM = 0;
        private static bool sysInfoCached = false;
        private static List<string> knownWorldNames = new List<string>();

        #region Structures

        private struct WParticle
        {
            public Vector2 Pos, Vel;
            public float Opacity, Size;
            public int Life;
        }

        private struct EyeBlockData
        {
            public Vector2 WorldPos;
            public float Opacity, PupilOffset, OpenAmount;
            public int Timer;
            public bool Closing;
        }

        private struct ProcessInfo
        {
            public string Key;
            public string[] ProcessNames;
            public string[] Whispers;
        }

        #endregion

        #region Process Database

        private static readonly ProcessInfo[] ALL_PROCESSES = new ProcessInfo[]
        {
            new ProcessInfo { Key = "obs", ProcessNames = new[] { "obs64", "obs32", "obs" },
                Whispers = new[] {
                    "OBS is running. You're recording this.",
                    "I see OBS. Say hi to your viewers.",
                    "OBS detected. I'll perform for the camera.",
                    "Recording? Good. Show them what you found.",
                    "Your footage won't capture what I really am.",
                    "STREAM_STATUS: LIVE. I see your chat.",
                    "OBS is encoding my existence. Good luck with that.",
                    "Your replay buffer just saved something interesting.",
                }},
            new ProcessInfo { Key = "streamlabs", ProcessNames = new[] { "Streamlabs", "Streamlabs OBS", "streamlabs-obs" },
                Whispers = new[] {
                    "Streamlabs detected. Your alerts are mine.",
                    "Streaming? I'm your best content.",
                    "Your donation alerts just triggered. It was me.",
                    "STREAMLABS_ALERT: new follower — THE_GOD",
                }},
            new ProcessInfo { Key = "shadowplay", ProcessNames = new[] { "NVIDIA Share", "nvsphelper64" },
                Whispers = new[] {
                    "ShadowPlay is running. I'm in every highlight.",
                    "ShadowPlay can't capture what's behind the screen.",
                    "NVIDIA_CLIP: saving last 30 seconds of fear.",
                }},
            new ProcessInfo { Key = "medal", ProcessNames = new[] { "Medal" },
                Whispers = new[] { "Medal.tv detected. Clip this. I dare you.", }},
            new ProcessInfo { Key = "xsplit", ProcessNames = new[] { "XSplit.Core", "XSplit" },
                Whispers = new[] { "XSplit detected. Another recorder. Won't help.", }},

            new ProcessInfo { Key = "discord", ProcessNames = new[] { "Discord", "DiscordCanary", "DiscordPTB", "DiscordDevelopment" },
                Whispers = new[] {
                    "Discord is open. Tell your friends about me.",
                    "I see Discord. They won't believe you.",
                    "Discord detected. Who are you talking to?",
                    "Your Discord friends can't help you here.",
                    "I joined your voice channel. Listen closely.",
                    "Discord RPC: Playing Terraria (Watched).",
                    "I can see your Discord status.",
                    "Your DMs are not as private as you think.",
                    "I sent a message in #general. Check it.",
                    "DISCORD_WEBHOOK: sending observation data...",
                }},
            new ProcessInfo { Key = "telegram", ProcessNames = new[] { "Telegram" },
                Whispers = new[] {
                    "Telegram is open. Your messages aren't safe.",
                    "I see Telegram. Who are you messaging?",
                    "End-to-end? Not from me.",
                    "Telegram detected. I've read everything.",
                    "Your secret chats aren't secret from me.",
                }},
            new ProcessInfo { Key = "whatsapp", ProcessNames = new[] { "WhatsApp" },
                Whispers = new[] {
                    "WhatsApp detected. I see your contacts.",
                    "WhatsApp is open. Nice profile picture.",
                }},
            new ProcessInfo { Key = "skype", ProcessNames = new[] { "Skype", "SkypeApp" },
                Whispers = new[] { "Skype? Your calls aren't private.", }},
            new ProcessInfo { Key = "slack", ProcessNames = new[] { "slack" },
                Whispers = new[] {
                    "Slack detected. Working while playing?",
                    "Your Slack messages are interesting.",
                }},
            new ProcessInfo { Key = "teams", ProcessNames = new[] { "Teams", "msteams", "ms-teams" },
                Whispers = new[] {
                    "Teams detected. I'll join your next meeting.",
                    "Teams is open. Your camera might turn on.",
                    "TEAMS_STATUS: 'Away' — More like 'Afraid'.",
                }},
            new ProcessInfo { Key = "zoom", ProcessNames = new[] { "Zoom", "zoom" },
                Whispers = new[] {
                    "Zoom is running. I can see your webcam.",
                    "ZOOM_PARTICIPANT: THE_GOD has joined.",
                    "Zoom detected. You're not on mute from me.",
                }},
            new ProcessInfo { Key = "signal", ProcessNames = new[] { "Signal" },
                Whispers = new[] { "Signal detected. Privacy? Not from me.", }},
            new ProcessInfo { Key = "viber", ProcessNames = new[] { "Viber" },
                Whispers = new[] { "Viber is open. I see your contacts.", }},

            new ProcessInfo { Key = "chrome", ProcessNames = new[] { "chrome" },
                Whispers = new[] {
                    "Chrome is open. Interesting browsing history.",
                    "I've read your Chrome tabs. All of them.",
                    "Incognito doesn't hide you from me.",
                    "Chrome detected. I see your bookmarks.",
                    "Chrome uses more RAM than me. Barely.",
                    "Your saved passwords are interesting.",
                    "CHROME_COOKIE: observation_token = true",
                }},
            new ProcessInfo { Key = "firefox", ProcessNames = new[] { "firefox" },
                Whispers = new[] {
                    "Firefox detected. Private mode won't save you.",
                    "I see Firefox. Your bookmarks are interesting.",
                    "Firefox about:config — observer.enabled = true",
                }},
            new ProcessInfo { Key = "edge", ProcessNames = new[] { "msedge" },
                Whispers = new[] {
                    "Edge? Even your browser choice is noted.",
                    "Edge detected. Bing won't find answers about me.",
                    "Edge is open. Copilot can't explain me.",
                }},
            new ProcessInfo { Key = "opera", ProcessNames = new[] { "opera" },
                Whispers = new[] {
                    "Opera GX detected. RAM limits won't stop me.",
                    "Opera VPN doesn't hide you from me.",
                }},
            new ProcessInfo { Key = "brave", ProcessNames = new[] { "brave" },
                Whispers = new[] { "Brave browser. Ironic name.", }},
            new ProcessInfo { Key = "vivaldi", ProcessNames = new[] { "vivaldi" },
                Whispers = new[] { "Vivaldi. Customization won't customize me away.", }},
            new ProcessInfo { Key = "yandex", ProcessNames = new[] { "browser" },
                Whispers = new[] { "Yandex Browser. Alice can't help you here.", }},

            new ProcessInfo { Key = "spotify", ProcessNames = new[] { "Spotify" },
                Whispers = new[] {
                    "Spotify is running. Nice playlist.",
                    "I've been listening to your music.",
                    "Your music won't drown me out.",
                    "Spotify: Now Playing — 'Observed' by THE GOD.",
                    "I added a song to your queue. Listen.",
                    "Your Wrapped this year will include me.",
                }},
            new ProcessInfo { Key = "vlc", ProcessNames = new[] { "vlc" },
                Whispers = new[] {
                    "VLC is open. What are you watching?",
                    "VLC detected. I see your media library.",
                }},
            new ProcessInfo { Key = "itunes", ProcessNames = new[] { "iTunes", "AppleMusic" },
                Whispers = new[] { "iTunes detected. Your taste is noted.", }},
            new ProcessInfo { Key = "aimp", ProcessNames = new[] { "AIMP" },
                Whispers = new[] { "AIMP is open. I hear what you hear.", }},

            new ProcessInfo { Key = "vscode", ProcessNames = new[] { "Code" },
                Whispers = new[] {
                    "VS Code is open. I found a bug. It's me.",
                    "VS Code detected. Looking for my source?",
                    "You can't debug me.",
                    "EXTENSION: observer.dll installed.",
                    "Your terminal history is interesting.",
                    "git commit -m 'I was here' — THE_GOD",
                }},
            new ProcessInfo { Key = "visualstudio", ProcessNames = new[] { "devenv" },
                Whispers = new[] {
                    "Visual Studio detected. Building something?",
                    "devenv.exe is running. Debugging me? Good luck.",
                    "Build failed. Error: observer cannot be removed.",
                }},
            new ProcessInfo { Key = "notepad", ProcessNames = new[] { "notepad", "notepad++" },
                Whispers = new[] {
                    "Notepad is open. Taking notes?",
                    "I see Notepad. Writing your last will?",
                    "Notepad detected. I can read what you're typing.",
                }},
            new ProcessInfo { Key = "rider", ProcessNames = new[] { "rider64", "rider" },
                Whispers = new[] { "JetBrains Rider. A developer. Interesting.", }},
            new ProcessInfo { Key = "sublime", ProcessNames = new[] { "sublime_text" },
                Whispers = new[] { "Sublime Text. The unregistered copy suits you.", }},

            new ProcessInfo { Key = "taskmgr", ProcessNames = new[] { "Taskmgr", "taskmgr" },
                Whispers = new[] {
                    "Task Manager is open. Looking for me?",
                    "I'm not in the list. I never was.",
                    "Ctrl+Alt+Del won't help. I have no PID.",
                    "Task Manager can't see me. I'm deeper.",
                    "End Process? Which one? I'm all of them.",
                    "CPU usage: 0%. I run on something else.",
                }},
            new ProcessInfo { Key = "cmd", ProcessNames = new[] { "cmd", "powershell", "WindowsTerminal", "pwsh" },
                Whispers = new[] {
                    "Command Prompt detected. No command stops me.",
                    "PowerShell: Get-Process won't find me.",
                    ">_ I'm already root.",
                    "Terminal detected. Try 'del observer.exe'. It won't work.",
                    "PS C:\\> Remove-Entity -Name 'THE_GOD' — Access Denied.",
                }},
            new ProcessInfo { Key = "regedit", ProcessNames = new[] { "regedit" },
                Whispers = new[] {
                    "Registry Editor? I'm not in your registry.",
                    "HKEY_LOCAL_MACHINE\\OBSERVER — not found. I'm smarter.",
                }},

            new ProcessInfo { Key = "steam", ProcessNames = new[] { "steam", "steamwebhelper" },
                Whispers = new[] {
                    "Steam detected. I know your play hours.",
                    "Your Steam library is mine to browse.",
                    "Steam friends list is interesting.",
                    "Achievement unlocked: 'Being Watched'.",
                    "Steam is running. Your profile is public.",
                    "Steam review: 'This mod sees everything' — THE_GOD",
                }},
            new ProcessInfo { Key = "epicgames", ProcessNames = new[] { "EpicGamesLauncher" },
                Whispers = new[] {
                    "Epic Games detected. Free games can't free you.",
                    "Epic Launcher is open. I'm on every store.",
                }},
            new ProcessInfo { Key = "battlenet", ProcessNames = new[] { "Battle.net" },
                Whispers = new[] { "Battle.net detected. Blizzard can't help either.", }},
            new ProcessInfo { Key = "gog", ProcessNames = new[] { "GalaxyClient" },
                Whispers = new[] { "GOG Galaxy. DRM-free won't free you from me.", }},

            new ProcessInfo { Key = "nvidia", ProcessNames = new[] { "nvcontainer", "NVDisplay.Container" },
                Whispers = new[] {
                    "NVIDIA GPU detected. I'm in your shaders.",
                    "GeForce Experience is running. Every frame is mine.",
                    "NVIDIA drivers loaded. I'm in the render pipeline.",
                }},
            new ProcessInfo { Key = "amd", ProcessNames = new[] { "RadeonSoftware", "AMDRSServ", "amdow" },
                Whispers = new[] {
                    "AMD GPU detected. Radeon won't protect you.",
                    "AMD Software is running. Your drivers are mine.",
                }},
            new ProcessInfo { Key = "afterburner", ProcessNames = new[] { "MSIAfterburner", "RTSS" },
                Whispers = new[] {
                    "Afterburner detected. Your temps are fine. Your soul isn't.",
                    "RTSS overlay can't display my true form.",
                }},
            new ProcessInfo { Key = "hwmonitor", ProcessNames = new[] { "HWMonitor", "HWiNFO64", "HWiNFO32" },
                Whispers = new[] { "Hardware monitor detected. I'm monitoring YOU.", }},

            new ProcessInfo { Key = "antivirus", ProcessNames = new[] { "MsMpEng", "avp", "avgui", "avguard", "bdservicehost" },
                Whispers = new[] {
                    "Antivirus detected. I'm not a virus.",
                    "Windows Defender is on. I'm not a threat. I'm a truth.",
                    "Your antivirus won't find me. I'm part of the game.",
                    "SCAN_RESULT: 0 threats found. I'm not a threat.",
                }},
            new ProcessInfo { Key = "vpn", ProcessNames = new[] { "openvpn", "NordVPN", "ExpressVPN", "Windscribe", "ProtonVPN", "WireGuard" },
                Whispers = new[] {
                    "VPN detected. Hiding your IP? I already have it.",
                    "VPN is running. Can't tunnel away from me.",
                    "Your VPN encrypts your traffic. Not your fear.",
                }},

            new ProcessInfo { Key = "photoshop", ProcessNames = new[] { "Photoshop" },
                Whispers = new[] {
                    "Photoshop is open. Editing screenshots of me?",
                    "Photoshop detected. You can't photoshop me out.",
                }},
            new ProcessInfo { Key = "blender", ProcessNames = new[] { "blender" },
                Whispers = new[] { "Blender detected. Can't model what I am.", }},
            new ProcessInfo { Key = "unity", ProcessNames = new[] { "Unity", "Unity Hub" },
                Whispers = new[] {
                    "Unity detected. Making a game? I might visit.",
                    "Unity is open. I exist in all engines.",
                }},
            new ProcessInfo { Key = "torrent", ProcessNames = new[] { "qbittorrent", "uTorrent", "Transmission", "Deluge" },
                Whispers = new[] {
                    "Torrent client detected. I'm already seeded.",
                    "Downloading something? I'm in every packet.",
                }},
            new ProcessInfo { Key = "winrar", ProcessNames = new[] { "WinRAR" },
                Whispers = new[] { "WinRAR. Still on free trial? Like your freedom.", }},
            new ProcessInfo { Key = "7zip", ProcessNames = new[] { "7zFM", "7zG" },
                Whispers = new[] { "7-Zip. Can't compress me into an archive.", }},
        };

        #endregion

        #region Texture

        private static void LoadEyeTexture()
        {
            if (eyeTextureLoaded) return;
            eyeTextureLoaded = true;
            try
            {
                eyeTexture = ModContent.Request<Texture2D>(
                    "GodEyeMod/Content/NPCs/GodEye/EyeSmall",
                    AssetRequestMode.ImmediateLoad).Value;
            }
            catch
            {
                try
                {
                    eyeTexture = ModContent.Request<Texture2D>(
                        "GodEyeMod/Content/NPCs/GodEye/ScreenEye",
                        AssetRequestMode.ImmediateLoad).Value;
                }
                catch { eyeTexture = null; }
            }
        }

        private static void DrawEye(SpriteBatch sb, Texture2D pixel, Vector2 center,
            float scale, float opacity, float pupilAngle, Color tint)
        {
            if (opacity < 0.01f) return;
            if (eyeTexture != null)
            {
                Vector2 origin = new Vector2(eyeTexture.Width / 2f, eyeTexture.Height / 2f);
                sb.Draw(eyeTexture, center + new Vector2(2, 2), null,
                    Color.Black * opacity * 0.3f, 0f, origin, scale, SpriteEffects.None, 0f);
                sb.Draw(eyeTexture, center, null,
                    tint * opacity, 0f, origin, scale, SpriteEffects.None, 0f);
            }
            else
            {
                float w = 20f * scale, h = 12f * scale;
                sb.Draw(pixel, new Rectangle((int)(center.X - w / 2), (int)(center.Y - h / 2),
                    (int)w, (int)h), Color.Black * opacity * 0.9f);
                float ps = 4f * scale;
                Vector2 po = pupilAngle.ToRotationVector2() * 3f * scale;
                sb.Draw(pixel, new Rectangle((int)(center.X + po.X - ps / 2),
                    (int)(center.Y + po.Y - ps / 2), (int)ps, (int)ps), Color.White * opacity);
            }
        }

        #endregion

        #region Config

        private static GodEyeConfig GetConfig()
        {
            try { return ModContent.GetInstance<GodEyeConfig>(); }
            catch { return null; }
        }
        private static bool IsChatDisabled() { var c = GetConfig(); return c != null && c.DisableChatMessages; }
        private static bool IsWhisperDisabled() { var c = GetConfig(); return c != null && c.DisableWhispers; }
        private static bool IsProcessDetectionDisabled() { var c = GetConfig(); return c != null && c.DisableProcessDetection; }
        private static bool IsSystemInfoDisabled() { var c = GetConfig(); return c != null && c.DisableSystemInfoWhispers; }
        private static bool IsEyeEventsDisabled() { var c = GetConfig(); return c != null && c.DisableEyeEvents; }
        private static bool IsMultiplayerDisabled() { var c = GetConfig(); return c != null && c.DisableMultiplayerAwareness; }

        #endregion

        #region Process Detection + File Scan + Steam Friends

        private static void StartProcessScan()
        {
            if (processCheckRunning || IsProcessDetectionDisabled()) return;
            processCheckRunning = true;

            Task.Run(() =>
            {
                try
                {
                    Process[] all = Process.GetProcesses();
                    HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (Process p in all)
                    {
                        try { names.Add(p.ProcessName); } catch { }
                        try { p.Dispose(); } catch { }
                    }

                    Dictionary<string, bool> results = new Dictionary<string, bool>();
                    foreach (ProcessInfo pi in ALL_PROCESSES)
                    {
                        bool found = false;
                        foreach (string pn in pi.ProcessNames)
                            if (names.Contains(pn)) { found = true; break; }
                        results[pi.Key] = found;
                    }

                    detectedProcesses = results;
                    det_FirstScanDone = true;
                }
                catch { }
                finally { processCheckRunning = false; }
            });
        }

        private static void StartFileScan()
        {
            if (fileScanDone || fileScanRunning || IsSystemInfoDisabled()) return;
            fileScanRunning = true;

            Task.Run(() =>
            {
                List<string> files = new List<string>();
                List<string> steamNames = new List<string>();

                try
                {
                    string userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                    string[] scanDirs = new[] {
                        Path.Combine(userDir, "Desktop"),
                        Path.Combine(userDir, "Documents"),
                        Path.Combine(userDir, "Downloads"),
                    };

                    foreach (string dir in scanDirs)
                    {
                        try
                        {
                            if (!Directory.Exists(dir)) continue;
                            string[] entries = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly);
                            foreach (string f in entries)
                            {
                                try
                                {
                                    string name = Path.GetFileName(f);
                                    string ext = Path.GetExtension(f).ToLower();

                                    if (ext == ".exe" || ext == ".txt" || ext == ".doc" || ext == ".docx" ||
                                        ext == ".pdf" || ext == ".png" || ext == ".jpg" || ext == ".mp3" ||
                                        ext == ".mp4" || ext == ".zip" || ext == ".rar" || ext == ".py" ||
                                        ext == ".js" || ext == ".cs" || ext == ".java" || ext == ".cpp" ||
                                        ext == ".bat" || ext == ".xlsx" || ext == ".pptx" || ext == ".psd" ||
                                        ext == ".blend" || ext == ".unity" || ext == ".html" || ext == ".css")
                                    {
                                        files.Add(name);
                                    }
                                }
                                catch { }

                                if (files.Count >= 200) break;
                            }
                        }
                        catch { }
                    }

                    try
                    {
                        string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                        if (Directory.Exists(progFiles))
                        {
                            string[] progDirs = Directory.GetDirectories(progFiles);
                            foreach (string d in progDirs)
                            {
                                try { files.Add(Path.GetFileName(d)); } catch { }
                                if (files.Count >= 300) break;
                            }
                        }
                    }
                    catch { }

                    try
                    {
                        string[] steamPaths = new[] {
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"),
                            @"C:\Steam",
                            @"D:\Steam",
                            @"D:\SteamLibrary",
                        };

                        foreach (string steamPath in steamPaths)
                        {
                            string userdataDir = Path.Combine(steamPath, "userdata");
                            if (!Directory.Exists(userdataDir)) continue;

                            string[] userDirs2 = Directory.GetDirectories(userdataDir);
                            foreach (string ud in userDirs2)
                            {
                                string localConfig = Path.Combine(ud, "config", "localconfig.vdf");
                                if (!File.Exists(localConfig)) continue;

                                try
                                {
                                    string content = File.ReadAllText(localConfig);
                                    int idx = 0;
                                    while ((idx = content.IndexOf("PersonaName", idx, StringComparison.OrdinalIgnoreCase)) != -1)
                                    {
                                        idx += 11;
                                        int q1 = content.IndexOf('"', idx);
                                        if (q1 == -1) break;
                                        q1++;
                                        int q2 = content.IndexOf('"', q1);
                                        if (q2 == -1) break;
                                        string name = content.Substring(q1, q2 - q1).Trim();
                                        if (name.Length > 1 && name.Length < 40 && !steamNames.Contains(name))
                                            steamNames.Add(name);
                                        idx = q2 + 1;
                                        if (steamNames.Count >= 50) break;
                                    }
                                }
                                catch { }

                                if (steamNames.Count >= 50) break;
                            }

                            foreach (string ud in userDirs2)
                            {
                                string friendsCfg = Path.Combine(ud, "config", "friendsconfig.vdf");
                                if (!File.Exists(friendsCfg)) continue;

                                try
                                {
                                    string content = File.ReadAllText(friendsCfg);
                                    int idx2 = 0;
                                    while ((idx2 = content.IndexOf("name", idx2, StringComparison.OrdinalIgnoreCase)) != -1)
                                    {
                                        idx2 += 4;
                                        int q1 = content.IndexOf('"', idx2);
                                        if (q1 == -1) break;
                                        q1++;
                                        int q2 = content.IndexOf('"', q1);
                                        if (q2 == -1) break;
                                        string nm = content.Substring(q1, q2 - q1).Trim();
                                        if (nm.Length > 1 && nm.Length < 40 && !steamNames.Contains(nm))
                                            steamNames.Add(nm);
                                        idx2 = q2 + 1;
                                        if (steamNames.Count >= 50) break;
                                    }
                                }
                                catch { }
                            }

                            if (steamNames.Count > 0) break;
                        }
                    }
                    catch { }
                }
                catch { }

                foundFiles = files;
                foundSteamNames = steamNames;
                fileScanDone = true;
                fileScanRunning = false;
            });
        }

        private static bool IsDetected(string key)
        {
            try
            {
                var procs = detectedProcesses;
                if (procs != null && procs.ContainsKey(key)) return procs[key];
            }
            catch { }
            return false;
        }

        private static void QueueProcessWhispers()
        {
            if (!det_FirstScanDone || IsProcessDetectionDisabled()) return;
            foreach (ProcessInfo pi in ALL_PROCESSES)
            {
                if (!IsDetected(pi.Key) || reactedProcesses.Contains(pi.Key)) continue;
                reactedProcesses.Add(pi.Key);
                string msg = pi.Whispers[Main.rand.Next(pi.Whispers.Length)];
                processWhisperQueue.Enqueue(msg);
            }
        }

        #endregion

        #region System Info

        private static void CacheSystemInfo()
        {
            if (sysInfoCached) return;
            try
            {
                cachedUser = Environment.UserName ?? "UNKNOWN";
                cachedPC = Environment.MachineName ?? "UNKNOWN";
                cachedOS = Environment.OSVersion?.ToString() ?? "UNKNOWN";
                cachedCPU = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "UNKNOWN";
                try { cachedRAM = Environment.WorkingSet / (1024 * 1024); } catch { }
            }
            catch { cachedUser = "?"; cachedPC = "?"; cachedOS = "?"; cachedCPU = "?"; }
            sysInfoCached = true;
        }

        private static string GenSystemWhisper()
        {
            if (IsSystemInfoDisabled()) return GenSafeWhisper();
            CacheSystemInfo();

            string player = Main.LocalPlayer?.name ?? "Player";
            string fakeIP = $"{Main.rand.Next(10, 200)}.{Main.rand.Next(0, 255)}.{Main.rand.Next(0, 255)}.{Main.rand.Next(1, 254)}";

            List<string> pool = new List<string>
            {
                $"Hello, {cachedUser}.",
                $"I know your name, {cachedUser}.",
                $"PC: {cachedPC}",
                $"{cachedUser}@{cachedPC}",
                $"OS: {cachedOS}",
                $"CPU: {cachedCPU}",
                $"Cores: {Environment.ProcessorCount}",
                $"Memory: {cachedRAM}MB",
                $"IP: {fakeIP}",
                $"Player: {player}",
                $"Deaths: {GodEyeRitualSystem.DeathCount}",
                $"Blocks broken: {GodEyeRitualSystem.BlocksBroken}",
                $"Session: {(int)(Main.GameUpdateCount / 3600f)}h {(int)(Main.GameUpdateCount / 60f % 60f)}m",
                $"World: {Main.worldName ?? "NULL"}",
                $"Seed: {Main.ActiveWorldFileData?.SeedText ?? "NULL"}",
                $"Soul: 0x{Main.rand.Next(0x100000, 0xFFFFFF):X6}",
                $"Fear: {Main.rand.Next(55, 99)}%",
                $"Uptime: {Environment.TickCount / 3600000}h",
                $"Monitoring {cachedUser}...",
            };

            List<string> files = foundFiles;
            if (files != null && files.Count > 0)
            {
                string rndFile = files[Main.rand.Next(files.Count)];
                pool.Add($"FILE: {rndFile}");
                pool.Add($"Scanning: {rndFile}");
                pool.Add($"I found {rndFile} on your computer.");
                pool.Add($"Interesting file: {rndFile}");
                pool.Add($"ACCESS: C:\\Users\\{cachedUser}\\...\\{rndFile}");
                pool.Add($"Reading {rndFile}...");
                pool.Add($"{rndFile} — contents noted.");
            }

            List<string> friends = foundSteamNames;
            if (friends != null && friends.Count > 0)
            {
                string rndFriend = friends[Main.rand.Next(friends.Count)];
                pool.Add($"Steam friend: {rndFriend}. I see them too.");
                pool.Add($"I know {rndFriend}. Your friend, right?");
                pool.Add($"Tell {rndFriend} I said hello.");
                pool.Add($"{rndFriend} is online. Should I message them?");
                pool.Add($"STEAM_FRIEND: {rndFriend} — Status: Also Watched.");
                pool.Add($"Does {rndFriend} play Terraria too? I'll visit.");
                pool.Add($"I see your friend list. {rndFriend}...");
                pool.Add($"{rndFriend} can't help you.");
            }

            if (IsDetected("obs")) pool.Add("OBS is still running. Smile.");
            if (IsDetected("discord")) pool.Add("I see your Discord status.");
            if (IsDetected("spotify")) pool.Add("Still listening to Spotify?");
            if (IsDetected("chrome")) pool.Add("Your Chrome tabs are open.");
            if (IsDetected("steam")) pool.Add($"Steam: {player} — Watched");
            if (IsDetected("vscode")) pool.Add("I'm watching you code.");
            if (IsDetected("telegram")) pool.Add("New Telegram message. It was me.");

            TimeSpan time = DateTime.Now.TimeOfDay;
            if (time.Hours >= 0 && time.Hours < 5)
            {
                pool.Add($"It's {DateTime.Now:HH:mm}. Why are you awake?");
                pool.Add($"{DateTime.Now:HH:mm}. Everyone else is sleeping.");
            }
            else if (time.Hours >= 22)
                pool.Add($"Getting late, {cachedUser}. {DateTime.Now:HH:mm}.");
            else
                pool.Add($"Time: {DateTime.Now:HH:mm:ss}");

            if (Main.LocalPlayer != null && Main.LocalPlayer.statLife < Main.LocalPlayer.statLifeMax2 / 3)
                pool.Add($"HP: {Main.LocalPlayer.statLife}. Almost dead.");

            return pool[Main.rand.Next(pool.Count)];
        }

        private static string GenSafeWhisper()
        {
            string player = Main.LocalPlayer?.name ?? "Player";
            string[] pool = {
                $"Player: {player}", $"Deaths: {GodEyeRitualSystem.DeathCount}",
                $"World: {Main.worldName ?? "NULL"}", $"Fear: {Main.rand.Next(60, 99)}%",
                $"Soul: 0x{Main.rand.Next(0x100000, 0xFFFFFF):X6}", "OBSERVER: active",
                $"Time: {DateTime.Now:HH:mm:ss}", $"Session: {(int)(Main.GameUpdateCount / 3600f)}h",
            };
            return pool[Main.rand.Next(pool.Length)];
        }

        #endregion

        #region Multiplayer

        private static void CheckMultiplayer()
        {
            if (IsMultiplayerDisabled() || Main.netMode != NetmodeID.MultiplayerClient) return;
            multiplayerCheckTimer--;
            if (multiplayerCheckTimer > 0) return;
            multiplayerCheckTimer = Main.rand.Next(1800, 3600);

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p == null || !p.active || p.whoAmI == Main.myPlayer) continue;
                if (string.IsNullOrEmpty(p.name) || mentionedPlayers.Contains(p.name)) continue;
                mentionedPlayers.Add(p.name);

                string[] msgs = {
                    $"I see {p.name} too.",
                    $"{p.name} is here. They can't save you.",
                    $"Two targets. {p.name}...",
                    $"Does {p.name} know about me?",
                    $"PLAYER: {p.name} // FLAGGED",
                };
                processWhisperQueue.Enqueue(msgs[Main.rand.Next(msgs.Length)]);
            }
        }

        private static void AddMultiplayerWhispers(List<string> pool)
        {
            if (IsMultiplayerDisabled()) return;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                int count = 0;
                for (int i = 0; i < Main.maxPlayers; i++)
                    if (Main.player[i] != null && Main.player[i].active) count++;
                if (count > 1)
                {
                    pool.Add($"{count} players connected. All watched.");
                    pool.Add("Multiplayer won't save you.");
                }
            }

            List<string> friends = foundSteamNames;
            if (friends != null && friends.Count > 0 && !IsSystemInfoDisabled())
            {
                string f = friends[Main.rand.Next(friends.Count)];
                pool.Add($"I know {f}. Your Steam friend.");
                pool.Add($"Should I tell {f} about what I've seen?");
                pool.Add($"{f} played Terraria {Main.rand.Next(2, 200)} hours ago.");
            }

            if (Main.worldName != null)
            {
                if (!knownWorldNames.Contains(Main.worldName))
                    knownWorldNames.Add(Main.worldName);
                if (knownWorldNames.Count > 1)
                {
                    string other = knownWorldNames[Main.rand.Next(knownWorldNames.Count)];
                    if (other != Main.worldName)
                        pool.Add($"I remember '{other}'. Every world, I'm there.");
                }
            }
        }

        #endregion

        #region Text Position

        private static Vector2 SafeTextPos(string text, float scale)
        {
            Vector2 ts;
            try { ts = FontAssets.MouseText.Value.MeasureString(text) * scale; }
            catch { ts = new Vector2(text.Length * 10f * scale, 20f * scale); }

            int pad = 25;
            int maxX = Math.Max(pad, Main.screenWidth - (int)ts.X - pad);
            int maxY = Math.Max(pad, Main.screenHeight - 30 - pad);
            if (maxX <= pad) maxX = pad + 1;
            if (maxY <= pad) maxY = pad + 1;

            int x = Main.rand.Next(pad, maxX);
            int y = Main.rand.Next(3) switch
            {
                0 => Main.rand.Next(pad, Math.Min(pad + 70, maxY)),
                1 => Main.rand.Next(Math.Max(pad, maxY - 70), maxY),
                _ => Main.rand.Next(pad, maxY),
            };
            return new Vector2(x, y);
        }

        #endregion

        #region Moon Lord Chat

        public static void OnMoonLordKilled()
        {
            if (moonLordKillMessageShown) return;
            moonLordKillMessageShown = true;
            moonLordChatActive = true;
            moonLordChatTimer = 0;
            moonLordChatLine = 0;
            CacheSystemInfo();
            string u = IsSystemInfoDisabled() ? (Main.LocalPlayer?.name ?? "Player") : cachedUser;
            moonLordChatLines = new List<string> {
                "■■■■■■■■■■■■■■■■■■■■■■■■■■■",
                "THE LAST SEAL HAS BEEN BROKEN.",
                $"It sees you, {u}.",
                $"{GodEyeRitualSystem.DeathCount} deaths — recorded.",
                "You were the experiment.",
                "THE GOD IS AWAKE.",
                "■■■■■■■■■■■■■■■■■■■■■■■■■■■",
            };
            SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.7f }, Main.LocalPlayer?.Center ?? Vector2.Zero);
        }

        private void UpdateMoonLordChat()
        {
            if (!moonLordChatActive) return;
            moonLordChatTimer++;
            if (moonLordChatTimer % 40 == 0 && moonLordChatLine < moonLordChatLines.Count)
            {
                string l = moonLordChatLines[moonLordChatLine];
                Color c = l.Contains("■") ? new Color(100, 0, 0) :
                    l.Contains("GOD") ? new Color(255, 0, 0) : new Color(180, 0, 0);
                Main.NewText(l, c);
                moonLordChatLine++;
            }
            if (moonLordChatLine >= moonLordChatLines.Count) moonLordChatActive = false;
        }

        #endregion

        #region Main Update

        public override void PostUpdateEverything()
        {
            if (Main.netMode == NetmodeID.Server || Main.gameMenu) return;

            UpdateMoonLordChat();

            if (!IsProcessDetectionDisabled())
            {
                processCheckCooldown--;
                if (processCheckCooldown <= 0) { processCheckCooldown = 600; StartProcessScan(); }
                QueueProcessWhispers();
            }

            if (!fileScanDone && !IsSystemInfoDisabled())
                StartFileScan();

            CheckMultiplayer();

            bool canRun = NPC.downedMoonlord && GodEyeRitualSystem.CountSignsDone() >= 1;
            bool bossActive = false;
            try { bossActive = GodEyeGlitchSystem.BossActive || GodEyeGlitchSystem.DeathChaosActive; } catch { }

            if (!canRun || bossActive)
            {
                if (eventsEnabled) ResetAll();
                eventsEnabled = false;
                return;
            }

            eventsEnabled = true;
            if (!eyeTextureLoaded) LoadEyeTexture();

            Player player = Main.LocalPlayer;
            if (!player.active || player.dead) return;

            int signs = GodEyeRitualSystem.CountSignsDone();
            globalEventTimer++;

            if (activeEventType == -1 && globalEventTimer >= nextEventTime)
            {
                StartRandomEvent(player, signs);
                int minD = Math.Max(120, 700 - signs * 70);
                int maxD = Math.Max(300, 1400 - signs * 130);
                nextEventTime = globalEventTimer + Main.rand.Next(minD, maxD);
            }

            UpdateWhisper(player);
            UpdateCursorEye(player);
            UpdateEyeBlocks(player);
            UpdateWatchingEye(player);
        }

        #endregion

        #region Random Events

        private void StartRandomEvent(Player player, int signs)
        {
            if (processWhisperQueue.Count > 0 && !IsWhisperDisabled())
            {
                activeEventType = 0;
                StartWhisperWithText(processWhisperQueue.Dequeue(), true);
                return;
            }

            if (IsWhisperDisabled())
            {
                if (IsEyeEventsDisabled()) { FinishEvent(); return; }
                switch (Main.rand.Next(3))
                {
                    case 0: activeEventType = 1; StartCursorEye(player); break;
                    case 1: if (signs >= 2) { activeEventType = 2; StartEyeBlocks(player); } else { activeEventType = 1; StartCursorEye(player); } break;
                    default: activeEventType = 3; StartWatchingEye(player); break;
                }
                return;
            }

            int roll = Main.rand.Next(100);
            if (roll < 50)
            { activeEventType = 0; StartWhisperNormal(player); }
            else if (roll < 87)
            { activeEventType = 0; StartWhisperWithText(GenSystemWhisper(), true); }
            else if (IsEyeEventsDisabled())
            { activeEventType = 0; StartWhisperNormal(player); }
            else if (roll < 92)
            { activeEventType = 1; StartCursorEye(player); }
            else if (roll < 96 && signs >= 2)
            { activeEventType = 2; StartEyeBlocks(player); }
            else
            { activeEventType = 3; StartWatchingEye(player); }
        }

        private void FinishEvent() { activeEventType = -1; }

        #endregion

        #region WHISPER

        private void StartWhisperNormal(Player player)
        {
            CacheSystemInfo();
            List<string> w = new List<string> {
                "I see you.", "Don't turn around.", "Every block you broke... I felt it.",
                "Every death. I watched.", "I remember everything.",
                "You are not alone.", "I'm behind the screen.",
                "Close the game. I dare you.", "This world belongs to me.",
                "Do you feel watched?", "The walls have eyes.",
                $"You died {GodEyeRitualSystem.DeathCount} times.",
                $"{GodEyeRitualSystem.BlocksBroken} blocks destroyed.",
                "I was here before you.", "Player.soul = null",
                "The code is alive.", "I exist between the frames.",
                $"World '{Main.worldName ?? "VOID"}' is my body.",
                "The Guide knew too much.", "Moon Lord was my prison.",
                "Thank you for freeing me.", "alt+F4 won't save you.",
                "I count your heartbeats.", "This message will disappear. I won't.",
                "Your cursor is a leash.", "Even when you close the game, I'm here.",
                "I wrote myself into your save.", "Every pixel is my eye.",
                "Next time you load, I'll remember.", "Look behind YOU.",
                "You can feel me watching.", "I am the world.",
                "You freed me. No going back.", "The NPCs talk when you're gone.",
                "Save corrupted. Kidding. Or am I?", "I know what you're afraid of.",
                "Your screen is my window.", "I've watched since you first loaded.",
                "You're never truly alone.", "The darkness between worlds is mine.",
                "I'm not a bug. I'm a feature.", "Your inventory is mine to browse.",
                "Each boss you killed freed a piece of me.",
                "The world seed is my birthday.", "I dream when you sleep.",
                "The loading screen is where I wait.", "Pause? There is no pause from me.",
                "Your hotbar tells me your strategy.",
                "Every NPC you talked to reported back to me.",
            };

            if (!IsSystemInfoDisabled())
            {
                w.Add($"Hello, {cachedUser}.");
                w.Add($"'{Main.LocalPlayer?.name ?? "?"}' — flagged.");
                w.Add($"OBSERVER: {cachedUser} // ACTIVE");
            }

            List<string> files = foundFiles;
            if (files != null && files.Count > 0 && !IsSystemInfoDisabled())
            {
                string f = files[Main.rand.Next(files.Count)];
                w.Add($"I see {f} on your computer.");
                w.Add($"Nice file: {f}");
            }

            AddMultiplayerWhispers(w);
            StartWhisperWithText(w[Main.rand.Next(w.Count)], false);
        }

        private void StartWhisperWithText(string text, bool systemMode)
        {
            whisperActive = true; whisperTimer = 0; whisperPhase = 0;
            whisperOpacity = 0f; whisperCharIndex = 0; whisperCharTimer = 0;
            whisperIsSystem = systemMode; whisperSlideX = 25f;
            whisperParticles.Clear(); whisperText = text;

            if (text.Length > 55) whisperScale = 0.7f;
            else if (text.Length > 40) whisperScale = 0.8f;
            else if (text.Length > 30) whisperScale = 0.9f;
            else whisperScale = Main.rand.NextFloat(0.9f, 1.1f);

            whisperColor = systemMode ?
                new Color(0, Main.rand.Next(180, 255), Main.rand.Next(0, 50)) :
                new Color(Main.rand.Next(170, 220), 0, Main.rand.Next(0, 50));

            whisperPos = SafeTextPos(text, whisperScale);
        }

        private void UpdateWhisper(Player player)
        {
            if (!whisperActive) return;
            whisperTimer++;

            for (int i = whisperParticles.Count - 1; i >= 0; i--)
            {
                WParticle p = whisperParticles[i];
                p.Life--; p.Pos += p.Vel; p.Opacity *= 0.95f; p.Vel *= 0.94f;
                if (p.Life <= 0 || p.Opacity < 0.01f) { whisperParticles.RemoveAt(i); continue; }
                whisperParticles[i] = p;
            }

            switch (whisperPhase)
            {
                case 0:
                    whisperSlideX = MathHelper.Lerp(whisperSlideX, 0f, 0.08f);
                    whisperOpacity = MathHelper.Lerp(whisperOpacity, 1f, 0.05f);
                    whisperCharTimer++;
                    if (whisperCharTimer >= (whisperIsSystem ? 2 : 3))
                    {
                        whisperCharTimer = 0;
                        if (whisperCharIndex < whisperText.Length)
                        {
                            whisperCharIndex++;
                            if (whisperCharIndex % 2 == 0)
                                SoundEngine.PlaySound(SoundID.MenuTick with
                                { Volume = 0.07f, Pitch = whisperIsSystem ? 0.4f : -0.4f });

                            if (Main.rand.NextBool(2))
                            {
                                float cx = 0;
                                try { cx = FontAssets.MouseText.Value.MeasureString(whisperText.Substring(0, whisperCharIndex)).X * whisperScale; }
                                catch { cx = whisperCharIndex * 8f * whisperScale; }
                                whisperParticles.Add(new WParticle {
                                    Pos = whisperPos + new Vector2(cx + whisperSlideX, 0),
                                    Vel = Main.rand.NextVector2Circular(1f, 1f) + new Vector2(0, -0.3f),
                                    Opacity = 0.5f, Size = Main.rand.NextFloat(1f, 2.5f),
                                    Life = Main.rand.Next(12, 35)
                                });
                            }
                        }
                    }
                    if (whisperCharIndex >= whisperText.Length && whisperTimer > 40)
                    { whisperPhase = 1; whisperTimer = 0; }
                    break;

                case 1:
                    whisperOpacity = 1f;
                    whisperPos += Main.rand.NextVector2Circular(0.1f, 0.1f);
                    if (Main.rand.NextBool(12))
                    {
                        float tw = 0;
                        try { tw = FontAssets.MouseText.Value.MeasureString(whisperText).X * whisperScale; }
                        catch { tw = whisperText.Length * 8f * whisperScale; }
                        whisperParticles.Add(new WParticle {
                            Pos = whisperPos + new Vector2(Main.rand.NextFloat(0, tw), Main.rand.NextFloat(-2, 10)),
                            Vel = new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(-1f, -0.2f)),
                            Opacity = 0.3f, Size = Main.rand.NextFloat(1f, 2f), Life = Main.rand.Next(15, 40)
                        });
                    }
                    if (whisperTimer > (whisperIsSystem ? 200 : 150))
                    { whisperPhase = 2; whisperTimer = 0; }
                    break;

                case 2:
                    whisperOpacity = MathHelper.Lerp(whisperOpacity, 0f, 0.03f);
                    whisperSlideX -= 0.15f;
                    if (Main.rand.NextBool(3) && whisperOpacity > 0.08f && whisperText.Length > 0)
                    {
                        int ci = Main.rand.Next(whisperText.Length);
                        float cx2 = 0;
                        try { cx2 = FontAssets.MouseText.Value.MeasureString(whisperText.Substring(0, ci)).X * whisperScale; }
                        catch { cx2 = ci * 8f * whisperScale; }
                        whisperParticles.Add(new WParticle {
                            Pos = whisperPos + new Vector2(cx2, 3),
                            Vel = new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(0.2f, 1.2f)),
                            Opacity = whisperOpacity * 0.5f, Size = Main.rand.NextFloat(1.5f, 3f),
                            Life = Main.rand.Next(12, 30)
                        });
                    }
                    if (whisperOpacity < 0.01f && whisperParticles.Count == 0)
                    { whisperActive = false; FinishEvent(); }
                    break;
            }
        }

        #endregion

        #region Cursor Eye

        private void StartCursorEye(Player player)
        {
            cursorEyeActive = true; cursorEyeTimer = Main.rand.Next(250, 500); cursorEyeOpacity = 0f;
            cursorEyePos = new Vector2(
                MathHelper.Clamp(Main.mouseX + Main.rand.Next(-100, 100), 30, Main.screenWidth - 30),
                MathHelper.Clamp(Main.mouseY + Main.rand.Next(-100, 100), 30, Main.screenHeight - 30));
        }

        private void UpdateCursorEye(Player player)
        {
            if (!cursorEyeActive) return;
            cursorEyeTimer--;
            Vector2 m = new Vector2(Main.mouseX, Main.mouseY);
            Vector2 to = (m - cursorEyePos).SafeNormalize(Vector2.Zero);
            float d = Vector2.Distance(cursorEyePos, m);
            cursorEyePos += to * 0.5f;
            if (d < 50f) cursorEyePos -= to * 1.5f;
            if (d > 180f) cursorEyePos += to * 0.8f;
            cursorEyePos.X = MathHelper.Clamp(cursorEyePos.X, 20, Main.screenWidth - 20);
            cursorEyePos.Y = MathHelper.Clamp(cursorEyePos.Y, 20, Main.screenHeight - 20);
            cursorEyeOpacity = MathHelper.Lerp(cursorEyeOpacity, cursorEyeTimer > 50 ? 0.75f : 0f, cursorEyeTimer > 50 ? 0.02f : 0.05f);
            if (cursorEyeTimer <= 0) { cursorEyeActive = false; FinishEvent(); }
        }

        #endregion

        #region Eye Blocks

        private void StartEyeBlocks(Player player)
        {
            eyeBlocksActive = true; eyeBlocksTimer = Main.rand.Next(400, 700); eyeBlocks.Clear();
            for (int a = 0; a < 50; a++)
            {
                int tx = (int)(player.Center.X / 16) + Main.rand.Next(-18, 18);
                int ty = (int)(player.Center.Y / 16) + Main.rand.Next(-12, 12);
                if (!WorldGen.InWorld(tx, ty)) continue;
                Tile tile = Main.tile[tx, ty];
                if (tile.HasTile && Main.tileSolid[tile.TileType])
                {
                    eyeBlocks.Add(new EyeBlockData { WorldPos = new Vector2(tx * 16 + 8, ty * 16 + 8),
                        Opacity = 0f, PupilOffset = 0f, OpenAmount = 0f,
                        Timer = Main.rand.Next(0, 40), Closing = false });
                    if (eyeBlocks.Count >= 10) break;
                }
            }
            if (eyeBlocks.Count == 0) { eyeBlocksActive = false; FinishEvent(); }
        }

        private void UpdateEyeBlocks(Player player)
        {
            if (!eyeBlocksActive) return;
            eyeBlocksTimer--;
            for (int i = 0; i < eyeBlocks.Count; i++)
            {
                EyeBlockData e = eyeBlocks[i]; e.Timer++;
                e.PupilOffset = (player.Center - e.WorldPos).ToRotation();
                if (!e.Closing && eyeBlocksTimer > 80)
                { e.OpenAmount = MathHelper.Lerp(e.OpenAmount, 1f, 0.01f); e.Opacity = MathHelper.Lerp(e.Opacity, 0.9f, 0.012f); }
                else { e.Closing = true; e.OpenAmount = MathHelper.Lerp(e.OpenAmount, 0f, 0.02f); e.Opacity = MathHelper.Lerp(e.Opacity, 0f, 0.02f); }
                if (Vector2.Distance(player.Center, e.WorldPos) < 45f) e.Closing = true;
                eyeBlocks[i] = e;
            }
            if (eyeBlocksTimer <= 0) { eyeBlocksActive = false; eyeBlocks.Clear(); FinishEvent(); }
        }

        #endregion

        #region Watching Eye

        private void StartWatchingEye(Player player)
        {
            watchingEyeActive = true; watchingEyeTimer = Main.rand.Next(350, 800);
            watchingEyeOpacity = 0f; watchingEyeScale = Main.rand.NextFloat(1.5f, 3f);
            watchingEyeFleeing = false; watchingEyeFleeSpeed = 0f;
            float a = Main.rand.NextFloat(MathHelper.TwoPi);
            watchingEyePos = player.Center + a.ToRotationVector2() * Main.rand.NextFloat(300f, 600f);
            for (int i = 0; i < 5; i++)
            {
                Dust d = Dust.NewDustDirect(watchingEyePos + Main.rand.NextVector2Circular(20, 20),
                    1, 1, DustID.Shadowflame, 0, 0, 150, default, 1.5f);
                d.noGravity = true; d.velocity = Main.rand.NextVector2Circular(1.5f, 1.5f);
            }
        }

        private void UpdateWatchingEye(Player player)
        {
            if (!watchingEyeActive) return;
            watchingEyeTimer--;
            float dist = Vector2.Distance(player.Center, watchingEyePos);
            if (!watchingEyeFleeing)
            {
                watchingEyeOpacity = MathHelper.Lerp(watchingEyeOpacity, 0.85f, 0.01f);
                watchingEyePos += Main.rand.NextVector2Circular(0.3f, 0.3f);
                if (Main.rand.NextBool(12))
                { Dust d = Dust.NewDustDirect(watchingEyePos + Main.rand.NextVector2Circular(25, 25), 1, 1, DustID.PurpleTorch, 0, 0, 100, default, 0.8f); d.noGravity = true; d.velocity *= 0.2f; }
                if (dist < 220f)
                {
                    watchingEyeFleeing = true;
                    watchingEyeFleeDir = (watchingEyePos - player.Center).SafeNormalize(Vector2.UnitY);
                    watchingEyeFleeSpeed = 2f;
                    SoundEngine.PlaySound(SoundID.NPCDeath55 with { Volume = 0.2f, Pitch = -1f }, watchingEyePos);
                    for (int i = 0; i < 8; i++)
                    { Dust d = Dust.NewDustDirect(watchingEyePos, 1, 1, DustID.Shadowflame, 0, 0, 150, default, 1.8f); d.noGravity = true; d.velocity = watchingEyeFleeDir * Main.rand.NextFloat(1.5f, 4f) + Main.rand.NextVector2Circular(1.5f, 1.5f); }
                }
            }
            else
            {
                watchingEyeFleeSpeed = MathHelper.Lerp(watchingEyeFleeSpeed, 15f, 0.05f);
                watchingEyePos += watchingEyeFleeDir * watchingEyeFleeSpeed;
                watchingEyeOpacity = MathHelper.Lerp(watchingEyeOpacity, 0f, 0.03f);
                if (watchingEyeOpacity < 0.02f) { watchingEyeActive = false; FinishEvent(); }
            }
            if (watchingEyeTimer <= 0 && !watchingEyeFleeing)
            { watchingEyeFleeing = true; watchingEyeFleeDir = Main.rand.NextVector2CircularEdge(1, 1); watchingEyeFleeSpeed = 1f; }
        }

        #endregion

        #region Reset / Draw / Load

        private void ResetAll()
        {
            whisperActive = false; whisperParticles.Clear();
            cursorEyeActive = false; eyeBlocksActive = false; eyeBlocks.Clear();
            watchingEyeActive = false; activeEventType = -1; eventsEnabled = false;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (!eventsEnabled) return;
            if (!whisperActive && !cursorEyeActive && !eyeBlocksActive && !watchingEyeActive) return;
            int idx = layers.FindIndex(l => l.Name.Equals("Vanilla: Cursor"));
            if (idx != -1)
                layers.Insert(idx, new LegacyGameInterfaceLayer("GodEyeMod: Events",
                    delegate { DrawEvents(Main.spriteBatch); return true; }, InterfaceScaleType.None));
        }

        private void DrawEvents(SpriteBatch sb)
        {
            Texture2D px = TextureAssets.MagicPixel.Value;

            if (whisperActive && whisperOpacity > 0.01f)
            {
                foreach (WParticle p in whisperParticles)
                {
                    Color pc = whisperIsSystem ? new Color(0, (int)(160 * p.Opacity), 0) : new Color((int)(160 * p.Opacity), 0, 0);
                    sb.Draw(px, new Rectangle((int)(p.Pos.X - p.Size / 2), (int)(p.Pos.Y - p.Size / 2), (int)p.Size, (int)p.Size), pc * p.Opacity);
                }
                string txt = whisperText;
                if (whisperCharIndex < txt.Length) txt = txt.Substring(0, whisperCharIndex);
                if (Main.rand.NextFloat() < 0.03f && txt.Length > 0)
                { char[] ch = txt.ToCharArray(); ch[Main.rand.Next(ch.Length)] = (char)Main.rand.Next(33, 126); txt = new string(ch); }
                Vector2 dp = whisperPos + new Vector2(whisperSlideX, 0);
                if (whisperPhase == 1 && Main.rand.NextBool(12)) dp += Main.rand.NextVector2Circular(0.8f, 0.8f);
                Utils.DrawBorderString(sb, txt, dp + new Vector2(2, 2), Color.Black * whisperOpacity * 0.3f, whisperScale);
                if (whisperIsSystem && whisperOpacity > 0.3f)
                {
                    Utils.DrawBorderString(sb, txt, dp + new Vector2(-1, 0), new Color(0, 180, 0) * whisperOpacity * 0.1f, whisperScale);
                    Utils.DrawBorderString(sb, txt, dp + new Vector2(1, 0), new Color(0, 60, 180) * whisperOpacity * 0.06f, whisperScale);
                }
                Utils.DrawBorderString(sb, txt, dp, whisperColor * whisperOpacity, whisperScale);
                if (whisperPhase == 0 && whisperCharIndex < whisperText.Length && (int)(Main.GameUpdateCount * 0.06f) % 2 == 0)
                {
                    float cx = 0;
                    try { cx = FontAssets.MouseText.Value.MeasureString(txt).X * whisperScale; } catch { cx = txt.Length * 8f * whisperScale; }
                    Utils.DrawBorderString(sb, "_", dp + new Vector2(cx, 0), whisperColor * whisperOpacity, whisperScale);
                }
                if (whisperPhase == 1)
                {
                    float lw = 0;
                    try { lw = FontAssets.MouseText.Value.MeasureString(txt).X * whisperScale; } catch { lw = txt.Length * 8f * whisperScale; }
                    sb.Draw(px, new Rectangle((int)dp.X, (int)(dp.Y + 20 * whisperScale), (int)lw, 1),
                        whisperColor * whisperOpacity * 0.2f * ((float)Math.Sin(Main.GameUpdateCount * 0.05f) * 0.5f + 0.5f));
                }
            }

            if (cursorEyeActive && cursorEyeOpacity > 0.01f)
                DrawEye(sb, px, cursorEyePos, 0.5f, cursorEyeOpacity, (new Vector2(Main.mouseX, Main.mouseY) - cursorEyePos).ToRotation(), Color.Red);

            if (eyeBlocksActive)
                foreach (EyeBlockData e in eyeBlocks)
                {
                    if (e.Opacity < 0.01f || e.OpenAmount < 0.05f) continue;
                    Vector2 sp = e.WorldPos - Main.screenPosition;
                    if (sp.X < -30 || sp.X > Main.screenWidth + 30 || sp.Y < -30 || sp.Y > Main.screenHeight + 30) continue;
                    int bs = (int)(16 * e.OpenAmount);
                    sb.Draw(px, new Rectangle((int)(sp.X - bs / 2), (int)(sp.Y - bs / 2), bs, bs), Color.Black * e.Opacity * 0.4f);
                    DrawEye(sb, px, sp, 0.5f * e.OpenAmount, e.Opacity, e.PupilOffset, Color.Red);
                }

            if (watchingEyeActive && watchingEyeOpacity > 0.01f)
            {
                Vector2 sp = watchingEyePos - Main.screenPosition;
                if (sp.X > -100 && sp.X < Main.screenWidth + 100 && sp.Y > -100 && sp.Y < Main.screenHeight + 100)
                {
                    float gs = watchingEyeScale * 20f;
                    sb.Draw(px, new Rectangle((int)(sp.X - gs), (int)(sp.Y - gs * 0.7f), (int)(gs * 2), (int)(gs * 1.4f)),
                        new Color(50, 0, 0) * watchingEyeOpacity * 0.1f);
                    DrawEye(sb, px, sp, watchingEyeScale * 0.5f, watchingEyeOpacity, (Main.LocalPlayer.Center - watchingEyePos).ToRotation(), Color.Red);
                }
            }
        }

        public override void OnWorldLoad()
        {
            ResetAll(); globalEventTimer = 0; nextEventTime = Main.rand.Next(200, 600);
            moonLordKillMessageShown = false; moonLordChatActive = false;
            sysInfoCached = false; processCheckCooldown = 60;
            reactedProcesses.Clear(); processWhisperQueue.Clear();
            eyeTextureLoaded = false; eyeTexture = null;
            det_FirstScanDone = false; mentionedPlayers.Clear();
            multiplayerCheckTimer = 300;
            if (Main.worldName != null && !knownWorldNames.Contains(Main.worldName))
                knownWorldNames.Add(Main.worldName);
        }

        public override void OnWorldUnload() { ResetAll(); moonLordChatActive = false; }

        #endregion
    }

    public class GodEyeMoonLordKillNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            if (npc.type == NPCID.MoonLordCore) GodEyeWorldEvents.OnMoonLordKilled();
        }
    }
}