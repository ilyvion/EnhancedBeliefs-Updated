using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

[DefOf]
public static class EnhancedBeliefsDefOf
{
    public static MemeDef Supremacist;
    public static MemeDef Loyalist;
    public static MemeDef Guilty;
    public static MentalStateDef IdeoChange;
    public static ThingDef EB_UnfinishedIdeobook;
    public static ThingDef EB_Ideobook;
    public static JobDef EB_CompleteReligiousBook;
    public static JobDef EB_BurnReligiousBook;
    public static ThoughtDef EB_ReligiousBookDestroyed;


    static EnhancedBeliefsDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(EnhancedBeliefsDefOf));
    }
}

namespace EnhancedBeliefs
{
    public class EnhancedBeliefsMod : Mod
    {
        public Harmony harmonyInstance;

        public EnhancedBeliefsMod(ModContentPack content) : base(content)
        {
            harmonyInstance = new Harmony(id: "rimworld.smartkar.enhancedbeliefs.main");
            harmonyInstance.PatchAll();
        }
    }
}
