using RimWorld;
using Verse;

namespace Loadout;

[DefOf]
public static class LoadoutJobDefOf
{
    /// <summary>Take a worn garment off and keep it (inventory, or the floor if it will not fit).</summary>
    public static JobDef Loadout_StashApparel;

    /// <summary>Take a worn garment off and leave it on the floor for a hauler.</summary>
    public static JobDef Loadout_DoffApparel;

    /// <summary>Put back on a garment previously stashed in the pawn's inventory.</summary>
    public static JobDef Loadout_WearFromInventory;

    static LoadoutJobDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(LoadoutJobDefOf));
    }
}
