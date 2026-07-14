using System.Linq;
using Verse;

namespace Sidearms;

/// <summary>
/// Puts CompSidearms on every pawn def. ThingWithComps.ExposeData calls InitializeComps on
/// LoadingVars, so pawns in an existing save pick the comp up on load without a migration step.
/// </summary>
[StaticConstructorOnStartup]
public static class CompInjector
{
    static CompInjector()
    {
        var props = new CompProperties_Sidearms();
        var injected = 0;

        foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(IsPawnDef))
        {
            def.comps ??= new System.Collections.Generic.List<CompProperties>();
            if (def.comps.Any(c => c is CompProperties_Sidearms)) continue;
            def.comps.Add(props);
            injected++;
        }

        Log.Message($"{SidearmsMod.LogPrefix} injected CompSidearms into {injected} pawn defs.");
    }

    // Humanlikes only. Animals and mechs fight with built-in verbs and would only pay the tick cost.
    private static bool IsPawnDef(ThingDef def) =>
        def.thingClass != null
        && typeof(Pawn).IsAssignableFrom(def.thingClass)
        && def.race is { Humanlike: true };
}
