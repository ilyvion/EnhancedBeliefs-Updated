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
        if (comp == null)
        {
            EnhancedBeliefsMod.ErrorOnce($"Pawn_ExposeData: GameComponent_EnhancedBeliefs is null. "
                + "This should not happen. Please report this issue and any related logs.",
                typeof(Pawn_ExposeData).GetHashCode() + typeof(GameComponent_EnhancedBeliefs).GetHashCode());
            return;
        }
        var pawnTracker = comp.PawnTracker;
        if (pawnTracker == null)
        {
            EnhancedBeliefsMod.ErrorOnce($"Pawn_ExposeData: PawnTracker is null. "
                + "This should not happen. Please report this issue and any related logs.",
                typeof(Pawn_ExposeData).GetHashCode() + typeof(GameComponent_EnhancedBeliefs.PawnIdeoTracker).GetHashCode());
            return;
        }
        var data = pawnTracker.TryGetIdeoTracker(__instance);

        Scribe_Deep.Look(ref data, "EB_IdeoTrackerData", __instance);

        if (Scribe.mode == LoadSaveMode.PostLoadInit && data != null)
        {
            if (data.Pawn is not Pawn pawn || (pawn != __instance && !pawn.Dead))
            {
                EnhancedBeliefsMod.Warning($"Tried to scribe IdeoTrackerData for pawn {__instance} but "
                    + $"the data is for pawn {data.Pawn?.ToString() ?? "[null]"}. "
                    + $"This should not happen. Overriding data pawn to match the current pawn.");
                data.ForceNewPawn(__instance);
            }
            comp.PawnTracker.SetIdeoTracker(__instance, data);
        }
    }
}
