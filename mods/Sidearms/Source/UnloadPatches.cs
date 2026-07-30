using HarmonyLib;
using Verse;

namespace Sidearms;

/// <summary>
/// Keeps sidearms out of the inventory sweep. Every path that empties a pawn's inventory — a
/// caravan arriving home, a cancelled caravan, a shuttle unloading, a pawn dropped from a caravan —
/// works by setting UnloadEverything, and all of them then ask FirstUnloadableThing what to take
/// next, so hiding the sidearms from that one property covers all of them at once. It also settles
/// HasAnyUnloadableThing, which reads the same property: a pawn carrying nothing but sidearms is
/// never marked as having anything to unload in the first place.
///
/// The property is answered against a container holding everything but the sidearms, rather than by
/// vetting the answer or reimplementing the property. What counts as an item the pawn means to keep
/// is a real set of rules — drug policy, inventory stock, how much packed food the pawn's food need
/// justifies — and all of them stay vanilla's to decide, on a smaller inventory.
/// </summary>
[HarmonyPatch(typeof(Pawn_InventoryTracker), nameof(Pawn_InventoryTracker.FirstUnloadableThing), MethodType.Getter)]
public static class Patch_Pawn_InventoryTracker_FirstUnloadableThing
{
    // Reused across calls: every job giver looking for something to unload reads this property, and
    // the contents are dead the moment the call returns.
    private static readonly ThingOwner<Thing> WithoutSidearms = new();

    private static bool inUse;

    public static void Prefix(Pawn_InventoryTracker __instance, ref ThingOwner<Thing> __state)
    {
        // A patch of someone else's asking for this property from inside the property. Rare enough
        // that answering it the vanilla way is better than keeping a pool of containers around.
        if (inUse) return;

        var comp = __instance.pawn?.GetComp<CompSidearms>();
        if (comp == null || comp.Sidearms.Count == 0) return;

        // Written through the list rather than TryAdd: adding through the ThingOwner would take
        // ownership of the weapons away from the pawn. This container is a view, and the pawn's own
        // container stays untouched, so anything already walking it is undisturbed.
        var items = WithoutSidearms.InnerListForReading;
        items.Clear();
        foreach (var item in __instance.innerContainer.InnerListForReading)
        {
            if (!comp.IsSidearm(item)) items.Add(item);
        }

        __state = __instance.innerContainer;
        __instance.innerContainer = WithoutSidearms;
        inUse = true;
    }

    /// <summary>A finalizer rather than a postfix, so the pawn gets its inventory back even if the
    /// property throws.</summary>
    public static void Finalizer(Pawn_InventoryTracker __instance, ThingOwner<Thing> __state)
    {
        if (__state == null) return;

        __instance.innerContainer = __state;
        WithoutSidearms.InnerListForReading.Clear();
        inUse = false;
    }
}
