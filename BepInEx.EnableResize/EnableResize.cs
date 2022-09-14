using BepInEx.Configuration;
using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace BepInEx
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class EnableResize : BaseUnityPlugin
    {
        public const string GUID = "BepInEx.EnableResize";
        public const string PluginName = "Enable Resize";
        public const string Version = "2.0";

        public static ConfigEntry<bool> ConfigEnableResize { get; private set; }

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        // Almost the same: https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowlongptra
        private const int GWL_STYLE = -16;

        // https://docs.microsoft.com/en-us/windows/win32/winmsg/window-styles
        private const int WS_CAPTION = 0XC00000;
        private const int WS_MAXIMIZEBOX = 0x10000;
        private const int WS_MINIMIZEBOX = 0x20000;
        private const int WS_SYSMENU = 0x80000;
        private const int WS_THICKFRAME = 0x40000;

        private const string GET_CLASS_NAME_MAGIC = "UnityWndClass";
        private IntPtr WindowHandle = IntPtr.Zero;

        private int windowStyle = 0;
        private bool fullScreen = false;
        private bool prevFullScreen = true;
        private int resolutionCheck = 0;
        private int prevResolutionCheck = 1;
        private int borderlessStyle = 1;
        private int prevBorderlessStyle = 0;
        private int borderlessMask = WS_CAPTION | WS_MAXIMIZEBOX | WS_MINIMIZEBOX | WS_SYSMENU | WS_THICKFRAME;
        private WaitForSecondsRealtime oneSecond = new WaitForSecondsRealtime(1f);

        internal void Awake()
        {
            ConfigEnableResize = Config.Bind("Config", "Enable Resize", false, "Whether to allow the game window to be resized. Requires game restart to take effect.");
            if (!ConfigEnableResize.Value) return;

            var pid = Process.GetCurrentProcess().Id;
            EnumWindows((w, param) =>
            {
                if (w == IntPtr.Zero) return true;
                if (GetWindowThreadProcessId(w, out uint lpdwProcessId) == 0) return true;
                if (lpdwProcessId != pid) return true;
                var cn = new StringBuilder(256);
                if (GetClassName(w, cn, cn.Capacity) == 0) return true;
                if (cn.ToString() != GET_CLASS_NAME_MAGIC) return true;
                WindowHandle = w;
                return false;
            }, IntPtr.Zero);

            if (WindowHandle == IntPtr.Zero) return;

            StartCoroutine(TestScreen());
            ConfigEnableResize.SettingChanged += (sender, args) =>  StartCoroutine(TestScreen());
        }

        private IEnumerator TestScreen()
        {
            while (true)
            {
                if (!ConfigEnableResize.Value) yield break;

                fullScreen = Screen.fullScreen;
                resolutionCheck = Screen.width + Screen.height;
                windowStyle = GetWindowLong(WindowHandle, GWL_STYLE);

                // If zero, is in borderless mode
                borderlessStyle = windowStyle & borderlessMask;

                if (!fullScreen && prevFullScreen ||
                    resolutionCheck != prevResolutionCheck ||
                    (borderlessStyle != 0) && (prevBorderlessStyle == 0))
                {
                    ResizeWindow();
                }

                prevBorderlessStyle = borderlessStyle;
                prevFullScreen = fullScreen;
                prevResolutionCheck = resolutionCheck;
                yield return oneSecond;
            }
        }

        private void ResizeWindow()
        {
            if (fullScreen) return;
            if (borderlessStyle == 0) return;
            windowStyle = GetWindowLong(WindowHandle, GWL_STYLE);
            windowStyle |= WS_THICKFRAME | WS_MAXIMIZEBOX;
            SetWindowLong(WindowHandle, GWL_STYLE, windowStyle);
        }
    }
}