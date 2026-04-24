using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ReinforcementCall
{
	// Token: 0x02000004 RID: 4
	public class CompMissionBoardRC : ThingComp
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000002 RID: 2 RVA: 0x00002078 File Offset: 0x00000278
		public CompProperties_MissionBoardRC Props => (CompProperties_MissionBoardRC)props;

		// Token: 0x06000003 RID: 3 RVA: 0x00002095 File Offset: 0x00000295
		public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn myPawn)
		{
			FloatMenuOption failureReason = GetFailureReason(myPawn);
			bool flag = failureReason != null;
			if (flag)
			{
				yield return failureReason;
			}
			else
			{
				FloatMenuOption option = IncidentFloatMenuOption(myPawn);
				bool flag2 = option != null;
				if (flag2)
				{
					yield return option;
				}
			}
			yield break;
		}

		// Token: 0x06000004 RID: 4 RVA: 0x000020AC File Offset: 0x000002AC
		public FloatMenuOption IncidentFloatMenuOption(Pawn negotiator)
		{
			string text = "SelectMissionBoardRC".Translate();
			return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, delegate ()
			{
				GiveUseMissionBoardJob(negotiator);
			}, MenuOptionPriority.Default, null, null, 0f, null, null), negotiator, parent, "ReservedBy");
		}

		// Token: 0x06000005 RID: 5 RVA: 0x0000211C File Offset: 0x0000031C
		public void GiveUseMissionBoardJob(Pawn negotiator)
		{
			Job job = new Job(MBDefsOf.UseMissionBoardRC, parent);
			negotiator.jobs.TryTakeOrderedJob(job, JobTag.Misc);
		}

		// Token: 0x06000006 RID: 6 RVA: 0x00002150 File Offset: 0x00000350
		private FloatMenuOption GetFailureReason(Pawn myPawn)
		{
			bool flag = !ReachabilityUtility.CanReach(myPawn, parent, PathEndMode.InteractionCell, Danger.Some, false, false, TraverseMode.ByPawn);
			FloatMenuOption result = flag ? new FloatMenuOption("CannotUseNoPath".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null) : null;
			return result;
		}

		// Token: 0x04000006 RID: 6
		public int tickAtLastMission = -1;
	}
}
