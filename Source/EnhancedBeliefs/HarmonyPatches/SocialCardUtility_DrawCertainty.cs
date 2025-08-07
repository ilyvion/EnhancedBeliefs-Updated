namespace EnhancedBeliefs.HarmonyPatches;

[HarmonyPatch(typeof(SocialCardUtility), nameof(SocialCardUtility.DrawPawnCertainty))]
internal static class SocialCardUtility_DrawCertainty
{
    private static Rect containerRect;
    internal static Rect ContainerRect => containerRect;

    private static bool Prefix(Pawn pawn, Rect rect)
    {
        var num = rect.x + 17f;
        Rect iconRect = new(num, rect.y + (rect.height / 2f) - 16f, 32f, 32f);
        pawn.Ideo.DrawIcon(iconRect);
        num += 42f;
        Text.Anchor = TextAnchor.MiddleLeft;
        Rect rect3 = new(num, rect.y, (rect.width / 2f) - num, rect.height);
        Widgets.Label(rect3, pawn.Ideo.name.Truncate(rect3.width));
        Text.Anchor = TextAnchor.UpperLeft;
        num += rect3.width + 10f;
        containerRect = new Rect(iconRect.x, rect.y + (rect.height / 2f) - 16f, 0f, 32f);
        Rect barRect = new(num, rect.y + (rect.height / 2f) - 16f, rect.width - num - 26f, 32f);
        containerRect.xMax = barRect.xMax;

        if (Widgets.ButtonInvisible(containerRect))
        {
            IdeoUIUtility.OpenIdeoInfo(pawn.Ideo);
        }

        _ = Widgets.FillableBar(barRect.ContractedBy(4f), pawn.ideo.Certainty, SocialCardUtility.BarFullTexHor);

        return false;
    }
}
