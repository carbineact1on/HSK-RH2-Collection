using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace JoyFacilities
{
    public class JobExtension : DefModExtension
    {
        public FleckDef moteThrowObject;
        public int throwIntervalTicks = -1;
        public SoundDef throwSoundDef;
        public EffecterDef pawnEffecterDef;
    }
}
