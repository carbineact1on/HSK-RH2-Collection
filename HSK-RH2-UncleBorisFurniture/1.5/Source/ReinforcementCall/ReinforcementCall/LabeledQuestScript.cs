using System;
using System.Xml;
using RimWorld;
using Verse;

namespace ReinforcementCall
{
	// Token: 0x02000008 RID: 8
	public class LabeledQuestScript
	{
		// Token: 0x06000015 RID: 21 RVA: 0x00002768 File Offset: 0x00000968
		public override string ToString()
		{
			return string.Concat(new string[]
			{
				"(",
				(this.incidentDef == null) ? "null" : this.incidentDef.ToString(),
				" w=",
				this.customLabel,
				")"
			});
		}

		// Token: 0x06000016 RID: 22 RVA: 0x000027C3 File Offset: 0x000009C3
		public void LoadDataFromXmlCustom(XmlNode xmlRoot)
		{
			DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "incidentDef", xmlRoot.Name, null, null);
			this.customLabel = xmlRoot.FirstChild.Value;
		}

		// Token: 0x04000009 RID: 9
		public QuestScriptDef incidentDef;

		// Token: 0x0400000A RID: 10
		public string customLabel;
	}
}
