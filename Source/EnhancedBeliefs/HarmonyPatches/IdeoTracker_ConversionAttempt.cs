namespace EnhancedBeliefs.HarmonyPatches;

// Debates use meme/precept symbol instead for their motes
[HarmonyPatch(typeof(Pawn_IdeoTracker), nameof(Pawn_IdeoTracker.IdeoConversionAttempt))]
internal static class IdeoTracker_ConversionAttempt
{
    private static bool Prefix(Pawn_IdeoTracker __instance, float certaintyReduction, Ideo initiatorIdeo, bool applyCertaintyFactor, ref bool __result)
    {
        var comp = Current.Game.GetComponent<GameComponent_EnhancedBeliefs>();
        var data = comp.PawnTracker.EnsurePawnHasIdeoTracker(__instance.pawn);
        __result = data.OverrideConversionAttempt(certaintyReduction, initiatorIdeo, applyCertaintyFactor);
        return false;
    }
}
