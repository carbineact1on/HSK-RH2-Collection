using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ReinforcementCall
{
	// Token: 0x0200000B RID: 11
	public class JobDriver_UseRedPhoneRC : JobDriver
	{
		// Token: 0x06000021 RID: 33 RVA: 0x00002D08 File Offset: 0x00000F08
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			Pawn pawn = this.pawn;
			LocalTargetInfo targetA = this.job.targetA;
			Job job = this.job;
			return pawn.Reserve(targetA, job, 1, -1, null, errorOnFailed);
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002D40 File Offset: 0x00000F40
		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull(TargetIndex.A);
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell).FailOn(delegate(Toil to)
			{
				Building_RedPhoneRC building_RedPhoneRC = (Building_RedPhoneRC)to.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing;
				return !building_RedPhoneRC.CanUseCommsNow;
			});
			Toil openComms = new Toil();
			openComms.initAction = delegate()
			{
				Pawn actor = openComms.actor;
				Building_RedPhoneRC building_RedPhoneRC = (Building_RedPhoneRC)actor.jobs.curJob.GetTarget(TargetIndex.A).Thing;
				bool canUseCommsNow = building_RedPhoneRC.CanUseCommsNow;
				if (canUseCommsNow)
				{
					this.OpenCommsWith(actor, actor.jobs.curJob.commTarget as Faction);
				}
			};
			yield return openComms;
			yield break;
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002D50 File Offset: 0x00000F50
		public void OpenCommsWith(Pawn negotiator, Faction faction)
		{
			Dialog_Negotiation dialog_Negotiation = new Dialog_Negotiation(negotiator, faction, ReinforcementDialogMakerRC.FactionDialogFor(negotiator, faction), true);
			dialog_Negotiation.soundAmbient = RCDefsOf.AmbientRedPhoneRC;
			RCDefsOf.PickUpRedPhoneRC.PlayOneShot(SoundInfo.OnCamera(MaintenanceType.None));
			Find.WindowStack.Add(dialog_Negotiation);
		}
	}
}
