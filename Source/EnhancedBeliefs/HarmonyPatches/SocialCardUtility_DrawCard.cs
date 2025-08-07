namespace EnhancedBeliefs.HarmonyPatches;

// Ugly code cut into two chunks because DrawPawnCertainty is called before rendering anything else in the tab and UI will get overwritten by other elements
[HarmonyPatch(typeof(SocialCardUtility), nameof(SocialCardUtility.DrawSocialCard))]
internal static class SocialCardUtility_DrawCard
{
    private static Rect hoverRect;
    private static bool opinionMenuOpen = false;
    private static Vector2 scroll;
    public const int maxIdeosPreview = 10;

    private static void Postfix(Pawn pawn)
    {
        var hovering = Mouse.IsOver(SocialCardUtility_DrawCertainty.ContainerRect);

        if (hovering || (opinionMenuOpen && Mouse.IsOver(hoverRect)))
        {
            Widgets.DrawHighlight(SocialCardUtility_DrawCertainty.ContainerRect);
            DrawOpinionTab(SocialCardUtility_DrawCertainty.ContainerRect, pawn, hovering);
        }
        else
        {
            opinionMenuOpen = false;
        }
    }

    private static void DrawOpinionTab(Rect containerRect, Pawn pawn, bool hovering)
    {
        var comp = Current.Game.GetComponent<GameComponent_EnhancedBeliefs>();
        var data = comp.PawnTracker.EnsurePawnHasIdeoTracker(pawn);
        opinionMenuOpen = true;

        if (hovering)
        {
            var certaintyChange = (pawn.ideo.CertaintyChangePerDay >= 0f ? "+" : "") + pawn.ideo.CertaintyChangePerDay.ToStringPercent();

            var tip = "{0}'s certainty in {1}: ".Formatted(pawn, pawn.Ideo) + pawn.ideo.Certainty.ToStringPercent() + "\n\n";
            tip += "Certainty change per day from beliefs: " + certaintyChange + "\n";

            if (pawn.needs.mood.CurLevelPercentage < 0.8 && Find.TickManager.TicksGame - data.LastPositiveThoughtTick > 180000f)
            {
                tip += "Certainty loss from lack of belief affirmation: " + GameComponent_EnhancedBeliefs.CertaintyLossFromInactivity.Evaluate((Find.TickManager.TicksGame - data.LastPositiveThoughtTick) / 60000f).ToStringPercent() + "\n";
            }

            tip += "\n";

            TooltipHandler.TipRegion(containerRect, () => tip.Resolve(), 10218219);
        }

        var maxNameWidth = 0f;

        foreach (var ideo in Find.IdeoManager.ideos)
        {
            maxNameWidth = Math.Max(maxNameWidth, Text.CalcSize(ideo.name).x);
        }

        Rect opinionRect = new(containerRect.x, containerRect.y + 40f, 264f + maxNameWidth, (Math.Min(Find.IdeoManager.ideos.Count, maxIdeosPreview) * 38f) + 8f);

        var tabRect = opinionRect.ContractedBy(4f);
        tabRect.height = Find.IdeoManager.ideos.Count * 38f;
        hoverRect = new Rect(opinionRect.x, opinionRect.y - 8f, opinionRect.width, opinionRect.height + 8f);

        Widgets.DrawShadowAround(opinionRect);
        Widgets.DrawWindowBackground(opinionRect);

        if (Find.IdeoManager.ideos.Count > maxIdeosPreview)
        {
            Widgets.BeginScrollView(opinionRect, ref scroll, tabRect, false);
        }

        for (var i = 0; i < Find.IdeoManager.ideos.Count; i++)
        {
            var ideo = Find.IdeoManager.ideos[i];
            Log.Warning($"Ideo {i}: {ideo.name} ({ideo.id})");

            Rect iconRect = new(tabRect.x + 4, tabRect.y + 4 + (i * 38f), 32f, 32f);
            ideo.DrawIcon(iconRect);

            Text.Anchor = TextAnchor.MiddleLeft;
            Rect textRect = new(iconRect.x + 40, iconRect.y, maxNameWidth, 32f);
            Widgets.Label(textRect, ideo.name);
            Text.Anchor = TextAnchor.UpperLeft;

            Rect barRect = new(textRect.x + textRect.width + 8f, textRect.y, 200f, 32f);
            var opinion = data.IdeoOpinion(ideo);
            _ = Widgets.FillableBar(barRect.ContractedBy(4f), opinion, SocialCardUtility.BarFullTexHor);

            Rect tooltipRect = new(tabRect.x + 4, tabRect.y + 4 + (i * 38f), 248f + maxNameWidth, 32f);
            if (Widgets.ButtonInvisible(tooltipRect))
            {
                IdeoUIUtility.OpenIdeoInfo(ideo);
            }

            if (Mouse.IsOver(tooltipRect))
            {
                Widgets.DrawHighlight(tooltipRect);

                var opinionRundown = data.DetailedIdeoOpinion(ideo);

                var tip = "Opinion of {0}: {1}\n\n".Formatted(ideo.name, opinion.ToStringPercent());
                tip += "From memes and precepts: " + opinionRundown[0].ToStringPercent() + "\n";
                tip += "From personal beliefs: " + opinionRundown[1].ToStringPercent() + "\n";
                tip += "From interpersonal relationships: " + opinionRundown[2].ToStringPercent() + "\n";

                TooltipHandler.TipRegion(tooltipRect, () => tip.Resolve(), 10218220);
            }
        }

        if (Find.IdeoManager.ideos.Count > maxIdeosPreview)
        {
            Widgets.EndScrollView();
        }
    }
}
