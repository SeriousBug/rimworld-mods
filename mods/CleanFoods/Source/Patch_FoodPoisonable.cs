using HarmonyLib;
using RimWorld;
using Verse;

namespace CleanFoods;

/// <summary>
/// Vanilla <see cref="CompFoodPoisonable.Notify_RecipeProduced"/> rolls two poison tracks when a
/// meal is cooked: the kitchen's room cleanliness (FilthyKitchen) and the cook's Cooking skill
/// (IncompetentCook). This replaces the method to keep only the kitchen track, on a tunable
/// cleanliness curve, and never consult the cook's skill. Foodborne illness comes from hygiene, not
/// incompetence.
///
/// Raw meat, rotten food, and insect meat are poisoned through their own food properties elsewhere,
/// not through this method, so they are unaffected.
/// </summary>
[HarmonyPatch(typeof(CompFoodPoisonable), nameof(CompFoodPoisonable.Notify_RecipeProduced))]
public static class Patch_CompFoodPoisonable_Notify_RecipeProduced
{
    public static bool Prefix(CompFoodPoisonable __instance, Pawn pawn)
    {
        var cleanliness = pawn.GetRoom()?.GetStat(RoomStatDefOf.Cleanliness)
                          ?? RoomStatDefOf.Cleanliness.roomlessScore;
        var chance = CleanFoodsMod.PoisonChanceFor(cleanliness);
        if (chance > 0f && Rand.Chance(chance))
        {
            __instance.SetPoisoned(FoodPoisonCause.FilthyKitchen);
        }
        return false;
    }
}
