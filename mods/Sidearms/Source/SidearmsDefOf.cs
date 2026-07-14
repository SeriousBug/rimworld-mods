using RimWorld;
using Verse;

namespace Sidearms;

[DefOf]
public static class SidearmsDefOf
{
    public static JobDef Sidearms_TakeSidearm;

    static SidearmsDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(SidearmsDefOf));
}
