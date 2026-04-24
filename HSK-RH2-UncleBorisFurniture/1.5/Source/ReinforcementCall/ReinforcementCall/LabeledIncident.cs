using System;
using System.Xml;
using RimWorld;
using Verse;

namespace ReinforcementCall
{
	// Token: 0x02000007 RID: 7
	public class LabeledIncident
	{
		// Token: 0x06000012 RID: 18 RVA: 0x000026DC File Offset: 0x000008DC
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

		// Token: 0x06000013 RID: 19 RVA: 0x00002737 File Offset: 0x00000937
		public void LoadDataFromXmlCustom(XmlNode xmlRoot)
		{
			DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "incidentDef", xmlRoot.Name, null, null);
			this.customLabel = xmlRoot.FirstChild.Value;
		}

		// Token: 0x04000007 RID: 7
		public IncidentDef incidentDef;

		// Token: 0x04000008 RID: 8
		public string customLabel;
	}
}
