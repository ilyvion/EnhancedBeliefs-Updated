namespace EnhancedBeliefs.HarmonyPatches;

[HarmonyPatch(typeof(SocialCardUtility), nameof(SocialCardUtility.DrawPawnCertainty))]
[HotSwappable]
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

        if (Mouse.IsOver(containerRect))
        {
            Widgets.DrawHighlight(containerRect);

            var certaintyChange = (pawn.ideo.CertaintyChangePerDay >= 0f ? "+" : "") + pawn.ideo.CertaintyChangePerDay.ToStringPercent();

            var tip = "EnhancedBeliefs.PawnCertaintyTooltip".Translate(pawn.Named("PAWN"), pawn.Ideo.Named("IDEO"), pawn.ideo.Certainty.ToStringPercent()) + "\n\n";
            tip += "EnhancedBeliefs.CertainChangePerDay".Translate(certaintyChange) + "\n";

            var comp = Current.Game.GetComponent<GameComponent_EnhancedBeliefs>();
            var data = comp.PawnTracker.EnsurePawnHasIdeoTracker(pawn);
            if (pawn.needs.mood.CurLevelPercentage < 0.8 && Find.TickManager.TicksGame - data.LastPositiveThoughtTick > 180000f)
            {
                tip += "EnhancedBeliefs.CertaintyLossFromInactivity".Translate(GameComponent_EnhancedBeliefs.CertaintyLossFromInactivity.Evaluate((Find.TickManager.TicksGame - data.LastPositiveThoughtTick) / 60000f).ToStringPercent()) + "\n";
            }

            TooltipHandler.TipRegion(containerRect, tip);
        }
        if (Widgets.ButtonInvisible(containerRect))
        {
            IdeoUIUtility.OpenIdeoInfo(pawn.Ideo);
        }


        _ = Widgets.FillableBar(barRect.ContractedBy(4f), pawn.ideo.Certainty, SocialCardUtility.BarFullTexHor);

        return false;
    }
}
