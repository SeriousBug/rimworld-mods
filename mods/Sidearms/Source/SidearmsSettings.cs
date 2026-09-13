using UnityEngine;
using Verse;

namespace Sidearms;

public class SidearmsSettings : ModSettings
{
    // Bumped when a new setting needs a different default for an existing user than for a new one.
    private const int CurrentConfigVersion = 1;

    // Every weapon a pawn carries is a sidearm, with no list to manage and nothing to mark. New
    // installs start here; anyone who had the mod before this existed keeps the older behaviour,
    // because turning it on mid-colony hands every pawn a button for whatever they are hauling.
    public bool allCarriedWeaponsAreSidearms = true;

    // Set once the choice is the player's, so the upgrade check below stops overriding it.
    public bool carriedWeaponsChoiceMade;

    private int configVersion = CurrentConfigVersion;

    public int maxSidearms = 2;
    public float maxSidearmMassFraction = 0.5f;

    public bool autoSwitchToMelee = true;
    public bool autoSwitchBackToRanged = true;
    public bool autoSwitchToLongerRange;

    public bool applyToNonPlayerPawns = true;

    public bool giveNpcMeleeSidearms = true;

    // Silver. Enough for a knife, a club or a spear; not enough for every raider corpse to drop a
    // longsword the player would not otherwise have seen.
    public float npcSidearmMaxPrice = 60f;

    // A swap costs the pawn a stance delay, so a pawn that flip-flops every check is worse off
    // than one that never swaps at all. This floor is what keeps that from happening.
    public int swapCooldownTicks = 120;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref allCarriedWeaponsAreSidearms, "allCarriedWeaponsAreSidearms", defaultValue: true);
        Scribe_Values.Look(ref carriedWeaponsChoiceMade, "carriedWeaponsChoiceMade", defaultValue: false);
        Scribe_Values.Look(ref configVersion, "configVersion", 0);
        Scribe_Values.Look(ref maxSidearms, "maxSidearms", 2);
        Scribe_Values.Look(ref maxSidearmMassFraction, "maxSidearmMassFraction", 0.5f);
        Scribe_Values.Look(ref autoSwitchToMelee, "autoSwitchToMelee", defaultValue: true);
        Scribe_Values.Look(ref autoSwitchBackToRanged, "autoSwitchBackToRanged", defaultValue: true);
        Scribe_Values.Look(ref autoSwitchToLongerRange, "autoSwitchToLongerRange", defaultValue: false);
        Scribe_Values.Look(ref applyToNonPlayerPawns, "applyToNonPlayerPawns", defaultValue: true);
        Scribe_Values.Look(ref giveNpcMeleeSidearms, "giveNpcMeleeSidearms", defaultValue: true);
        Scribe_Values.Look(ref npcSidearmMaxPrice, "npcSidearmMaxPrice", 60f);
        Scribe_Values.Look(ref swapCooldownTicks, "swapCooldownTicks", 120);

        // A settings file with no version was written before carried weapons were sidearms on
        // their own, so this is an upgrade and the colony keeps the behaviour it was built on.
        if (Scribe.mode == LoadSaveMode.LoadingVars && configVersion < CurrentConfigVersion)
        {
            configVersion = CurrentConfigVersion;
            allCarriedWeaponsAreSidearms = false;
            carriedWeaponsChoiceMade = true;
        }
    }

    /// <summary>
    /// The other half of the upgrade check, for a player who never opened the settings window and
    /// so has no settings file to read a version off. A save with sidearms already marked in it was
    /// played on a version that needed them marked.
    /// </summary>
    public void NotifyLoadedSaveHasMarkedSidearms()
    {
        if (carriedWeaponsChoiceMade) return;

        allCarriedWeaponsAreSidearms = false;
        carriedWeaponsChoiceMade = true;
        LoadedModManager.GetMod<SidearmsMod>()?.WriteSettings();
    }

    public void DoWindowContents(Rect inRect)
    {
        var list = new Listing_Standard();
        list.Begin(inRect);

        var allCarried = allCarriedWeaponsAreSidearms;
        list.CheckboxLabeled("Sidearms_Setting_AllCarried".Translate(), ref allCarried,
            "Sidearms_Setting_AllCarried_Tip".Translate());
        if (allCarried != allCarriedWeaponsAreSidearms)
        {
            allCarriedWeaponsAreSidearms = allCarried;
            carriedWeaponsChoiceMade = true;
        }

        list.GapLine();

        list.Label("Sidearms_Setting_MaxSidearms".Translate(maxSidearms));
        maxSidearms = Mathf.RoundToInt(list.Slider(maxSidearms, 0f, 6f));

        list.Label("Sidearms_Setting_MassFraction".Translate(maxSidearmMassFraction.ToStringPercent()));
        maxSidearmMassFraction = list.Slider(maxSidearmMassFraction, 0.1f, 1f);

        list.GapLine();

        list.CheckboxLabeled("Sidearms_Setting_AutoMelee".Translate(), ref autoSwitchToMelee,
            "Sidearms_Setting_AutoMelee_Tip".Translate());

        if (autoSwitchToMelee)
        {
            list.CheckboxLabeled("Sidearms_Setting_AutoBackToRanged".Translate(), ref autoSwitchBackToRanged,
                "Sidearms_Setting_AutoBackToRanged_Tip".Translate());
        }

        list.CheckboxLabeled("Sidearms_Setting_AutoLongerRange".Translate(), ref autoSwitchToLongerRange,
            "Sidearms_Setting_AutoLongerRange_Tip".Translate());

        list.GapLine();

        list.CheckboxLabeled("Sidearms_Setting_ApplyToNpcs".Translate(), ref applyToNonPlayerPawns,
            "Sidearms_Setting_ApplyToNpcs_Tip".Translate());

        list.CheckboxLabeled("Sidearms_Setting_NpcMelee".Translate(), ref giveNpcMeleeSidearms,
            "Sidearms_Setting_NpcMelee_Tip".Translate());

        if (giveNpcMeleeSidearms)
        {
            list.Label("Sidearms_Setting_NpcMeleePrice".Translate(npcSidearmMaxPrice.ToString("F0")));
            npcSidearmMaxPrice = Mathf.Round(list.Slider(npcSidearmMaxPrice, 10f, 300f));
        }

        list.Label("Sidearms_Setting_Cooldown".Translate(swapCooldownTicks));
        swapCooldownTicks = Mathf.RoundToInt(list.Slider(swapCooldownTicks, 30f, 600f));

        list.End();
    }
}
