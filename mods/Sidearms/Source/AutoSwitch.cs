using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Sidearms;

/// <summary>
/// Decides, on a timer, whether the weapon a pawn is holding still suits the fight they are in.
///
/// This watches where the enemies actually are rather than reacting to individual attacks. Reacting
/// to a hit means a pawn only draws a knife after already being clubbed once, and misses the case
/// where an enemy is closing but has not landed anything yet.
/// </summary>
public static class AutoSwitch
{
    // An enemy this close is either already swinging or will be before a gun could fire again.
    private const float MeleeThreatRadius = 2.9f;

    // Leaving melee clears a wider bar than entering it, so a pawn does not flip weapons while an
    // enemy dances along the edge of the threshold.
    private const float MeleeClearRadius = 5f;

    // An NPC re-draws its gun the moment the engine will let it fire again, so the only hysteresis
    // it needs is enough to survive an attacker stepping out and straight back in.
    private const float NpcMeleeClearRadius = 2.9f;

    public static void Evaluate(CompSidearms comp)
    {
        var pawn = comp.Pawn;
        if (!SidearmsUtility.CanCarrySidearms(pawn)) return;
        if (!pawn.Spawned || pawn.Downed || pawn.Map == null) return;

        var settings = SidearmsMod.Settings;
        if (!settings.autoSwitchToMelee && !settings.autoSwitchToLongerRange) return;
        if (!settings.applyToNonPlayerPawns && !pawn.Faction.IsPlayerSafe()) return;

        // Everything past this point costs real work, and the overwhelming majority of pawns for
        // the overwhelming majority of ticks are not in a fight at all. A pawn with a weapon to put
        // back still has to be looked at, even at peace, or it never gets put back.
        if (!MightBeFighting(pawn) && comp.PreferredPrimary == null) return;

        if (settings.autoSwitchToMelee && TryHandleMelee(comp, pawn)) return;

        if (settings.autoSwitchToLongerRange) TryHandleRange(comp, pawn);
    }

    private static bool MightBeFighting(Pawn pawn)
    {
        if (pawn.Drafted) return true;
        if (pawn.mindState?.enemyTarget != null) return true;
        if (pawn.mindState?.meleeThreat != null) return true;

        var job = pawn.CurJobDef;
        return job == JobDefOf.AttackMelee
               || job == JobDefOf.AttackStatic
               || job == JobDefOf.Wait_Combat;
    }

    private static bool TryHandleMelee(CompSidearms comp, Pawn pawn)
    {
        var primary = pawn.equipment.Primary;
        var holdingMelee = primary != null && primary.def.IsMeleeWeapon;
        var npc = !pawn.Faction.IsPlayerSafe();

        if (npc ? GunBlockedByMelee(comp, pawn) : HostileWithin(pawn, MeleeThreatRadius))
        {
            if (holdingMelee) return false;
            if (!comp.CanSwapNow) return false;

            var melee = BestMelee(comp, pawn);
            if (melee == null) return false;

            // Remember the gun, so it goes back in hand once the enemy is off them.
            return Swap(comp, pawn, melee, intendedPrimary: primary);
        }

        if (!SidearmsMod.Settings.autoSwitchBackToRanged) return false;
        if (!holdingMelee) return false;
        if (HostileWithin(pawn, npc ? NpcMeleeClearRadius : MeleeClearRadius)) return false;

        return TryRestorePreferred(comp, pawn);
    }

