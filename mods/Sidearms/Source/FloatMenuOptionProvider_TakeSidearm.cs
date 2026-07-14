using RimWorld;
using Verse;
using Verse.AI;

namespace Sidearms;

/// <summary>
/// Adds "Carry as sidearm" when right-clicking a weapon on the ground.
///
/// FloatMenuMakerMap builds its provider list by reflecting over every non-abstract subclass of
/// FloatMenuOptionProvider, so subclassing is enough; there is nothing to register and nothing
/// to patch.
/// </summary>
public class FloatMenuOptionProvider_TakeSidearm : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool RequiresManipulation => true;

    protected override bool AppliesInt(FloatMenuContext context) =>
        SidearmsUtility.CanCarrySidearms(context.FirstSelectedPawn);

    protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
    {
        var pawn = context.FirstSelectedPawn;

        if (!SidearmsUtility.IsEligibleWeapon(clickedThing)) return null;
        if (!clickedThing.Spawned) return null;

        var weapon = (ThingWithComps)clickedThing;
        var label = "Sidearms_TakeAsSidearm".Translate(weapon.LabelShort);

        if (pawn.WorkTagIsDisabled(WorkTags.Violent))
        {
            return Disabled(label, "IsIncapableOfViolenceLower".Translate(pawn.LabelShort, pawn));
        }

        var comp = pawn.GetComp<CompSidearms>();
        if (!comp.HasRoomFor(weapon))
        {
            return Disabled(label, "Sidearms_NoRoom".Translate());
        }

        if (!EquipmentUtility.CanEquip(weapon, pawn, out var cantReason, checkBonded: false))
        {
            return Disabled(label, cantReason);
        }

        if (!pawn.CanReach(weapon, PathEndMode.ClosestTouch, Danger.Deadly))
        {
            return Disabled(label, "NoPath".Translate());
        }

        return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption(label, () =>
            {
                var job = JobMaker.MakeJob(SidearmsDefOf.Sidearms_TakeSidearm, weapon);
                job.count = 1;
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }),
            pawn, weapon);
    }

    private static FloatMenuOption Disabled(string label, string reason) =>
        new($"{label}: {reason.CapitalizeFirst()}", null);
}
