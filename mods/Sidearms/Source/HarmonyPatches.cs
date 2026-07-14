using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Sidearms;

/// <summary>
/// One gizmo per carried sidearm; clicking it puts that weapon in the pawn's hands and stows the
/// current one.
/// </summary>
[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
public static class Patch_Pawn_GetGizmos
{
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn __instance)
    {
        foreach (var gizmo in values) yield return gizmo;

        if (!SidearmsUtility.CanCarrySidearms(__instance)) yield break;
        if (__instance.Faction is not { IsPlayer: true }) yield break;
        if (!__instance.IsColonistPlayerControlled) yield break;

        var comp = __instance.GetComp<CompSidearms>();

        foreach (var weapon in comp.Sidearms)
        {
            yield return SwapGizmo(__instance, weapon);
        }
    }

    private static Gizmo SwapGizmo(Pawn pawn, ThingWithComps weapon)
    {
        var command = new Command_Action
        {
            defaultLabel = weapon.LabelShortCap,
            defaultDesc = "Sidearms_SwapTo_Desc".Translate(weapon.LabelShort),
            icon = weapon.def.uiIcon,
            iconAngle = weapon.def.uiIconAngle,
            iconOffset = weapon.def.uiIconOffset,
            defaultIconColor = weapon.DrawColor,
            action = () =>
            {
                if (!SidearmsUtility.TryEquipFromInventory(pawn, weapon)) return;

                // A weapon the player chose by hand is the weapon the pawn should go back to, not
                // one for the auto-switch to undo the moment the shooting stops.
                pawn.GetComp<CompSidearms>()?.NotifySwapped(weapon, weapon);
            },
        };

        if (!EquipmentUtility.CanEquip(weapon, pawn, out var cantReason, checkBonded: false))
        {
            command.Disable(cantReason.CapitalizeFirst());
        }

        return command;
    }
}

/// <summary>
/// The mod's own swaps move weapons between ThingOwners directly, so AddEquipment only fires for
/// vanilla equip paths: the player issuing an Equip order, or a pawn being generated with a gun.
/// Either way the pawn is now holding what someone else decided it should hold, and any pending
/// auto-restore is stale.
/// </summary>
[HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.AddEquipment))]
public static class Patch_Pawn_EquipmentTracker_AddEquipment
{
    public static void Postfix(Pawn_EquipmentTracker __instance, ThingWithComps newEq)
    {
        var comp = __instance.pawn?.GetComp<CompSidearms>();
        if (comp == null) return;

        comp.Unregister(newEq);
        comp.NotifyPrimaryChangedExternally();
    }
}
