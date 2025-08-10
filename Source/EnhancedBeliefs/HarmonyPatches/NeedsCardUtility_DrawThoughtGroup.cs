namespace EnhancedBeliefs.HarmonyPatches;

// Adds the appropriate ideo icon to the Thought_ReligiousBookDestroyed thought
[HarmonyPatch(typeof(NeedsCardUtility), nameof(NeedsCardUtility.DrawThoughtGroup))]
internal static class NeedsCardUtility_DrawThoughtGroup
{
    private static void Postfix(Rect rect, List<Thought> ___thoughtGroup)
    {
        var leadingThought = PawnNeedsUIUtility.GetLeadingThoughtInGroup(___thoughtGroup);

        if (leadingThought is not Thought_ReligiousBookDestroyed religiousBookDestroyedThought)
        {
            return;
        }

        if (ModsConfig.IdeologyActive && !Find.IdeoManager.classicMode)
        {
            IdeoUIUtility.DoIdeoIcon(new Rect(rect.x + 235f + 32f + 10f, rect.y, 20f, 20f), religiousBookDestroyedThought.DestroyedBookIdeo, doTooltip: false, delegate
            {
                IdeoUIUtility.OpenIdeoInfo(religiousBookDestroyedThought.DestroyedBookIdeo);
            });
        }
    }
}
