using HarmonyLib;
using RimWorld;
using Verse;

namespace LocalizedCleanliness;

/// <summary>
/// Vanilla <see cref="HediffComp_Infecter"/> captures the patient's whole-room
/// <c>InfectionChanceFactor</c> at tend time and multiplies the later infection roll by it. This
/// postfix recomputes that factor from cleanliness measured locally around the patient (the bed),
/// reusing the vanilla InfectionChanceFactor curve. Only the captured value is changed; the rest of
/// the infection logic is untouched.
/// </summary>
[HarmonyPatch(typeof(HediffComp_Infecter), nameof(HediffComp_Infecter.CompTended))]
public static class Patch_HediffComp_Infecter_CompTended
{
    public static void Postfix(HediffComp_Infecter __instance, ref float ___infectionChanceFactorFromTendRoom)
    {
        var s = LocalizedCleanlinessMod.Settings;
        if (!s.LocalActiveFor(s.localForTending))
        {
            return;
        }

        var pawn = __instance.Pawn;
        if (pawn == null || !pawn.Spawned || pawn.GetRoom() == null)
        {
            return;
        }

        var cleanliness = LocalCleanliness.At(pawn.Map, pawn.Position, s.radius, s.falloffPower);
        ___infectionChanceFactorFromTendRoom = RoomStatDefOf.InfectionChanceFactor.curve.Evaluate(cleanliness);
    }
}
