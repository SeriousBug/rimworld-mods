using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Loadout;

public class LoadoutSettings : ModSettings
{
    public bool stashToInventory = true;
    public bool standDownOnUndraft = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref stashToInventory, "stashToInventory", defaultValue: true);
        Scribe_Values.Look(ref standDownOnUndraft, "standDownOnUndraft", defaultValue: true);
    }
}

public class LoadoutMod : Mod
{
    public const string LogPrefix = "[Loadout]";

    public static LoadoutSettings Settings { get; private set; }

    public LoadoutMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<LoadoutSettings>();
        new Harmony("kaan.loadout").PatchAll(Assembly.GetExecutingAssembly());
        Log.Message($"{LogPrefix} assembly loaded and Harmony patches applied.");
    }

    public override string SettingsCategory() => "Loadout";

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var list = new Listing_Standard();
        list.Begin(inRect);
        list.CheckboxLabeled(
            "Loadout_Setting_StashToInventory".Translate(),
            ref Settings.stashToInventory,
            "Loadout_Setting_StashToInventoryDesc".Translate());
        list.Gap();
        list.CheckboxLabeled(
            "Loadout_Setting_StandDownOnUndraft".Translate(),
            ref Settings.standDownOnUndraft,
            "Loadout_Setting_StandDownOnUndraftDesc".Translate());
        list.End();
    }
}
