using HarmonyLib;
using RimWorld;
using Verse;

namespace Loadout;

/// <summary>
/// Stand down when the player undrafts a geared-up pawn. Drafting deliberately does not gear up:
/// pawns get drafted to reposition or to haul a corpse, and a full change of clothes every time
/// would be worse than useless.
///
/// The setter ends the pawn's current job itself (Pawn_DraftController.cs:58-71), so the postfix is
/// free to start the swap-back sequence.
/// </summary>
[HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted), MethodType.Setter)]
public static class Patch_DraftController_Drafted
{
    public static void Prefix(Pawn_DraftController __instance, out bool __state)
    {
        __state = __instance.Drafted;
    }

    public static void Postfix(Pawn_DraftController __instance, bool __state)
    {
        if (!LoadoutMod.Settings.standDownOnUndraft)
        {
            return;
        }

        var wasUndrafted = __state && !__instance.Drafted;
        if (!wasUndrafted)
        {
            return;
        }

        var pawn = __instance.pawn;
        var comp = pawn?.GetComp<CompLoadout>();
        if (comp is { GearedUp: true } && LoadoutSwapper.CanUseLoadout(pawn))
        {
            LoadoutSwapper.StandDown(comp);
        }
    }
}
