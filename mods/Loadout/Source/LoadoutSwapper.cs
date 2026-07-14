using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Loadout;

public static class LoadoutSwapper
{
    /// <summary>Vanilla's own threshold for "this upgrade is not worth a trip".</summary>
    private const float MinScoreGainToCare = 0.05f;

    public static bool CanUseLoadout(Pawn pawn)
    {
        return pawn != null
               && pawn.IsColonistPlayerControlled
               && pawn.outfits != null
               && pawn.apparel != null
               && !pawn.Dead
               && !pawn.Downed;
    }

    /// <summary>
    /// Policies are save objects identified only by label, and the player may rename or delete any of
    /// them, so fall back to the database default rather than assuming Soldier exists.
    /// </summary>
    public static ApparelPolicy DefaultCombatPolicy()
    {
        var database = Current.Game?.outfitDatabase;
        if (database == null)
        {
            return null;
        }

        var soldierLabel = "OutfitSoldier".Translate().ToString();
        return database.AllOutfits.FirstOrDefault(p => p.label == soldierLabel)
               ?? database.DefaultOutfit();
    }

    /// <summary>Apparel the mod must never take off: forced by the player, or locked by the game.</summary>
    private static bool IsUntouchable(Pawn pawn, Apparel apparel)
    {
        return !pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(apparel)
               || pawn.apparel.IsLocked(apparel);
    }

