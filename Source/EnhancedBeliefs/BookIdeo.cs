using Verse.Grammar;

using static RimWorld.IdeoFoundation_Deity;

#if v1_5
using PlanetTile = int;
#else
using RimWorld.Planet;
#endif

namespace EnhancedBeliefs;

internal sealed class BookIdeo : Book
{
    private ReadingOutcomeDoer_CertaintyChange? doer;
    public ReadingOutcomeDoer_CertaintyChange Doer
    {
        get
        {
            if (doer == null)
            {
                var comp = GetComp<CompBook>();

                foreach (var doer in comp.doers)
                {
                    if (doer is ReadingOutcomeDoer_CertaintyChange change)
                    {
                        this.doer = change;
                        break;
                    }
                }
            }

            return (doer) ?? throw new InvalidOperationException(
                "Tried to get Doer on a EnhancedBeliefs.BookIdeo without a ReadingOutcomeDoer_CertaintyChange. This should not happen.");
        }
    }

    public Ideo? Ideo
    {
        get => Doer?.ideo;
        set
        {
            if (Doer == null)
            {
                Log.Error("Tried to set Ideo on a book without a ReadingOutcomeDoer_CertaintyChange. This should not happen.");
                return;
            }
            Doer.ideo = value;
        }
    }

    public override void PostQualitySet()
    {
    }

    public override void GenerateBook(Pawn? author = null, long? fixedDate = null)
    {
        base.GenerateBook(author, fixedDate);

        if (Ideo != null)
        {
            RegenerateName(Ideo);
        }
    }

    // Ensure that traders get their book ideo
    public override void PostGeneratedForTrader(TraderKindDef trader, PlanetTile forTile, Faction forFaction)
    {
        base.PostGeneratedForTrader(trader, forTile, forFaction);

        Ideo ??= forFaction == null || forFaction.ideos == null
                ? Find.IdeoManager.IdeosListForReading.RandomElement()
                : forFaction.ideos.PrimaryIdeo;

        RegenerateName(Ideo);
    }

    // Checks for null ideos in case something goes wrong
    public override void TickRare()
    {
        base.TickRare();

        if (Ideo == null)
        {
            Ideo = Find.IdeoManager.IdeosListForReading.RandomElement();
            RegenerateName(Ideo);
        }
    }

    //Completely copied over from ideo generation code, also generates description
    // TODO: Consider using a reverse transpiler to avoid code duplication
    private void RegenerateName(Ideo ideo)
    {
        var request = default(GrammarRequest);
        request.Includes.Add(ideo.culture.ideoNameMaker);
        var foundation = ideo.foundation;
        var foundationDeity = foundation as IdeoFoundation_Deity;
        foundation.AddPlaceRules(ref request);
        foundationDeity?.AddDeityRules(ref request);
        List<SymbolSource> list = [];
        if (ideo.memes.Any(m => !m.symbolPacks.NullOrEmpty()))
        {
            list.Add(SymbolSource.Pack);
        }
        if (foundationDeity != null && foundationDeity.deities.Count >= 1 && !ideo.memes.Any(m => !m.allowSymbolsFromDeity))
        {
            list.Add(SymbolSource.Deity);
        }
        if (list.Count == 0)
        {
            return;
        }
        switch (list.RandomElementByWeight(s => s == SymbolSource.Pack ? 1f : 0.5f))
        {
            case SymbolSource.Pack:
                SetupFromSymbolPack(ideo);
                break;
            case SymbolSource.Deity:
                SetupFromDeity(ideo);
                break;
            default:
                break;
        }
        title = GenText.CapitalizeAsTitle(GrammarResolver.Resolve("r_ideoName", request, null, false, null, null, null, true));

        var patterns = (from entry in ideo.memes.Where(meme => meme.descriptionMaker?.patterns != null).SelectMany(meme => meme.descriptionMaker.patterns)
                        group entry by entry.def into grp
                        select grp.MaxBy(entry => entry.weight)).ToList();
        if (!list.Any())
        {
            return;
        }

        var def = patterns.RandomElementByWeight(entry => entry.weight).def;
        descriptionFlavor = IdeoDescriptionUtility.ResolveDescription(Ideo, def, true).text;
        description = GenerateFullDescription();

        void AddMemeContent(Ideo ideo)
        {
            foreach (var item in ideo.memes)
            {
                if (item.generalRules != null)
                {
                    request.IncludesBare.Add(item.generalRules);
                }
            }
        }

        void AddSymbolPack(IdeoSymbolPack pack, MemeCategory memeCategory)
        {
            request.Constants.SetOrAdd("forcePrefix", pack.prefix.ToString());
            var text = pack.prefix ? (GrammarResolver.Resolve("hyphenPrefix", request) + "-") : string.Empty;
            if (pack.ideoName != null)
            {
                if (memeCategory == MemeCategory.Structure)
                {
                    request.Rules.Add(new Rule_String("packIdeoNameStructure", text + pack.ideoName));
                }
                else
                {
                    request.Rules.Add(new Rule_String("packIdeoName", text + pack.ideoName));
                }
            }
            if (pack.theme != null)
            {
                request.Rules.Add(new Rule_String("packTheme", pack.theme));
            }
            if (pack.adjective != null)
            {
                request.Rules.Add(new Rule_String("packAdjective", text + pack.adjective));
            }
            if (pack.member != null)
            {
                request.Rules.Add(new Rule_String("packMember", text + pack.member));
            }
        }

        void SetupFromDeity(Ideo ideo)
        {
            request.Rules.Add(new Rule_String("keyDeity", ideo.KeyDeityName));
            AddMemeContent(ideo);
        }

        void SetupFromSymbolPack(Ideo ideo)
        {
            MemeDef result;
            if (ideo.StructureMeme.symbolPackOverride)
            {
                result = ideo.StructureMeme;
            }
            else if (!ideo.memes.Where(m => m.symbolPacks.HasData() && m.symbolPacks.Any()).TryRandomElement(out result))
            {
                result = ideo.memes.Where(m => m.symbolPacks.HasData()).RandomElement();
            }
            AddMemeContent(ideo);
            if (result.symbolPacks.TryRandomElement(out var result2))
            {
                AddSymbolPack(result2, result.category);
            }
            else
            {
                AddSymbolPack(result.symbolPacks.RandomElement(), result.category);
            }
        }
    }
}
