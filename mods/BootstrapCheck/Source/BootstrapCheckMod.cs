using System.Reflection;
using HarmonyLib;
using Verse;

namespace BootstrapCheck;

public class BootstrapCheckMod : Mod
{
    public const string LogPrefix = "[BootstrapCheck]";

    public BootstrapCheckMod(ModContentPack content) : base(content)
    {
        new Harmony("kaan.bootstrapcheck").PatchAll(Assembly.GetExecutingAssembly());
        Log.Message($"{LogPrefix} assembly loaded and Harmony patches applied.");
    }
}

// Game.FinalizeInit runs once a save or new colony has finished loading, by which point every
// Def, Patch and translation has resolved. Verse/Game.cs:707.
[HarmonyPatch(typeof(Game), nameof(Game.FinalizeInit))]
public static class Patch_Game_FinalizeInit
{
    public static void Postfix()
    {
        const string p = BootstrapCheckMod.LogPrefix;

        var research = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("BootstrapCheck_Test");
        Log.Message(research == null
            ? $"{p} FAIL Defs: BootstrapCheck_Test is not in the DefDatabase."
            : $"{p} PASS Defs: BootstrapCheck_Test loaded. PASS DefInjected if this label is injected -> \"{research.label}\"");

        var bed = DefDatabase<ThingDef>.GetNamedSilentFail("Bed");
        var patched = bed != null && bed.description.Contains(p);
        Log.Message(patched
            ? $"{p} PASS Patches: the Bed description carries the patched text."
            : $"{p} FAIL Patches: Bed description is \"{bed?.description}\"");
    }
}
