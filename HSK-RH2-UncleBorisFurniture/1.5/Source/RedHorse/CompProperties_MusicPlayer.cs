using Verse;

namespace RedHorse
{
	public class CompProperties_MusicPlayer : CompProperties
	{
		public bool isRadio = false;

		public CompProperties_MusicPlayer()
		{
			compClass = typeof(Comp_MusicPlayer);
		}
	}
}
