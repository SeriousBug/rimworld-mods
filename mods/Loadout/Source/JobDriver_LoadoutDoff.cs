using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Loadout;

/// <summary>
/// Takes one worn garment off, paying its real EquipDelay. Backs both Loadout_StashApparel (keep it
/// in the inventory so the swap back cannot fail) and Loadout_DoffApparel (leave it for a hauler).
/// </summary>
public class JobDriver_LoadoutDoff : JobDriver
{
    private int duration;

    private Apparel Apparel => (Apparel)job.GetTarget(TargetIndex.A).Thing;

    private bool StashToInventory => job.def == LoadoutJobDefOf.Loadout_StashApparel;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref duration, "duration", 0);
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    public override void Notify_Starting()
    {
        base.Notify_Starting();
        duration = (int)(Apparel.GetStatValue(StatDefOf.EquipDelay) * 60f);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);

        yield return Toils_General.Wait(duration)
            .PlaySustainerOrSound(Apparel.def.apparel.soundRemove)
            .WithProgressBarToilDelay(TargetIndex.A);

        yield return Toils_General.Do(delegate
        {
            var apparel = Apparel;
            if (!pawn.apparel.WornApparel.Contains(apparel))
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            if (StashToInventory && CanCarry(apparel) && pawn.apparel.TryMoveToInventory(apparel))
            {
                EndJobWith(JobCondition.Succeeded);
                return;
            }

            // Either we were asked to drop it, or the pawn cannot carry it. Dropping unforbidden means
            // a hauler will store it rather than leaving it on the ground forever.
            if (!pawn.apparel.TryDrop(apparel, out var dropped, pawn.PositionHeld, forbid: false))
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            dropped?.SetForbidden(value: false, warnOnFail: false);
            EndJobWith(JobCondition.Succeeded);
        });
    }

    private bool CanCarry(Apparel apparel)
    {
        return pawn.inventory != null
               && MassUtility.CanEverCarryAnything(pawn)
               && !MassUtility.WillBeOverEncumberedAfterPickingUp(pawn, apparel, 1);
    }
}
