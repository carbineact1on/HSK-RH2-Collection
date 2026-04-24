using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using Verse;
using Verse.AI;

namespace RedHorse
{
	public class JobDriver_ListenToMusicPlayer : JobDriver
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

		[DebuggerHidden]
		public override IEnumerable<Toil> MakeNewToils()
		{
			this.EndOnDespawnedOrNull(TargetIndex.A);
			if ((job.targetA.Thing?.TryGetComp<Comp_MusicPlayer>()?.isRadio).GetValueOrDefault())
			{
				report = "Listening to the radio.";
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
				if ((job.targetA.Thing?.TryGetComp<Comp_MusicPlayer>()?.isRadio).GetValueOrDefault())
				{
					report = "Listening to the radio.";
				}
				ListenTickAction();
			});
			toil.AddFinishAction(delegate
			{
				JoyUtility.TryGainRecRoomThought(pawn);
			});
			toil.defaultCompleteMode = ToilCompleteMode.Delay;
			toil.defaultDuration = job.def.joyDuration * 2;
			yield return toil;
		}

		protected virtual void ListenTickAction()
		{
			Comp_MusicPlayer comp_MusicPlayer = base.TargetA.Thing?.TryGetComp<Comp_MusicPlayer>();
			if (comp_MusicPlayer == null || !comp_MusicPlayer.IsOn())
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
	}
}
