using HarmonyLib;
using RimWorld;
using Verse;

namespace LocalizedCleanliness;

/// <summary>
/// Surgery success chance is scaled by the operating bed's <c>SurgerySuccessChanceFactor</c> stat,
/// which pulls whole-room cleanliness in through a <see cref="StatPart_RoomStat"/> for the
/// SurgerySuccessChanceCleanlinessFactor room stat. This prefix replaces only that one room stat's
/// contribution with cleanliness measured locally around the bed, reusing the same vanilla curve.
///
/// StatPart_RoomStat also serves research speed, reading, and a few other stats, so the patch is
/// scoped by defName and falls through to the original for everything else.
/// </summary>
[HarmonyPatch(typeof(StatPart_RoomStat), nameof(StatPart_RoomStat.TransformValue))]
public static class Patch_StatPart_RoomStat_TransformValue
{
    private const string SurgeryCleanlinessDefName = "SurgerySuccessChanceCleanlinessFactor";

    public static bool Prefix(StatRequest req, ref float val, RoomStatDef ___roomStat)
    {
        var s = LocalizedCleanlinessMod.Settings;
        if (!s.LocalActiveFor(s.localForSurgery) || ___roomStat?.defName != SurgeryCleanlinessDefName)
        {
            return true;
        }
        if (!req.HasThing || req.Thing is not { Spawned: true, Map: not null } thing)
        {
            return true;
        }

        var cleanliness = LocalCleanliness.At(thing.Map, thing.Position, s.radius, s.falloffPower);
        val *= ___roomStat.curve.Evaluate(cleanliness);
        return false;
    }
}
