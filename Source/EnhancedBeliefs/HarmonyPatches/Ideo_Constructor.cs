namespace EnhancedBeliefs.HarmonyPatches;

[HarmonyPatch(typeof(Ideo), MethodType.Constructor)]
internal static class Ideo_Constructor
{
    private static void Postfix(Ideo __instance)
    {
        _ = Current.Game.GetComponent<GameComponent_EnhancedBeliefs>()
            .IdeoTracker.AddPawnTrackerToIdeo(__instance);
    }
}
