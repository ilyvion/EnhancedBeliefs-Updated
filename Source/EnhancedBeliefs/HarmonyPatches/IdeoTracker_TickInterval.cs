namespace EnhancedBeliefs.HarmonyPatches;

#if v1_5
[HarmonyPatch(typeof(Pawn_IdeoTracker), nameof(Pawn_IdeoTracker.IdeoTrackerTick))]
#else
[HarmonyPatch(typeof(Pawn_IdeoTracker), nameof(Pawn_IdeoTracker.IdeoTrackerTickInterval))]
#endif
internal static class IdeoTracker_TickInterval
{
    private static void Postfix(Pawn_IdeoTracker __instance)
    {
        var pawn = __instance.pawn;

        if (!pawn.Destroyed && pawn.Map != null && __instance.ideo != null && !Find.IdeoManager.classicMode && pawn.IsHashIntervalTick(GenTicks.TickLongInterval))
        {
            var comp = Current.Game.GetComponent<GameComponent_EnhancedBeliefs>();
            var data = comp.PawnTracker.EnsurePawnHasIdeoTracker(pawn);
            data.RecalculateRelationshipIdeoOpinions();
        }
    }
}
