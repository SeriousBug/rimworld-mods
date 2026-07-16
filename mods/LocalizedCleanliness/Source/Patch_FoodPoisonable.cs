using HarmonyLib;
using RimWorld;
using Verse;

namespace LocalizedCleanliness;

/// <summary>
/// Vanilla <see cref="CompFoodPoisonable.Notify_RecipeProduced"/> rolls two poison tracks when a meal
/// is cooked: the kitchen's room cleanliness (FilthyKitchen) and the cook's Cooking skill
/// (IncompetentCook). This reimplements the method so the cook-skill roll can be dropped and the
/// kitchen roll can use cleanliness measured locally around the cook instead of the flat room
/// average. Both changes are settings-gated; with both off, the original method runs unchanged.
///
/// The vanilla FoodPoisonChance room-stat curve is reused as-is, just evaluated against the local
/// cleanliness value. Raw meat, rotten food, and insect meat are poisoned through their own food
/// properties elsewhere, not through this method, so they are unaffected.
/// </summary>
[HarmonyPatch(typeof(CompFoodPoisonable), nameof(CompFoodPoisonable.Notify_RecipeProduced))]
public static class Patch_CompFoodPoisonable_Notify_RecipeProduced
{
    public static bool Prefix(CompFoodPoisonable __instance, Pawn pawn)
    {
        var s = LocalizedCleanlinessMod.Settings;
        var useLocal = s.LocalActiveFor(s.localForCooking) && pawn.Spawned;
        if (!s.removeCookSkill && !useLocal)
        {
            return true;
        }

        float kitchenChance;
        if (useLocal)
        {
            var cleanliness = LocalCleanliness.At(pawn.Map, pawn.Position, s.radius, s.falloffPower);
            kitchenChance = RoomStatDefOf.FoodPoisonChance.curve.Evaluate(cleanliness);
        }
        else
        {
            kitchenChance = pawn.GetRoom()?.GetStat(RoomStatDefOf.FoodPoisonChance)
                            ?? RoomStatDefOf.FoodPoisonChance.roomlessScore;
        }

        if (Rand.Chance(kitchenChance))
        {
            __instance.SetPoisoned(FoodPoisonCause.FilthyKitchen);
        }
        else if (!s.removeCookSkill && Rand.Chance(pawn.GetStatValue(StatDefOf.FoodPoisonChance)))
        {
            __instance.SetPoisoned(FoodPoisonCause.IncompetentCook);
        }
        return false;
    }
}
