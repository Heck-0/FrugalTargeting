using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace FrugalTargeting
{
    internal enum TargetingMode
    {
        Default,
        SelectiveFire
    }

    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.heck0.frugaltargeting";
        public const string Name = "Frugal Targeting";
        public const string Version = "0.1.0";

        internal static ConfigEntry<bool> Enabled;

        internal static ConfigEntry<bool> ColorUnengageable;
        internal static ConfigEntry<Color> UnengageableColor;

        internal static ConfigEntry<TargetingMode> Mode;
        internal static ConfigEntry<KeyboardShortcut> ToggleModeKey;
        internal static ConfigEntry<float> BombReleaseWindow;
        internal static ConfigEntry<bool> LogWeaponInfo;

        internal static ManualLogSource Log;

        internal static bool Selective => Enabled.Value && Mode.Value == TargetingMode.SelectiveFire;

        private void Update()
        {
            if (!ToggleModeKey.Value.IsDown()) return;

            Mode.Value = Mode.Value == TargetingMode.Default ? TargetingMode.SelectiveFire : TargetingMode.Default;
            var text = Mode.Value == TargetingMode.SelectiveFire
                ? "Targeting: <b>Selective fire</b>"
                : "Targeting: <b>Default</b>";
            try { SceneSingleton<AircraftActionsReport>.i.ReportText(text, 3f); }
            catch { Logger.LogInfo(text); }
        }

        private void Awake()
        {
            Log = Logger;
            LogWeaponInfo = Config.Bind("Debug", "LogWeaponInfo", true,
                "Write a line to LogOutput.log with the weapon's type flags and target requirements whenever you switch weapon station.");
            Mode = Config.Bind("Targeting", "Mode", TargetingMode.Default,
                "Default: vanilla firing. SelectiveFire: only engageable targets are fired at, and nothing fires if none qualify.");
            ToggleModeKey = Config.Bind("Targeting", "ToggleModeKey", new KeyboardShortcut(KeyCode.C),
                "Key that switches between targeting modes.");            BombReleaseWindow = Config.Bind("Targeting", "BombReleaseWindowSeconds", 3f,
                new ConfigDescription(
                    "Bombs count as engageable only when the HUD release countdown (REL) is within this many seconds of zero.",
                    new AcceptableValueRange<float>(0.5f, 15f)));
            ColorUnengageable = Config.Bind("HUD", "ColorUnengageableTargets", true,
                "Tint selected targets that won't be fired at (out of range/arc) instead of the normal selected color.");
            UnengageableColor = Config.Bind("HUD", "UnengageableColor", new Color(1f, 0.85f, 0f, 1f),
                "Marker color for selected targets that won't be fired at.");
            Enabled = Config.Bind("General", "Enabled", true,
                "Light the fire indicator when any selected target is authorized, and engage only those targets.");

            new Harmony(Guid).PatchAll();
            Logger.LogInfo($"{Name} {Version} loaded");
        }
    }
}
