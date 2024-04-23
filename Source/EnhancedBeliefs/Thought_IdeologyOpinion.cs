using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EnhancedBeliefs
{
    public class Thought_IdeologyOpinion : Thought_SituationalSocial
    {
        public int lastCacheTick = -1;
        public int lastCachedStage = 2;

        public override int CurStageIndex
        {
            get
            {
                if (Find.TickManager.TicksGame - lastCacheTick < 250)
                {
                    return lastCachedStage;
                }

                lastCacheTick = Find.TickManager.TicksGame;

                if (pawn.Ideo == null || otherPawn.Ideo == null)
                {
                    lastCachedStage = 2;
                    return 2;
                }

                GameComponent_EnhancedBeliefs comp = Current.Game.GetComponent<GameComponent_EnhancedBeliefs>();
                IdeoTrackerData tracker = comp.pawnTrackerData[pawn];

                float opinion = tracker.IdeoOpinion(otherPawn.Ideo);
                
                if (opinion < 0.05f)
                {
                    lastCachedStage = 0;
                    return 0;
                }

                if (opinion < 0.15f)
                {
                    lastCachedStage = 1;
                    return 1;
                }

                if (opinion < 0.7f)
                {
                    lastCachedStage = 2;
                    return 2;
                }

                if (opinion < 0.9f)
                {
                    lastCachedStage = 3;
                    return 3;
                }

                lastCachedStage = 4;
                return 4;
            }
        }
    }
}
