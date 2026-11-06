using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using ReLogic.Content;

namespace GodEyeMod.Content.NPCs.GodEye
{
    public class GodEyeIconReplacer : ModSystem
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr handle);

        [DllImport("user32.dll")]
        private static extern IntPtr CreateIconIndirect(ref ICONINFO piconinfo);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint nPlanes, uint nBitCount, IntPtr lpBits);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateDIBSection(
            IntPtr hdc, ref BITMAPINFO pbmi, uint usage,
            out IntPtr ppvBits, IntPtr hSection, uint offset);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool SetWindowText(IntPtr hWnd, string lpString);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        private const uint WM_SETICON = 0x0080;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;

        private static bool _isSupported = false;
        private static IntPtr _windowHandle = IntPtr.Zero;
        private static IntPtr _deathIconHandle = IntPtr.Zero;
        private static bool _deathIconApplied = false;

        private static Texture2D _deathIconTexture = null;
        private static bool _triedLoadTexture = false;

        public static bool ShouldApplyDeathIcon = false;

        private static int _titleGlitchTimer = 0;
        private static bool _titleGlitching = false;
        private static int _currentTitleIndex = 0;

        private static string[] deathTitles = new string[]
        {
            "REALITY.EXE HAS STOPPED WORKING",
            "Y̸O̵U̷ ̶K̵I̴L̶L̷E̵D̸ ̵G̸O̷D̶",
            "ERROR: WORLD NOT FOUND",
            "terraria.exe - Application Error",
            "FATAL EXCEPTION IN: universe.dll",
            "THE VOID WATCHES",
            "0x00000000 CRITICAL_PROCESS_DIED",
            "THERE IS NOTHING LEFT",
            "WORLD.DAT: CORRUPTED",
            "I SEE EVERYTHING",
            "PLAYER_SOUL.DLL MISSING",
            "UNHANDLED EXCEPTION: GodDeathError",
        };

        private static string _finalTitle = "I WILL RETURN";

        public override void Load()
        {
            if (Main.dedServ) return;

            try
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    _isSupported = false;
                    return;
                }
                _isSupported = true;
            }
            catch
            {
                _isSupported = false;
            }
        }

        private static IntPtr FindTerrariaWindow()
        {
            IntPtr found = IntPtr.Zero;

            try
            {
                using (var proc = Process.GetCurrentProcess())
                {
                    IntPtr mainWnd = proc.MainWindowHandle;
                    if (mainWnd != IntPtr.Zero && IsWindow(mainWnd))
                        return mainWnd;
                }
            }
            catch { }

            try
            {
                uint currentPid = (uint)Environment.ProcessId;

                EnumWindows((hWnd, lParam) =>
                {
                    GetWindowThreadProcessId(hWnd, out uint pid);
                    if (pid == currentPid && IsWindowVisible(hWnd))
                    {
                        var sb = new System.Text.StringBuilder(256);
                        GetWindowText(hWnd, sb, sb.Capacity);
                        string title = sb.ToString();

                        if (title.Contains("Terraria", StringComparison.OrdinalIgnoreCase) ||
                            title.Contains("tModLoader", StringComparison.OrdinalIgnoreCase))
                        {
                            found = hWnd;
                            return false;
                        }
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch { }

            if (found != IntPtr.Zero) return found;

            try
            {
                uint currentPid = (uint)Environment.ProcessId;
                EnumWindows((hWnd, lParam) =>
                {
                    GetWindowThreadProcessId(hWnd, out uint pid);
                    if (pid == currentPid && IsWindowVisible(hWnd))
                    {
                        found = hWnd;
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch { }

            return found;
        }

        private void LoadDeathIconTexture()
        {
            if (_deathIconTexture != null || _triedLoadTexture) return;
            _triedLoadTexture = true;

            try
            {
                _deathIconTexture = ModContent.Request<Texture2D>(
                    "GodEyeMod/Content/NPCs/GodEye/DeathIcon",
                    AssetRequestMode.ImmediateLoad
                ).Value;
            }
            catch (Exception ex)
            {
                Mod.Logger.Warn("GodEyeIconReplacer: Cannot load death icon: " + ex.Message);
                _deathIconTexture = null;
            }
        }

        public override void PostUpdateEverything()
        {
            if (!_isSupported || Main.dedServ) return;

            if (_windowHandle == IntPtr.Zero || !IsWindow(_windowHandle))
            {
                _windowHandle = FindTerrariaWindow();
                if (_windowHandle == IntPtr.Zero) return;
            }

            if (ShouldApplyDeathIcon && !_deathIconApplied)
            {
                ApplyDeathIcon();
                StartTitleGlitch();
                ShouldApplyDeathIcon = false;
            }

            if (_titleGlitching)
            {
                UpdateTitleGlitch();
            }
        }

        private void ApplyDeathIcon()
        {
            LoadDeathIconTexture();
            if (_deathIconTexture == null) return;

            try
            {
                IntPtr newIcon = CreateIconFromTexture(_deathIconTexture);
                if (newIcon == IntPtr.Zero) return;

                if (_deathIconHandle != IntPtr.Zero)
                    DestroyIcon(_deathIconHandle);

                _deathIconHandle = newIcon;

                SendMessage(_windowHandle, WM_SETICON, (IntPtr)ICON_SMALL, newIcon);
                SendMessage(_windowHandle, WM_SETICON, (IntPtr)ICON_BIG, newIcon);

                _deathIconApplied = true;
            }
            catch { }
        }

        private void StartTitleGlitch()
        {
            _titleGlitching = true;
            _titleGlitchTimer = 0;
            _currentTitleIndex = Main.rand.Next(deathTitles.Length);

            try
            {
                SetWindowText(_windowHandle, deathTitles[_currentTitleIndex]);
            }
            catch { }
        }

        private void UpdateTitleGlitch()
        {
            if (_windowHandle == IntPtr.Zero || !IsWindow(_windowHandle)) return;

            _titleGlitchTimer++;

            try
            {
                if (_titleGlitchTimer < 300)
                {
                    if (_titleGlitchTimer % 120 == 0)
                    {
                        _currentTitleIndex = (_currentTitleIndex + 1) % deathTitles.Length;
                        SetWindowText(_windowHandle, deathTitles[_currentTitleIndex]);
                    }
                }
                else if (_titleGlitchTimer < 600)
                {
                    if (_titleGlitchTimer % 30 == 0)
                    {
                        if (Main.rand.NextBool(3))
                        {
                            string glitch = "";
                            for (int i = 0; i < Main.rand.Next(10, 40); i++)
                                glitch += (char)Main.rand.Next(33, 126);
                            SetWindowText(_windowHandle, glitch);
                        }
                        else
                        {
                            _currentTitleIndex = Main.rand.Next(deathTitles.Length);
                            SetWindowText(_windowHandle, deathTitles[_currentTitleIndex]);
                        }
                    }
                }
                else if (_titleGlitchTimer < 900)
                {
                    if (_titleGlitchTimer % 10 == 0)
                    {
                        string garbage = "";
                        int len = Main.rand.Next(5, 60);
                        for (int i = 0; i < len; i++)
                        {
                            if (Main.rand.NextBool(4))
                                garbage += " ";
                            else
                                garbage += (char)Main.rand.Next(33, 126);
                        }
                        SetWindowText(_windowHandle, garbage);
                    }
                }
                else if (_titleGlitchTimer == 900)
                {
                    SetWindowText(_windowHandle, "");
                }
                else if (_titleGlitchTimer == 930)
                {
                    SetWindowText(_windowHandle, "...");
                }
                else if (_titleGlitchTimer == 960)
                {
                    SetWindowText(_windowHandle, _finalTitle);
                    _titleGlitching = false;
                }
            }
            catch { }
        }

        private IntPtr CreateIconFromTexture(Texture2D texture)
        {
            int w = texture.Width;
            int h = texture.Height;

            var pixels = new Color[w * h];
            texture.GetData(pixels);

            IntPtr hdc = CreateCompatibleDC(IntPtr.Zero);
            if (hdc == IntPtr.Zero) return IntPtr.Zero;

            var bmi = new BITMAPINFO();
            bmi.bmiHeader.biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>();
            bmi.bmiHeader.biWidth = w;
            bmi.bmiHeader.biHeight = -h;
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 32;
            bmi.bmiHeader.biCompression = 0;

            IntPtr ppvBits;
            IntPtr hbmColor = CreateDIBSection(hdc, ref bmi, 0, out ppvBits, IntPtr.Zero, 0);

            if (hbmColor == IntPtr.Zero || ppvBits == IntPtr.Zero)
            {
                DeleteDC(hdc);
                return IntPtr.Zero;
            }

            byte[] bgraData = new byte[w * h * 4];
            for (int i = 0; i < w * h; i++)
            {
                Color c = pixels[i];
                byte a = c.A;
                byte r, g, b;

                if (a > 0)
                {
                    r = (byte)Math.Min(255, c.R * 255 / a);
                    g = (byte)Math.Min(255, c.G * 255 / a);
                    b = (byte)Math.Min(255, c.B * 255 / a);
                }
                else { r = 0; g = 0; b = 0; }

                bgraData[i * 4 + 0] = b;
                bgraData[i * 4 + 1] = g;
                bgraData[i * 4 + 2] = r;
                bgraData[i * 4 + 3] = a;
            }

            Marshal.Copy(bgraData, 0, ppvBits, bgraData.Length);

            int maskStride = ((w + 31) / 32) * 4;
            byte[] maskData = new byte[maskStride * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (pixels[y * w + x].A < 128)
                    {
                        int byteIndex = y * maskStride + x / 8;
                        int bitIndex = 7 - (x % 8);
                        maskData[byteIndex] |= (byte)(1 << bitIndex);
                    }
                }
            }

            IntPtr maskPtr = Marshal.AllocHGlobal(maskData.Length);
            Marshal.Copy(maskData, 0, maskPtr, maskData.Length);
            IntPtr hbmMask = CreateBitmap(w, h, 1, 1, maskPtr);
            Marshal.FreeHGlobal(maskPtr);

            if (hbmMask == IntPtr.Zero)
            {
                DeleteObject(hbmColor);
                DeleteDC(hdc);
                return IntPtr.Zero;
            }

            var iconInfo = new ICONINFO
            {
                fIcon = true,
                xHotspot = 0,
                yHotspot = 0,
                hbmMask = hbmMask,
                hbmColor = hbmColor
            };

            IntPtr hIcon = CreateIconIndirect(ref iconInfo);

            DeleteObject(hbmColor);
            DeleteObject(hbmMask);
            DeleteDC(hdc);

            return hIcon;
        }

        public static void TriggerDeathIcon()
        {
            ShouldApplyDeathIcon = true;
        }

        public static bool IsDeathIconActive => _deathIconApplied;

        public override void OnWorldUnload()
        {
        }

        public override void Unload()
        {
            try
            {
                if (_deathIconHandle != IntPtr.Zero)
                {
                    if (_windowHandle != IntPtr.Zero && IsWindow(_windowHandle))
                    {
                        SendMessage(_windowHandle, WM_SETICON, (IntPtr)ICON_SMALL, IntPtr.Zero);
                        SendMessage(_windowHandle, WM_SETICON, (IntPtr)ICON_BIG, IntPtr.Zero);
                    }
                    DestroyIcon(_deathIconHandle);
                    _deathIconHandle = IntPtr.Zero;
                }
            }
            catch { }

            _deathIconTexture = null;
            _windowHandle = IntPtr.Zero;
            _deathIconApplied = false;
            _triedLoadTexture = false;
            _isSupported = false;
            _titleGlitching = false;
            ShouldApplyDeathIcon = false;
        }
    }
}