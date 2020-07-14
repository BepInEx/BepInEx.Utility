using BepInEx.Configuration;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BepInEx
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class EnableResize : BaseUnityPlugin
    {
        public const string GUID = "BepInEx.EnableResize";
        public const string PluginName = "Enable Resize";
        public const string Version = "1.5";
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private static bool _ConfigEnableResize;
        public static ConfigEntry<bool> ConfigEnableResize { get; private set; }

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

        private const int GWL_STYLE = -16;
        private const int WS_THICKFRAME = 0x40000;
        private const int WS_MAXIMIZEBOX = 0x10000;
        private const string GET_CLASS_NAME_MAGIC = "UnityWndClass";

        private IntPtr WindowHandle = IntPtr.Zero;
        private bool prev = false;

        internal void Awake()
        {
            ConfigEnableResize = Config.Bind("Config", "Enable Resize", false, "Whether to allow the game window to be resized. Requires game restart to take effect.");
            _ConfigEnableResize = ConfigEnableResize.Value;
            if (!_ConfigEnableResize) return;

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
            SceneManager.sceneLoaded += (s, lsm) =>
            {
                if (lsm == LoadSceneMode.Single) ResizeWindow();
            };
        }

        internal void Update()
        {
            if (!ConfigEnableResize.Value) return;
            var fs = Screen.fullScreen;
            if (!fs && prev) ResizeWindow();
            prev = fs;
        }

        private void ResizeWindow()
        {
            if (Screen.fullScreen) return;
            var style = GetWindowLong(WindowHandle, GWL_STYLE);
            style |= WS_THICKFRAME | WS_MAXIMIZEBOX;
            SetWindowLong(WindowHandle, GWL_STYLE, style);
        }
    }
}