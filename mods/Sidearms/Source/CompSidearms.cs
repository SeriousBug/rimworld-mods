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

    // A sidearm another mod put in the pawn's hands for a job. It stops being a sidearm the moment
    // it becomes the primary, and nothing puts it back when the job ends and it is stowed again.
    private ThingWithComps borrowedSidearm;
    private int borrowedTick;

    // Long enough for a work job, short enough that a weapon the pawn parted with for good is not
    // reclaimed hours later.
    private const int BorrowExpiryTicks = 2500;

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

    /// <summary>
    /// The weapons the pawn can swap to. Every weapon they carry, unless the player asked to pick
    /// their sidearms out by hand.
    /// </summary>
    public List<ThingWithComps> Sidearms
    {
        get
        {
            DropStaleEntries();
            if (!SidearmsMod.Settings.allCarriedWeaponsAreSidearms) return sidearms;

            var carried = new List<ThingWithComps>();
            foreach (var thing in Pawn.inventory.innerContainer)
            {
                if (SidearmsUtility.IsEligibleWeapon(thing)) carried.Add((ThingWithComps)thing);
            }

            return carried;
        }
    }

    /// <summary>Weapons the player marked, which is not the same set as the swappable ones.</summary>
    public bool HasMarkedSidearms => sidearms.Count > 0;

    public ThingWithComps PreferredPrimary => preferredPrimary;

    public bool CanSwapNow => Find.TickManager.TicksGame - lastSwapTick >= SidearmsMod.Settings.swapCooldownTicks;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Collections.Look(ref sidearms, "sidearms", LookMode.Reference);
        Scribe_References.Look(ref preferredPrimary, "preferredPrimary");
        Scribe_Values.Look(ref lastSwapTick, "lastSwapTick", -99999);

        Scribe_References.Look(ref borrowedSidearm, "borrowedSidearm");
        Scribe_Values.Look(ref borrowedTick, "borrowedTick", 0);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            sidearms ??= new List<ThingWithComps>();
            sidearms.RemoveAll(w => w == null);

            if (sidearms.Count > 0) SidearmsMod.Settings.NotifyLoadedSaveHasMarkedSidearms();
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

        ReclaimBorrowedSidearm();
        AutoSwitch.Evaluate(this);
    }

    /// <summary>
    /// Another mod equipped a sidearm to do a job with it, which unregistered it. Remembered here so
    /// it can be marked again when it comes back, rather than losing its button to a chopped tree.
    /// </summary>
    public void NotifyBorrowedSidearm(ThingWithComps weapon)
    {
        borrowedSidearm = weapon;
        borrowedTick = Find.TickManager.TicksGame;
        SidearmsMod.DevLog($"{Pawn.LabelShort}: {weapon.LabelCap} was a sidearm and is now in hand; watching for it to come back.");
    }

    private void ReclaimBorrowedSidearm()
    {
        if (borrowedSidearm == null) return;

        if (borrowedSidearm.Destroyed || Find.TickManager.TicksGame - borrowedTick > BorrowExpiryTicks)
        {
            borrowedSidearm = null;
            return;
        }

        if (!Pawn.inventory.innerContainer.Contains(borrowedSidearm)) return;

        var weapon = borrowedSidearm;
        borrowedSidearm = null;

        // No room check: it was a sidearm before it was borrowed, and it is in the inventory either
        // way. Refusing it here would leave a carried weapon with no button, which is the bug.
        if (Register(weapon))
        {
            SidearmsMod.DevLog($"{Pawn.LabelShort}: {weapon.LabelCap} came back to the inventory, marked as a sidearm again.");
        }
    }

    public bool IsSidearm(Thing weapon) => sidearms.Contains(weapon);

    /// <returns>Whether the weapon was not already tracked.</returns>
    public bool Register(ThingWithComps weapon)
    {
        if (sidearms.Contains(weapon)) return false;
        sidearms.Add(weapon);
        return true;
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

    /// <summary>
    /// A sidearm stops being one the moment it leaves the inventory: dropped, destroyed, hauled off
    /// by another pawn, or promoted to primary. Null when the weapon is still a sidearm.
    /// </summary>
    private string StaleReason(ThingWithComps weapon)
    {
        if (weapon == null) return "the entry was null";
        if (weapon.Destroyed) return "it was destroyed";
        if (!Pawn.inventory.innerContainer.Contains(weapon)) return "it is no longer in the inventory";
        return null;
    }

    private void DropStaleEntries()
    {
        if (preferredPrimary is { Destroyed: true }) preferredPrimary = null;

        sidearms.RemoveAll(w =>
        {
            var reason = StaleReason(w);
            if (reason == null) return false;
            SidearmsMod.DevLog($"{Pawn.LabelShort}: dropping sidearm {w?.LabelCap}, {reason}.");
            return true;
        });
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
