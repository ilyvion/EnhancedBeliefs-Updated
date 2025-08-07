namespace EnhancedBeliefs.HarmonyPatches;

// Another bootleg tracker, no idea why Tynan didn't implement it in vanilla considering the amount of work and performance it would've saved him
// Smh, backseat coding
[HarmonyPatch(typeof(Pawn_IdeoTracker), nameof(Pawn_IdeoTracker.SetIdeo))]
internal static class IdeoTracker_SetIdeo
{
    private static void Postfix(Pawn_IdeoTracker __instance, Ideo ideo)
    {
        Current.Game.GetComponent<GameComponent_EnhancedBeliefs>().SetIdeo(__instance.pawn, ideo);
    }
}
