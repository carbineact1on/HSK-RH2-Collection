using System.Text;
using Verse;

namespace RedHorse
{
	public class TuneDef : SoundDef
	{
		private string version = "0";

		public string artist;

		public float durationTime;

		public int Version
		{
			get
			{
				int result = 0;
				if (int.TryParse(version, out result))
				{
					return result;
				}
				return 0;
			}
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(base.LabelCap + " - " + artist);
			return stringBuilder.ToString();
		}
	}
}
