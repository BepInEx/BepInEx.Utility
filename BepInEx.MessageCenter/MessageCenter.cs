﻿using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;
using UnityEngine;

namespace BepInEx
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class MessageCenter : BaseUnityPlugin
    {
        public const string GUID = "com.bepis.messagecenter";
        public const string PluginName = "Message Center";
        public const string Version = "1.0";

        private static readonly List<LogEntry> _shownLogLines = new List<LogEntry>();

        private static float _showCounter;
        private static string _shownLogText = string.Empty;

        public static ConfigWrapper<bool> Enabled { get; private set; }

        public MessageCenter()
        {
            Enabled = Config.Wrap("General", "Show messages in UI", "Allow plugins to show on screen messages", true);
            Logging.Logger.Listeners.Add(new MessageLogListener());

            foreach (var dependencyError in Chainloader.DependencyErrors)
                ShowText(dependencyError);
        }

        private static void OnEntryLogged(LogEventArgs logEventArgs)
        {
            if (!Enabled.Value) return;
            if ("BepInEx".Equals(logEventArgs.Source.SourceName, StringComparison.Ordinal)) return;
            if ((logEventArgs.Level & LogLevel.Message) == LogLevel.None) return;

            if (logEventArgs.Data != null)
                ShowText(logEventArgs.Data.ToString());
        }

        private static void ShowText(string logText)
        {
            if (_showCounter <= 0)
                _shownLogLines.Clear();

            _showCounter = Mathf.Clamp(_showCounter, 7, 12);

            var logEntry = _shownLogLines.FirstOrDefault(x => x.Text.Equals(logText, StringComparison.Ordinal));
            if (logEntry == null)
            {
                logEntry = new LogEntry(logText);
                _shownLogLines.Add(logEntry);

                _showCounter += 0.8f;
            }

            logEntry.Count++;

            var logLines = _shownLogLines.Select(x => x.Count > 1 ? $"{x.Count}x {x.Text}" : x.Text).ToArray();
            _shownLogText = string.Join("\r\n", logLines);
        }

        private void Update()
        {
            if (_showCounter > 0)
                _showCounter -= Time.deltaTime;
        }

        private void OnGUI()
        {
            if (_showCounter <= 0) return;

            var textColor = Color.black;
            var outlineColor = Color.white;

            if (_showCounter <= 1)
            {
                textColor.a = _showCounter;
                outlineColor.a = _showCounter;
            }

            ShadowAndOutline.DrawOutline(new Rect(40, 20, Screen.width - 80, 160), _shownLogText, new GUIStyle
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 20
            }, textColor, outlineColor, 3);
        }
    }
}
