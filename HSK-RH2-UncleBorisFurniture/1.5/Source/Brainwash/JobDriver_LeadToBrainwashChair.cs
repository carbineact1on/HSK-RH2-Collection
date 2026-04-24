using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Brainwash
{
	public class JobDriver_LeadToBrainwashChair : JobDriver
	{
		protected Pawn Takee => (Pawn)job.GetTarget(TargetIndex.A).Thing;
		protected Building Chair => (Building)job.GetTarget(TargetIndex.B).Thing;
		protected Building Television => (Building)job.GetTarget(TargetIndex.C).Thing;
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(Takee, job, 1, -1, null, errorOnFailed) 
				&& pawn.ReserveSittableOrSpot(Chair.Position, job, errorOnFailed);
		}

		public override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedOrNull(TargetIndex.A);
			this.FailOnDestroyedOrNull(TargetIndex.B);
			this.FailOnDestroyedOrNull(TargetIndex.C);
			this.FailOn(() => Television.TryGetComp<CompPowerTrader>() != null && !Television.TryGetComp<CompPowerTrader>().PowerOn);
			this.FailOnAggroMentalStateAndHostile(TargetIndex.A);
			Toil goToTakee = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
				.FailOnDespawnedNullOrForbidden(TargetIndex.A)
				.FailOnDespawnedNullOrForbidden(TargetIndex.B)
				.FailOn(() => !pawn.CanReach(Chair, PathEndMode.OnCell, Danger.Deadly))
				.FailOn(() => Takee.Downed)
				.FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return goToTakee;
			Toil startCarrying = Toils_Haul.StartCarryThing(TargetIndex.A);
			Toil goToChair = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOn(() => !pawn.IsCarryingPawn(Takee));
			yield return Toils_Jump.JumpIf(goToChair, () => pawn.IsCarryingPawn(Takee));
			yield return startCarrying;
			yield return goToChair;
			yield return Toils_Reserve.Release(TargetIndex.B);
			yield return Toils_General.Do(delegate
			{
				IntVec3 position = Chair.Position;
				pawn.carryTracker.TryDropCarriedThing(position, ThingPlaceMode.Direct, out _);
				Takee.jobs.StopAll();
				Job job = BrainwashJob();
				Takee.jobs.StartJob(job);
			});
		}

		protected virtual Job BrainwashJob()
		{
			return JobMaker.MakeJob(BrainwashDefOf.RedHorse_WatchBrainwashTelevision, Television, Chair);
		}
	}

	public class JobDriver_LeadToBrainwashChairForPersonalityChange : JobDriver_LeadToBrainwashChair
	{
		protected override Job BrainwashJob()
		{
            return JobMaker.MakeJob(BrainwashDefOf.RedHorse_StartBrainwashTelevision, Television, Chair);
        }
    }
}
