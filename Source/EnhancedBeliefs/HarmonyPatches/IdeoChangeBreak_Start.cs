using Verse.AI;

namespace EnhancedBeliefs.HarmonyPatches;

// Changing ideo break to instead use new mechanics and handler
[HarmonyPatch(typeof(MentalState_IdeoChange), nameof(MentalState_IdeoChange.PreStart))]
internal static class IdeoChangeBreak_Start
{
    private static bool Prefix(MentalState_IdeoChange __instance)
    {
        var pawn = __instance.pawn;
        __instance.oldIdeo = pawn.Ideo;
        __instance.oldRole = __instance.oldIdeo.GetRole(pawn);

        pawn.ideo.Certainty = Mathf.Clamp01(pawn.ideo.Certainty - 0.5f);

        var comp = Current.Game.GetComponent<GameComponent_EnhancedBeliefs>();
        var data = comp.PawnTracker.EnsurePawnHasIdeoTracker(pawn);

        if (data.CheckConversion(noBreakdown: true, opinionThreshold: 0.4f) == ConversionOutcome.Success)
        {
            __instance.newIdeo = pawn.Ideo;
            __instance.changedIdeo = true;
        }

        __instance.newCertainty = pawn.ideo.Certainty;

        return false;
    }
}
