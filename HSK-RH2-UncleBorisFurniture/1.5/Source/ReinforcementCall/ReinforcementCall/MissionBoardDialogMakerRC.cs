using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace ReinforcementCall
{
	// Token: 0x02000006 RID: 6
	public static class MissionBoardDialogMakerRC
	{
		// Token: 0x0600000C RID: 12 RVA: 0x00002248 File Offset: 0x00000448
		public static DiaNode MissionSelectWindow(Building missionBoard, Map map)
		{
			DiaNode diaNode = new DiaNode("SelectMissionRC".Translate());
			bool flag = map != null && map.IsPlayerHome && missionBoard.GetComp<CompMissionBoardRC>() != null;
			if (flag)
			{
				List<LabeledIncident> doableMissions = missionBoard.GetComp<CompMissionBoardRC>().Props.doableMissions;
				List<LabeledQuestScript> doableQuests = missionBoard.GetComp<CompMissionBoardRC>().Props.doableQuests;
				bool flag2 = doableMissions != null;
				if (flag2)
				{
					foreach (LabeledIncident labeledIncident in doableMissions)
					{
						diaNode.options.Add(MissionBoardDialogMakerRC.StartMissionOption(map, labeledIncident, missionBoard));
					}
				}
				bool flag3 = doableQuests != null;
				if (flag3)
				{
					foreach (LabeledQuestScript labeledQuestScript in doableQuests)
					{
						diaNode.options.Add(MissionBoardDialogMakerRC.StartQuestOption(map, labeledQuestScript, missionBoard));
					}
				}
			}
			DiaOption diaOption = new DiaOption("(" + "Close".Translate() + ")");
			diaOption.resolveTree = true;
			diaNode.options.Add(diaOption);
			return diaNode;
		}

		// Token: 0x0600000D RID: 13 RVA: 0x000023B0 File Offset: 0x000005B0
		private static void StartMission(Map map, IncidentDef incident)
		{
			IncidentParms incidentParms = new IncidentParms();
			incidentParms.target = map;
			incidentParms.points = StorytellerUtility.DefaultThreatPointsNow(map);
			incident.Worker.TryExecute(incidentParms);
			MBDefsOf.OneShotMissionSelectedRC.PlayOneShotOnCamera(null);
		}

		// Token: 0x0600000E RID: 14 RVA: 0x000023F0 File Offset: 0x000005F0
		private static void StartQuest(Map map, QuestScriptDef quest)
		{
			QuestUtility.SendLetterQuestAvailable(QuestUtility.GenerateQuestAndMakeAvailable(quest, new IncidentParms
			{
				target = map,
				points = StorytellerUtility.DefaultThreatPointsNow(map)
			}.points));
			MBDefsOf.OneShotMissionSelectedRC.PlayOneShotOnCamera(null);
		}

		// Token: 0x0600000F RID: 15 RVA: 0x00002438 File Offset: 0x00000638
		public static DiaOption StartMissionOption(Map map, LabeledIncident labeledIncident, Building missionBoard)
		{
			int silverToPay = 500;
			string customLabel = labeledIncident.customLabel;
			DiaOption diaOption = new DiaOption(customLabel);
			int num = missionBoard.GetComp<CompMissionBoardRC>().tickAtLastMission + 60000 - Find.TickManager.TicksGame;
			bool flag = num > 0;
			DiaOption result;
			if (flag)
			{
				DiaOption diaOption2 = new DiaOption(customLabel);
				diaOption2.Disable("WaitTime".Translate(num.ToStringTicksToPeriod(true, false, true, true)));
				result = diaOption2;
			}
			else
			{
				bool flag2 = MissionBoardDialogMakerRC.AmountSendableSilver(map) < silverToPay;
				if (flag2)
				{
					DiaOption diaOption3 = new DiaOption(customLabel);
					diaOption3.Disable("NeedSilverLaunchable".Translate(silverToPay.ToString()));
					result = diaOption3;
				}
				else
				{
					diaOption.action = delegate()
					{
						MissionBoardDialogMakerRC.StartMission(map, labeledIncident.incidentDef);
						TradeUtility.LaunchThingsOfType(ThingDefOf.Silver, silverToPay, map, null);
						missionBoard.GetComp<CompMissionBoardRC>().tickAtLastMission = Find.TickManager.TicksGame;
					};
					diaOption.resolveTree = true;
					result = diaOption;
				}
			}
			return result;
		}

		// Token: 0x06000010 RID: 16 RVA: 0x00002558 File Offset: 0x00000758
		public static DiaOption StartQuestOption(Map map, LabeledQuestScript labeledQuestScript, Building missionBoard)
		{
			int silverToPay = 500;
			string customLabel = labeledQuestScript.customLabel;
			DiaOption diaOption = new DiaOption(customLabel);
			int num = missionBoard.GetComp<CompMissionBoardRC>().tickAtLastMission + 60000 - Find.TickManager.TicksGame;
			bool flag = num > 0;
			DiaOption result;
			if (flag)
			{
				DiaOption diaOption2 = new DiaOption(customLabel);
				diaOption2.Disable("WaitTime".Translate(num.ToStringTicksToPeriod(true, false, true, true)));
				result = diaOption2;
			}
			else
			{
				bool flag2 = MissionBoardDialogMakerRC.AmountSendableSilver(map) < silverToPay;
				if (flag2)
				{
					DiaOption diaOption3 = new DiaOption(customLabel);
					diaOption3.Disable("NeedSilverLaunchable".Translate(silverToPay.ToString()));
					result = diaOption3;
				}
				else
				{
					diaOption.action = delegate()
					{
						MissionBoardDialogMakerRC.StartQuest(map, labeledQuestScript.incidentDef);
						TradeUtility.LaunchThingsOfType(ThingDefOf.Silver, silverToPay, map, null);
						missionBoard.GetComp<CompMissionBoardRC>().tickAtLastMission = Find.TickManager.TicksGame;
					};
					diaOption.resolveTree = true;
					result = diaOption;
				}
			}
			return result;
		}

		// Token: 0x06000011 RID: 17 RVA: 0x00002678 File Offset: 0x00000878
		private static int AmountSendableSilver(Map map)
		{
			return (from t in TradeUtility.AllLaunchableThingsForTrade(map, null)
			where t.def == ThingDefOf.Silver
			select t).Sum((Thing t) => t.stackCount);
		}
	}
}
