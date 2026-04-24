using System;
using System.Collections.Generic;
using Verse;

namespace ReinforcementCall
{
	// Token: 0x02000003 RID: 3
	public class CompProperties_MissionBoardRC : CompProperties
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		public CompProperties_MissionBoardRC()
		{
			this.compClass = typeof(CompMissionBoardRC);
		}

		// Token: 0x04000004 RID: 4
		public List<LabeledIncident> doableMissions = null;

		// Token: 0x04000005 RID: 5
		public List<LabeledQuestScript> doableQuests = null;
	}
}
