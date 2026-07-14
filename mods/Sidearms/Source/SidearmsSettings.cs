using UnityEngine;
using Verse;

namespace Sidearms;

public class SidearmsSettings : ModSettings
{
    public int maxSidearms = 2;
    public float maxSidearmMassFraction = 0.5f;

    public bool autoSwitchToMelee = true;
    public bool autoSwitchBackToRanged = true;
    public bool autoSwitchToLongerRange;

    public bool applyToNonPlayerPawns = true;

    // A swap costs the pawn a stance delay, so a pawn that flip-flops every check is worse off
    // than one that never swaps at all. This floor is what keeps that from happening.
    public int swapCooldownTicks = 120;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref maxSidearms, "maxSidearms", 2);
        Scribe_Values.Look(ref maxSidearmMassFraction, "maxSidearmMassFraction", 0.5f);
        Scribe_Values.Look(ref autoSwitchToMelee, "autoSwitchToMelee", defaultValue: true);
        Scribe_Values.Look(ref autoSwitchBackToRanged, "autoSwitchBackToRanged", defaultValue: true);
        Scribe_Values.Look(ref autoSwitchToLongerRange, "autoSwitchToLongerRange", defaultValue: false);
        Scribe_Values.Look(ref applyToNonPlayerPawns, "applyToNonPlayerPawns", defaultValue: true);
        Scribe_Values.Look(ref swapCooldownTicks, "swapCooldownTicks", 120);
    }

    public void DoWindowContents(Rect inRect)
    {
        var list = new Listing_Standard();
        list.Begin(inRect);

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

        list.Label("Sidearms_Setting_Cooldown".Translate(swapCooldownTicks));
        swapCooldownTicks = Mathf.RoundToInt(list.Slider(swapCooldownTicks, 30f, 600f));

        list.End();
    }
}
