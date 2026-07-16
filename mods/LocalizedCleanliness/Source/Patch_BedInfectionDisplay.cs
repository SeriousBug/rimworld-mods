using HarmonyLib;
using RimWorld;
using Verse;

namespace LocalizedCleanliness;

/// <summary>
/// A medical bed's inspect string prints the whole-room <c>InfectionChanceFactor</c> stat directly
/// (<see cref="Building_Bed.GetInspectString"/>), bypassing the <see cref="StatPart_RoomStat"/> seam
/// the surgery patch hooks. This postfix rewrites just that one line to the cleanliness measured
/// locally around the bed, matching what <see cref="Patch_HediffComp_Infecter_CompTended"/> already
/// does to the actual infection roll, and relabels it "from surroundings" instead of "from room".
///
/// The vanilla line is reconstructed exactly (same label, same whole-room value) so only it is
/// replaced; the rest of the inspect string is untouched. Gated by the tending toggle, so when
/// localized tending is off the display stays whole-room, consistent with the mechanic.
/// </summary>
[HarmonyPatch(typeof(Building_Bed), nameof(Building_Bed.GetInspectString))]
public static class Patch_Building_Bed_GetInspectString
{
    public static void Postfix(Building_Bed __instance, ref string __result)
    {
        var s = LocalizedCleanlinessMod.Settings;
        if (!s.LocalActiveFor(s.localForTending) || !__instance.Medical || !__instance.Spawned)
        {
            return;
        }

        var room = __instance.GetRoom();
        if (room == null)
        {
            return;
        }

        var wholeRoomLine = "RoomInfectionChanceFactor".Translate() + ": "
            + room.GetStat(RoomStatDefOf.InfectionChanceFactor).ToStringPercent();
        if (!__result.Contains(wholeRoomLine))
        {
            return;
        }

        var cleanliness = LocalCleanliness.At(__instance.Map, __instance.Position, s.radius, s.falloffPower);
        var localLine = "LC_RoomInfectionChanceFactorLocal".Translate() + ": "
            + RoomStatDefOf.InfectionChanceFactor.curve.Evaluate(cleanliness).ToStringPercent();
        __result = __result.Replace(wholeRoomLine, localLine);
    }
}
