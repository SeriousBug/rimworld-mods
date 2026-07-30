using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Sidearms;

/// <summary>
/// Per-pawn sidearm bookkeeping. The weapons themselves live in the pawn's normal inventory;
/// this only records which of the inventory's weapons the pawn treats as a sidearm, so that
/// hauled or traded weapons are not mistaken for ones the pawn means to fight with.
/// </summary>
public class CompSidearms : ThingComp
{
    private List<ThingWithComps> sidearms = new();

    // The weapon the pawn goes back to once the reason for the auto-swap is gone. Null means the
    // pawn is holding what it wants to hold.
    private ThingWithComps preferredPrimary;

    private int lastSwapTick = -99999;
    private int sinceLastCheck;
    private int sincePolicySync;

    private const int CheckIntervalTicks = 30;

    // Another mod fetching a sidearm takes far longer than this, so nothing is waiting on it.
    private const int PolicySyncIntervalTicks = 600;

    public Pawn Pawn => (Pawn)parent;

    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);

        // Spread the pawns across the interval. Without this, everyone who spawned on the same tick
        // (a raid, a caravan arrival) evaluates on the same tick forever, and the cost lands as a
        // spike instead of a flat background load.
        sinceLastCheck = Mathf.Abs(parent.thingIDNumber) % CheckIntervalTicks;
        sincePolicySync = Mathf.Abs(parent.thingIDNumber) % PolicySyncIntervalTicks;
    }

    public List<ThingWithComps> Sidearms
    {
        get
        {
            DropStaleEntries();
            return sidearms;
        }
    }

    public ThingWithComps PreferredPrimary => preferredPrimary;

    public bool CanSwapNow => Find.TickManager.TicksGame - lastSwapTick >= SidearmsMod.Settings.swapCooldownTicks;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Collections.Look(ref sidearms, "sidearms", LookMode.Reference);
        Scribe_References.Look(ref preferredPrimary, "preferredPrimary");
        Scribe_Values.Look(ref lastSwapTick, "lastSwapTick", -99999);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            sidearms ??= new List<ThingWithComps>();
            sidearms.RemoveAll(w => w == null);
        }
    }

    public override void CompTick() => Evaluate(1);

    public override void CompTickInterval(int delta) => Evaluate(delta);

    private void Evaluate(int delta)
    {
        sincePolicySync += delta;
        if (sincePolicySync >= PolicySyncIntervalTicks)
        {
            sincePolicySync = 0;
            ManageSidearmPoliciesCompat.SyncAssignedSidearms(this);
        }

        // Combat state changes on the order of seconds, not ticks, so re-evaluating every tick
        // would be wasted work on every pawn on the map.
        sinceLastCheck += delta;
        if (sinceLastCheck < CheckIntervalTicks) return;
        sinceLastCheck = 0;

        AutoSwitch.Evaluate(this);
    }

    public bool IsSidearm(Thing weapon) => sidearms.Contains(weapon);

    public void Register(ThingWithComps weapon)
    {
        if (!sidearms.Contains(weapon)) sidearms.Add(weapon);
    }

    public void Unregister(ThingWithComps weapon)
    {
        sidearms.Remove(weapon);
        if (preferredPrimary == weapon) preferredPrimary = null;
    }

    public void NotifySwapped(ThingWithComps newPrimary, ThingWithComps intendedPrimary)
    {
        lastSwapTick = Find.TickManager.TicksGame;
        preferredPrimary = intendedPrimary == newPrimary ? null : intendedPrimary;
    }

    /// <summary>The pawn is holding what it wants to hold; forget any pending restore.</summary>
    public void ClearPreferredPrimary() => preferredPrimary = null;

    /// <summary>
    /// The player equipped or dropped something by hand, so whatever restore we had in mind is
    /// no longer what the pawn wants.
    /// </summary>
    public void NotifyPrimaryChangedExternally() => preferredPrimary = null;

    private void DropStaleEntries()
    {
        // A sidearm stops being one the moment it leaves the inventory: dropped, destroyed,
        // hauled off by another pawn, or promoted to primary.
        sidearms.RemoveAll(w =>
            w == null || w.Destroyed || !Pawn.inventory.innerContainer.Contains(w));

        if (preferredPrimary != null && preferredPrimary.Destroyed)
        {
            preferredPrimary = null;
        }
    }

    // Sidearms another mod's policy is responsible for are not counted. That mod has its own count
    // and weight ceilings, and a weapon the player cannot remove here counting against a limit here
    // would leave a pawn stuck below both.
    private IEnumerable<ThingWithComps> OwnSidearms =>
        Sidearms.Where(w => !ManageSidearmPoliciesCompat.IsPolicyManaged(Pawn, w));

    public int UsedSidearmSlots => OwnSidearms.Count();

    public bool HasRoomFor(Thing weapon)
    {
        if (UsedSidearmSlots >= SidearmsMod.Settings.maxSidearms) return false;
        return WithinMassBudget(weapon);
    }

    private bool WithinMassBudget(Thing weapon)
    {
        var settings = SidearmsMod.Settings;
        var capacity = MassUtility.Capacity(Pawn);
        if (capacity <= 0f) return false;

        var budget = capacity * settings.maxSidearmMassFraction;
        var used = OwnSidearms.Sum(w => w.GetStatValue(StatDefOf.Mass) * w.stackCount);
        return used + weapon.GetStatValue(StatDefOf.Mass) <= budget;
    }

    public override string CompInspectStringExtra()
    {
        var carried = Sidearms;
        if (carried.Count == 0) return null;
        return "Sidearms_Inspect".Translate(carried.Select(w => w.LabelShortCap).ToCommaList());
    }
}

public class CompProperties_Sidearms : CompProperties
{
    public CompProperties_Sidearms()
    {
        compClass = typeof(CompSidearms);
    }
}
