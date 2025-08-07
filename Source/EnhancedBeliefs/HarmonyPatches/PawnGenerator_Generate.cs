namespace EnhancedBeliefs.HarmonyPatches;

[HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.TryGenerateNewPawnInternal))]
internal static class PawnGenerator_Generate
{
    [HarmonyPostfix]
    private static void CleanUpIfPawnGeneratedFailed([HarmonyArgument("__result")] Pawn generatedPawn)
    {
        if (generatedPawn != null || PawnComponentsUtility_Initialize.LastPawn == null)
        {
            return;
        }

        var comp = Current.Game.GetComponent<GameComponent_EnhancedBeliefs>();
        _ = comp.PawnTracker.RemoveTracker(PawnComponentsUtility_Initialize.LastPawn);
    }
}
