using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Loadout;

/// <summary>
/// Carry one garment out of the pawn's inventory and put it away. Standing down queues one of these
/// per piece of armour that came off, so the gear ends up in storage rather than in a heap on the
/// floor where the pawn happened to be standing.
///
/// Storage is either a cell (stockpile, or a shelf, which is an ISlotGroupParent) or a container that
/// holds things directly (an outfit stand, which is an IHaulDestination but not an ISlotGroupParent).
/// The cell-only finder never returns a stand however high its priority, so this branches the same way
/// HaulAIUtility.HaulToStorageJob does. That is all it takes for gear to land on a stand.
///
/// Not JobDefOf.UnloadYourInventory: that unloads everything, including the pawn's food and medicine.
/// </summary>
public class JobDriver_LoadoutDeposit : JobDriver
{
    private const TargetIndex ApparelInd = TargetIndex.A;
    private const TargetIndex StoreInd = TargetIndex.B;

    private bool toContainer;

    private Apparel Apparel => (Apparel)job.GetTarget(ApparelInd).Thing;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref toContainer, "toContainer", defaultValue: false);
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        var findTarget = ToilMaker.MakeToil("LoadoutDepositFindTarget");
        findTarget.initAction = delegate
        {
            var apparel = Apparel;

            // The swap may have dropped it rather than stashing it (an overloaded pawn), or the pawn
            // may have put it back on. Either way there is nothing to deposit.
            if (apparel == null || pawn.inventory == null
                                || !pawn.inventory.innerContainer.Contains(apparel))
            {
                EndJobWith(JobCondition.Succeeded);
                return;
            }

            if (!StoreUtility.TryFindBestBetterStorageFor(apparel, pawn, Map, StoragePriority.Unstored,
                    pawn.Faction, out var cell, out var haulDestination))
            {
                DropHere(apparel);
                return;
            }

            if (haulDestination is ISlotGroupParent)
            {
                toContainer = false;
                job.SetTarget(StoreInd, cell);
            }
            else if (haulDestination is Thing container
                     && container.TryGetInnerInteractableThingOwner() != null)
            {
                toContainer = true;
                job.SetTarget(StoreInd, container);
            }
            else
            {
                DropHere(apparel);
                return;
            }

            job.count = 1;
        };
        yield return findTarget;

        yield return Toils_Reserve.Reserve(StoreInd);

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

        var carryToCell = Toils_Haul.CarryHauledThingToCell(StoreInd);
        var finish = ToilMaker.MakeToil("LoadoutDepositFinish");
        finish.initAction = delegate { };
        finish.defaultCompleteMode = ToilCompleteMode.Instant;

        yield return Toils_Jump.JumpIf(carryToCell, () => !toContainer);
        yield return Toils_Haul.CarryHauledThingToContainer();
        yield return Toils_Haul.DepositHauledThingInContainer(StoreInd, TargetIndex.None);
        yield return Toils_Jump.Jump(finish);

        yield return carryToCell;
        yield return Toils_Haul.PlaceHauledThingInCell(StoreInd, carryToCell, storageMode: true);

        yield return finish;
    }

    /// <summary>Nowhere to put it. Leave it unforbidden for a hauler rather than carrying it forever.</summary>
    private void DropHere(Apparel apparel)
    {
        pawn.inventory.innerContainer.TryDrop(apparel, ThingPlaceMode.Near, 1, out var dropped);
        dropped?.SetForbidden(value: false, warnOnFail: false);
        EndJobWith(JobCondition.Succeeded);
    }
}
