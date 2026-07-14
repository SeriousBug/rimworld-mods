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

    /// <summary>
    /// Loadout_StashApparel means "this came off to gear up": remember it so standing down can put it
    /// back, whether it ended up in the inventory or on the floor. Loadout_DoffApparel is the
    /// stand-down case and is not remembered.
    /// </summary>
    private bool Remember => job.def == LoadoutJobDefOf.Loadout_StashApparel;

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
            var comp = pawn.GetComp<CompLoadout>();

            if (!pawn.apparel.WornApparel.Contains(apparel))
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            if (Remember && CanCarry(apparel) && pawn.apparel.TryMoveToInventory(apparel))
            {
                comp?.Stashed.Add(apparel);
                EndJobWith(JobCondition.Succeeded);
                return;
            }

            // Either the setting says drop, or the pawn cannot carry it. Dropping unforbidden means a
            // hauler stores it rather than leaving it on the ground forever.
            if (!pawn.apparel.TryDrop(apparel, out var dropped, pawn.PositionHeld, forbid: false))
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            dropped?.SetForbidden(value: false, warnOnFail: false);
            if (Remember && dropped != null)
            {
                comp?.Stashed.Add(dropped);
            }

            EndJobWith(JobCondition.Succeeded);
        });
    }

    private bool CanCarry(Apparel apparel)
    {
        return LoadoutMod.Settings.stashToInventory
               && pawn.inventory != null
               && MassUtility.CanEverCarryAnything(pawn)
               && !MassUtility.WillBeOverEncumberedAfterPickingUp(pawn, apparel, 1);
    }
}
