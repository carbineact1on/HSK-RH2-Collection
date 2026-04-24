using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Brainwash
{
	public class WorkGiver_Warden_Brainwash : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.OnCell;
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
			return pawn.Map.mapPawns.AllPawns.Where(x => x.CanBeBrainwashedBy(pawn) && x != pawn && (x.IsSlaveOfColony || x.IsPrisonerOfColony));
        }
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Pawn prisoner = (Pawn)t;
            CompChangePersonality comp = prisoner.TryGetComp<CompChangePersonality>();
            if (comp.TryGetNearbyTelevisionAndChair(pawn, out var televisionAndChair))
            {
                Job job = JobMaker.MakeJob(BrainwashDefOf.RedHorse_LeadToBrainwashChair, t, televisionAndChair.chair,
                    televisionAndChair.television);
                job.count = 1;
                return job;
            }
            return null;
        }
	}
}
