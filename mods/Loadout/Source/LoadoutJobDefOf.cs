using RimWorld;
using Verse;

namespace Loadout;

[DefOf]
public static class LoadoutJobDefOf
{
    /// <summary>Swap into a garment, remembering whatever it displaces (gearing up).</summary>
    public static JobDef Loadout_EquipStash;

    /// <summary>Swap into a garment, carrying whatever it displaces to be put away (standing down).</summary>
    public static JobDef Loadout_EquipDeposit;

    /// <summary>Take a garment off and remember it (gearing up).</summary>
    public static JobDef Loadout_StashApparel;

    /// <summary>Take a garment off to be put away (standing down).</summary>
    public static JobDef Loadout_DoffApparel;

    /// <summary>Carry one garment from the inventory into storage.</summary>
    public static JobDef Loadout_DepositApparel;

    static LoadoutJobDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(LoadoutJobDefOf));
    }
}
