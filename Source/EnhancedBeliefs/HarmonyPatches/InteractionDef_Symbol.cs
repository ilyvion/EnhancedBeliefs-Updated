namespace EnhancedBeliefs.HarmonyPatches;

// Debates use meme/precept symbol instead for their motes
[HarmonyPatch(typeof(InteractionDef), nameof(InteractionDef.GetSymbol))]
internal static class InteractionDef_Symbol
{
    private static bool Prefix(InteractionDef __instance, ref Texture2D __result)
    {
        if (__instance.Worker is InteractionWorker_IdeologicalDebateMeme worker)
        {
            if (worker.topic != null)
            {
                __result = worker.topic.Icon;
                worker.topic = null;
                return false;
            }
        }

        if (__instance.Worker is InteractionWorker_IdeologicalDebatePrecept worker2)
        {
            if (worker2.topic != null)
            {
                __result = worker2.topic.Icon;
                worker2.topic = null;
                return false;
            }
        }

        return true;
    }
}
