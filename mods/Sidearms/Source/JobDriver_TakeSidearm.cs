using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Sidearms;

/// <summary>
/// Walks to a weapon, puts it in the pawn's inventory, and records it as a sidearm.
///
/// Vanilla's TakeInventory job would do the carrying part, but it has no way to say "and this one
/// is a weapon I intend to fight with", which is the whole distinction this mod turns on.
/// </summary>
public class JobDriver_TakeSidearm : JobDriver
{
    private ThingWithComps Target => (ThingWithComps)job.GetTarget(TargetIndex.A).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        pawn.Reserve(job.targetA, job, 1, 1, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
        this.FailOnBurningImmobile(TargetIndex.A);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
            .FailOnDespawnedNullOrForbidden(TargetIndex.A);

        var take = ToilMaker.MakeToil(nameof(JobDriver_TakeSidearm));
        take.defaultCompleteMode = ToilCompleteMode.Instant;
        take.initAction = () =>
        {
            var weapon = Target;
            if (weapon == null || weapon.Destroyed)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            var comp = pawn.GetComp<CompSidearms>();
            if (comp == null || !comp.HasRoomFor(weapon))
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            // A weapon lying on the map belongs to Map.spawnedThings, and ThingOwner.TryAdd refuses
            // anything that is already in a container. SplitOff is what despawns it and hands over
            // ownership, even when it is taking the whole stack.
            var toTake = (ThingWithComps)weapon.SplitOff(1);

            if (!pawn.inventory.innerContainer.TryAdd(toTake))
            {
                GenPlace.TryPlaceThing(toTake, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            comp.Register(toTake);
            toTake.def.soundInteract?.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
        };

        yield return take;
    }
}
