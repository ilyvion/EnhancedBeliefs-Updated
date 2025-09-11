namespace EnhancedBeliefs.HarmonyPatches;

[HarmonyPatch(typeof(Pawn_IdeoTracker), nameof(Pawn_IdeoTracker.CertaintyChangePerDay), MethodType.Getter)]
internal static class IdeoTracker_CertaintyChange
{
    private static bool Prefix(Pawn_IdeoTracker __instance, ref float __result)
    {
        __result = 0;

        var pawn = __instance.pawn;
        var comp = Current.Game.GetComponent<GameComponent_EnhancedBeliefs>();

        var data = comp.PawnTracker.EnsurePawnHasIdeoTracker(pawn);

        // 1 recache per rare tick should be enough
        if (data.CachedCertaintyChange == -9999f || pawn.IsHashIntervalTick(GenTicks.TickRareInterval))
        {
            data.CertaintyChangeRecache(comp);
        }

        __result += data.CachedCertaintyChange;

        if (__result > 0)
        {
            data.UpdateLastPositiveThoughtTick();
        }

        return false;
    }
}
