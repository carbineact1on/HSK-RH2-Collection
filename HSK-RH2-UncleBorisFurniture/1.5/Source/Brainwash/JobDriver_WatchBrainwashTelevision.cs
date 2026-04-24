using RimWorld;
using UnityEngine;
using Verse;

namespace Brainwash
{
    public class JobDriver_WatchBrainwashTelevision : JobDriver_WatchBrainwashBase
    {
        public override void BrainwashEffect()
        {
            int propagandaEffect = pawn.story.traits.GetTrait(BrainwashDefOf.Nerves, 2) != null ? 25 : 50;
            if (pawn.guest.will > 0)
            {
                float will = pawn.guest.will;
                pawn.guest.will = Mathf.Max(0f, pawn.guest.will - propagandaEffect);
                propagandaEffect -= (int)(will - pawn.guest.will);
                string text = "TextMote_WillReduced".Translate(will.ToString("F1"), pawn.guest.will.ToString("F1"));
                if (pawn.needs.mood != null && pawn.needs.mood.CurLevelPercentage < 0.4f)
                {
                    text += "\n(" + "lowMood".Translate() + ")";
                }
                MoteMaker.ThrowText(pawn.DrawPos / 2f, pawn.Map, text, 8f);
                if (pawn.guest.will == 0f)
                {
                    TaggedString taggedString = "Brainwash_MessagePrisonerWillBroken".Translate(pawn);
                    Messages.Message(taggedString, pawn, MessageTypeDefOf.PositiveEvent);
                }
            }

            if (pawn.guest.resistance > 0)
            {
                float resistance = pawn.guest.resistance;
                pawn.guest.resistance = Mathf.Max(0f, pawn.guest.resistance - propagandaEffect);
                string text = "TextMote_ResistanceReduced".Translate(resistance.ToString("F1"), pawn.guest.resistance.ToString("F1"));
                if (pawn.needs.mood != null && pawn.needs.mood.CurLevelPercentage < 0.4f)
                {
                    text += "\n(" + "lowMood".Translate() + ")";
                }
                MoteMaker.ThrowText(pawn.DrawPos / 2f, pawn.Map, text, 8f);
                if (pawn.guest.resistance == 0f)
                {
                    pawn.guest.Recruitable = true;
                    TaggedString taggedString2 = "Brainwash_MessagePrisonerResistanceBroken".Translate(pawn.Named("PRISONER"));
                    Messages.Message(taggedString2, pawn, MessageTypeDefOf.PositiveEvent);
                }
            }
            else
            {
                pawn.guest.Recruitable = true;
            }

            pawn.health.AddHediff(HediffDefOf.CatatonicBreakdown);
            Messages.Message("Brainwash_PawnHavingBreakdown".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.CautionInput);
        }
    }
}
