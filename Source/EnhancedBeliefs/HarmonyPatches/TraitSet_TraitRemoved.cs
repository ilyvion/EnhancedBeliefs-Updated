namespace EnhancedBeliefs.HarmonyPatches;

[HarmonyPatch(typeof(TraitSet), nameof(TraitSet.RemoveTrait))]
internal static class TraitSet_TraitRemoved
{
    private static void Postfix(TraitSet __instance)
    {
        Current.Game.GetComponent<GameComponent_EnhancedBeliefs>().PawnTracker?.TryGetIdeoTracker(__instance.pawn)?.RecacheAllBaseOpinions();
    }
}
