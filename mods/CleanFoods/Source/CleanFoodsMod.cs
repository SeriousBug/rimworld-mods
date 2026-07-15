using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace CleanFoods;

public class CleanFoodsSettings : ModSettings
{
    public float cleanThreshold = -2f;
    public float filthyFloor = -5f;
    public float maxChance = 0.05f;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref cleanThreshold, "cleanThreshold", -2f);
        Scribe_Values.Look(ref filthyFloor, "filthyFloor", -5f);
        Scribe_Values.Look(ref maxChance, "maxChance", 0.05f);
    }
}

public class CleanFoodsMod : Mod
{
    public const string LogPrefix = "[CleanFoods]";

    public static CleanFoodsSettings Settings { get; private set; }

    public CleanFoodsMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<CleanFoodsSettings>();
        new Harmony("connor.cleanfoods").PatchAll(Assembly.GetExecutingAssembly());
        Log.Message($"{LogPrefix} assembly loaded and Harmony patches applied.");
    }

    /// <summary>
    /// Poison chance for a cooking room at the given cleanliness: zero at or above the clean
    /// threshold, rising linearly to <c>maxChance</c> at or below the filthy floor.
    /// </summary>
    public static float PoisonChanceFor(float cleanliness)
    {
        var s = Settings;
        if (cleanliness >= s.cleanThreshold)
        {
            return 0f;
        }
        if (cleanliness <= s.filthyFloor)
        {
            return s.maxChance;
        }
        var span = s.cleanThreshold - s.filthyFloor;
        if (span <= 0f)
        {
            return s.maxChance;
        }
        var t = (s.cleanThreshold - cleanliness) / span;
        return s.maxChance * t;
    }

    public override string SettingsCategory() => "Connor's Clean Foods!";

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var list = new Listing_Standard();
        list.Begin(inRect);

        list.Label("CleanFoods_Setting_CleanThreshold".Translate(Settings.cleanThreshold.ToString("0.0")),
            tooltip: "CleanFoods_Setting_CleanThresholdDesc".Translate());
        Settings.cleanThreshold = list.Slider(Settings.cleanThreshold, -5f, 0.5f);

        list.Gap();
        list.Label("CleanFoods_Setting_FilthyFloor".Translate(Settings.filthyFloor.ToString("0.0")),
            tooltip: "CleanFoods_Setting_FilthyFloorDesc".Translate());
        Settings.filthyFloor = list.Slider(Settings.filthyFloor, -10f, -0.5f);

        list.Gap();
        list.Label("CleanFoods_Setting_MaxChance".Translate((Settings.maxChance * 100f).ToString("0.0")),
            tooltip: "CleanFoods_Setting_MaxChanceDesc".Translate());
        Settings.maxChance = list.Slider(Settings.maxChance, 0f, 1f);

        // Keep the floor strictly below the threshold so the curve always has a positive span.
        if (Settings.filthyFloor >= Settings.cleanThreshold)
        {
            Settings.filthyFloor = Settings.cleanThreshold - 0.5f;
        }

        list.Gap();
        if (list.ButtonText("CleanFoods_Setting_Reset".Translate()))
        {
            Settings.cleanThreshold = -2f;
            Settings.filthyFloor = -5f;
            Settings.maxChance = 0.05f;
        }

        list.End();
    }
}
