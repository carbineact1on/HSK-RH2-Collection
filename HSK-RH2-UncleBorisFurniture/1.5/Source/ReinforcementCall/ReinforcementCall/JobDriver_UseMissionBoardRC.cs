using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ReinforcementCall
{
	// Token: 0x02000005 RID: 5
	public class JobDriver_UseMissionBoardRC : JobDriver
	{
		// Token: 0x06000008 RID: 8 RVA: 0x000021B8 File Offset: 0x000003B8
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			Pawn pawn = this.pawn;
			LocalTargetInfo targetA = this.job.targetA;
			Job job = this.job;
			return pawn.Reserve(targetA, job, 1, -1, null, errorOnFailed);
		}

		// Token: 0x06000009 RID: 9 RVA: 0x000021F0 File Offset: 0x000003F0
		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull(TargetIndex.A);
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell);
			Toil openMissionSelect = new Toil();
			openMissionSelect.initAction = delegate()
			{
				Pawn actor = openMissionSelect.actor;
				Building missionBoard = (Building)actor.jobs.curJob.GetTarget(TargetIndex.A).Thing;
				this.StartMissionSelection(missionBoard);
			};
			yield return openMissionSelect;
			yield break;
		}

		// Token: 0x0600000A RID: 10 RVA: 0x00002200 File Offset: 0x00000400
		public void StartMissionSelection(Building missionBoard)
		{
			Dialog_NodeTree dialog_NodeTree = new Dialog_NodeTree(MissionBoardDialogMakerRC.MissionSelectWindow(missionBoard, this.pawn.Map), true, false, null);
			dialog_NodeTree.soundAmbient = MBDefsOf.AmbientMissionBoardRC;
			Find.WindowStack.Add(dialog_NodeTree);
		}
	}
}
