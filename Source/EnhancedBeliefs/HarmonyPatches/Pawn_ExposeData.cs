namespace EnhancedBeliefs.HarmonyPatches;

// Scribing data in a postfix to ensure that no junk data is saved
[HarmonyPatch(typeof(Pawn), nameof(Pawn.ExposeData))]
internal static class Pawn_ExposeData
{
    private static void Postfix(Pawn __instance)
    {
        if (__instance.ideo == null)
        {
            return;
        }

        var comp = Current.Game.GetComponent<GameComponent_EnhancedBeliefs>();
        var data = comp.PawnTracker.TryGetIdeoTracker(__instance);

        Scribe_Deep.Look(ref data, "EB_IdeoTrackerData", __instance);

        if (Scribe.mode != LoadSaveMode.Saving && data != null)
        {
            if (data.Pawn is not Pawn pawn || (pawn != __instance && !pawn.Dead))
            {
                Log.Warning($"Tried to scribe IdeoTrackerData for pawn {__instance} but "
                    + $"the data is for pawn {data.Pawn?.ToString() ?? "[null]"}. "
                    + $"This should not happen. Overriding data pawn to match the current pawn.");
                data.ForceNewPawn(__instance);
            }
            comp.PawnTracker.SetIdeoTracker(__instance, data);
        }
    }
}
