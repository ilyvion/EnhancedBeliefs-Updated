namespace EnhancedBeliefs.HarmonyPatches;

[HarmonyPatch(typeof(IdeoDevelopmentTracker), nameof(IdeoDevelopmentTracker.Notify_Reformed))]
internal static class FluidIdeoTracker_Reformed
{
    private static void Postfix(IdeoDevelopmentTracker __instance)
    {
        Current.Game.GetComponent<GameComponent_EnhancedBeliefs>().BaseOpinionRecache(__instance.ideo);
    }
}
