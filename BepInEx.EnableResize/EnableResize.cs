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
        private int borderlessStyle = 1;
        private const int borderlessMask = WS_CAPTION | WS_MAXIMIZEBOX | WS_MINIMIZEBOX | WS_SYSMENU | WS_THICKFRAME;
        private int resizableStyle = 1;
        private const int resizableMask = WS_THICKFRAME | WS_MAXIMIZEBOX;
        private WaitForSecondsRealtime oneSecond = new WaitForSecondsRealtime(1f);
        private bool isInitialized = false;

        internal void Awake()
        {
            ConfigEnableResize = Config.Bind("Config", "Enable Resize", false, "Whether to allow the game window to be resized.");
            ConfigEnableResize.SettingChanged += (sender, args) => Initialize();

            Initialize();
        }

        private void Initialize()
        {
            if (!ConfigEnableResize.Value) return;

            if (isInitialized)
            {
                StartCoroutine(TestScreen());
                return;
            }

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

            isInitialized = true;

            StartCoroutine(TestScreen());
        }

        private IEnumerator TestScreen()
        {
            while (true)
            {
                if (!ConfigEnableResize.Value) yield break;

                fullScreen = Screen.fullScreen;
                windowStyle = GetWindowLong(WindowHandle, GWL_STYLE);

                // If zero, is in borderless mode
                borderlessStyle = windowStyle & borderlessMask;

                // if zero, is not resizable
                resizableStyle = windowStyle & resizableMask;

                if (resizableStyle == 0 &&
                    borderlessStyle != 0 &&
                    fullScreen == false)
                {
                    ResizeWindow();
                }

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