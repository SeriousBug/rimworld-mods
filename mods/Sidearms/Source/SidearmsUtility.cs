using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Sidearms;

public static class SidearmsUtility
{
    public static CompSidearms GetComp(this Pawn pawn) => pawn?.GetComp<CompSidearms>();

    public static bool CanCarrySidearms(Pawn pawn) =>
        pawn is { Dead: false, Destroyed: false }
        && pawn.RaceProps is { Humanlike: true }
        && pawn.equipment != null
        && pawn.inventory != null
        && pawn.GetComp<CompSidearms>() != null;

    public static bool IsEligibleWeapon(Thing thing) =>
        thing is ThingWithComps { def: { IsWeapon: true, destroyOnDrop: false } }
        && thing.def.equipmentType == EquipmentType.Primary
        && !thing.def.IsApparel;

    /// <summary>
    /// Moves <paramref name="weapon"/> from the pawn's inventory into their hands, stowing whatever
    /// they were holding as a sidearm. The old primary is stowed, never dropped.
    /// </summary>
    public static bool TryEquipFromInventory(Pawn pawn, ThingWithComps weapon)
    {
        if (!CanCarrySidearms(pawn) || weapon == null || weapon.Destroyed) return false;
        if (!pawn.inventory.innerContainer.Contains(weapon)) return false;
        if (pawn.equipment.Primary == weapon) return true;
        if (!EquipmentUtility.CanEquip(weapon, pawn)) return false;

        var comp = pawn.GetComp<CompSidearms>();
        var previous = pawn.equipment.Primary;

        // Swapping invalidates the verb the current attack job is aiming with, so the job has to be
        // torn down and re-issued against the same target once the new weapon is in hand.
        var resume = CaptureAttackJob(pawn);

        if (previous != null && !StowPrimary(pawn, previous))
        {
            return false;
        }

        var toEquip = weapon;
        if (weapon.stackCount > 1)
        {
            toEquip = (ThingWithComps)weapon.SplitOff(1);
        }

        if (!pawn.inventory.innerContainer.TryTransferToContainer(
                toEquip, pawn.equipment.GetDirectlyHeldThings(), canMergeWithExistingStacks: false))
        {
            Log.Warning($"{SidearmsMod.LogPrefix} {pawn.LabelShort} could not take {toEquip.LabelShort} from inventory.");
            return false;
        }

        comp.Unregister(toEquip);
        if (previous != null) comp.Register(previous);

        pawn.stances?.CancelBusyStanceSoft();
        ResumeAttackJob(pawn, resume);

        return true;
    }

    private static bool StowPrimary(Pawn pawn, ThingWithComps primary)
    {
        if (!EquipmentUtility.QuestLodgerCanUnequip(primary, pawn)) return false;

        if (pawn.equipment.TryTransferEquipmentToContainer(primary, pawn.inventory.innerContainer))
        {
            return true;
        }

        Log.Warning($"{SidearmsMod.LogPrefix} {pawn.LabelShort} could not stow {primary.LabelShort}.");
        return false;
    }

    /// <summary>Registers a weapon already in the pawn's inventory as a sidearm.</summary>
    public static bool TryAddSidearm(Pawn pawn, ThingWithComps weapon)
    {
        if (!CanCarrySidearms(pawn) || !IsEligibleWeapon(weapon)) return false;

        var comp = pawn.GetComp<CompSidearms>();
        if (comp.IsSidearm(weapon)) return true;
        if (!comp.HasRoomFor(weapon)) return false;

        comp.Register(weapon);
        return true;
    }

    public static IEnumerable<ThingWithComps> CarriedSidearms(Pawn pawn) =>
        CanCarrySidearms(pawn)
            ? pawn.GetComp<CompSidearms>().Sidearms
            : Enumerable.Empty<ThingWithComps>();

    private readonly struct AttackJobResume
    {
        public readonly LocalTargetInfo Target;
        public readonly bool PlayerForced;
        public readonly bool Valid;

        public AttackJobResume(LocalTargetInfo target, bool playerForced)
        {
            Target = target;
            PlayerForced = playerForced;
            Valid = true;
        }
    }

    private static AttackJobResume CaptureAttackJob(Pawn pawn)
    {
        var job = pawn.CurJob;
        if (job == null) return default;
        if (job.def != JobDefOf.AttackStatic && job.def != JobDefOf.AttackMelee) return default;

        return new AttackJobResume(job.targetA, job.playerForced);
    }

    private static void ResumeAttackJob(Pawn pawn, AttackJobResume resume)
    {
        if (!resume.Valid) return;

        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false);

        var target = resume.Target;
        if (!target.IsValid || target.ThingDestroyed) return;
        if (target.Thing is Pawn { Dead: true }) return;

        var jobDef = pawn.equipment.Primary?.def.IsMeleeWeapon ?? true
            ? JobDefOf.AttackMelee
            : JobDefOf.AttackStatic;

        var job = JobMaker.MakeJob(jobDef, target);
        job.playerForced = resume.PlayerForced;
        pawn.jobs.StartJob(job, JobCondition.InterruptForced);
    }
}
