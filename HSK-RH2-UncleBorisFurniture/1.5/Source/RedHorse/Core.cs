using HarmonyLib;
using Verse;

namespace RedHorse
{
    [StaticConstructorOnStartup]
    public static class Core
    {
        static Core()
        {
            new Harmony("RedHorse.Mod").PatchAll();
        }
    }
}
