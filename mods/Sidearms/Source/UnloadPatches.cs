using HarmonyLib;
using Verse;

namespace Sidearms;

/// <summary>
/// Keeps sidearms out of the inventory sweep. Every path that empties a pawn's inventory — a
/// caravan arriving home, a cancelled caravan, a shuttle unloading, a pawn dropped from a caravan —
/// works by setting UnloadEverything, and all of them then ask FirstUnloadableThing what to take
/// next, so excluding sidearms here covers all of them at once.
/// </summary>
[HarmonyPatch(typeof(Pawn_InventoryTracker), nameof(Pawn_InventoryTracker.FirstUnloadableThing), MethodType.Getter)]
public static class Patch_Pawn_InventoryTracker_FirstUnloadableThing
{
    /// <summary>
    /// Vanilla returns the first thing in container order that the pawn is not meant to keep, so
    /// what the postfix is allowed to discard depends on where the sidearms sit. With them at the
    /// back, a sidearm can only come back as the answer once everything ahead of it is spoken for,
    /// which is what makes discarding it safe: there is no cargo left hiding behind it.
    /// </summary>
    public static void Prefix(Pawn_InventoryTracker __instance)
    {
        var comp = __instance.pawn?.GetComp<CompSidearms>();
        if (comp == null || comp.Sidearms.Count == 0) return;

        // Assignment through the indexer only. Remove/Insert would bump the list's version and
        // break any enumeration of the inventory further up the stack.
        var items = __instance.innerContainer.InnerListForReading;
        var write = 0;
        for (var i = 0; i < items.Count; i++)
        {
            if (comp.IsSidearm(items[i])) continue;

            if (i != write)
            {
                var item = items[i];
                for (var j = i; j > write; j--) items[j] = items[j - 1];
                items[write] = item;
            }

            write++;
        }
    }

    public static void Postfix(Pawn_InventoryTracker __instance, ref ThingCount __result)
    {
        if (__result.Thing == null) return;

        var comp = __instance.pawn?.GetComp<CompSidearms>();
        if (comp != null && comp.IsSidearm(__result.Thing)) __result = default;
    }
}
