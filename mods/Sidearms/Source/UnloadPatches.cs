using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace Sidearms;

/// <summary>
/// Keeps sidearms out of the inventory sweep. Every path that empties a pawn's inventory — a
/// caravan arriving home, a cancelled caravan, a shuttle unloading, a pawn dropped from a caravan —
/// works by setting UnloadEverything, and all of them then ask FirstUnloadableThing what to take
/// next, so one skip inside that loop covers all of them at once. It also settles
/// HasAnyUnloadableThing, which reads the same property: a pawn carrying nothing but sidearms is
/// never marked as having anything to unload in the first place.
/// </summary>
[HarmonyPatch(typeof(Pawn_InventoryTracker), nameof(Pawn_InventoryTracker.FirstUnloadableThing), MethodType.Getter)]
public static class Patch_Pawn_InventoryTracker_FirstUnloadableThing
{
    public static bool IsSidearm(Thing item, Pawn_InventoryTracker inventory)
    {
        var comp = inventory?.pawn?.GetComp<CompSidearms>();
        return comp != null && comp.IsSidearm(item);
    }

    /// <summary>
    /// Vanilla walks the inventory and returns the first thing the pawn is not meant to keep. This
    /// adds one more reason to keep something, at the top of that loop:
    ///
    /// <code>
    ///     foreach (Thing item in innerContainer)
    ///     {
    ///         if (IsSidearm(item, this)) continue;
    ///         ...
    /// </code>
    ///
    /// Skipping inside the loop rather than filtering the answer afterwards is what keeps this
    /// independent of what order the inventory happens to be in, and leaves the property's own rules
    /// about drugs, inventory stock and packed food to vanilla.
    /// </summary>
    public static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var code = new List<CodeInstruction>(instructions);

        // The property enumerates two collections. The one over the inventory is the one whose
        // Current is a Thing.
        var current = code.FindIndex(c =>
            c.operand is MethodInfo { Name: "get_Current" } m && m.ReturnType == typeof(Thing));

        // Where `continue` goes: the block that calls the same enumerator's MoveNext. It starts on
        // the instruction before the call, which loads the enumerator.
        var enumerator = current < 0 ? null : ((MethodInfo)code[current].operand).DeclaringType;
        var moveNext = current < 0
            ? -1
            : code.FindIndex(current, c =>
                c.operand is MethodInfo { Name: "MoveNext" } m && m.DeclaringType == enumerator);

        if (current < 0 || moveNext <= current + 1)
        {
            Log.Error($"{SidearmsMod.LogPrefix} could not find the inventory loop in " +
                      "FirstUnloadableThing, so sidearms will be unloaded with the rest of the " +
                      "inventory. The rest of the mod is unaffected.");
            return code;
        }

        var nextItem = generator.DefineLabel();
        code[moveNext - 1].labels.Add(nextItem);

        // The item is on the stack at this point rather than in a local, so the check reads it with
        // a Dup. That way nothing here depends on which local vanilla stores the item in.
        var keepChecking = generator.DefineLabel();
        code[current + 1].labels.Add(keepChecking);

        code.InsertRange(current + 1, new[]
        {
            new CodeInstruction(OpCodes.Dup),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(
                typeof(Patch_Pawn_InventoryTracker_FirstUnloadableThing), nameof(IsSidearm))),
            new CodeInstruction(OpCodes.Brfalse, keepChecking),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Br, nextItem),
        });

        return code;
    }
}
