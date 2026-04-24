using RimWorld;
using Verse;

namespace Brainwash
{
    public class TraitEntry : IExposable
    {
        public string GetLabel(Pawn pawn) => traitDef.DataAtDegree(degree).GetLabelCapFor(pawn);
        public TraitDef traitDef;
        public int degree;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref traitDef, "traitDef");
            Scribe_Values.Look(ref degree, "degree");
        }
    }
}
