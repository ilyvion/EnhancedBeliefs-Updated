namespace EnhancedBeliefs;

[HotSwappable]
internal sealed class ITab_Opinion : ITab
{
    private const float HeightForAtMostIdeoCount = 10f;
    private const float Padding = 4f;
    private const float BarWidth = 200f;
    private const float IconSize = 32f;
    private const float RowHeight = IconSize + (2 * Padding);
    private const float IconTextGap = 2 * Padding;

    private static Vector2 scroll;

    public ITab_Opinion()
    {
        labelKey = "EnhancedBeliefs.TabOpinion";
    }

    protected override void FillTab()
    {
        var ideos = Find.IdeoManager.IdeosListForReading;

        var maxNameWidth = ideos.Select(ideo => Text.CalcSize(ideo.name).x)
                                .DefaultIfEmpty(0f)
                                .Max();

        var width = IconSize + IconTextGap + maxNameWidth + BarWidth + (6 * Padding) + GenUI.ScrollBarWidth;
        var height = (Math.Min(ideos.Count, HeightForAtMostIdeoCount) * RowHeight) + Text.LineHeight + (2 * Padding);
        size = new Vector2(width, height);

        var tabContentRect = new Rect(0f, 0f, width, height).ContractedBy(Padding);
        tabContentRect.yMin += Text.LineHeight;

        var comp = Current.Game.GetComponent<GameComponent_EnhancedBeliefs>();
        var data = comp.PawnTracker.EnsurePawnHasIdeoTracker(SelPawn);

        var headerRect = new Rect()
        {
            x = 2 * Padding,
            height = Text.LineHeight + (2 * Padding),
            width = width - Text.LineHeight - Padding,
        };
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(headerRect, "EnhancedBeliefs.IdeologyOpinions".Translate());
        Text.Anchor = TextAnchor.UpperLeft;

        Widgets.BeginGroup(tabContentRect);

        var viewRect = new Rect()
        {
            width = tabContentRect.width - GenUI.ScrollBarWidth - Padding,
            height = ideos.Count * RowHeight,
        };

        Widgets.BeginScrollView(tabContentRect.AtZero(), ref scroll, viewRect, true);

        var pos = Padding;
        foreach (var (ideo, opinion) in ideos
            .Select(ideo => (ideo, opinion: data.IdeoOpinion(ideo)))
            .OrderByDescending(ideo => ideo.ideo == SelPawn.Ideo)
            .ThenByDescending(ideo => ideo.opinion))
        {
            // Icon
            Rect iconRect = new(Padding, pos, IconSize, IconSize);
            // Widgets.DrawRectFast(iconRect, Color.red);
            ideo.DrawIcon(iconRect);

            // Text
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect textRect = new(iconRect.xMax + IconTextGap, pos, maxNameWidth, IconSize);
            // Widgets.DrawRectFast(textRect, Color.blue);
            Widgets.Label(textRect, ideo.name);
            Text.Anchor = TextAnchor.UpperLeft;

            // Opinion bar
            Rect barRect = new(textRect.xMax + (2 * Padding), pos, BarWidth, IconSize);
            _ = Widgets.FillableBar(barRect.ContractedBy(Padding), opinion, SocialCardUtility.BarFullTexHor);
            // Widgets.DrawRectFast(barRect, Color.cyan.ToTransparent(0.5f));

            // Tooltip and mouse click handling
            Rect tooltipRect = new(Padding, pos, barRect.xMax - Padding, IconSize);
            if (Widgets.ButtonInvisible(tooltipRect))
            {
                IdeoUIUtility.OpenIdeoInfo(ideo);
            }
            if (Mouse.IsOver(tooltipRect))
            {
                Widgets.DrawHighlight(tooltipRect);

                var opinionRundown = data.DetailedIdeoOpinion(ideo);

                var tip = "EnhancedBeliefs.PawnOpinionTooltip".Translate(SelPawn.Named("PAWN"), ideo.Named("IDEO"), opinion.ToStringPercent()) + "\n\n";

                tip += "EnhancedBeliefs.PawnOptionToolTip.FromMemesAndPrecepts".Translate(opinionRundown.BaseOpinion.ToStringPercent()) + "\n";
                tip += "EnhancedBeliefs.PawnOptionToolTip.FromPersonalBeliefs".Translate(opinionRundown.PersonalOpinion.ToStringPercent()) + "\n";
                tip += "EnhancedBeliefs.PawnOptionToolTip.FromInterpersonalRelationships".Translate(opinionRundown.RelationshipOpinion.ToStringPercent()) + "\n";

                TooltipHandler.TipRegion(tooltipRect, tip);
            }

            if (ideo == SelPawn.Ideo)
            {
                Widgets.DrawLineHorizontal(0f, pos + RowHeight - (Padding / 2), width);
                pos += Padding;
            }

            pos += RowHeight;
        }

        Widgets.EndScrollView();

        Widgets.EndGroup();
    }

    // Only show tab for pawns with an ideology
    public override bool Hidden => SelPawn?.Ideo is null;
    public override bool IsVisible => SelPawn?.Ideo is not null;
}
