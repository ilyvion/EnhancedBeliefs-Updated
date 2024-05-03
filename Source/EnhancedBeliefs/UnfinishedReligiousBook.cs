using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace EnhancedBeliefs
{
    public class UnfinishedReligiousBook : UnfinishedThing
    {
        public Ideo ideo;
        public bool isOpen = false;
        public UnfinishedBookExtension extension;

        public UnfinishedBookExtension Extension
        {
            get
            {
                if (extension == null)
                {
                    extension = def.GetModExtension<UnfinishedBookExtension>();
                }

                return extension;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                if (!Find.IdeoManager.IdeosListForReading.Contains(ideo))
                {
                    ideo = null;
                }
            }

            Scribe_References.Look(ref ideo, "ideo");
            Scribe_Values.Look(ref isOpen, "isOpen", false);
        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (!isOpen)
            {
                base.DrawAt(drawLoc, flip);
                return;
            }

            Rot4 rot = ((!(base.ParentHolder is Pawn_CarryTracker pawn_CarryTracker)) ? base.Rotation : pawn_CarryTracker.pawn.Rotation);
            Extension.openGraphic.Graphic.Draw(drawLoc, flip ? rot.Opposite : rot, this);
        }
    }

    public class UnfinishedBookExtension : DefModExtension
    {
        public GraphicData openGraphic;
    }
}
