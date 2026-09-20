using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace LaunchAuthorizationPlus
{
    internal enum TargetingMode
    {
        Default,
        Strict,
        FireOnce
    }

    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.heck0.launchauthorizationplus";
        public const string Name = "LaunchAuthorization+";
        public const string Version = "0.1.0";

        internal static ConfigEntry<bool> Enabled;

        internal static ConfigEntry<bool> ColorUnengageable;
        internal static ConfigEntry<Color> UnengageableColor;
        internal static ConfigEntry<Color> FiredColor;

        internal static TargetingMode Mode = TargetingMode.Default;
        internal static ConfigEntry<KeyboardShortcut> ToggleModeKey;
        internal static ConfigEntry<float> ResetHoldSeconds;
        internal static ConfigEntry<float> BombReleaseWindow;
        internal static ConfigEntry<bool> LaserAllowFactionLasing;
        internal static ConfigEntry<bool> LogWeaponInfo;

        internal static ManualLogSource Log;

        internal static bool Strict => Enabled.Value && Mode != TargetingMode.Default;

        internal static bool FireOnce => Enabled.Value && Mode == TargetingMode.FireOnce;

        private static bool ToggleDown()
        {
            var shortcut = ToggleModeKey.Value;
            if (shortcut.MainKey == KeyCode.None || !UnityInput.Current.GetKeyDown(shortcut.MainKey)) return false;
            foreach (var modifier in shortcut.Modifiers)
                if (!UnityInput.Current.GetKey(modifier)) return false;
            return true;
        }

        private bool togglePending;
        private bool holdHandled;
        private float toggleStart;

        private void Update()
        {
            var key = ToggleModeKey.Value.MainKey;
            if (key == KeyCode.None) return;

            if (!togglePending)
            {
                if (!ToggleDown()) return;
                togglePending = true;
                holdHandled = false;
                toggleStart = Time.unscaledTime;
                return;
            }

            if (!UnityInput.Current.GetKey(key))
            {
                togglePending = false;
                if (!holdHandled) CycleMode();
                return;
            }

            if (holdHandled || Time.unscaledTime - toggleStart < ResetHoldSeconds.Value) return;

            holdHandled = true;
            if (Mode != TargetingMode.FireOnce) return;
            FiredTracker.Clear();
            Report("Launch Authorization: <b>Fired marks reset</b>");
        }

        private void CycleMode()
        {
            Mode = Mode == TargetingMode.Default ? TargetingMode.Strict
                : Mode == TargetingMode.Strict ? TargetingMode.FireOnce
                : TargetingMode.Default;
            FiredTracker.Clear();
            Report(Mode == TargetingMode.FireOnce ? "Launch Authorization: <b>Strict Fire Once</b>"
                : Mode == TargetingMode.Strict ? "Launch Authorization: <b>Strict</b>"
                : "Launch Authorization: <b>Default</b>");
        }

        private void Report(string text)
        {
            try { SceneSingleton<AircraftActionsReport>.i.ReportText(text, 3f); }
            catch { Logger.LogInfo(text); }
        }

        private void Awake()
        {
            Log = Logger;
            LogWeaponInfo = Config.Bind("Debug", "LogWeaponInfo", true,
                "Write a line to LogOutput.log with the weapon's type flags and target requirements whenever you switch weapon station.");
            ToggleModeKey = Config.Bind("Targeting", "ToggleModeKey", new KeyboardShortcut(KeyCode.C),
                "Tap to cycle Launch Authorization: Default (vanilla firing) -> Strict (only engageable targets are fired at; nothing fires if none qualify) -> Strict Fire Once (Strict, and each target is fired at only once; fired targets are marked with FiredColor, and pressing fire when all are marked resets them). In Strict Fire Once, hold the key to reset all fired marks without changing mode. The game always starts in Default.");
            ResetHoldSeconds = Config.Bind("Targeting", "ResetHoldSeconds", 0.5f,
                new ConfigDescription(
                    "How long to hold the toggle key to reset fired marks in Strict Fire Once. A tap shorter than this cycles the mode instead.",
                    new AcceptableValueRange<float>(0.2f, 3f)));
            LaserAllowFactionLasing = Config.Bind("Targeting", "LaserAllowFactionLasing", false,
                "Laser-guided weapons only count a target as engageable if your own designator is lasing it. Turn this on to also accept targets lased by a teammate.");
            BombReleaseWindow = Config.Bind("Targeting", "BombReleaseWindowSeconds", 2f,
                new ConfigDescription(
                    "Bombs count as engageable only when the HUD release countdown (REL) is within this many seconds of zero.",
                    new AcceptableValueRange<float>(0.5f, 15f)));
            ColorUnengageable = Config.Bind("HUD", "ColorUnengageableTargets", true,
                "Tint selected targets that won't be fired at (out of range/arc) instead of the normal selected color.");
            UnengageableColor = Config.Bind("HUD", "UnengageableColor", new Color(1f, 0.85f, 0f, 1f),
                "Marker color for selected targets that won't be fired at.");
            FiredColor = Config.Bind("HUD", "FiredColor", new Color(1f, 0.2f, 0.9f, 1f),
                "Marker color for targets already fired on in Strict Fire Once mode.");
            Enabled = Config.Bind("General", "Enabled", true,
                "Light the fire indicator when any selected target is authorized, and engage only those targets.");

            new Harmony(Guid).PatchAll();
            Logger.LogInfo($"{Name} {Version} loaded");
        }
    }
}
