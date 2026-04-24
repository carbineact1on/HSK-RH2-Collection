using RimWorld;
using Verse;

namespace Brainwash
{
    public class SkillEntry : IExposable
    {
        public SkillDef skillDef;
        public int level;
        public Passion passion;
        public void ExposeData()
        {
            Scribe_Defs.Look(ref skillDef, "skillDef");
            Scribe_Values.Look(ref level, "level");
        }
    }
}
