using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Loadout;

/// <summary>
/// Carry one garment out of the pawn's inventory and put it away in storage. Standing down queues one
/// of these per piece of armour that came off, so the gear ends up on a shelf rather than in a heap on
/// the floor where the pawn happened to be standing.
///
/// Mirrors JobDriver_UnloadYourInventory's transfer-then-haul toils, but for a single named item, and
/// without touching the rest of the pawn's inventory (that job would also dump their food and meds).
/// </summary>
public class JobDriver_LoadoutDeposit : JobDriver
{
    private const TargetIndex ApparelInd = TargetIndex.A;
    private const TargetIndex StoreCellInd = TargetIndex.B;

    private Apparel Apparel => (Apparel)job.GetTarget(ApparelInd).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        var findCell = ToilMaker.MakeToil("LoadoutDepositFindCell");
        findCell.initAction = delegate
        {
            var apparel = Apparel;

            // The swap may have dropped it instead of stashing it (an overloaded pawn), or it may have
            // been put back on. Either way there is nothing to deposit.
            if (apparel == null || pawn.inventory == null
                                || !pawn.inventory.innerContainer.Contains(apparel))
            {
                EndJobWith(JobCondition.Succeeded);
                return;
            }

            if (!StoreUtility.TryFindBestBetterStoreCellFor(apparel, pawn, Map, StoragePriority.Unstored,
                    pawn.Faction, out var cell))
            {
                // Nowhere to put it. Leave it on the floor unforbidden for a hauler rather than making
                // the pawn carry armour around forever.
                pawn.inventory.innerContainer.TryDrop(apparel, ThingPlaceMode.Near, 1, out var dropped);
                dropped?.SetForbidden(value: false, warnOnFail: false);
                EndJobWith(JobCondition.Succeeded);
                return;
            }

            job.SetTarget(StoreCellInd, cell);
            job.count = 1;
        };
        yield return findCell;

        yield return Toils_Reserve.Reserve(StoreCellInd);
        yield return Toils_Goto.GotoCell(StoreCellInd, PathEndMode.Touch);

        var takeOut = ToilMaker.MakeToil("LoadoutDepositTakeOut");
        takeOut.initAction = delegate
        {
            var apparel = Apparel;
            if (apparel == null || !pawn.inventory.innerContainer.Contains(apparel))
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            pawn.inventory.innerContainer.TryTransferToContainer(
                apparel, pawn.carryTracker.innerContainer, 1, out var carried);
            job.count = 1;
            job.SetTarget(ApparelInd, carried);
            carried?.SetForbidden(value: false, warnOnFail: false);
        };
        yield return takeOut;

        var carryToCell = Toils_Haul.CarryHauledThingToCell(StoreCellInd);
        yield return carryToCell;
        yield return Toils_Haul.PlaceHauledThingInCell(StoreCellInd, carryToCell, storageMode: true);
    }
}
