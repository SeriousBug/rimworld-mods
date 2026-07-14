using RimWorld;
using Verse;

namespace Loadout;

[DefOf]
public static class LoadoutJobDefOf
{
    /// <summary>Swap into a garment, keeping whatever it displaces (gearing up).</summary>
    public static JobDef Loadout_EquipStash;

    /// <summary>Swap into a garment, leaving whatever it displaces for a hauler (standing down).</summary>
    public static JobDef Loadout_EquipDrop;

    /// <summary>Take a garment off and keep it (inventory, or the floor if it will not fit).</summary>
    public static JobDef Loadout_StashApparel;

    /// <summary>Take a garment off and leave it on the floor for a hauler.</summary>
    public static JobDef Loadout_DoffApparel;

    static LoadoutJobDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(LoadoutJobDefOf));
    }
}
