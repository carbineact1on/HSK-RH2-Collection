using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RedHorse
{
	public class JobDriver_PlayMusicPlayer : JobDriver
	{
		private int duration = 400;

		private string report = "";

		protected int Duration => duration;

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
				report = "Playing the radio.";
			}
			yield return Toils_Reserve.Reserve(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			Toil toil = new Toil
			{
				defaultCompleteMode = ToilCompleteMode.Delay,
				defaultDuration = Duration
			};
			toil.WithProgressBarToilDelay(TargetIndex.A);
			if ((job.targetA.Thing?.TryGetComp<Comp_MusicPlayer>()?.isRadio).GetValueOrDefault())
			{
				ToilEffects.PlaySustainerOrSound(toil, RedHorseDefOf.RedHorse_RadioSeeking);
			}
			else
			{
				ToilEffects.PlaySustainerOrSound(toil, RedHorseDefOf.RedHorse_GramophoneWindup);
			}
			toil.initAction = delegate
			{
				(job.targetA.Thing?.TryGetComp<Comp_MusicPlayer>())?.StopMusic();
			};
			yield return toil;
			yield return new Toil
			{
				defaultCompleteMode = ToilCompleteMode.Instant,
				initAction = delegate
				{
					(job.targetA.Thing?.TryGetComp<Comp_MusicPlayer>())?.PlayMusic(pawn);
				}
			};
		}
	}
}
