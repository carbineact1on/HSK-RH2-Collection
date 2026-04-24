using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RedHorse
{
	public class JobDriver_AutoPlayListenToMusicPlayer : JobDriver
	{
		private int duration = 400;

		private string report = "";

		public Comp_MusicPlayer MusicPlayer
		{
			get
			{
				Comp_MusicPlayer comp_MusicPlayer = pawn.jobs.curJob.GetTarget(TargetIndex.A).Thing?.TryGetComp<Comp_MusicPlayer>();
				if (comp_MusicPlayer == null)
				{
					throw new InvalidOperationException("Music player is missing.");
				}
				return comp_MusicPlayer;
			}
		}

		protected int Duration => duration;

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
			if (!MusicPlayer.IsOn())
			{
				if (job.targetA.Thing?.TryGetComp<Comp_MusicPlayer>().isRadio ?? false)
				{
					report = "playing the radio.";
				}
				yield return Toils_Reserve.Reserve(TargetIndex.A);
				yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
				Toil wind = new Toil
				{
					defaultCompleteMode = ToilCompleteMode.Delay,
					defaultDuration = Duration
				};
				wind.WithProgressBarToilDelay(TargetIndex.A);
				if ((job.targetA.Thing?.TryGetComp<Comp_MusicPlayer>()?.isRadio).GetValueOrDefault())
				{
					ToilEffects.PlaySustainerOrSound(wind, RedHorseDefOf.RedHorse_RadioSeeking);
				}
				else
				{
					ToilEffects.PlaySustainerOrSound(wind, RedHorseDefOf.RedHorse_GramophoneWindup);
				}
				wind.initAction = delegate
				{
					MusicPlayer.StopMusic();
				};
				yield return wind;
				yield return new Toil
				{
					defaultCompleteMode = ToilCompleteMode.Instant,
					initAction = delegate
					{
						MusicPlayer.PlayMusic(pawn);
					}
				};
			}
			Toil toil;
			if (base.TargetC.HasThing && base.TargetC.Thing is Building_Bed)
			{
				this.KeepLyingDown(TargetIndex.C);
				yield return Toils_Reserve.Reserve(TargetIndex.C, ((Building_Bed)base.TargetC.Thing).SleepingSlotsCount);
				yield return Toils_Bed.ClaimBedIfNonMedical(TargetIndex.C);
				yield return Toils_Bed.GotoBed(TargetIndex.C);
				toil = Toils_LayDown.LayDown(TargetIndex.C, true, false, true, true);
				toil.AddFailCondition(() => !pawn.Awake());
			}
			else
			{
				if (base.TargetC.HasThing)
				{
					yield return Toils_Reserve.Reserve(TargetIndex.C);
				}
				yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
				toil = new Toil();
			}
			toil.AddPreTickAction(delegate
			{
				ListenTickAction();
				if ((job.targetA.Thing?.TryGetComp<Comp_MusicPlayer>()?.isRadio).GetValueOrDefault())
				{
					report = "Listening to the radio.";
				}
			});
			toil.AddFinishAction(delegate
			{
				JoyUtility.TryGainRecRoomThought(pawn);
			});
			toil.defaultCompleteMode = ToilCompleteMode.Delay;
			toil.defaultDuration = job.def.joyDuration;
			yield return toil;
		}

		protected virtual void ListenTickAction()
		{
			if (!MusicPlayer.IsOn())
			{
				EndJobWith(JobCondition.Incompletable);
				return;
			}
			pawn.rotationTracker.FaceCell(base.TargetA.Cell);
			pawn.GainComfortFromCellIfPossible();
			float statValue = base.TargetThingA.GetStatValue(StatDefOf.JoyGainFactor);
			float num = statValue;
			JoyUtility.JoyTickCheckEnd(pawn, JoyTickFullJoyAction.EndJob, num, (Building)null);
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}
	}
}