    /// <summary>
    /// Whether the engine is currently refusing to let this pawn shoot: Verb_LaunchProjectile
    /// .Available() returns false for a non-player pawn whose melee threat is standing next to it.
    /// While that holds, the gun in its hands is dead weight and a melee weapon costs it nothing.
    ///
    /// The condition also decides when the gun goes back. A pawn left holding a melee weapon after
    /// the block lifts is routed down JobGiver_AIFightEnemy's melee branch and chases its target
    /// across the map instead of shooting it.
    /// </summary>
    private static bool GunBlockedByMelee(CompSidearms comp, Pawn pawn)
    {
        var mindState = pawn.mindState;
        var threat = mindState?.meleeThreat;
        if (threat == null) return false;

        // Once the pawn has swapped, the gun the block applies to is the one it means to go back to.
        var gun = comp.PreferredPrimary ?? pawn.equipment.Primary;
        if (gun != null && !gun.def.IsMeleeWeapon && !NpcSidearmGenerator.BlockedInMelee(gun.def))
        {
            return false;
        }

        if (!mindState.MeleeThreatStillThreat) return false;

        return threat.Position.AdjacentTo8WayOrInside(pawn.Position);
    }

    private static bool TryHandleRange(CompSidearms comp, Pawn pawn)
    {
        // Melee has already had its say. If anyone is that close, reach is not the problem.
        if (HostileWithin(pawn, MeleeClearRadius)) return false;
        if (!comp.CanSwapNow) return false;

        var target = pawn.mindState?.enemyTarget;
        if (target == null || !target.Spawned || target.Destroyed) return false;

        var distance = pawn.Position.DistanceTo(target.Position);
        var primary = pawn.equipment.Primary;
        if (RangeOf(primary) >= distance) return false;

        // The shortest weapon that still reaches: a longer gun is usually a worse gun up close,
        // and this pawn may have to close the distance again later.
        var better = comp.Sidearms
            .Where(w => RangeOf(w) >= distance && EquipmentUtility.CanEquip(w, pawn))
            .OrderBy(RangeOf)
            .FirstOrDefault();

        if (better == null) return false;

        return Swap(comp, pawn, better, intendedPrimary: comp.PreferredPrimary ?? primary);
    }

    private static bool TryRestorePreferred(CompSidearms comp, Pawn pawn)
    {
        var preferred = comp.PreferredPrimary;
        if (preferred == null) return false;

        if (pawn.equipment.Primary == preferred)
        {
            comp.ClearPreferredPrimary();
            return false;
        }

        if (!pawn.inventory.innerContainer.Contains(preferred))
        {
            comp.ClearPreferredPrimary();
            return false;
        }

        if (!comp.CanSwapNow) return false;

        return Swap(comp, pawn, preferred, intendedPrimary: preferred);
    }

    /// <summary>
    /// Constant-time: reads the cells around the pawn out of the thing grid. Walking the map's
    /// hostile-target list instead would cost O(enemies) for every pawn, which is at its worst
    /// during a big raid, the exact moment the frame budget is already gone.
    /// </summary>
    private static bool HostileWithin(Pawn pawn, float radius)
    {
        foreach (var thing in GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, radius, useCenter: false))
        {
            if (thing is not Pawn other) continue;
            if (other == pawn || other.Dead || other.Downed) continue;
            if (!other.HostileTo(pawn)) continue;

            return true;
        }

        return false;
    }

    private static bool Swap(CompSidearms comp, Pawn pawn, ThingWithComps weapon, ThingWithComps intendedPrimary)
    {
        if (!SidearmsUtility.TryEquipFromInventory(pawn, weapon)) return false;

        comp.NotifySwapped(weapon, intendedPrimary);
        return true;
    }

    private static ThingWithComps BestMelee(CompSidearms comp, Pawn pawn) =>
        comp.Sidearms
            .Where(w => w.def.IsMeleeWeapon && EquipmentUtility.CanEquip(w, pawn))
            .OrderByDescending(w => w.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS))
            .FirstOrDefault();

    /// <summary>How far away an enemy can be and still be hit with this weapon.</summary>
    public static float RangeOf(ThingWithComps weapon)
    {
        var verbs = weapon?.def.Verbs;
        if (verbs.NullOrEmpty()) return 0f;

        var best = 0f;
        foreach (var verb in verbs)
        {
            if (verb.IsMeleeAttack) continue;
            if (verb.range > best) best = verb.range;
        }

        return best;
    }
}
