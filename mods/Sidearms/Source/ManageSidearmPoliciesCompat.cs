using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Sidearms;

/// <summary>
/// Manage Sidearm Policies decides which weapons end up in a colonist's inventory; this mod decides
/// which of them the pawn fights with. Neither knows about the other's bookkeeping, so a weapon a
/// policy fetched is just cargo here: no gizmo, and the auto-switch never considers it. Reading the
/// policy's assignments closes that gap in one direction, and handing it the weapons the player
/// takes by hand closes the other, so it does not go fetch a second knife for a slot that is
/// already filled.
///
/// Bound by reflection: the mod is a soft dependency, and there is no shared assembly to compile
/// against.
/// </summary>
[StaticConstructorOnStartup]
public static class ManageSidearmPoliciesCompat
{
    private const string PackageId = "spoiledink.managesidearms";

    private static MethodInfo getPolicy;
    private static MethodInfo getAssignedSidearms;
    private static MethodInfo assignSidearm;
    private static MethodInfo getSubPolicyForWeapon;
    private static MethodInfo isAssigned;

    public static bool Active { get; private set; }

    static ManageSidearmPoliciesCompat()
    {
        var loaded = false;
        foreach (var mod in LoadedModManager.RunningModsListForReading)
        {
            if (!mod.PackageId.Equals(PackageId, StringComparison.OrdinalIgnoreCase)) continue;
            loaded = true;
            break;
        }

        if (!loaded) return;

        var extensions = AccessTools.TypeByName("ManageSidearmPolicies.PawnSidearmPolicyExtensions");
        getPolicy = AccessTools.Method(extensions, "GetSidearmPolicy", new[] { typeof(Pawn) });

        var policyType = getPolicy?.ReturnType;
        if (policyType != null)
        {
            getAssignedSidearms = AccessTools.Method(policyType, "GetAssignedSidearms", new[] { typeof(Pawn) });
            assignSidearm = AccessTools.Method(policyType, "AssignSidearm", new[] { typeof(Pawn), typeof(Thing) });
            getSubPolicyForWeapon = AccessTools.Method(policyType, "GetSubPolicyForWeapon", new[] { typeof(Thing) });
            isAssigned = AccessTools.Method(policyType, "IsAssigned", new[] { typeof(Pawn), typeof(Thing) });
        }

        Active = getAssignedSidearms != null && assignSidearm != null
            && getSubPolicyForWeapon != null && isAssigned != null;

        Log.Message(Active
            ? $"{SidearmsMod.LogPrefix} Manage Sidearm Policies found; its assigned sidearms will be treated as sidearms."
            : $"{SidearmsMod.LogPrefix} Manage Sidearm Policies is loaded but its policy API did not bind; running without compatibility.");
    }

    /// <summary>Registers the weapons the pawn's policy put in their inventory as sidearms.</summary>
    public static void SyncAssignedSidearms(CompSidearms comp)
    {
        if (!Active) return;

        var pawn = comp.Pawn;
        if (pawn?.Faction is not { IsPlayer: true }) return;
        if (!SidearmsUtility.CanCarrySidearms(pawn)) return;

        var policy = PolicyFor(pawn);
        if (policy == null) return;

        if (Invoke(getAssignedSidearms, policy, new object[] { pawn }) is not List<Thing> assigned) return;

        foreach (var thing in assigned)
        {
            if (thing is not ThingWithComps weapon) continue;
            if (!SidearmsUtility.IsEligibleWeapon(weapon)) continue;
            if (!pawn.inventory.innerContainer.Contains(weapon)) continue;

            comp.Register(weapon);
        }
    }

    /// <summary>
    /// Whether the pawn's policy is the reason this weapon is in their inventory. Those weapons are
    /// left out of this mod's sidearm limits: the policy has its own count and weight ceilings, and
    /// two sets of limits fighting over the same inventory would leave the player unable to satisfy
    /// either.
    /// </summary>
    public static bool IsPolicyManaged(Pawn pawn, Thing weapon)
    {
        if (!Active) return false;
        if (pawn?.Faction is not { IsPlayer: true }) return false;

        var policy = PolicyFor(pawn);
        if (policy == null) return false;

        return Invoke(isAssigned, policy, new object[] { pawn, weapon }) is true;
    }

    /// <summary>
    /// Offers a weapon the pawn just picked up to the policy, so a slot it fills is not filled
    /// twice. The policy ignores weapons no slot asked for.
    /// </summary>
    public static void NotifyTakenAsSidearm(Pawn pawn, Thing weapon)
    {
        if (!Active) return;
        if (pawn?.Faction is not { IsPlayer: true }) return;

        var policy = PolicyFor(pawn);
        if (policy == null) return;

        if (Invoke(getSubPolicyForWeapon, policy, new object[] { weapon }) == null) return;

        Invoke(assignSidearm, policy, new object[] { pawn, weapon });
    }

    private static object PolicyFor(Pawn pawn)
    {
        return Invoke(getPolicy, null, new object[] { pawn });
    }

    /// <summary>
    /// A mismatch against a version of the other mod this was not written against would otherwise
    /// throw on a tick, once per pawn, forever. One report and the compatibility goes quiet instead.
    /// </summary>
    private static object Invoke(MethodInfo method, object instance, object[] args)
    {
        try
        {
            return method.Invoke(instance, args);
        }
        catch (Exception e)
        {
            Active = false;
            Log.Error($"{SidearmsMod.LogPrefix} Manage Sidearm Policies compatibility disabled: {method?.Name} failed. {e}");
            return null;
        }
    }
}
