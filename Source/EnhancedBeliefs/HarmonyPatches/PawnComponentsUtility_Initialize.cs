namespace EnhancedBeliefs.HarmonyPatches;

[HarmonyPatch(typeof(PawnComponentsUtility), nameof(PawnComponentsUtility.CreateInitialComponents))]
internal static class PawnComponentsUtility_Initialize
{
    internal static Pawn? LastPawn
    {
        get; private set;
    }

    private static void Postfix(Pawn pawn)
    {
        LastPawn = null;

        if (pawn.ideo == null)
        {
            return;
        }

        var comp = Current.Game.GetComponent<GameComponent_EnhancedBeliefs>();

        _ = comp.PawnTracker.EnsurePawnHasIdeoTracker(pawn);
        LastPawn = pawn;
    }
}
