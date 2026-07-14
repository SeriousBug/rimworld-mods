using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Loadout;

public class CompProperties_Loadout : CompProperties
{
    public CompProperties_Loadout()
    {
        compClass = typeof(CompLoadout);
    }
}

/// <summary>
/// Per-pawn loadout state. Lives on every Human, so every access path guards on the pawn actually
/// being a player colonist.
/// </summary>
public class CompLoadout : ThingComp
{
    private ApparelPolicy combatPolicy;
    private ApparelPolicy policyBeforeGearUp;
    private List<Apparel> stashed = new List<Apparel>();
    private bool gearedUp;

    public Pawn Pawn => (Pawn)parent;

    public bool GearedUp => gearedUp;

    /// <summary>The policy worn while geared up. Defaults to the vanilla Soldier policy.</summary>
    public ApparelPolicy CombatPolicy
    {
        get
        {
            if (combatPolicy == null)
            {
                combatPolicy = LoadoutSwapper.DefaultCombatPolicy();
            }
            return combatPolicy;
        }
        set => combatPolicy = value;
    }

    public ApparelPolicy PolicyBeforeGearUp => policyBeforeGearUp;

    public List<Apparel> Stashed => stashed;

    public void MarkGearedUp(ApparelPolicy previous)
    {
        gearedUp = true;
        policyBeforeGearUp = previous;
    }

    public void MarkStoodDown()
    {
        gearedUp = false;
        policyBeforeGearUp = null;
        stashed.Clear();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref gearedUp, "gearedUp", defaultValue: false);
        Scribe_References.Look(ref combatPolicy, "combatPolicy");
        Scribe_References.Look(ref policyBeforeGearUp, "policyBeforeGearUp");
        Scribe_Collections.Look(ref stashed, "stashed", LookMode.Reference);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            // A reference to a destroyed Thing is dropped on save and resolves to null on load, and
            // TakeResolvedRefList inserts that null into the list rather than skipping it.
            stashed ??= new List<Apparel>();
            stashed.RemoveAll(a => a == null || a.Destroyed);
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (!LoadoutSwapper.CanUseLoadout(Pawn))
        {
            yield break;
        }

        yield return new Command_Loadout(this);
    }
}

/// <summary>
/// A ThingComp gizmo is yielded once per selected pawn, and identical Commands merge into one button
/// in the drawer. There is deliberately no ProcessGroupInput override here: GizmoGridDrawer already
/// calls ProcessInput on every other gizmo in the group (Gizmo.alsoClickIfOtherInGroupClicked
/// defaults to true) and then calls ProcessGroupInput on the clicked one, so overriding it would fire
/// each pawn's action a second time and cancel the click out.
/// </summary>
public class Command_Loadout : Command_Action
{
    private readonly CompLoadout comp;

    public Command_Loadout(CompLoadout comp)
    {
        this.comp = comp;

        var gearedUp = comp.GearedUp;
        defaultLabel = (gearedUp ? "Loadout_StandDown" : "Loadout_GearUp").Translate();
        defaultDesc = (gearedUp ? "Loadout_StandDownDesc" : "Loadout_GearUpDesc").Translate(
            comp.CombatPolicy?.label ?? "Loadout_NoPolicy".Translate().ToString());
        icon = TexCommand.Draft;
        action = () =>
        {
            if (comp.GearedUp)
            {
                LoadoutSwapper.StandDown(comp);
            }
            else
            {
                LoadoutSwapper.GearUp(comp);
            }
        };
    }

    public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
    {
        get
        {
            var database = Current.Game?.outfitDatabase;
            if (database == null)
            {
                yield break;
            }

            foreach (var policy in database.AllOutfits)
            {
                var captured = policy;
                yield return new FloatMenuOption(
                    "Loadout_UsePolicy".Translate(captured.label),
                    () => comp.CombatPolicy = captured);
            }
        }
    }
}
