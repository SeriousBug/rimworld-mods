using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Sidearms;

/// <summary>
/// Hands a melee weapon to non-player pawns who generate with a gun.
///
/// Verb_LaunchProjectile.Available() returns false for a non-player pawn whose melee threat is
/// standing next to it, so a raider that has been hit in melee cannot fire at all and falls back to
/// bashing with the butt of its gun (9 blunt on a 2s cooldown). A knife roughly halves the time that
/// pawn needs to fight back, and it costs them nothing, because the gun was producing no shots.
/// </summary>
[HarmonyPatch(typeof(PawnWeaponGenerator), nameof(PawnWeaponGenerator.TryGenerateWeaponFor))]
public static class Patch_PawnWeaponGenerator_TryGenerateWeaponFor
{
    public static void Postfix(Pawn pawn, PawnGenerationRequest request)
    {
        var settings = SidearmsMod.Settings;
        if (!settings.giveNpcMeleeSidearms) return;

        // Without the auto-switch these pawns never draw the thing; it would only be loot.
        if (!settings.applyToNonPlayerPawns || !settings.autoSwitchToMelee) return;

        NpcSidearmGenerator.TryGiveMeleeSidearm(pawn);
    }
}

public static class NpcSidearmGenerator
{
    private static List<ThingStuffPair> meleePairs;

    public static void TryGiveMeleeSidearm(Pawn pawn)
    {
        if (!SidearmsUtility.CanCarrySidearms(pawn)) return;
        if (pawn.Faction == null || pawn.Faction.IsPlayer) return;
        if (pawn.WorkTagIsDisabled(WorkTags.Violent)) return;

        var primary = pawn.equipment.Primary;
        if (primary == null || primary.def.IsMeleeWeapon) return;

        // A weapon the pawn can still fire with someone in its face gives it no reason to draw a
        // knife, so it does not need one.
        if (!BlockedInMelee(primary.def)) return;

        var comp = pawn.GetComp<CompSidearms>();
        if (AlreadyCarriesMelee(pawn, comp)) return;

        if (!TryPickMelee(pawn, out var pair)) return;

        var weapon = (ThingWithComps)ThingMaker.MakeThing(pair.thing, pair.stuff);
        PawnGenerator.PostProcessGeneratedGear(weapon, pawn);

        if (!comp.HasRoomFor(weapon) || !EquipmentUtility.CanEquip(weapon, pawn))
        {
            weapon.Destroy();
            return;
        }

        // Not-for-sale, or a trade caravan's guards would offer their own sidearms back to the
        // player across the counter. It drops the item on the floor rather than reporting a failed
        // add, so the container is the only thing worth believing.
        pawn.inventory.TryAddItemNotForSale(weapon);

        if (!pawn.inventory.innerContainer.Contains(weapon))
        {
            if (!weapon.Destroyed) weapon.Destroy();
            return;
        }

        SidearmsUtility.TryAddSidearm(pawn, weapon);
    }

    /// <summary>
    /// Mirrors the guard in Verb_LaunchProjectile.Available(): a verb flagged
    /// ai_ProjectileLaunchingIgnoresMeleeThreats keeps firing with an enemy adjacent.
    /// </summary>
    public static bool BlockedInMelee(ThingDef weapon)
    {
        var verbs = weapon.Verbs;
        if (verbs.NullOrEmpty()) return false;

        foreach (var verb in verbs)
        {
            if (verb.IsMeleeAttack) continue;
            if (!verb.ai_ProjectileLaunchingIgnoresMeleeThreats) return true;
        }

        return false;
    }

    private static bool AlreadyCarriesMelee(Pawn pawn, CompSidearms comp)
    {
        foreach (var thing in pawn.inventory.innerContainer)
        {
            if (thing.def.IsMeleeWeapon) return true;
        }

        return comp.Sidearms.Count >= SidearmsMod.Settings.maxSidearms;
    }

    private static bool TryPickMelee(Pawn pawn, out ThingStuffPair result)
    {
        var candidates = new List<ThingStuffPair>();
        var maxPrice = SidearmsMod.Settings.npcSidearmMaxPrice;
        var techLevel = pawn.Faction.def.techLevel;

        foreach (var pair in MeleePairs())
        {
            if (pair.Price > maxPrice) continue;
            if (pair.thing.techLevel > techLevel) continue;
            if (!AllowedFor(pawn, pair.thing)) continue;

            candidates.Add(pair);
        }

        return candidates.TryRandomElementByWeight(pair => pair.Commonality, out result);
    }

    private static bool AllowedFor(Pawn pawn, ThingDef weapon)
    {
        if (pawn.Ideo != null && pawn.Ideo.GetDispositionForWeapon(weapon) == IdeoWeaponDisposition.Despised)
        {
            return false;
        }

        var forbidden = pawn.genes?.Xenotype?.forbiddenWeaponClasses;
        if (forbidden == null || weapon.weaponClasses.NullOrEmpty()) return true;

        foreach (var weaponClass in forbidden)
        {
            if (weapon.weaponClasses.Contains(weaponClass)) return false;
        }

        return true;
    }

    private static List<ThingStuffPair> MeleePairs()
    {
        if (meleePairs != null) return meleePairs;

        meleePairs = new List<ThingStuffPair>();
        foreach (var pair in PawnWeaponGenerator.AllWeaponPairs)
        {
            if (!pair.thing.IsMeleeWeapon) continue;
            if (PawnWeaponGenerator.IsDerpWeapon(pair.thing, pair.stuff)) continue;
            if (pair.stuff != null && !pair.stuff.stuffProps.allowedInStuffGeneration) continue;

            meleePairs.Add(pair);
        }

        return meleePairs;
    }
}
