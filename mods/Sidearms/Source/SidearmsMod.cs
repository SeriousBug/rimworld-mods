using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Sidearms;

public class SidearmsMod : Mod
{
    public const string LogPrefix = "[Sidearms]";

    public static SidearmsSettings Settings { get; private set; }

    public SidearmsMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<SidearmsSettings>();
        new Harmony("kaan.sidearms").PatchAll(Assembly.GetExecutingAssembly());
        Log.Message($"{LogPrefix} loaded.");
    }

    public override string SettingsCategory() => "Sidearms";

    public override void DoSettingsWindowContents(Rect inRect) => Settings.DoWindowContents(inRect);
}
