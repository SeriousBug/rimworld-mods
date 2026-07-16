using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace LocalizedCleanliness;

public class LocalizedCleanlinessSettings : ModSettings
{
    public bool removeCookSkill = false;

    public bool localCleanliness = true;
    public bool localForCooking = true;
    public bool localForSurgery = true;
    public bool localForTending = true;

    public float radius = 4f;
    public float falloffPower = 1f;

    public bool LocalActiveFor(bool areaToggle) => localCleanliness && areaToggle;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref removeCookSkill, "removeCookSkill", defaultValue: false);
        Scribe_Values.Look(ref localCleanliness, "localCleanliness", defaultValue: true);
        Scribe_Values.Look(ref localForCooking, "localForCooking", defaultValue: true);
        Scribe_Values.Look(ref localForSurgery, "localForSurgery", defaultValue: true);
        Scribe_Values.Look(ref localForTending, "localForTending", defaultValue: true);
        Scribe_Values.Look(ref radius, "radius", 4f);
        Scribe_Values.Look(ref falloffPower, "falloffPower", 1f);
    }
}

public class LocalizedCleanlinessMod : Mod
{
    public const string LogPrefix = "[LocalizedCleanliness]";

    public static LocalizedCleanlinessSettings Settings { get; private set; }

    public LocalizedCleanlinessMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<LocalizedCleanlinessSettings>();
        new Harmony("connor.localizedcleanliness").PatchAll(Assembly.GetExecutingAssembly());
        Log.Message($"{LogPrefix} assembly loaded and Harmony patches applied.");
    }

    public override string SettingsCategory() => "Connor's Localized Cleanliness!";

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var s = Settings;
        var list = new Listing_Standard();
        list.Begin(inRect);

        list.CheckboxLabeled("LC_Setting_RemoveCookSkill".Translate(), ref s.removeCookSkill,
            "LC_Setting_RemoveCookSkillDesc".Translate());

        list.GapLine();
        list.CheckboxLabeled("LC_Setting_LocalCleanliness".Translate(), ref s.localCleanliness,
            "LC_Setting_LocalCleanlinessDesc".Translate());

        if (s.localCleanliness)
        {
            var fullWidth = list.ColumnWidth;
            list.Indent();
            list.ColumnWidth = fullWidth - 12f;

            list.CheckboxLabeled("LC_Setting_ForCooking".Translate(), ref s.localForCooking);
            list.CheckboxLabeled("LC_Setting_ForSurgery".Translate(), ref s.localForSurgery);
            list.CheckboxLabeled("LC_Setting_ForTending".Translate(), ref s.localForTending);

            list.ColumnWidth = fullWidth;
            list.Outdent();

            list.Gap();
            list.Label("LC_Setting_Radius".Translate(s.radius.ToString("0.0")),
                tooltip: "LC_Setting_RadiusDesc".Translate());
            s.radius = Mathf.Round(list.Slider(s.radius, 1.5f, 12f) * 2f) / 2f;

            list.Gap();
            list.Label("LC_Setting_Falloff".Translate(s.falloffPower.ToString("0.0")),
                tooltip: "LC_Setting_FalloffDesc".Translate());
            s.falloffPower = list.Slider(s.falloffPower, 0f, 4f);
        }

        list.Gap();
        if (list.ButtonText("LC_Setting_Reset".Translate()))
        {
            s.removeCookSkill = false;
            s.localCleanliness = true;
            s.localForCooking = true;
            s.localForSurgery = true;
            s.localForTending = true;
            s.radius = 4f;
            s.falloffPower = 1f;
        }

        list.End();
    }
}
