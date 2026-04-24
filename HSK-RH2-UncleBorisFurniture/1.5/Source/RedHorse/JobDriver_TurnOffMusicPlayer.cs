using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RedHorse
{
	public class JobDriver_TurnOffMusicPlayer : JobDriver
	{
		private string report = "";

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}

		public override string GetReport()
		{
			if (report != "")
			{
				return base.ReportStringProcessed(report);
			}
			return base.GetReport();
		}

		public override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			if ((job.targetA.Thing?.TryGetComp<Comp_MusicPlayer>()?.isRadio).GetValueOrDefault())
			{
				report = "Turning off radio.";
			}
			yield return Toils_Reserve.Reserve(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			yield return new Toil
			{
				defaultCompleteMode = ToilCompleteMode.Instant,
				initAction = delegate
				{
					(job.targetA.Thing?.TryGetComp<Comp_MusicPlayer>())?.StopMusic();
				}
			};
		}
	}
}