    public static void GearUp(CompLoadout comp)
    {
        var pawn = comp.Pawn;
        if (!CanUseLoadout(pawn) || comp.GearedUp)
        {
            return;
        }

        var policy = comp.CombatPolicy;
        if (policy == null)
        {
            Messages.Message("Loadout_NoPolicyMessage".Translate(pawn.LabelShort, pawn),
                pawn, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }

        var previousPolicy = pawn.outfits.CurrentApparelPolicy;
        var armour = SelectArmour(pawn, policy);

        // With nothing to change into, gearing up would only undress the pawn.
        if (armour.Count == 0)
        {
            Messages.Message("Loadout_NoGearMessage".Translate(pawn.LabelShort, policy.label, pawn),
                pawn, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }

        var jobs = new List<Job>();
        comp.Stashed.Clear();

        // Each swap is one job, so the pawn walks over dressed and changes at the stockpile. The job
        // itself takes off whatever the new piece displaces and remembers it.
        foreach (var piece in armour)
        {
            jobs.Add(JobMaker.MakeJob(LoadoutJobDefOf.Loadout_EquipStash, piece));
        }

        // Whatever the policy disallows but no armour piece displaces (a hat, with no helmet to swap
        // into) comes off afterwards, so the pawn is never undressed while travelling.
        foreach (var worn in pawn.apparel.WornApparel.ToList())
        {
            if (IsUntouchable(pawn, worn) || policy.filter.Allows(worn))
            {
                continue;
            }

            var handledBySwap = armour.Any(a =>
                !ApparelUtility.CanWearTogether(a.def, worn.def, pawn.RaceProps.body));
            if (handledBySwap)
            {
                continue;
            }

            jobs.Add(JobMaker.MakeJob(LoadoutJobDefOf.Loadout_StashApparel, worn));
        }

        // Setting the policy fires Notify_OutfitChanged, which resets nextApparelOptimizeTick, so an
        // undrafted pawn re-optimises immediately afterwards and agrees with what we just put on
        // rather than undoing it.
        pawn.outfits.CurrentApparelPolicy = policy;
        comp.MarkGearedUp(previousPolicy);

        Log.Message($"{LoadoutMod.LogPrefix} {pawn.LabelShort} gearing up: policy {previousPolicy?.label ?? "none"} -> " +
                    $"{policy.label}, stashing {comp.Stashed.Count}, wearing {armour.Count}, " +
                    $"{jobs.Count} jobs, drafted={pawn.Drafted}");

        StartSequence(pawn, jobs);
    }

    public static void StandDown(CompLoadout comp)
    {
        var pawn = comp.Pawn;
        if (!CanUseLoadout(pawn) || !comp.GearedUp)
        {
            return;
        }

        var restored = comp.PolicyBeforeGearUp ?? Current.Game?.outfitDatabase?.DefaultOutfit();
        var stashed = comp.Stashed.Where(a => a != null && !a.Destroyed).ToList();

        var jobs = new List<Job>();

        // Swapping back is the same one-job-per-piece swap, so the armour comes off exactly when the
        // old clothes go on. A garment carried in the inventory needs no trip; one left on the floor
        // is walked to in armour, not in underwear.
        var restorable = new List<Apparel>();
        foreach (var apparel in stashed)
        {
            if (pawn.apparel.WornApparel.Contains(apparel))
            {
                continue;
            }

            // Do not plan a piece that could only go on by removing gear the player forced since.
            var blockedByUntouchable = pawn.apparel.WornApparel.Any(worn =>
                IsUntouchable(pawn, worn)
                && !ApparelUtility.CanWearTogether(worn.def, apparel.def, pawn.RaceProps.body));
            if (blockedByUntouchable)
            {
                continue;
            }

            var carried = pawn.inventory != null && pawn.inventory.innerContainer.Contains(apparel);
            var onMap = apparel.Spawned
                        && apparel.Map == pawn.Map
                        && pawn.CanReserveAndReach(apparel, PathEndMode.ClosestTouch, pawn.NormalMaxDanger());

            if (!carried && !onMap)
            {
                // Burnt, stolen, or worn by someone else. Skipped: the restored policy lets the vanilla
                // optimiser find a replacement.
                continue;
            }

            restorable.Add(apparel);
            jobs.Add(JobMaker.MakeJob(LoadoutJobDefOf.Loadout_EquipDrop, apparel));
        }

        // Armour the restored policy disallows but no restored garment displaces comes off afterwards.
        foreach (var worn in pawn.apparel.WornApparel.ToList())
        {
            if (IsUntouchable(pawn, worn) || stashed.Contains(worn))
            {
                continue;
            }

            if (restored != null && restored.filter.Allows(worn))
            {
                continue;
            }

            var handledBySwap = restorable.Any(a =>
                !ApparelUtility.CanWearTogether(a.def, worn.def, pawn.RaceProps.body));
            if (handledBySwap)
            {
                continue;
            }

            jobs.Add(JobMaker.MakeJob(LoadoutJobDefOf.Loadout_DoffApparel, worn));
        }

        if (restored != null)
        {
            pawn.outfits.CurrentApparelPolicy = restored;
        }

        var recovered = restorable.Count;
        var lost = comp.Stashed.Count - recovered;
        comp.MarkStoodDown();

        Log.Message($"{LoadoutMod.LogPrefix} {pawn.LabelShort} standing down: policy -> " +
                    $"{restored?.label ?? "none"}, restoring {recovered} stashed" +
                    (lost > 0 ? $" ({lost} lost, optimiser will replace)" : "") +
                    $", {jobs.Count} jobs, drafted={pawn.Drafted}");

        StartSequence(pawn, jobs);
    }

    /// <summary>
    /// Interrupt whatever the pawn is doing and run the swap.
    ///
    /// Deliberately not TryTakeOrderedJob: that sets Job.playerForced, and JobDriver_Wear turns a
    /// player-forced wear into forcedHandler.SetForced(apparel, true), which would pin the armour as
    /// forced gear and make the swap back refuse to remove it.
    ///
    /// Queued jobs are taken ahead of the think tree, which is what lets a drafted pawn change clothes
    /// at all: the drafted branch only supplies a job once the queue is empty.
    /// </summary>
    private static void StartSequence(Pawn pawn, List<Job> jobs)
    {
        if (jobs.Count == 0)
        {
            return;
        }

        pawn.jobs.ClearQueuedJobs();
        pawn.jobs.StartJob(
            jobs[0],
            JobCondition.InterruptForced,
            resumeCurJobAfterwards: false,
            cancelBusyStances: true,
            tag: JobTag.ChangingApparel);

        for (var i = 1; i < jobs.Count; i++)
        {
            pawn.jobs.jobQueue.EnqueueLast(jobs[i], JobTag.ChangingApparel);
        }
    }

    /// <summary>
    /// Pick the apparel the pawn should gear up into. Scores with vanilla's own ApparelScoreRaw so a
    /// "best armour" here means the same thing it means to JobGiver_OptimizeApparel, then greedily
    /// takes the best non-conflicting pieces.
    /// </summary>
    private static List<Apparel> SelectArmour(Pawn pawn, ApparelPolicy policy)
    {
        var chosen = new List<Apparel>();
        var map = pawn.Map;
        if (map == null)
        {
            return chosen;
        }

        // Mirrors JobGiver_OptimizeApparel: map apparel, plus the contents of every haul source, since
        // 1.6 pawns wear straight out of shelves and outfit stands without hauling them out first.
        // The two passes overlap, and a duplicate here would mean two Wear jobs for one garment.
        var found = new List<Thing>();
        map.listerThings.GetAllThings(in found, ThingRequestGroup.Apparel, null, lookInHaulSources: true);

        var seen = new HashSet<Apparel>();
        var candidates = new List<Apparel>();

        void Consider(Thing thing)
        {
            if (thing is Apparel apparel && seen.Add(apparel) && IsWearableCandidate(pawn, apparel, policy))
            {
                candidates.Add(apparel);
            }
        }

        foreach (var thing in found)
        {
            Consider(thing);
        }

        foreach (var source in map.haulDestinationManager.AllHaulSourcesListForReading)
        {
            foreach (var held in (IEnumerable<Thing>)source.GetDirectlyHeldThings())
            {
                Consider(held);
            }
        }

        candidates.SortByDescending(a => JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, a));

        foreach (var candidate in candidates)
        {
            if (JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, candidate) < MinScoreGainToCare)
            {
                continue;
            }

            var conflictsWithChosen = chosen.Any(c =>
                !ApparelUtility.CanWearTogether(c.def, candidate.def, pawn.RaceProps.body));
            if (conflictsWithChosen)
            {
                continue;
            }

            // Never plan a piece that could only go on by displacing gear we are forbidden to remove.
            var conflictsWithUntouchable = pawn.apparel.WornApparel.Any(worn =>
                IsUntouchable(pawn, worn)
                && !ApparelUtility.CanWearTogether(worn.def, candidate.def, pawn.RaceProps.body));
            if (conflictsWithUntouchable)
            {
                continue;
            }

            chosen.Add(candidate);
        }

        return chosen;
    }

