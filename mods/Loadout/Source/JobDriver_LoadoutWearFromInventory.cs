using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Loadout;

/// <summary>
/// Puts back on a garment the pawn stashed in their own inventory. The vanilla Wear job cannot do
/// this: it paths to a spawned Thing, and inventory apparel is not spawned.
/// </summary>
public class JobDriver_LoadoutWearFromInventory : JobDriver
{
    private int duration;

    private Apparel Apparel => (Apparel)job.GetTarget(TargetIndex.A).Thing;

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
        this.FailOn(() => pawn.inventory == null
                          || !pawn.inventory.innerContainer.Contains(Apparel));

        yield return Toils_General.Wait(duration)
            .PlaySustainerOrSound(Apparel.def.apparel.soundWear)
            .WithProgressBarToilDelay(TargetIndex.A);

        yield return Toils_General.Do(delegate
        {
            var apparel = Apparel;
            if (!pawn.inventory.innerContainer.Contains(apparel))
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            if (!ApparelUtility.HasPartsToWear(pawn, apparel.def) || !apparel.PawnCanWear(pawn))
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            pawn.inventory.innerContainer.Remove(apparel);
            pawn.apparel.Wear(apparel, dropReplacedApparel: true);
            EndJobWith(JobCondition.Succeeded);
        });
    }
}
