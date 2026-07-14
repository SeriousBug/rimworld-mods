using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Loadout;

/// <summary>
/// Put one garment on, taking off whatever it conflicts with at the moment it goes on.
///
/// The point of doing both halves in one job is that the pawn stays dressed on the way there. Taking
/// the clothes off first and then walking to the armour would send them across the colony naked.
///
/// Handles apparel lying in a stockpile, held in a shelf or outfit stand, or carried in the pawn's own
/// inventory, which is where gear-up puts their clothes.
///
/// Loadout_EquipStash keeps the displaced garments (inventory, or the floor if they will not fit).
/// Loadout_EquipDrop leaves them for a hauler. Gearing up wants the first, standing down the second.
/// </summary>
public class JobDriver_LoadoutEquip : JobDriver
{
    private int duration;

    private Apparel Apparel => (Apparel)job.GetTarget(TargetIndex.A).Thing;

    private bool FromInventory =>
        pawn.inventory != null && pawn.inventory.innerContainer.Contains(Apparel);

    private bool FromApparelSource => !FromInventory && Apparel.ParentHolder is IApparelSource;

    private IApparelSource ApparelSource => (IApparelSource)job.GetTarget(TargetIndex.B).Thing;

    private bool KeepDisplaced => job.def == LoadoutJobDefOf.Loadout_EquipStash;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref duration, "duration", 0);
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (FromInventory)
        {
            return true;
        }

        if (FromApparelSource)
        {
            return pawn.Reserve((Thing)Apparel.ParentHolder, job, 1, -1, null, errorOnFailed);
        }

        return pawn.Reserve(Apparel, job, 1, -1, null, errorOnFailed);
    }

    public override void Notify_Starting()
    {
        base.Notify_Starting();

        if (FromApparelSource)
        {
            job.targetB = (Thing)Apparel.ParentHolder;
        }

        duration = (int)(Apparel.GetStatValue(StatDefOf.EquipDelay) * 60f);
        foreach (var displaced in Displaced())
        {
            duration += (int)(displaced.GetStatValue(StatDefOf.EquipDelay) * 60f);
        }
    }

    private List<Apparel> Displaced()
    {
        var apparel = Apparel;
        return pawn.apparel.WornApparel
            .Where(worn => !ApparelUtility.CanWearTogether(apparel.def, worn.def, pawn.RaceProps.body))
            .ToList();
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOnBurningImmobile(TargetIndex.A);

        var fromInventory = FromInventory;
        var usingSource = FromApparelSource;

        if (!fromInventory)
        {
            if (usingSource)
            {
                yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.InteractionCell)
                    .FailOnDespawnedNullOrForbidden(TargetIndex.B);
            }
            else
            {
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                    .FailOnDespawnedNullOrForbidden(TargetIndex.A);
            }
        }

        var wait = Toils_General.Wait(duration);
        if (!fromInventory)
        {
            wait.WithProgressBarToilDelay(usingSource ? TargetIndex.B : TargetIndex.A);
            wait.FailOnDespawnedNullOrForbidden(usingSource ? TargetIndex.B : TargetIndex.A);
        }
        else
        {
            wait.WithProgressBarToilDelay(TargetIndex.A);
        }
        wait.PlaySustainerOrSound(Apparel.def.apparel.soundWear);
        yield return wait;

        yield return Toils_General.Do(delegate
        {
            var apparel = Apparel;
            var comp = pawn.GetComp<CompLoadout>();

            foreach (var displaced in Displaced())
            {
                // Never strip gear the player forced or the game locked. SelectArmour already refuses
                // to plan a piece that would need to, so reaching here means the situation changed
                // mid-job; bail rather than take it off.
                if (!pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(displaced)
                    || pawn.apparel.IsLocked(displaced))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                if (KeepDisplaced && CanCarry(displaced) && pawn.apparel.TryMoveToInventory(displaced))
                {
                    comp?.Stashed.Add(displaced);
                    continue;
                }

                if (pawn.apparel.TryDrop(displaced, out var dropped, pawn.PositionHeld, forbid: false))
                {
                    dropped?.SetForbidden(value: false, warnOnFail: false);
                    if (KeepDisplaced && dropped != null)
                    {
                        // Could not carry it. Still remember it, so standing down goes and fetches it.
                        comp?.Stashed.Add(dropped);
                    }
                }
            }

            if (fromInventory)
            {
                pawn.inventory.innerContainer.Remove(apparel);
            }
            else if (usingSource)
            {
                ApparelSource.RemoveApparel(apparel);
            }

            if (!ApparelUtility.HasPartsToWear(pawn, apparel.def) || !apparel.PawnCanWear(pawn))
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            pawn.apparel.Wear(apparel);
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
