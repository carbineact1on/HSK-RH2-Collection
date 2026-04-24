using RimWorld;
using Verse;
using Verse.AI;

namespace RedHorse
{
	public class JoyGiver_ListenToBuilding : JoyGiver_InteractBuilding
	{
		public override bool CanInteractWith(Pawn pawn, Thing t, bool inBed)
		{
			Log.Message("Interacting with " + t);
			if (!base.CanInteractWith(pawn, t, inBed))
			{
				return false;
			}
			if (inBed)
			{
				Building_Bed bed = pawn.CurrentBed();
				return ListenBuildingUtility.CanListenFromBed(pawn, bed, t);
			}
			return true;
		}

		public override Job TryGivePlayJob(Pawn pawn, Thing t)
		{
			if (!ListenBuildingUtility.TryFindBestListenCell(t, pawn, def.desireSit, out var result, out var chair) 
				&& !ListenBuildingUtility.TryFindBestListenCell(t, pawn, desireSit: false, out result, out chair))
			{
				return null;
			}
			if (chair != null && result == chair.Position && !pawn.Map.reservationManager.CanReserve(pawn, chair))
			{
				return null;
			}
			return new Job(def.jobDef, t, result, chair);
		}
	}
}