    /// <summary>
    /// The same filter JobGiver_OptimizeApparel applies (JobGiver_OptimizeApparel.cs:133-155), so
    /// "gear the pawn can wear" means here what it means to vanilla. Worn apparel is never a candidate:
    /// it is not in listerThings.
    /// </summary>
    private static bool IsWearableCandidate(Pawn pawn, Apparel apparel, ApparelPolicy policy)
    {
        if (!policy.filter.Allows(apparel)
            || !apparel.IsInAnyStorage()
            || apparel.IsForbidden(pawn)
            || apparel.IsBurning()
            || (apparel.def.apparel.gender != Gender.None && apparel.def.apparel.gender != pawn.gender)
            || !apparel.def.apparel.developmentalStageFilter.Has(pawn.DevelopmentalStage)
            || !ApparelUtility.HasPartsToWear(pawn, apparel.def)
            || (CompBiocodable.IsBiocoded(apparel) && !CompBiocodable.IsBiocodedFor(apparel, pawn)))
        {
            return false;
        }

        // Apparel inside a shelf or outfit stand is reached by targeting the container, not the item.
        LocalTargetInfo target = apparel;
        if (apparel.ParentHolder is IApparelSource source && source is Thing sourceThing)
        {
            if (!source.ApparelSourceEnabled)
            {
                return false;
            }
            target = sourceThing;
        }

        return pawn.CanReserveAndReach(target, PathEndMode.OnCell, pawn.NormalMaxDanger());
    }
}
